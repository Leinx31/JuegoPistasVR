using System.Text;
using CrimeVR.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CrimeVR.UI
{
    public class DesktopInventoryOverlay : MonoBehaviour
    {
        [SerializeField] private VRInventorySystem inventorySystem;
        [SerializeField] private Key toggleKey = Key.Space;
        [SerializeField] private Key navigateUpKey = Key.UpArrow;
        [SerializeField] private Key navigateDownKey = Key.DownArrow;

        private Canvas overlayCanvas;
        private TMP_Text titleText;
        private TMP_Text listText;
        private TMP_Text detailText;
        private int selectedIndex;

        public static bool IsAnyOverlayOpen { get; private set; }
        public bool IsOpen => overlayCanvas != null && overlayCanvas.enabled;

        private readonly StringBuilder listBuilder = new StringBuilder(512);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            IsAnyOverlayOpen = false;
        }

        private void Awake()
        {
            IsAnyOverlayOpen = false;
            if (inventorySystem == null)
                inventorySystem = FindAnyObjectByType<VRInventorySystem>();

            CreateOverlay();
            SetVisible(false);
        }

        private void OnEnable()
        {
            if (inventorySystem != null)
                inventorySystem.EvidenceAdded += HandleEvidenceAdded;
        }

        private void OnDisable()
        {
            if (inventorySystem != null)
                inventorySystem.EvidenceAdded -= HandleEvidenceAdded;

            if (IsOpen)
                IsAnyOverlayOpen = false;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard[toggleKey].wasPressedThisFrame)
            {
                ToggleVisibility();
                return;
            }

            if (!IsOpen)
                return;

            if (keyboard[navigateUpKey].wasPressedThisFrame)
                MoveSelection(-1);
            else if (keyboard[navigateDownKey].wasPressedThisFrame)
                MoveSelection(1);
        }

        public void Configure(VRInventorySystem targetInventorySystem)
        {
            inventorySystem = targetInventorySystem;
            Refresh();
        }

        private void ToggleVisibility()
        {
            SetVisible(!IsOpen);
            Refresh();
        }

        private void SetVisible(bool visible)
        {
            if (overlayCanvas == null)
                return;

            overlayCanvas.enabled = visible;
            IsAnyOverlayOpen = visible;
        }

        private void MoveSelection(int delta)
        {
            if (inventorySystem == null || inventorySystem.Count == 0)
                return;

            selectedIndex = Mathf.Clamp(selectedIndex + delta, 0, inventorySystem.Count - 1);
            Refresh();
        }

        private void HandleEvidenceAdded(InventoryEvidenceRecord _)
        {
            if (inventorySystem != null && inventorySystem.Count > 0)
                selectedIndex = Mathf.Clamp(selectedIndex, 0, inventorySystem.Count - 1);

            Refresh();
        }

        private void Refresh()
        {
            if (titleText == null || listText == null || detailText == null)
                return;

            int count = inventorySystem != null ? inventorySystem.Count : 0;
            titleText.text = $"Inventario de investigacion ({count})";

            if (inventorySystem == null || count == 0)
            {
                listText.text = "Sin pistas registradas.";
                detailText.text = "Presiona Espacio para cerrar.";
                return;
            }

            selectedIndex = Mathf.Clamp(selectedIndex, 0, count - 1);

            listBuilder.Clear();
            for (int i = 0; i < inventorySystem.CollectedEvidence.Count; i++)
            {
                InventoryEvidenceRecord record = inventorySystem.CollectedEvidence[i];
                listBuilder.Append(i == selectedIndex ? "> " : "  ");
                listBuilder.Append(record.displayName);
                listBuilder.Append(" [");
                listBuilder.Append(record.category);
                listBuilder.Append(']');
                if (i < inventorySystem.CollectedEvidence.Count - 1)
                    listBuilder.AppendLine();
            }

            listText.text = listBuilder.ToString();

            InventoryEvidenceRecord selected = inventorySystem.CollectedEvidence[selectedIndex];
            detailText.text = $"{selected.displayName}\n\nCategoria: {selected.category}\n\n{selected.description}\n\nFlechas: navegar  |  Espacio: cerrar";
        }

        private void CreateOverlay()
        {
            GameObject canvasObject = new GameObject("DesktopInventoryOverlay");
            canvasObject.transform.SetParent(transform, false);

            overlayCanvas = canvasObject.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 1000;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject blocker = CreateImage(canvasObject.transform, "Blocker", new Color(0f, 0f, 0f, 0.65f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            blocker.transform.SetAsFirstSibling();

            GameObject panel = CreateImage(canvasObject.transform, "Panel", new Color(0.08f, 0.1f, 0.12f, 0.96f), new Vector2(0.18f, 0.14f), new Vector2(0.82f, 0.86f), Vector2.zero, Vector2.zero);
            CreateImage(panel.transform, "HeaderBar", new Color(0.14f, 0.18f, 0.22f, 1f), new Vector2(0f, 0.86f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            CreateImage(panel.transform, "ListPanel", new Color(0.12f, 0.14f, 0.16f, 0.92f), new Vector2(0.03f, 0.08f), new Vector2(0.42f, 0.82f), Vector2.zero, Vector2.zero);
            CreateImage(panel.transform, "DetailPanel", new Color(0.11f, 0.12f, 0.14f, 0.92f), new Vector2(0.47f, 0.08f), new Vector2(0.97f, 0.82f), Vector2.zero, Vector2.zero);

            titleText = CreateText(panel.transform, "Title", new Vector2(0.04f, 0.885f), new Vector2(0.94f, 0.985f), 28, FontStyles.Bold, TextAlignmentOptions.Left);
            listText = CreateText(panel.transform, "ListText", new Vector2(0.05f, 0.12f), new Vector2(0.4f, 0.78f), 23, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            detailText = CreateText(panel.transform, "DetailText", new Vector2(0.5f, 0.12f), new Vector2(0.94f, 0.78f), 22, FontStyles.Normal, TextAlignmentOptions.TopLeft);
        }

        private static GameObject CreateImage(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = color;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return go;
        }

        private static TMP_Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, int fontSize, FontStyles style, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return text;
        }
    }
}
