using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using CrimeVR.UI;

namespace CrimeVR.Player
{
    [DisallowMultipleComponent]
    public class DesktopDebugController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private VRPlayerRigReferences playerRigReferences;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private GameObject xrDeviceSimulatorRoot;

        [Header("Movement")]
        [SerializeField] private bool desktopModeEnabled = true;
        [SerializeField] private float moveSpeed = 2.2f;
        [SerializeField] private float sprintMultiplier = 1.65f;
        [SerializeField] private float turnSpeed = 120f;
        [SerializeField] private float mouseYawSensitivity = 0.12f;
        [SerializeField] private float mousePitchSensitivity = 0.12f;
        [SerializeField] private float pitchMin = -70f;
        [SerializeField] private float pitchMax = 70f;
        [SerializeField] private float standingEyeHeight = 1.7f;
        [SerializeField] private bool disableSimulatorWhileDesktopMode = false;

        private float originalCameraOffsetY;
        private bool cachedCameraOffsetY;
        private float currentPitch;

        private void Awake()
        {
            if (!Application.isEditor)
            {
                enabled = false;
                return;
            }

            if (playerRigReferences == null)
                playerRigReferences = GetComponent<VRPlayerRigReferences>();

            if (characterController == null)
                characterController = GetComponent<CharacterController>();

            if (xrDeviceSimulatorRoot == null)
            {
                GameObject simulator = GameObject.Find("XR Device Simulator");
                if (simulator != null)
                    xrDeviceSimulatorRoot = simulator;
            }
        }

        private void OnEnable()
        {
            ApplyDesktopModeState();
            CacheInitialPitch();
        }

        private void OnDisable()
        {
            RestoreSimulatorState();
            RestoreCameraOffsetHeight();
        }

        private void Update()
        {
            if (!desktopModeEnabled || playerRigReferences == null || IsRuntimeXrHeadTrackingActive())
                return;

            if (DesktopInventoryOverlay.IsAnyOverlayOpen)
                return;

            HandleMovement();
            HandleRotation();
            HandleLookPitch();
        }

        public void Configure(VRPlayerRigReferences rigReferences, CharacterController targetCharacterController, GameObject simulatorRoot)
        {
            playerRigReferences = rigReferences;
            characterController = targetCharacterController;
            xrDeviceSimulatorRoot = simulatorRoot;
            ApplyDesktopModeState();
        }

        private void ApplyDesktopModeState()
        {
            if (!desktopModeEnabled || playerRigReferences == null || IsRuntimeXrHeadTrackingActive())
                return;

            if (playerRigReferences.CameraOffset != null)
            {
                if (!cachedCameraOffsetY)
                {
                    originalCameraOffsetY = playerRigReferences.CameraOffset.localPosition.y;
                    cachedCameraOffsetY = true;
                }

                Vector3 offsetPosition = playerRigReferences.CameraOffset.localPosition;
                offsetPosition.y = standingEyeHeight;
                playerRigReferences.CameraOffset.localPosition = offsetPosition;
            }

            if (disableSimulatorWhileDesktopMode && xrDeviceSimulatorRoot != null && xrDeviceSimulatorRoot.activeSelf)
                xrDeviceSimulatorRoot.SetActive(false);
        }

        private void CacheInitialPitch()
        {
            if (playerRigReferences == null || playerRigReferences.PlayerCamera == null)
                return;

            float pitch = playerRigReferences.PlayerCamera.transform.localEulerAngles.x;
            if (pitch > 180f)
                pitch -= 360f;

            currentPitch = pitch;
        }

        private void RestoreSimulatorState()
        {
            if (disableSimulatorWhileDesktopMode && xrDeviceSimulatorRoot != null && !xrDeviceSimulatorRoot.activeSelf)
                xrDeviceSimulatorRoot.SetActive(true);
        }

        private void RestoreCameraOffsetHeight()
        {
            if (!cachedCameraOffsetY || playerRigReferences == null || playerRigReferences.CameraOffset == null)
                return;

            Vector3 offsetPosition = playerRigReferences.CameraOffset.localPosition;
            offsetPosition.y = originalCameraOffsetY;
            playerRigReferences.CameraOffset.localPosition = offsetPosition;
        }

        private static bool IsRuntimeXrHeadTrackingActive()
        {
            return Application.isPlaying && !Application.isEditor && XRSettings.isDeviceActive;
        }

        private void HandleMovement()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            Transform referenceTransform = playerRigReferences.PlayerCamera != null
                ? playerRigReferences.PlayerCamera.transform
                : transform;

            Vector3 planarForward = Vector3.ProjectOnPlane(referenceTransform.forward, Vector3.up).normalized;
            if (planarForward.sqrMagnitude < 0.001f)
                planarForward = transform.forward;

            Vector3 planarRight = Vector3.ProjectOnPlane(referenceTransform.right, Vector3.up).normalized;
            if (planarRight.sqrMagnitude < 0.001f)
                planarRight = transform.right;

            float horizontal = 0f;
            float vertical = 0f;

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                horizontal -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                horizontal += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                vertical -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                vertical += 1f;

            Vector3 moveDirection = (planarForward * vertical) + (planarRight * horizontal);
            if (moveDirection.sqrMagnitude > 1f)
                moveDirection.Normalize();

            float currentMoveSpeed = moveSpeed;
            if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
                currentMoveSpeed *= sprintMultiplier;

            Vector3 motion = moveDirection * (currentMoveSpeed * Time.deltaTime);
            if (characterController != null && characterController.enabled)
                characterController.Move(motion);
            else
                transform.position += motion;
        }

        private void HandleRotation()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;

            float turnInput = mouse.delta.ReadValue().x * mouseYawSensitivity;

            if (turnInput == 0f)
                return;

            transform.Rotate(Vector3.up, turnInput, Space.World);
        }

        private void HandleLookPitch()
        {
            if (playerRigReferences == null || playerRigReferences.PlayerCamera == null)
                return;

            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;

            float pitchInput = 0f;

            if (mouse != null)
                pitchInput -= mouse.delta.ReadValue().y * mousePitchSensitivity;

            if (keyboard != null)
            {
                if (keyboard.pageUpKey.isPressed)
                    pitchInput -= turnSpeed * Time.deltaTime * 0.75f;
                if (keyboard.pageDownKey.isPressed)
                    pitchInput += turnSpeed * Time.deltaTime * 0.75f;
            }

            if (Mathf.Approximately(pitchInput, 0f))
                return;

            currentPitch = Mathf.Clamp(currentPitch + pitchInput, pitchMin, pitchMax);
            Vector3 cameraEuler = playerRigReferences.PlayerCamera.transform.localEulerAngles;
            cameraEuler.x = currentPitch;
            cameraEuler.y = 0f;
            cameraEuler.z = 0f;
            playerRigReferences.PlayerCamera.transform.localEulerAngles = cameraEuler;
        }
    }
}
