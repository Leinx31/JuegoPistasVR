using CrimeVR.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;

namespace CrimeVR.Interaction
{
    public class ObjectInspectionController : MonoBehaviour
    {
        [SerializeField] private VRPlayerRigReferences playerRigReferences;
        [SerializeField] private InputActionReference inspectionToggleAction;
        [SerializeField] private InputActionReference inventoryToggleAction;
        [SerializeField] private MonoBehaviour inventoryPanelBehaviour;
        [SerializeField] private TMP_Text inspectionStatusText;

        private InspectableObject currentInspectable;
        private bool wasInspectPressedLastFrame;
        private bool wasInventoryPressedLastFrame;

        private UI.InventoryPanelView inventoryPanel;

        private void Awake()
        {
            inventoryPanel = inventoryPanelBehaviour as UI.InventoryPanelView;
        }

        private void OnEnable()
        {
            inspectionToggleAction?.action?.Enable();
            inventoryToggleAction?.action?.Enable();
            UpdateInspectionStatus();
        }

        private void OnDisable()
        {
            inspectionToggleAction?.action?.Disable();
            inventoryToggleAction?.action?.Disable();
        }

        private void Update()
        {
            HandleInspectionToggle();
            HandleInventoryToggle();
        }

        public void Configure(
            VRPlayerRigReferences rigReferences,
            InputActionReference inspectAction,
            InputActionReference inventoryAction,
            UI.InventoryPanelView panelView,
            TMP_Text statusText)
        {
            playerRigReferences = rigReferences;
            inspectionToggleAction = inspectAction;
            inventoryToggleAction = inventoryAction;
            inventoryPanelBehaviour = panelView;
            inventoryPanel = panelView;
            inspectionStatusText = statusText;
        }

        private void HandleInspectionToggle()
        {
            bool isPressed = inspectionToggleAction != null && inspectionToggleAction.action != null && inspectionToggleAction.action.IsPressed();
            if (isPressed && !wasInspectPressedLastFrame)
            {
                if (currentInspectable != null)
                    EndInspection();
                else
                    TryBeginInspection();
            }

            wasInspectPressedLastFrame = isPressed;
        }

        private void HandleInventoryToggle()
        {
            bool isPressed = inventoryToggleAction != null && inventoryToggleAction.action != null && inventoryToggleAction.action.IsPressed();
            if (isPressed && !wasInventoryPressedLastFrame && inventoryPanel != null)
                inventoryPanel.ToggleVisibility();

            wasInventoryPressedLastFrame = isPressed;
        }

        private void TryBeginInspection()
        {
            if (playerRigReferences == null || playerRigReferences.RightDirectInteractor == null)
                return;

            XRDirectInteractor rightInteractor = playerRigReferences.RightDirectInteractor;
            if (!rightInteractor.hasSelection || rightInteractor.interactablesSelected.Count == 0)
                return;

            IXRSelectInteractable selectedInteractable = rightInteractor.interactablesSelected[0];
            MonoBehaviour selectedBehaviour = selectedInteractable as MonoBehaviour;
            if (selectedBehaviour == null)
                return;

            InspectableObject inspectable = selectedBehaviour.GetComponent<InspectableObject>();
            if (inspectable == null)
                return;

            XRGrabInteractable grabInteractable = selectedBehaviour.GetComponent<XRGrabInteractable>();
            if (grabInteractable != null && rightInteractor.interactionManager != null)
                rightInteractor.interactionManager.SelectExit((IXRSelectInteractor)rightInteractor, (IXRSelectInteractable)grabInteractable);

            inspectable.BeginInspection(playerRigReferences.InspectionAnchor);
            currentInspectable = inspectable;
            UpdateInspectionStatus();
        }

        private void EndInspection()
        {
            if (currentInspectable == null)
                return;

            currentInspectable.EndInspection();
            currentInspectable = null;
            UpdateInspectionStatus();
        }

        private void UpdateInspectionStatus()
        {
            if (inspectionStatusText == null)
                return;

            inspectionStatusText.text = currentInspectable == null
                ? "Trigger derecho: inspeccionar evidencia en mano"
                : "Trigger derecho: salir de inspeccion";
        }
    }
}
