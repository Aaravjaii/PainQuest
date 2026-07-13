// Assets/Scripts/Guide/GuideLOD.cs
using UnityEngine;

namespace PainQuest.Guide
{
    /// <summary>
    /// Reduces the render cost of the 3D guide character based on distance to
    /// the player camera. This is what stops the guide's Ultra-quality
    /// shadows/textures from competing with pose-detection inference for GPU
    /// time when the player is facing it up close.
    /// Attach to the root of the guide character.
    /// </summary>
    [DisallowMultipleComponent]
    public class GuideLOD : MonoBehaviour
    {
        [Header("References")]
        public Transform playerCamera;
        public Renderer[] guideRenderers;
        [Tooltip("Optional accent lights attached to the guide, if any.")]
        public Light[] guideLights;

        [Header("Distance Bands (meters)")]
        public float highDetailDistance = 8f;
        public float mediumDetailDistance = 15f;
        public float cullDistance = 15f;

        [Header("Check Rate")]
        [Tooltip("Seconds between LOD re-evaluations — no need to do this every frame.")]
        public float updateInterval = 0.25f;

        private float _timer;
        private enum DetailLevel { High, Medium, Culled }
        private DetailLevel _currentLevel = (DetailLevel)(-1); // force first apply

        void Reset()
        {
            guideRenderers = GetComponentsInChildren<Renderer>();
        }

        void Start()
        {
            if (playerCamera == null && Camera.main != null)
                playerCamera = Camera.main.transform;

            ApplyLevel(DetailLevel.High); // known starting state
        }

        void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < updateInterval) return;
            _timer = 0f;

            if (playerCamera == null) return;

            float distance = Vector3.Distance(transform.position, playerCamera.position);
            bool facingPlayer = IsFacingCamera();

            DetailLevel target;
            if (distance > cullDistance && !facingPlayer)
                target = DetailLevel.Culled;
            else if (distance > highDetailDistance)
                target = DetailLevel.Medium;
            else
                target = DetailLevel.High;

            if (target != _currentLevel)
                ApplyLevel(target);
        }

        private bool IsFacingCamera()
        {
            if (playerCamera == null) return true;
            Vector3 toCamera = (playerCamera.position - transform.position).normalized;
            return Vector3.Dot(transform.forward, toCamera) > 0.2f;
        }

        private void ApplyLevel(DetailLevel level)
        {
            _currentLevel = level;

            switch (level)
            {
                case DetailLevel.High:
                    SetRenderersEnabled(true);
                    SetShadows(UnityEngine.Rendering.ShadowCastingMode.On);
                    SetLightsEnabled(true);
                    break;

                case DetailLevel.Medium:
                    SetRenderersEnabled(true);
                    // Shadows are the single biggest cost when the player is
                    // facing the guide close-up — drop them before culling geometry.
                    SetShadows(UnityEngine.Rendering.ShadowCastingMode.Off);
                    SetLightsEnabled(false);
                    break;

                case DetailLevel.Culled:
                    SetRenderersEnabled(false);
                    SetLightsEnabled(false);
                    break;
            }
        }

        private void SetRenderersEnabled(bool value)
        {
            if (guideRenderers == null) return;
            foreach (var r in guideRenderers)
                if (r != null) r.enabled = value;
        }

        private void SetShadows(UnityEngine.Rendering.ShadowCastingMode mode)
        {
            if (guideRenderers == null) return;
            foreach (var r in guideRenderers)
                if (r != null) r.shadowCastingMode = mode;
        }

        private void SetLightsEnabled(bool value)
        {
            if (guideLights == null) return;
            foreach (var l in guideLights)
                if (l != null) l.enabled = value;
        }
    }
}