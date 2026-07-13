using UnityEngine;

namespace Sun_Temple
{
    // ═══════════════════════════════════════════════════════════════════════════
    // CursorLock — PASTE OVER original in Sun_Temple/Scripts/FPSController/
    // Escape toggles as before. Starts unlocked for the selection UI.
    // ═══════════════════════════════════════════════════════════════════════════
    public class CursorLock : MonoBehaviour
    {
        bool _locked = false;

        void Start()
        {
            Apply(false); // start unlocked for quest selection UI
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                Apply(!_locked);
        }

        public void Lock()   => Apply(true);
        public void Unlock() => Apply(false);

        void Apply(bool locked)
        {
            _locked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible   = !locked;
        }
    }
}
