using UnityEngine;

namespace SunTemple
{
    // ═══════════════════════════════════════════════════════════════════════════
    // CharController_Motor  — PASTE OVER original in Sun_Temple/Scripts/FPSController/
    // Only change from original: cursor unlocks when SelectionPanel is active.
    // Movement is always enabled.
    // ═══════════════════════════════════════════════════════════════════════════
    public class CharController_Motor : MonoBehaviour
    {
        public float speed       = 10.0f;
        public float sensitivity = 60.0f;
        public float jumpForce   = 8.0f;

        CharacterController character;
        public GameObject cam;
        float moveFB, moveLR;
        float rotHorizontal, rotVertical;
        public bool webGLRightClickRotation = true;
        float gravity         = -9.8f;
        float verticalVelocity = 0f;

        void Start()
        {
            character = GetComponent<CharacterController>();
            webGLRightClickRotation = false;
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                webGLRightClickRotation = true;
                sensitivity *= 1.5f;
            }
        }

        void FixedUpdate()
        {
            moveFB = Input.GetAxis("Horizontal") * speed;
            moveLR = Input.GetAxis("Vertical")   * speed;
            rotHorizontal = Input.GetAxisRaw("Mouse X") * sensitivity;
            rotVertical   = Input.GetAxisRaw("Mouse Y") * sensitivity;

            if (character.isGrounded)
            {
                verticalVelocity = gravity;
                if (Input.GetKey(KeyCode.Space)) verticalVelocity = jumpForce;
            }
            else
            {
                verticalVelocity += gravity * Time.fixedDeltaTime;
            }

            Vector3 movement = new Vector3(moveFB, verticalVelocity, moveLR);

            if (webGLRightClickRotation)
            {
                if (Input.GetKey(KeyCode.Mouse0))
                    CameraRotation(cam, rotHorizontal, rotVertical);
            }
            else
            {
                CameraRotation(cam, rotHorizontal, rotVertical);
            }

            movement = transform.rotation * movement;
            character.Move(movement * Time.fixedDeltaTime);
        }

        void CameraRotation(GameObject cam, float rotH, float rotV)
        {
            transform.Rotate(0, rotH * Time.fixedDeltaTime, 0);
            cam.transform.Rotate(-rotV * Time.fixedDeltaTime, 0, 0);
            if (Mathf.Abs(cam.transform.localRotation.x) > 0.7)
            {
                float clamped = 0.7f * Mathf.Sign(cam.transform.localRotation.x);
                cam.transform.localRotation = new Quaternion(
                    clamped, cam.transform.localRotation.y,
                    cam.transform.localRotation.z, cam.transform.localRotation.w);
            }
        }
    }
}
