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
            if (playerRigReferences == null || playerRigReferences.PlayerCamera == null)
                return;

            UpdateFocusHighlight();

            if (DesktopInventoryOverlay.IsAnyOverlayOpen)
                return;

            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            bool grabTriggered = false;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                grabTriggered = true;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
                grabTriggered = true;
            try
            {
                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E))
                    grabTriggered = true;
            }
            catch { }

            if (grabTriggered)
                HandleGrabOrRelease();

            bool inspectTriggered = false;
            if (mouse != null && mouse.rightButton.wasPressedThisFrame)
                inspectTriggered = true;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
                inspectTriggered = true;
            try
            {
                if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.R))
                    inspectTriggered = true;
            }
            catch { }

            if (inspectTriggered)
                ToggleInspection();

            bool inventoryTriggered = false;
            if (keyboard != null && keyboard.iKey.wasPressedThisFrame) inventoryTriggered = true;
            try { if (Input.GetKeyDown(KeyCode.I)) inventoryTriggered = true; } catch { }
            if (inventoryTriggered)
                ToggleInventory();

            bool flashTriggered = false;
            if (keyboard != null && keyboard.fKey.wasPressedThisFrame) flashTriggered = true;
            try { if (Input.GetKeyDown(KeyCode.F)) flashTriggered = true; } catch { }
            if (flashTriggered)
                ToggleHeldFlashlight();

            bool collectTriggered = false;
            if (keyboard != null && keyboard.gKey.wasPressedThisFrame) collectTriggered = true;
            try { if (Input.GetKeyDown(KeyCode.G)) collectTriggered = true; } catch { }
            if (collectTriggered)
                CollectHeldEvidence();

            UpdateHeldObjectPose();
        }

        private void OnGUI()
        {
            if (Application.isPlaying && !DesktopInventoryOverlay.IsAnyOverlayOpen)
            {
                float x = Screen.width / 2f;
                float y = Screen.height / 2f;
                GUI.color = focusedInspectable != null ? Color.cyan : new Color(1f, 1f, 1f, 0.65f);
                GUI.DrawTexture(new Rect(x - 3f, y - 3f, 6f, 6f), Texture2D.whiteTexture);
            }
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

            // Disparar audio 3D y registro de pista en libreta
            ClueInteractable clue = interactable.GetComponent<ClueInteractable>();
            if (clue != null)
            {
                clue.TriggerInspection();
            }
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
