using System.Text;
using CrimeVR.Inventory;
using TMPro;
using UnityEngine;

namespace CrimeVR.UI
{
    public class InventoryPanelView : MonoBehaviour
    {
        [SerializeField] private VRInventorySystem inventorySystem;
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private string emptyLabel = "Sin evidencias registradas.";
        [SerializeField] private int maxVisibleEntries = 6;
        [SerializeField] private bool startsVisible = true;

        private readonly StringBuilder builder = new StringBuilder(512);

        private void OnEnable()
        {
            if (inventorySystem != null)
                inventorySystem.EvidenceAdded += HandleEvidenceAdded;

            SetVisible(startsVisible);
            Refresh();
        }

        private void OnDisable()
        {
            if (inventorySystem != null)
                inventorySystem.EvidenceAdded -= HandleEvidenceAdded;
        }

        public void Configure(VRInventorySystem targetInventorySystem, Canvas targetCanvas, TMP_Text targetTitleText, TMP_Text targetBodyText)
        {
            inventorySystem = targetInventorySystem;
            rootCanvas = targetCanvas;
            titleText = targetTitleText;
            bodyText = targetBodyText;
        }

        public void ToggleVisibility()
        {
            if (rootCanvas == null)
                return;

            SetVisible(!rootCanvas.enabled);
        }

        public void SetVisible(bool isVisible)
        {
            if (rootCanvas != null)
                rootCanvas.enabled = isVisible;
        }

        public void Refresh()
        {
            if (titleText != null)
            {
                int count = inventorySystem != null ? inventorySystem.Count : 0;
                titleText.text = $"Inventario de Evidencias ({count})";
            }

            if (bodyText == null)
                return;

            if (inventorySystem == null || inventorySystem.Count == 0)
            {
                bodyText.text = emptyLabel;
                return;
            }

            builder.Clear();
            int total = Mathf.Min(maxVisibleEntries, inventorySystem.CollectedEvidence.Count);
            for (int i = 0; i < total; i++)
            {
                InventoryEvidenceRecord record = inventorySystem.CollectedEvidence[i];
                builder.Append(i + 1);
                builder.Append(". ");
                builder.Append(record.displayName);
                builder.Append(" [");
                builder.Append(record.category);
                builder.Append(']');
                if (i < total - 1)
                    builder.AppendLine();
            }

            if (inventorySystem.CollectedEvidence.Count > total)
            {
                builder.AppendLine();
                builder.Append("+");
                builder.Append(inventorySystem.CollectedEvidence.Count - total);
                builder.Append(" evidencias mas");
            }

            bodyText.text = builder.ToString();
        }

        private void HandleEvidenceAdded(InventoryEvidenceRecord _)
        {
            Refresh();
        }
    }
}
