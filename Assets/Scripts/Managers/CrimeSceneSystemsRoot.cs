using CrimeVR.Inventory;
using CrimeVR.Player;
using UnityEngine;
using CrimeVR.Interaction;
using CrimeVR.UI;

namespace CrimeVR.Managers
{
    public class CrimeSceneSystemsRoot : MonoBehaviour
    {
        [SerializeField] private VRPlayerRigReferences playerRig;
        [SerializeField] private VRInventorySystem inventorySystem;
        [SerializeField] private InventoryPanelView inventoryPanelView;
        [SerializeField] private ObjectInspectionController objectInspectionController;

        public VRPlayerRigReferences PlayerRig => playerRig;
        public VRInventorySystem InventorySystem => inventorySystem;
        public InventoryPanelView InventoryPanelView => inventoryPanelView;
        public ObjectInspectionController ObjectInspectionController => objectInspectionController;

        public void Configure(VRPlayerRigReferences rigReferences, VRInventorySystem inventory)
        {
            playerRig = rigReferences;
            inventorySystem = inventory;
        }

        public void SetInventoryPanelView(InventoryPanelView panelView)
        {
            inventoryPanelView = panelView;
        }

        public void SetObjectInspectionController(ObjectInspectionController controller)
        {
            objectInspectionController = controller;
        }
    }
}
