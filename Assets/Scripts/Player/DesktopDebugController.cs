using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace CrimeVR.Player
{
    [DisallowMultipleComponent]
    public class DesktopDebugController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private VRPlayerRigReferences playerRigReferences;
        [SerializeField] private GameObject xrDeviceSimulatorRoot;

        [Header("Movement Settings")]
        [SerializeField] private bool desktopModeEnabled = true;
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float sprintMultiplier = 1.8f;
        [SerializeField] private float mouseSensitivity = 0.18f;
        [SerializeField] private float pitchMin = -75f;
        [SerializeField] private float pitchMax = 75f;
        [SerializeField] private float standingEyeHeight = 1.7f;

        private float currentPitch;
        private CharacterController characterController;

        private void Awake()
        {
            if (!Application.isEditor)
            {
                enabled = false;
                return;
            }

            FindReferences();
        }

        private void OnEnable()
        {
            FindReferences();
            ApplyDesktopMode();
        }

        private void OnDisable()
        {
            if (characterController != null)
                characterController.enabled = true;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void FindReferences()
        {
            if (playerRigReferences == null)
                playerRigReferences = GetComponent<VRPlayerRigReferences>() ?? FindAnyObjectByType<VRPlayerRigReferences>();

            if (characterController == null && playerRigReferences != null)
                characterController = playerRigReferences.GetComponent<CharacterController>();

            if (xrDeviceSimulatorRoot == null)
            {
                GameObject sim = GameObject.Find("XR Device Simulator");
                if (sim != null)
                    xrDeviceSimulatorRoot = sim;
            }
        }

        private void ApplyDesktopMode()
        {
            if (!desktopModeEnabled || playerRigReferences == null)
                return;

            // Desactivar el CharacterController en modo escritorio para evitar bloqueos de físicas
            if (characterController != null)
                characterController.enabled = false;

            // Ajustar altura de la cámara a nivel de los ojos (1.7m)
            if (playerRigReferences.CameraOffset != null)
            {
                Vector3 offset = playerRigReferences.CameraOffset.localPosition;
                offset.y = standingEyeHeight;
                playerRigReferences.CameraOffset.localPosition = offset;
            }

            // Desactivar simulador para que no robe entradas de teclado
            if (xrDeviceSimulatorRoot != null)
                xrDeviceSimulatorRoot.SetActive(false);
        }

        private void Update()
        {
            if (!desktopModeEnabled)
                return;

            if (playerRigReferences == null)
                FindReferences();

            HandleCursorLock();
            HandleMovement();
            HandleLook();
        }

        private void HandleCursorLock()
        {
            if (IsMouseDown(0) || IsMouseDown(1))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (IsKeyDown(Key.Escape, KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void HandleMovement()
        {
            float horizontal = 0f;
            float vertical = 0f;

            if (IsKeyHeld(Key.W, KeyCode.W) || IsKeyHeld(Key.UpArrow, KeyCode.UpArrow))
                vertical += 1f;
            if (IsKeyHeld(Key.S, KeyCode.S) || IsKeyHeld(Key.DownArrow, KeyCode.DownArrow))
                vertical -= 1f;
            if (IsKeyHeld(Key.A, KeyCode.A) || IsKeyHeld(Key.LeftArrow, KeyCode.LeftArrow))
                horizontal -= 1f;
            if (IsKeyHeld(Key.D, KeyCode.D) || IsKeyHeld(Key.RightArrow, KeyCode.RightArrow))
                horizontal += 1f;

            Transform camTransform = playerRigReferences != null && playerRigReferences.PlayerCamera != null
                ? playerRigReferences.PlayerCamera.transform
                : transform;

            Vector3 planarForward = Vector3.ProjectOnPlane(camTransform.forward, Vector3.up).normalized;
            if (planarForward.sqrMagnitude < 0.001f)
                planarForward = transform.forward;

            Vector3 planarRight = Vector3.ProjectOnPlane(camTransform.right, Vector3.up).normalized;
            if (planarRight.sqrMagnitude < 0.001f)
                planarRight = transform.right;

            Vector3 moveDir = (planarForward * vertical + planarRight * horizontal);
            if (moveDir.sqrMagnitude > 1f)
                moveDir.Normalize();

            float speed = moveSpeed;
            if (IsKeyHeld(Key.LeftShift, KeyCode.LeftShift) || IsKeyHeld(Key.RightShift, KeyCode.RightShift))
                speed *= sprintMultiplier;

            Vector3 translation = moveDir * (speed * Time.deltaTime);
            transform.position += translation;
        }

        private void HandleLook()
        {
            Vector2 mouseDelta = GetMouseDelta();

            // Rotación horizontal (Yaw)
            if (Mathf.Abs(mouseDelta.x) > 0.0001f)
            {
                transform.Rotate(Vector3.up, mouseDelta.x * mouseSensitivity, Space.World);
            }

            // Rotación vertical (Pitch)
            if (Mathf.Abs(mouseDelta.y) > 0.0001f && playerRigReferences != null && playerRigReferences.PlayerCamera != null)
            {
                currentPitch = Mathf.Clamp(currentPitch - (mouseDelta.y * mouseSensitivity), pitchMin, pitchMax);
                Vector3 camAngles = playerRigReferences.PlayerCamera.transform.localEulerAngles;
                camAngles.x = currentPitch;
                camAngles.y = 0f;
                camAngles.z = 0f;
                playerRigReferences.PlayerCamera.transform.localEulerAngles = camAngles;
            }
        }

        private bool IsKeyHeld(Key inputKey, KeyCode legacyKey)
        {
            try
            {
                if (Keyboard.current != null && Keyboard.current[inputKey].isPressed)
                    return true;
            }
            catch { }

            try
            {
                if (Input.GetKey(legacyKey))
                    return true;
            }
            catch { }

            return false;
        }

        private bool IsKeyDown(Key inputKey, KeyCode legacyKey)
        {
            try
            {
                if (Keyboard.current != null && Keyboard.current[inputKey].wasPressedThisFrame)
                    return true;
            }
            catch { }

            try
            {
                if (Input.GetKeyDown(legacyKey))
                    return true;
            }
            catch { }

            return false;
        }

        private bool IsMouseDown(int buttonIndex)
        {
            try
            {
                if (Mouse.current != null)
                {
                    if (buttonIndex == 0 && Mouse.current.leftButton.wasPressedThisFrame) return true;
                    if (buttonIndex == 1 && Mouse.current.rightButton.wasPressedThisFrame) return true;
                }
            }
            catch { }

            try
            {
                if (Input.GetMouseButtonDown(buttonIndex))
                    return true;
            }
            catch { }

            return false;
        }

        private Vector2 GetMouseDelta()
        {
            Vector2 delta = Vector2.zero;
            try
            {
                if (Mouse.current != null)
                    delta = Mouse.current.delta.ReadValue();
            }
            catch { }

            if (delta.sqrMagnitude < 0.0001f)
            {
                try
                {
                    delta.x = Input.GetAxis("Mouse X") * 15f;
                    delta.y = Input.GetAxis("Mouse Y") * 15f;
                }
                catch { }
            }

            return delta;
        }

        public void Configure(VRPlayerRigReferences rigReferences, CharacterController targetCharacterController, GameObject simulatorRoot)
        {
            playerRigReferences = rigReferences;
            characterController = targetCharacterController;
            xrDeviceSimulatorRoot = simulatorRoot;
            ApplyDesktopMode();
        }
    }
}
