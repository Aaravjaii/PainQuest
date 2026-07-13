// Assets/Scripts/Scoring/SentisPoseDetector.cs
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.InferenceEngine;

namespace PainQuest.Scoring
{
    public class SentisPoseDetector : MonoBehaviour
    {
        [Header("Model")]
        public ModelAsset poseModelAsset;

        [Header("Performance")]
        [Tooltip("Minimum seconds between actual model inferences. This is " +
                 "the ONLY throttle, and it applies no matter how many " +
                 "different scripts call GetLatestPose() or how often — " +
                 "ExerciseScoringManager and PoseDebugOverlay can both poll " +
                 "this every frame and you will still only pay for one " +
                 "inference every 'minInferenceInterval' seconds. " +
                 "0.05-0.08 (12-20Hz) is a good range: fast enough to catch " +
                 "quick reps, slow enough not to stall on GPU readback.")]
        [Range(0.02f, 0.3f)]
        public float minInferenceInterval = 0.06f;

        const int InputSize = 256;
        const int LandmarkCount = 39;
        const int ValuesPerLandmark = 5;
        const float SmoothAlpha = 0.4f;

        private Worker _worker;
        private Model _model;
        private RenderTexture _resizeRT;
        private float[] _inputData;
        private bool _ready;
        private bool _isProcessing;
        private float _lastInferenceTime = -999f;

        private PoseResult _latest = new PoseResult { valid = false };
        private PoseResult _lastValidPose = null;
        private int _invalidCount = 0;

        void Awake()
        {
            if (poseModelAsset == null)
            {
                Debug.LogError("[SentisPoseDetector] No model assigned!");
                return;
            }

            try
            {
                _model = ModelLoader.Load(poseModelAsset);
                _worker = new Worker(_model, BackendType.GPUCompute);

                _resizeRT = new RenderTexture(InputSize, InputSize, 0,
                                 RenderTextureFormat.ARGB32);
                _resizeRT.Create();

                // NOTE: the old Texture2D + ReadPixels() round-trip is gone.
                // AsyncGPUReadback reads straight off _resizeRT.

                _inputData = new float[InputSize * InputSize * 3];

                _ready = true;
                Debug.Log($"[SentisPoseDetector] READY. minInferenceInterval={minInferenceInterval}s (~{1f / minInferenceInterval:F0}Hz)");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SentisPoseDetector] Init error: {e.Message}");
                _ready = false;
            }
        }

        /// <summary>
        /// Safe to call from as many scripts as you like, as often as you
        /// like (every Update(), every coroutine tick, whatever). Actual GPU
        /// inference only runs at most once every minInferenceInterval
        /// seconds; every other call just returns the cached latest result.
        /// The inference itself now runs as a non-blocking coroutine using
        /// AsyncGPUReadback, so calling this no longer stalls the frame.
        /// </summary>
        public PoseResult GetLatestPose(Texture source)
        {
            if (!_ready || source == null)
                return _lastValidPose ?? _latest;

            bool dueForInference = (Time.unscaledTime - _lastInferenceTime) >= minInferenceInterval;

            if (dueForInference && !_isProcessing)
            {
                _lastInferenceTime = Time.unscaledTime;
                _isProcessing = true;
                StartCoroutine(InferenceRoutine(source));
            }

            return _latest;
        }

        public void ResetPose()
        {
            _latest = new PoseResult { valid = false };
            _lastValidPose = null;
            _invalidCount = 0;
            _isProcessing = false;
            _lastInferenceTime = -999f;
            Debug.Log("[SentisPoseDetector] RESET");
        }

        // ═════════════════════════════════════════════════════════════════
        // FIXED: this used to be RunInferenceSync(), called directly inline.
        // It did Texture2D.ReadPixels() (a full CPU<->GPU pipeline stall —
        // the CPU has to block until the GPU finishes rendering AND the
        // readback completes) followed immediately by ReadbackAndClone() on
        // the tensor outputs (another blocking sync). At 12-20Hz that's a
        // hard stall on the main thread every ~50-80ms, which is exactly
        // what shows up as video/UI lag.
        //
        // AsyncGPUReadback.Request lets the GPU keep rendering while the
        // pixel data streams back in the background; we just resume this
        // coroutine on the frame it's actually ready, instead of blocking.
        // The tensor output readback is left synchronous — it's only 39
        // landmarks * 5 floats (195 floats), not the bottleneck — but if
        // you want to squeeze further, your Sentis/Inference Engine version
        // may support Tensor.ReadbackAndCloneAsync() for that step too.
        // ═════════════════════════════════════════════════════════════════
        private IEnumerator InferenceRoutine(Texture source)
        {
            // GPU-only draw call, cheap — not the problem.
            Graphics.Blit(source, _resizeRT);

            var request = AsyncGPUReadback.Request(_resizeRT, 0, TextureFormat.RGBA32);
            while (!request.done)
                yield return null;

            if (request.hasError)
            {
                Debug.LogWarning("[SentisPoseDetector] AsyncGPUReadback error, skipping frame.");
                _isProcessing = false;
                yield break;
            }

            var pixels = request.GetData<Color32>();
            int pixelCount = pixels.Length;
            for (int i = 0; i < pixelCount; i++)
            {
                Color32 c = pixels[i];
                int idx = i * 3;
                _inputData[idx] = c.r / 255f;
                _inputData[idx + 1] = c.g / 255f;
                _inputData[idx + 2] = c.b / 255f;
            }

            PoseResult result = null;
            try
            {
                using (var tensor = new Tensor<float>(
                    new TensorShape(1, InputSize, InputSize, 3), _inputData))
                {
                    _worker.Schedule(tensor);
                }

                var rawLM = _worker.PeekOutput("Identity") as Tensor<float>;
                var rawPR = _worker.PeekOutput("Identity_1") as Tensor<float>;

                if (rawLM == null)
                {
                    result = new PoseResult { valid = false };
                }
                else
                {
                    using (var lmCpu = rawLM.ReadbackAndClone())
                    using (var prCpu = rawPR?.ReadbackAndClone())
                    {
                        float presence = 0.5f;
                        if (prCpu != null)
                            presence = Sigmoid(prCpu.AsReadOnlySpan()[0]);

                        var span = lmCpu.AsReadOnlySpan();
                        result = new PoseResult
                        {
                            landmarks = new PoseLandmark[LandmarkCount],
                            valid = presence > 0.2f
                        };

                        for (int i = 0; i < LandmarkCount; i++)
                        {
                            int b = i * ValuesPerLandmark;
                            result.landmarks[i] = new PoseLandmark
                            {
                                x = span[b] / InputSize,
                                y = span[b + 1] / InputSize,
                                z = span[b + 2] / InputSize,
                                v = Sigmoid(span[b + 3])
                            };
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SentisPoseDetector] Error: {e.Message}");
                result = null;
            }

            if (result != null)
            {
                if (result.valid && result.landmarks != null)
                {
                    _latest = SmoothPose(_latest, result);
                    _lastValidPose = _latest;
                    _invalidCount = 0;
                }
                else
                {
                    _invalidCount++;
                    // Use last valid pose for up to 8 misses before
                    // giving up and reporting invalid.
                    if (_invalidCount < 8 && _lastValidPose != null)
                    {
                        _latest = _lastValidPose;
                    }
                }
            }

            _isProcessing = false;
        }

        private PoseResult SmoothPose(PoseResult prev, PoseResult next)
        {
            if (!next.valid || next.landmarks == null) return next;
            if (prev.landmarks == null || !prev.valid || prev.landmarks.Length == 0)
                return next;

            var s = new PoseResult
            {
                landmarks = new PoseLandmark[LandmarkCount],
                valid = true
            };

            for (int i = 0; i < LandmarkCount; i++)
            {
                var p = prev.landmarks[i];
                var n = next.landmarks[i];
                s.landmarks[i] = new PoseLandmark
                {
                    x = Mathf.Lerp(p.x, n.x, SmoothAlpha),
                    y = Mathf.Lerp(p.y, n.y, SmoothAlpha),
                    z = Mathf.Lerp(p.z, n.z, SmoothAlpha),
                    v = Mathf.Lerp(p.v, n.v, SmoothAlpha)
                };
            }
            return s;
        }

        private static float Sigmoid(float x) => 1f / (1f + Mathf.Exp(-x));

        void OnDestroy()
        {
            _worker?.Dispose();
            if (_resizeRT != null)
            {
                _resizeRT.Release();
                DestroyImmediate(_resizeRT);
            }
            _ready = false;
        }
    }
}