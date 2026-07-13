// Assets/Scripts/Scoring/PoseDebugOverlay.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PainQuest.Scoring
{
    public class PoseDebugOverlay : MonoBehaviour
    {
        [Header("References")]
        public SentisPoseDetector poseDetector;
        public RectTransform videoRect;
        public RenderTexture sourceTexture;
        public float updateInterval = 0.05f; // Fast updates

        [Header("Dot Appearance")]
        public float dotSize = 12f;
        public Color highConfidence = Color.green;
        public Color mediumConfidence = Color.yellow;
        public Color lowConfidence = Color.red;
        public bool showAllLandmarks = true;

        private readonly List<RectTransform> _dots = new List<RectTransform>();
        private float _timer = 0f;
        private PoseResult _lastPose = null;

        void Start()
        {
            if (videoRect == null)
                videoRect = GetComponent<RectTransform>();

            if (videoRect == null)
                Debug.LogError("[PoseDebugOverlay] No videoRect assigned!");
        }

        void Update()
        {
            if (poseDetector == null || videoRect == null || sourceTexture == null) return;

            _timer += Time.deltaTime;
            if (_timer < updateInterval) return;
            _timer = 0f;

            // FORCE get fresh pose
            PoseResult pose = poseDetector.GetLatestPose(sourceTexture);

            if (pose == null || pose.landmarks == null || !pose.valid)
            {
                // Keep last pose visible if valid
                if (_lastPose != null && _lastPose.valid)
                {
                    UpdateDots(_lastPose);
                }
                return;
            }

            _lastPose = pose;
            UpdateDots(pose);
        }

        private void UpdateDots(PoseResult pose)
        {
            EnsureDotCount(pose.landmarks.Length);

            Rect rect = videoRect.rect;
            bool poseValid = pose.valid;

            for (int i = 0; i < pose.landmarks.Length; i++)
            {
                var lm = pose.landmarks[i];
                var dot = _dots[i];

                bool relevant = showAllLandmarks || IsNamedBodyLandmark(i);
                dot.gameObject.SetActive(relevant && poseValid && lm.v > 0.15f);

                if (!relevant || !poseValid || lm.v <= 0.15f) continue;

                // Map to UI space
                float localX = (lm.x - 0.5f) * rect.width;
                float localY = (0.5f - lm.y) * rect.height;
                dot.anchoredPosition = new Vector2(localX, localY);

                var img = dot.GetComponent<Image>();
                if (img != null)
                {
                    img.color = lm.v > 0.6f ? highConfidence
                              : lm.v > 0.25f ? mediumConfidence
                              : lowConfidence;
                }
            }
        }

        private bool IsNamedBodyLandmark(int index)
        {
            switch (index)
            {
                case 0:
                case 11:
                case 12:
                case 13:
                case 14:
                case 15:
                case 16:
                case 23:
                case 24:
                case 25:
                case 26:
                case 27:
                case 28:
                    return true;
                default:
                    return false;
            }
        }

        private void EnsureDotCount(int count)
        {
            while (_dots.Count < count)
            {
                var go = new GameObject($"PoseDot_{_dots.Count}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(videoRect, false);
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(dotSize, dotSize);
                var img = go.GetComponent<Image>();
                img.raycastTarget = false;
                _dots.Add(rt);
            }
        }

        public void ClearDots()
        {
            foreach (var dot in _dots)
            {
                if (dot != null)
                    dot.gameObject.SetActive(false);
            }
        }

        void OnDestroy()
        {
            ClearDots();
        }
    }
}