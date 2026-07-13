// Assets/Scripts/Scoring/ExerciseScoringManager.cs
using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using TMPro;
using UnityEngine.UI;

namespace PainQuest.Scoring
{
    public class ExerciseScoringManager : MonoBehaviour
    {
        [Header("Pose Detector")]
        public SentisPoseDetector poseDetector;

        [Header("Reference Video")]
        public VideoPlayer referenceVideoPlayer;
        public RenderTexture referenceRT;
        public UnityEngine.UI.RawImage referenceVideoDisplay;

        [Header("Sampling")]
        [Tooltip("How often this loop asks the pose detector for the latest " +
                 "pose. This does NOT drive GPU cost directly -- actual " +
                 "inference frequency is capped separately on " +
                 "SentisPoseDetector.minInferenceInterval. This just needs " +
                 "to be frequent enough not to miss a fresh result.")]
        public float sampleInterval = 0.03f;

        [Header("UI")]
        public TMP_Text liveRepText;
        public TMP_Text liveScoreText;
        public TMP_Text liveGradeText;
        public TMP_Text liveAngleText;
        public TMP_Text liveFrameText;
        public TMP_Text liveRangeText;
        public TMP_Text livePhaseText;
        public TMP_Text liveRepStatusText;
        public Image liveRepStatusColor;

        [Tooltip("Persistent on-screen readout: shows whether the current frame " +
                 "counted a rep, and if not, why (too small a range, moving too " +
                 "fast, can't see your joints, or name mismatch). Unlike " +
                 "liveRepStatusText (which only flashes on a successful rep), " +
                 "this updates every frame so you can watch it live while tuning.")]
        public TMP_Text liveRepDiagnosticText;

        public TMP_Text finalRepText;
        public TMP_Text finalScoreText;
        public TMP_Text finalGradeText;
        public TMP_Text finalXPText;
        public GameObject scoringPanel;
        public GameObject finalPanel;
        public GameObject scoringUIRoot;

        [Header("Debug")]
        public bool logAngles = true;
        public bool calibrationMode = true;

        private RepAnalyzer _repAnalyzer;
        private ExerciseScoringConfig _config;
        private bool _scoring = false;
        private bool _videoReady = false;
        private Coroutine _scoringLoop;
        private ExerciseData _pendingData;
        private int _frameCounter = 0;
        private float _lastAngle = 90f;
        private bool _exerciseNameMatches = true;
        private float _minAngleThisFrame = 999f;
        private float _maxAngleThisFrame = -999f;
        private float _calibMinAngle = 999f;
        private float _calibMaxAngle = -999f;
        private int _consecutiveInvisibleJoints = 0;
        // ~0.5s worth of misses at a typical sampleInterval before we tell the
        // user their joints aren't visible, rather than flagging one bad frame.
        private const int InvisibleJointsWarningThreshold = 15;

        void Awake()
        {
            if (referenceRT == null)
            {
                referenceRT = new RenderTexture(640, 360, 0, RenderTextureFormat.ARGB32);
                referenceRT.Create();
            }
            referenceVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
            referenceVideoPlayer.targetTexture = referenceRT;
            referenceVideoPlayer.isLooping = true;
            referenceVideoPlayer.playOnAwake = false;
            referenceVideoPlayer.prepareCompleted += OnVideoPrepared;
            if (referenceVideoDisplay != null) referenceVideoDisplay.texture = referenceRT;
        }

        void Start() => HideAll();
        void OnDestroy() => referenceVideoPlayer.prepareCompleted -= OnVideoPrepared;

        public void PrepareExercise(ExerciseData data)
        {
            if (data == null) return;
            _pendingData = data;

            // ALWAYS use the code preset from ExerciseScoringConfig.cs. This
            // used to conditionally fall back to each ExerciseData asset's
            // own Inspector-serialized "scoringConfig" field if it looked
            // "customized" -- but every asset already had old manually-tuned
            // leftover values sitting there from before this preset system
            // existed, so every fix made to ExerciseScoringConfig.cs was
            // being silently ignored at runtime in favor of stale numbers
            // nobody was looking at anymore (e.g. Pistol Squat overcounting
            // because its stale config had no cooldown override, so it fell
            // back to a much shorter default). The code presets are the
            // single source of truth now -- full stop.
            _config = ExerciseScoringConfig.GetConfig(data.exerciseName);

            _videoReady = false;

            if (data.referenceVideo != null)
            {
                string clipName = data.referenceVideo.name;
                string[] extensions = { ".mp4", ".mov", ".avi", ".wmv", ".webm" };
                foreach (string ext in extensions)
                {
                    if (clipName.EndsWith(ext, System.StringComparison.OrdinalIgnoreCase))
                    {
                        clipName = clipName.Substring(0, clipName.Length - ext.Length);
                        break;
                    }
                }

                // Normalizes both names the same way ExerciseScoringConfig
                // does for preset lookup (strips spaces/underscores/dashes,
                // lowercases, strips a trailing "s"), so naming-convention
                // differences like "AirSquat" vs "Air Squat" vs "air-squat"
                // don't get treated as a real mismatch.
                string normalizedExercise = ExerciseScoringConfig.Normalize(data.exerciseName);
                string normalizedClip = ExerciseScoringConfig.Normalize(clipName);
                _exerciseNameMatches = normalizedExercise == normalizedClip;

                if (!_exerciseNameMatches)
                {
                    Debug.LogWarning(
                        $"[ScoringManager] Video name '{clipName}' (normalized: '{normalizedClip}') doesn't match " +
                        $"exercise '{data.exerciseName}' (normalized: '{normalizedExercise}')! " +
                        "Reps will not be counted for this session."
                    );
                }

                referenceVideoPlayer.clip = data.referenceVideo;
                referenceVideoPlayer.Prepare();
            }
            else
            {
                Debug.LogError($"[ScoringManager] No video for {data.exerciseName}");
                _exerciseNameMatches = false;
            }
        }

        public void StartVideoAndScoring()
        {
            if (_pendingData == null)
            {
                Debug.LogWarning("[ScoringManager] No exercise data prepared!");
                return;
            }

            Debug.Log($"[ScoringManager] START {_pendingData.exerciseName}");

            _repAnalyzer = new RepAnalyzer(
                _config.upThreshold,
                _config.downThreshold,
                _config.minRange,
                0.08f,
                0.6f,
                _config.minRepDurationOverride,
                _config.repCooldownOverride
            );
            _repAnalyzer.SetNameMatch(_exerciseNameMatches);

            if (poseDetector != null)
                poseDetector.ResetPose();

            _frameCounter = 0;
            _lastAngle = 90f;
            _minAngleThisFrame = 999f;
            _maxAngleThisFrame = -999f;
            _calibMinAngle = 999f;
            _calibMaxAngle = -999f;

            _lastShownReps = -1;
            _lastShownScore = -1;
            _lastShownGrade = null;
            _lastShownAngle = float.NaN;
            _lastShownFrame = -1;
            _lastShownRange = float.NaN;
            _lastShownPhase = null;
            _lastShownDiagnostic = null;
            _consecutiveInvisibleJoints = 0;

            if (scoringUIRoot != null) scoringUIRoot.SetActive(true);
            if (scoringPanel != null) scoringPanel.SetActive(true);
            if (finalPanel != null) finalPanel.SetActive(false);

            if (_videoReady)
                referenceVideoPlayer.Play();

            _scoring = true;
            if (_scoringLoop != null) StopCoroutine(_scoringLoop);
            _scoringLoop = StartCoroutine(ScoringLoop());
        }

        public float StopScoring()
        {
            _scoring = false;
            if (_scoringLoop != null)
            {
                StopCoroutine(_scoringLoop);
                _scoringLoop = null;
            }
            referenceVideoPlayer.Stop();

            float score = _repAnalyzer?.FormScore ?? 0f;
            int reps = _repAnalyzer?.RepCount ?? 0;
            float confidence = _repAnalyzer?.ExerciseMatchConfidence ?? 0f;

            if (!_exerciseNameMatches)
            {
                Debug.LogWarning("[ScoringManager] Exercise name mismatch - forcing score to 0!");
                score = 0f;
                reps = 0;
            }

            string grade = GradeFromScore(score);
            int xp = XPFromScore(score);

            if (calibrationMode && _calibMaxAngle > _calibMinAngle)
            {
                float observedRange = _calibMaxAngle - _calibMinAngle;
                float suggestedUp = _calibMaxAngle - observedRange * 0.05f;
                float suggestedDown = _calibMinAngle + observedRange * 0.05f;
                float suggestedMinRange = observedRange * 0.7f;

                Debug.Log(
                    $"[Calibration] {_pendingData?.exerciseName} -- Observed angle: " +
                    $"{_calibMinAngle:F1} to {_calibMaxAngle:F1} (range {observedRange:F1}). " +
                    $"Suggested config: upThreshold~{suggestedUp:F0}, downThreshold~{suggestedDown:F0}, " +
                    $"minRange~{suggestedMinRange:F0}. Current config: up={_config.upThreshold:F0}, " +
                    $"down={_config.downThreshold:F0}, minRange={_config.minRange:F0}."
                );
            }

            if (scoringPanel != null) scoringPanel.SetActive(false);
            ShowFinalPanel(reps, score, grade, xp, confidence);

            Debug.Log($"[ScoringManager] FINAL -- Reps:{reps} Form:{score:F1}% Grade:{grade}");
            return score;
        }

        public void HideScoringUI() => HideAll();

        void OnVideoPrepared(VideoPlayer vp)
        {
            _videoReady = true;
            if (_scoring) vp.Play();
        }

        // ═════════════════════════════════════════════════════════════════════
        // Feed every valid sample straight into the RepAnalyzer. No "only if
        // changed enough" gating -- that was silently dropping legitimate
        // samples. Duplicate/stale poses are harmless to re-feed (they just
        // don't move the state machine), so there's no need to filter them
        // out here; RepAnalyzer's own smoothing + hysteresis already
        // handles noise.
        // ═════════════════════════════════════════════════════════════════════
        IEnumerator ScoringLoop()
        {
            yield return new WaitForSeconds(0.1f);

            while (_scoring)
            {
                _frameCounter++;

                if (poseDetector != null)
                {
                    PoseResult pose = poseDetector.GetLatestPose(referenceRT);

                    if (pose != null && pose.landmarks != null && pose.valid)
                    {
                        float angle = GetTrackedAngle(pose, out bool jointsVisible);
                        _lastAngle = angle;
                        _consecutiveInvisibleJoints = jointsVisible ? 0 : _consecutiveInvisibleJoints + 1;

                        if (calibrationMode)
                        {
                            _calibMinAngle = Mathf.Min(_calibMinAngle, angle);
                            _calibMaxAngle = Mathf.Max(_calibMaxAngle, angle);
                        }

                        _minAngleThisFrame = Mathf.Min(_minAngleThisFrame, angle);
                        _maxAngleThisFrame = Mathf.Max(_maxAngleThisFrame, angle);

                        if (logAngles && _frameCounter % 10 == 0)
                            Debug.Log($"[Scoring] Frame:{_frameCounter} Angle:{angle:F1} Reps:{_repAnalyzer?.RepCount ?? 0} Conf:{_repAnalyzer?.ExerciseMatchConfidence ?? 0:F2}");

                        if (_repAnalyzer != null)
                        {
                            bool repDone = _repAnalyzer.Feed(angle);
                            UpdateLiveUI(repDone, angle);
                        }
                    }
                    else
                    {
                        _consecutiveInvisibleJoints++;
                        UpdateLiveUI(false, _lastAngle);
                    }
                }

                if (_frameCounter % 20 == 0)
                {
                    _minAngleThisFrame = 999f;
                    _maxAngleThisFrame = -999f;
                }

                yield return new WaitForSeconds(sampleInterval);
            }
        }

        private float GetTrackedAngle(PoseResult pose, out bool jointsVisible)
        {
            if (pose.landmarks == null || _config == null)
            {
                jointsVisible = false;
                return _lastAngle;
            }

            float? left = ComputeJointAngle(pose, _config.jointA, _config.jointVertex, _config.jointB);

            if (!_config.useBilateral)
            {
                jointsVisible = left.HasValue;
                return left ?? _lastAngle;
            }

            float? right = ComputeJointAngle(pose, _config.jointA_R, _config.jointVertex_R, _config.jointB_R);

            jointsVisible = left.HasValue || right.HasValue;

            if (left.HasValue && right.HasValue)
            {
                // Follow whichever side is closer to what we were already
                // tracking last frame, rather than always the smaller value.
                // Symmetric exercises (jumping jack, pushup, squat, etc.)
                // have left/right cross over each other from normal noise;
                // always picking Min() causes the selected value to snap
                // between sides on every crossover, which looks like a huge,
                // fast, physically-impossible jump even though both sides'
                // landmark tracking is fine. This stays locked onto one
                // continuous limb through normal noise, and still follows a
                // genuine alternation (bicycle crunch, cross jumps) since
                // that happens gradually enough to track correctly too.
                return Mathf.Abs(left.Value - _lastAngle) <= Mathf.Abs(right.Value - _lastAngle)
                    ? left.Value
                    : right.Value;
            }
            if (left.HasValue) return left.Value;
            if (right.HasValue) return right.Value;
            return _lastAngle;
        }

        private float? ComputeJointAngle(PoseResult pose, PoseJoint jointA, PoseJoint jointVertex, PoseJoint jointB)
        {
            int idxA = (int)jointA;
            int idxV = (int)jointVertex;
            int idxB = (int)jointB;

            if (idxA >= pose.landmarks.Length || idxV >= pose.landmarks.Length || idxB >= pose.landmarks.Length)
                return null;

            var lA = pose.landmarks[idxA];
            var lV = pose.landmarks[idxV];
            var lB = pose.landmarks[idxB];

            const float visThresh = 0.2f;
            if (lA.v < visThresh || lV.v < visThresh || lB.v < visThresh)
                return null;

            var dir1 = new Vector2(lA.x - lV.x, lA.y - lV.y);
            var dir2 = new Vector2(lB.x - lV.x, lB.y - lV.y);
            return Vector2.Angle(dir1, dir2);
        }

        // Cached last-displayed values so we only touch TMP_Text.text when the
        // displayed string actually changes. Every assignment to .text forces
        // TextMeshPro to rebuild its mesh -- doing that on 6+ fields every
        // single ~30ms scoring tick (30+ times/sec) is real, avoidable
        // main-thread cost. Angle/Range are rounded to the same precision as
        // what's displayed (F1/F0) before comparing, so we don't rebuild the
        // mesh over sub-decimal float noise that never shows up on screen.
        private int _lastShownReps = -1;
        private int _lastShownScore = -1;
        private string _lastShownGrade = null;
        private float _lastShownAngle = float.NaN;
        private int _lastShownFrame = -1;
        private float _lastShownRange = float.NaN;
        private string _lastShownPhase = null;
        private string _lastShownDiagnostic = null;

        private void UpdateLiveUI(bool repJustDone, float currentAngle)
        {
            if (_repAnalyzer == null) return;

            if (liveRepText != null && _repAnalyzer.RepCount != _lastShownReps)
            {
                _lastShownReps = _repAnalyzer.RepCount;
                liveRepText.text = $"Reps: {_lastShownReps} / {_config.targetReps}";
            }

            int scoreRounded = Mathf.RoundToInt(_repAnalyzer.FormScore);
            if (liveScoreText != null && scoreRounded != _lastShownScore)
            {
                _lastShownScore = scoreRounded;
                liveScoreText.text = $"Form: {scoreRounded}%";
            }

            if (liveGradeText != null)
            {
                string grade = GradeFromScore(_repAnalyzer.FormScore);
                if (grade != _lastShownGrade)
                {
                    _lastShownGrade = grade;
                    liveGradeText.text = grade;
                }
            }

            float angleRounded = Mathf.Round(currentAngle * 10f) / 10f;
            if (liveAngleText != null && !Mathf.Approximately(angleRounded, _lastShownAngle))
            {
                _lastShownAngle = angleRounded;
                liveAngleText.text = $"Angle: {angleRounded:F1}";
            }

            if (liveFrameText != null && _frameCounter != _lastShownFrame)
            {
                _lastShownFrame = _frameCounter;
                liveFrameText.text = $"Frame: {_frameCounter}";
            }

            if (liveRangeText != null)
            {
                float currentRange = _maxAngleThisFrame - _minAngleThisFrame;
                if (currentRange < 0) currentRange = 0;
                float rangeRounded = Mathf.Round(currentRange * 10f) / 10f;
                if (!Mathf.Approximately(rangeRounded, _lastShownRange))
                {
                    _lastShownRange = rangeRounded;
                    liveRangeText.text = $"Range: {rangeRounded:F1} / {_config.minRange:F0}";
                }
            }

            if (livePhaseText != null)
            {
                string phase = GetPhaseString(currentAngle);
                if (phase != _lastShownPhase)
                {
                    _lastShownPhase = phase;
                    livePhaseText.text = $"Phase: {phase}";

                    if (phase == "UP")
                        livePhaseText.color = Color.green;
                    else if (phase == "DOWN")
                        livePhaseText.color = Color.red;
                    else
                        livePhaseText.color = Color.yellow;
                }
            }

            if (repJustDone && liveRepStatusText != null)
            {
                liveRepStatusText.text = "REP COUNTED!";
                liveRepStatusText.color = Color.green;
                if (liveRepStatusColor != null)
                    liveRepStatusColor.color = Color.green;
            }

            if (liveRepDiagnosticText != null)
            {
                string diagnostic = BuildDiagnosticText(repJustDone, currentAngle, out Color diagColor);
                if (diagnostic != _lastShownDiagnostic)
                {
                    _lastShownDiagnostic = diagnostic;
                    liveRepDiagnosticText.text = diagnostic;
                    liveRepDiagnosticText.color = diagColor;
                }
            }
        }

        // Confidence here means "do we trust what the tracker is seeing right
        // now" -- i.e. the required joints are actually visible, OR the video
        // name doesn't match the exercise (a structural confidence-zero case,
        // checked first since nothing will ever count in that state
        // regardless of tracking quality).
        private string BuildDiagnosticText(bool repJustDone, float currentAngle, out Color color)
        {
            if (_repAnalyzer != null && _repAnalyzer.ExerciseMatchConfidence < 0.5f)
            {
                color = Color.red;
                return "Low Confidence: Video Doesn't Match This Exercise";
            }

            if (repJustDone && _repAnalyzer != null)
            {
                color = Color.green;
                return "Rep Counted";
            }

            bool lowConfidence = _consecutiveInvisibleJoints >= InvisibleJointsWarningThreshold;
            color = lowConfidence ? Color.red : Color.green;

            if (lowConfidence)
                return "Low Confidence: Can't See Required Joints";

            if (_repAnalyzer != null)
            {
                switch (_repAnalyzer.Status)
                {
                    case RepAnalyzer.RepStatus.RejectedRange:
                        return $"High Confidence: Range Too Small ({_repAnalyzer.LastAttemptedRangeDeg:F0}/{_repAnalyzer.MinAcceptedRangeDeg:F0})";
                    case RepAnalyzer.RepStatus.RejectedTooFast:
                        return "High Confidence: Too Fast -- Slow Down";
                    default:
                        return "High Confidence: Tracking...";
                }
            }

            return "High Confidence: Tracking...";
        }

        private string GetPhaseString(float angle)
        {
            if (_config == null) return "---";

            float upThreshold = _config.upThreshold;
            float downThreshold = _config.downThreshold;
            float hysteresis = Mathf.Clamp(Mathf.Abs(upThreshold - downThreshold) * 0.10f, 2f, 15f);

            if (angle > upThreshold - hysteresis)
                return "UP";
            else if (angle < downThreshold + hysteresis)
                return "DOWN";
            else
                return "MID";
        }

        private void ShowFinalPanel(int reps, float score, string grade, int xp, float confidence)
        {
            if (finalPanel != null) finalPanel.SetActive(true);
            if (finalRepText != null) finalRepText.text = $"Reps Completed: {reps}";
            if (finalScoreText != null) finalScoreText.text = $"Form Score: {score:F0}%";
            if (finalGradeText != null) finalGradeText.text = grade;
            if (finalXPText != null) finalXPText.text = $"+{xp} XP";

            if (confidence < 0.5f && finalGradeText != null)
                finalGradeText.text += "\nLow Confidence";
        }

        private void HideAll()
        {
            if (scoringUIRoot != null) scoringUIRoot.SetActive(false);
            if (scoringPanel != null) scoringPanel.SetActive(false);
            if (finalPanel != null) finalPanel.SetActive(false);
        }

        private static string GradeFromScore(float s)
        {
            if (s >= 90f) return "S";
            if (s >= 75f) return "A";
            if (s >= 60f) return "B";
            if (s >= 45f) return "C";
            return "D";
        }

        private static int XPFromScore(float s) => Mathf.RoundToInt(s * 1.5f);
    }
}