using CrimeVR.Evidence;
using CrimeVR.Interaction;
using CrimeVR.Inventory;
using CrimeVR.Managers;
using CrimeVR.Tools;
using CrimeVR.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CrimeVR.Player
{
    [DisallowMultipleComponent]
    public class DesktopInteractionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CrimeSceneSystemsRoot systemsRoot;
        [SerializeField] private VRPlayerRigReferences playerRigReferences;

        [Header("Desktop Interaction")]
        [SerializeField] private float interactDistance = 3f;
        [SerializeField] private float holdDistance = 1.15f;
        [SerializeField] private LayerMask interactionMask = Physics.DefaultRaycastLayers;

        private Transform holdAnchor;
        private XRGrabInteractable heldGrabInteractable;
        private Rigidbody heldRigidbody;
        private Transform heldOriginalParent;
        private bool heldOriginalUseGravity;
        private bool heldOriginalIsKinematic;
        private bool heldOriginalGrabEnabled;
        private UVFlashlightTool heldFlashlight;
        private InspectableObject activeInspection;
        private InspectableObject focusedInspectable;

        private void Awake()
        {
            if (systemsRoot == null)
                systemsRoot = FindAnyObjectByType<CrimeSceneSystemsRoot>();

            if (playerRigReferences == null && systemsRoot != null)
                playerRigReferences = systemsRoot.PlayerRig;

            EnsureHoldAnchor();
            EnsureDesktopInventoryOverlay();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || playerRigReferences == null || playerRigReferences.PlayerCamera == null)
                return;

            UpdateFocusHighlight();

            if (DesktopInventoryOverlay.IsAnyOverlayOpen)
                return;

            if (keyboard.eKey.wasPressedThisFrame)
                HandleGrabOrRelease();

            if (keyboard.rKey.wasPressedThisFrame)
                ToggleInspection();

            if (keyboard.iKey.wasPressedThisFrame)
                ToggleInventory();

            if (keyboard.fKey.wasPressedThisFrame)
                ToggleHeldFlashlight();

            if (keyboard.gKey.wasPressedThisFrame)
                CollectHeldEvidence();

            UpdateHeldObjectPose();
        }

        private void EnsureHoldAnchor()
        {
            if (playerRigReferences == null || playerRigReferences.PlayerCamera == null)
                return;

            Transform existing = playerRigReferences.PlayerCamera.transform.Find("DesktopHold_Anchor");
            if (existing != null)
            {
                holdAnchor = existing;
                return;
            }

            GameObject anchor = new GameObject("DesktopHold_Anchor");
            anchor.transform.SetParent(playerRigReferences.PlayerCamera.transform, false);
            anchor.transform.localPosition = new Vector3(0f, -0.08f, holdDistance);
            anchor.transform.localRotation = Quaternion.identity;
            holdAnchor = anchor.transform;
        }

        private void EnsureDesktopInventoryOverlay()
        {
            if (systemsRoot == null)
                return;

            DesktopInventoryOverlay overlay = systemsRoot.GetComponent<DesktopInventoryOverlay>();
            if (overlay == null)
                overlay = systemsRoot.gameObject.AddComponent<DesktopInventoryOverlay>();

            overlay.Configure(systemsRoot.InventorySystem);
        }

        private void HandleGrabOrRelease()
        {
            if (activeInspection != null)
            {
                EndInspection();
                return;
            }

            if (heldGrabInteractable != null)
            {
                ReleaseHeldObject();
                return;
            }

            if (!TryGetTargetedInteractable(out XRGrabInteractable targetedInteractable))
                return;

            GrabObject(targetedInteractable);
        }

        private bool TryGetTargetedInteractable(out XRGrabInteractable interactable)
        {
            interactable = null;

            Ray ray = new Ray(playerRigReferences.PlayerCamera.transform.position, playerRigReferences.PlayerCamera.transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactionMask, QueryTriggerInteraction.Ignore))
                return false;

            interactable = hit.collider.GetComponentInParent<XRGrabInteractable>();
            return interactable != null;
        }

        private void GrabObject(XRGrabInteractable interactable)
        {
            if (interactable == null)
                return;

            heldGrabInteractable = interactable;
            heldRigidbody = interactable.GetComponent<Rigidbody>();
            heldOriginalParent = interactable.transform.parent;
            heldFlashlight = interactable.GetComponent<UVFlashlightTool>();

            heldOriginalGrabEnabled = interactable.enabled;
            interactable.enabled = false;

            if (heldRigidbody != null)
            {
                heldOriginalUseGravity = heldRigidbody.useGravity;
                heldOriginalIsKinematic = heldRigidbody.isKinematic;
                heldRigidbody.linearVelocity = Vector3.zero;
                heldRigidbody.angularVelocity = Vector3.zero;
                heldRigidbody.useGravity = false;
                heldRigidbody.isKinematic = true;
            }

            interactable.transform.SetParent(holdAnchor, true);
            interactable.transform.position = holdAnchor.position;
            interactable.transform.rotation = holdAnchor.rotation;

            if (heldFlashlight != null)
                heldFlashlight.SetDesktopHeldState(true);
        }

        private void ReleaseHeldObject()
        {
            if (heldGrabInteractable == null)
                return;

            if (heldFlashlight != null)
                heldFlashlight.SetDesktopHeldState(false);

            heldGrabInteractable.transform.SetParent(heldOriginalParent, true);

            if (heldRigidbody != null)
            {
                heldRigidbody.useGravity = heldOriginalUseGravity;
                heldRigidbody.isKinematic = heldOriginalIsKinematic;
            }

            heldGrabInteractable.enabled = heldOriginalGrabEnabled;
            heldGrabInteractable = null;
            heldRigidbody = null;
            heldFlashlight = null;
        }

        private void UpdateHeldObjectPose()
        {
            if (heldGrabInteractable == null || holdAnchor == null)
                return;

            heldGrabInteractable.transform.position = holdAnchor.position;
            heldGrabInteractable.transform.rotation = holdAnchor.rotation;
        }

        private void ToggleInspection()
        {
            if (activeInspection != null)
            {
                EndInspection();
                return;
            }

            InspectableObject inspectableObject = heldGrabInteractable != null
                ? heldGrabInteractable.GetComponent<InspectableObject>()
                : focusedInspectable;

            if (inspectableObject == null || playerRigReferences.InspectionAnchor == null)
                return;

            activeInspection = inspectableObject;
            inspectableObject.BeginInspection(playerRigReferences.InspectionAnchor);
            RegisterInspection(inspectableObject);
        }

        private void EndInspection()
        {
            if (activeInspection == null)
                return;

            activeInspection.EndInspection();
            activeInspection = null;
        }

        private void ToggleInventory()
        {
            InventoryPanelView panelView = systemsRoot != null ? systemsRoot.InventoryPanelView : null;
            panelView?.ToggleVisibility();
        }

        private void ToggleHeldFlashlight()
        {
            if (heldFlashlight == null)
                return;

            heldFlashlight.ToggleLight();
        }

        private void CollectHeldEvidence()
        {
            if (heldGrabInteractable == null || systemsRoot == null)
                return;

            EvidenceCollectible evidence = heldGrabInteractable.GetComponent<EvidenceCollectible>();
            VRInventorySystem inventorySystem = systemsRoot.InventorySystem;
            if (evidence == null || inventorySystem == null)
                return;

            if (!inventorySystem.TryAddEvidence(evidence))
                return;

            EndInspection();
            heldFlashlight?.SetDesktopHeldState(false);
            evidence.Collect();
            heldGrabInteractable = null;
            heldRigidbody = null;
            heldFlashlight = null;
        }

        private void UpdateFocusHighlight()
        {
            InspectableObject nextFocused = null;
            Ray ray = new Ray(playerRigReferences.PlayerCamera.transform.position, playerRigReferences.PlayerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactionMask, QueryTriggerInteraction.Ignore))
                nextFocused = hit.collider.GetComponentInParent<InspectableObject>();

            if (focusedInspectable == nextFocused)
                return;

            if (focusedInspectable != null && focusedInspectable != activeInspection)
                focusedInspectable.SetHighlighted(false);

            focusedInspectable = nextFocused;

            if (focusedInspectable != null && focusedInspectable != activeInspection)
                focusedInspectable.SetHighlighted(true);
        }

        private void RegisterInspection(InspectableObject inspectableObject)
        {
            if (inspectableObject == null || !inspectableObject.RegisterOnInspect || systemsRoot == null || systemsRoot.InventorySystem == null)
                return;

            systemsRoot.InventorySystem.TryAddRecord(
                inspectableObject.ClueId,
                inspectableObject.DisplayName,
                inspectableObject.Description,
                inspectableObject.Category);
        }
    }
}
