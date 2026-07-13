// Assets/Scripts/Core/QualityOptimizer.cs
using UnityEngine;
namespace PainQuest.Core
{
    public class QualityOptimizer : MonoBehaviour
    {
        [Header("Auto-Apply")]
        [Tooltip("If true, Apply() runs automatically in Awake — you don't need " +
                 "another script to call it. Turn this off only if something " +
                 "else explicitly calls Apply() itself (e.g. after a settings menu).")]
        public bool applyOnAwake = true;

        [Header("Performance Settings")]
        public float shadowDistance = 40f;
        public int shadowCascades = 1;
        public int globalMipmapLimit = 1;
        // Deferred rendering (Built-in RP) does not support hardware MSAA —
        // setting this > 0 while the camera uses Deferred either does nothing
        // or silently costs extra without any visual benefit. Leave at 0
        // unless you've confirmed the active render path actually uses it.
        public int antiAliasing = 0;
        public int pixelLightCount = 0;
        public int targetFrameRate = 60;

        [Header("Particles")]
        [Tooltip("Soft particles sample the depth buffer every frame — real cost " +
                 "for fire/smoke VFX like torches. Off by default for performance.")]
        public bool disableSoftParticles = true;

        private float _origShadowDistance;
        private int _origShadowCascades;
        private int _origMipmapLimit;
        private int _origAA;
        private int _origPixelLights;
        private bool _origSoftParticles;
        private bool _applied;

        void Awake()
        {
            Application.targetFrameRate = targetFrameRate;

            if (applyOnAwake)
                Apply();
        }

        public void Apply()
        {
            if (_applied) return;
            _origShadowDistance = QualitySettings.shadowDistance;
            _origShadowCascades = QualitySettings.shadowCascades;
            _origMipmapLimit = QualitySettings.globalTextureMipmapLimit;
            _origAA = QualitySettings.antiAliasing;
            _origPixelLights = QualitySettings.pixelLightCount;
            _origSoftParticles = QualitySettings.softParticles;

            QualitySettings.shadowDistance = shadowDistance;
            QualitySettings.shadowCascades = shadowCascades;
            QualitySettings.globalTextureMipmapLimit = globalMipmapLimit;
            QualitySettings.antiAliasing = antiAliasing;
            QualitySettings.pixelLightCount = pixelLightCount;
            if (disableSoftParticles)
                QualitySettings.softParticles = false;

            _applied = true;
            Debug.Log(
                $"[QualityOptimizer] Applied — shadowDistance={shadowDistance}, " +
                $"shadowCascades={shadowCascades}, mipmapLimit={globalMipmapLimit}, " +
                $"AA={antiAliasing}, pixelLights={pixelLightCount}, " +
                $"softParticles={(disableSoftParticles ? "off" : "unchanged")}"
            );
        }

        public void Restore()
        {
            if (!_applied) return;
            QualitySettings.shadowDistance = _origShadowDistance;
            QualitySettings.shadowCascades = _origShadowCascades;
            QualitySettings.globalTextureMipmapLimit = _origMipmapLimit;
            QualitySettings.antiAliasing = _origAA;
            QualitySettings.pixelLightCount = _origPixelLights;
            QualitySettings.softParticles = _origSoftParticles;
            _applied = false;
            Debug.Log("[QualityOptimizer] Restored original settings");
        }

        void OnDestroy()
        {
            if (_applied) Restore();
        }
    }
}