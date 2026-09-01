using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;

namespace CrimeVR.Player
{
    public class VRPlayerRigReferences : MonoBehaviour
    {
        [Header("Rig Root")]
        [SerializeField] private Transform rigRoot;
        [SerializeField] private Transform cameraOffset;
        [SerializeField] private Camera playerCamera;

        [Header("Controllers")]
        [SerializeField] private Transform leftControllerRoot;
        [SerializeField] private Transform rightControllerRoot;

        [Header("Interactors")]
        [SerializeField] private XRDirectInteractor leftDirectInteractor;
        [SerializeField] private XRDirectInteractor rightDirectInteractor;
        [SerializeField] private XRRayInteractor leftRayInteractor;
        [SerializeField] private XRRayInteractor rightRayInteractor;

        [Header("Hand Visuals")]
        [SerializeField] private GameObject leftHandVisual;
        [SerializeField] private GameObject rightHandVisual;

        [Header("Gameplay Anchors")]
        [SerializeField] private Transform inventoryHolsterAnchor;
        [SerializeField] private Transform inspectionAnchor;

        [Header("Runtime Safety")]
        [SerializeField] private bool createSafetyFloorAtRuntime = false;
        [SerializeField] private Vector3 safetyFloorSize = new Vector3(1000f, 4f, 1000f);
        [SerializeField] private float safetyFloorY = -25f;
        [SerializeField] private float respawnHeightThreshold = -20f;
        [SerializeField] private float respawnYOffset = 2f;
        [SerializeField] private float idleGroundingSpeed = 2.5f;

        [Header("Head Tracking Fallback")]
        [SerializeField] private bool forceCenterEyePoseFallback = true;

        private Vector3 initialRigPosition;
        private Quaternion initialRigRotation;
        private GameObject runtimeSafetyFloor;
        private XROrigin xrOrigin;
        private CharacterController characterController;
        private TrackedPoseDriver trackedPoseDriver;
        private InputAction headPositionAction;
        private InputAction headRotationAction;
        private InputAction headTrackingStateAction;

        public Transform RigRoot => rigRoot;
        public Transform CameraOffset => cameraOffset;
        public Camera PlayerCamera => playerCamera;
        public Transform LeftControllerRoot => leftControllerRoot;
        public Transform RightControllerRoot => rightControllerRoot;
        public XRDirectInteractor LeftDirectInteractor => leftDirectInteractor;
        public XRDirectInteractor RightDirectInteractor => rightDirectInteractor;
        public XRRayInteractor LeftRayInteractor => leftRayInteractor;
        public XRRayInteractor RightRayInteractor => rightRayInteractor;
        public GameObject LeftHandVisual => leftHandVisual;
        public GameObject RightHandVisual => rightHandVisual;
        public Transform InventoryHolsterAnchor => inventoryHolsterAnchor;
        public Transform InspectionAnchor => inspectionAnchor;

        private void Awake()
        {
            if (rigRoot == null)
                rigRoot = transform;

            xrOrigin = GetComponent<XROrigin>();
            characterController = GetComponent<CharacterController>();
            EnsureCameraReferences();
            EnsureHeadTrackingDriver();
            initialRigPosition = rigRoot.position;
            initialRigRotation = rigRoot.rotation;
        }

        private void Start()
        {
            EnsureCameraReferences();
            EnsureHeadTrackingDriver();
            AlignTrackingOriginAndCollider();

            if (Application.isPlaying && createSafetyFloorAtRuntime)
                EnsureRuntimeSafetyFloor();
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying || rigRoot == null)
                return;

            ApplyHeadTrackingFallback();

            if (rigRoot.position.y < respawnHeightThreshold)
                RestoreSafeSpawn();
        }

        private void OnDestroy()
        {
            headPositionAction?.Dispose();
            headRotationAction?.Dispose();
            headTrackingStateAction?.Dispose();
        }

        public void Configure(
            Transform newRigRoot,
            Transform newCameraOffset,
            Camera newPlayerCamera,
            Transform newLeftControllerRoot,
            Transform newRightControllerRoot,
            XRDirectInteractor newLeftDirectInteractor,
            XRDirectInteractor newRightDirectInteractor,
            XRRayInteractor newLeftRayInteractor,
            XRRayInteractor newRightRayInteractor,
            Transform newInventoryHolsterAnchor,
            Transform newInspectionAnchor)
        {
            rigRoot = newRigRoot;
            cameraOffset = newCameraOffset;
            playerCamera = newPlayerCamera;
            leftControllerRoot = newLeftControllerRoot;
            rightControllerRoot = newRightControllerRoot;
            leftDirectInteractor = newLeftDirectInteractor;
            rightDirectInteractor = newRightDirectInteractor;
            leftRayInteractor = newLeftRayInteractor;
            rightRayInteractor = newRightRayInteractor;
            inventoryHolsterAnchor = newInventoryHolsterAnchor;
            inspectionAnchor = newInspectionAnchor;

            if (rigRoot != null)
            {
                initialRigPosition = rigRoot.position;
                initialRigRotation = rigRoot.rotation;
            }
        }

        public void SetHandVisuals(GameObject leftVisual, GameObject rightVisual)
        {
            leftHandVisual = leftVisual;
            rightHandVisual = rightVisual;
        }

        private void EnsureRuntimeSafetyFloor()
        {
            if (runtimeSafetyFloor != null)
                return;

            runtimeSafetyFloor = new GameObject("Runtime_SafetyFloor");
            runtimeSafetyFloor.hideFlags = HideFlags.DontSave;

            Vector3 floorPosition = new Vector3(initialRigPosition.x, safetyFloorY - (safetyFloorSize.y * 0.5f), initialRigPosition.z);
            runtimeSafetyFloor.transform.position = floorPosition;

            BoxCollider floorCollider = runtimeSafetyFloor.AddComponent<BoxCollider>();
            floorCollider.size = safetyFloorSize;
            floorCollider.center = Vector3.zero;
        }

        private void EnsureCameraReferences()
        {
            if (xrOrigin == null)
                xrOrigin = GetComponent<XROrigin>();

            if (playerCamera == null && xrOrigin != null)
                playerCamera = xrOrigin.Camera;

            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>(true);

            if (xrOrigin != null && playerCamera != null)
                xrOrigin.Camera = playerCamera;

            if (cameraOffset == null && xrOrigin != null && xrOrigin.CameraFloorOffsetObject != null)
                cameraOffset = xrOrigin.CameraFloorOffsetObject.transform;

            if (xrOrigin != null && cameraOffset != null)
                xrOrigin.CameraFloorOffsetObject = cameraOffset.gameObject;
        }

        private void EnsureHeadTrackingDriver()
        {
            if (playerCamera == null)
                return;

            trackedPoseDriver = playerCamera.GetComponent<TrackedPoseDriver>();
            if (trackedPoseDriver == null)
                trackedPoseDriver = playerCamera.gameObject.AddComponent<TrackedPoseDriver>();

            trackedPoseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            trackedPoseDriver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            trackedPoseDriver.ignoreTrackingState = false;

            if (headPositionAction == null)
            {
                headPositionAction = new InputAction(
                    name: "HMD Position",
                    type: InputActionType.PassThrough,
                    binding: "<XRHMD>/centerEyePosition",
                    expectedControlType: "Vector3");
            }

            if (headRotationAction == null)
            {
                headRotationAction = new InputAction(
                    name: "HMD Rotation",
                    type: InputActionType.PassThrough,
                    binding: "<XRHMD>/centerEyeRotation",
                    expectedControlType: "Quaternion");
            }

            if (headTrackingStateAction == null)
            {
                headTrackingStateAction = new InputAction(
                    name: "HMD Tracking State",
                    type: InputActionType.PassThrough,
                    binding: "<XRHMD>/trackingState",
                    expectedControlType: "Integer");
            }

            trackedPoseDriver.positionInput = new InputActionProperty(headPositionAction);
            trackedPoseDriver.rotationInput = new InputActionProperty(headRotationAction);
            trackedPoseDriver.trackingStateInput = new InputActionProperty(headTrackingStateAction);

            if (!headPositionAction.enabled)
                headPositionAction.Enable();

            if (!headRotationAction.enabled)
                headRotationAction.Enable();

            if (!headTrackingStateAction.enabled)
                headTrackingStateAction.Enable();
        }

        private void ApplyHeadTrackingFallback()
        {
            if (!forceCenterEyePoseFallback || playerCamera == null)
                return;

            if (!XRSettings.isDeviceActive)
                return;

            Vector3 headLocalPosition = InputTracking.GetLocalPosition(XRNode.CenterEye);
            Quaternion headLocalRotation = InputTracking.GetLocalRotation(XRNode.CenterEye);

            if (headLocalRotation == default)
                return;

            playerCamera.transform.localPosition = headLocalPosition;
            playerCamera.transform.localRotation = headLocalRotation;
        }

        private void AlignTrackingOriginAndCollider()
        {
            if (xrOrigin != null)
            {
                if (playerCamera != null)
                    xrOrigin.Camera = playerCamera;

                if (cameraOffset != null)
                    xrOrigin.CameraFloorOffsetObject = cameraOffset.gameObject;

                xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
                xrOrigin.CameraYOffset = 0f;
            }

            UpdateCharacterControllerToHeadsetHeight(forceMinimumHeight: true);

            if (rigRoot != null && rigRoot.position.y < safetyFloorY + respawnYOffset)
            {
                Vector3 liftedPosition = rigRoot.position;
                liftedPosition.y = safetyFloorY + respawnYOffset;
                rigRoot.position = liftedPosition;
                initialRigPosition = rigRoot.position;
            }
        }

        private void UpdateCharacterControllerToHeadsetHeight(bool forceMinimumHeight = false)
        {
            if (characterController == null || playerCamera == null || cameraOffset == null)
                return;

            float headsetHeight = Mathf.Max(playerCamera.transform.localPosition.y, 1.2f);
            if (!forceMinimumHeight && headsetHeight <= 0.01f)
                return;

            float targetHeight = Mathf.Clamp(headsetHeight, 1.2f, 2.2f);
            characterController.height = targetHeight;
            characterController.center = new Vector3(0f, targetHeight * 0.5f, 0f);
            characterController.radius = Mathf.Clamp(characterController.radius, 0.25f, 0.35f);
            characterController.stepOffset = Mathf.Min(0.3f, targetHeight * 0.2f);
        }

        private void RestoreSafeSpawn()
        {
            CharacterController controller = characterController != null ? characterController : rigRoot.GetComponent<CharacterController>();
            if (controller != null && controller.enabled)
                controller.enabled = false;

            Vector3 safePosition = initialRigPosition;
            if (safePosition.y < 0.5f)
                safePosition.y = 0.5f;

            rigRoot.SetPositionAndRotation(safePosition, initialRigRotation);

            if (controller != null)
                controller.enabled = true;
        }

        private void ApplyIdleGrounding()
        {
            if (characterController == null || !characterController.enabled || characterController.isGrounded)
                return;

            characterController.Move(Vector3.down * (idleGroundingSpeed * Time.deltaTime));
        }
    }
}
