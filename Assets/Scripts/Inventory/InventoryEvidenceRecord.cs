using System;

namespace CrimeVR.Inventory
{
    [Serializable]
    public struct InventoryEvidenceRecord
    {
        public string evidenceId;
        public string displayName;
        public string description;
        public string category;

        public InventoryEvidenceRecord(string evidenceId, string displayName, string description, string category)
        {
            this.evidenceId = evidenceId;
            this.displayName = displayName;
            this.description = description;
            this.category = category;
        }
    }
}
