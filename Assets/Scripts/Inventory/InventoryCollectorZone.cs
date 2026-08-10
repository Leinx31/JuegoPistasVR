using CrimeVR.Evidence;
using UnityEngine;

namespace CrimeVR.Inventory
{
    [RequireComponent(typeof(Collider))]
    public class InventoryCollectorZone : MonoBehaviour
    {
        [SerializeField] private VRInventorySystem inventorySystem;

        private void Reset()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        private void OnTriggerStay(Collider other)
        {
            if (inventorySystem == null)
                return;

            EvidenceCollectible collectible = other.GetComponentInParent<EvidenceCollectible>();
            if (collectible == null || collectible.IsCollected || collectible.IsBeingHeld)
                return;

            if (inventorySystem.TryAddEvidence(collectible))
                collectible.Collect();
        }

        public void SetInventorySystem(VRInventorySystem targetInventorySystem)
        {
            inventorySystem = targetInventorySystem;
        }
    }
}
