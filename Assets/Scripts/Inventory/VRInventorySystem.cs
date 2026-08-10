using System;
using System.Collections.Generic;
using UnityEngine;
using CrimeVR.Evidence;

namespace CrimeVR.Inventory
{
    public class VRInventorySystem : MonoBehaviour
    {
        [SerializeField] private int maxEvidenceSlots = 24;
        [SerializeField] private List<InventoryEvidenceRecord> collectedEvidence = new List<InventoryEvidenceRecord>();

        public IReadOnlyList<InventoryEvidenceRecord> CollectedEvidence => collectedEvidence;
        public event Action<InventoryEvidenceRecord> EvidenceAdded;
        public int Count => collectedEvidence.Count;

        public bool Contains(string evidenceId)
        {
            for (int i = 0; i < collectedEvidence.Count; i++)
            {
                if (collectedEvidence[i].evidenceId == evidenceId)
                    return true;
            }

            return false;
        }

        public bool TryAddEvidence(EvidenceCollectible collectible)
        {
            if (collectible == null || collectible.IsCollected)
                return false;

            if (collectedEvidence.Count >= maxEvidenceSlots || Contains(collectible.EvidenceId))
                return false;

            var record = new InventoryEvidenceRecord(
                collectible.EvidenceId,
                collectible.DisplayName,
                collectible.Description,
                collectible.Category);

            collectedEvidence.Add(record);
            EvidenceAdded?.Invoke(record);
            return true;
        }

        public bool TryAddRecord(string evidenceId, string displayName, string description, string category)
        {
            if (string.IsNullOrWhiteSpace(evidenceId))
                return false;

            if (collectedEvidence.Count >= maxEvidenceSlots || Contains(evidenceId))
                return false;

            var record = new InventoryEvidenceRecord(evidenceId, displayName, description, category);
            collectedEvidence.Add(record);
            EvidenceAdded?.Invoke(record);
            return true;
        }
    }
}
