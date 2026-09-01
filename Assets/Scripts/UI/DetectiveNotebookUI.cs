using System.Collections.Generic;
using CrimeVR.Evidence;
using CrimeVR.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrimeVR.UI
{
    public class DetectiveNotebookUI : MonoBehaviour
    {
        [Header("Encabezado y Estado")]
        [SerializeField] private TMP_Text caseTitleText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text statusMessageText;

        [Header("Contenedor de Pistas")]
        [SerializeField] private Transform clueListContainer;
        [SerializeField] private GameObject clueEntryPrefab;

        [Header("Controles")]
        [SerializeField] private Button solveCaseButton;
        [SerializeField] private CanvasGroup notebookCanvasGroup;

        [Header("Colores")]
        [SerializeField] private Color trueClueColor = new Color(0.2f, 0.8f, 0.4f, 1f);
        [SerializeField] private Color falseClueColor = new Color(0.9f, 0.3f, 0.2f, 1f);

        private readonly List<GameObject> activeEntries = new List<GameObject>();

        private void OnEnable()
        {
            if (CaseManager.Instance != null)
            {
                CaseManager.Instance.OnClueDiscovered += HandleClueDiscovered;
                CaseManager.Instance.OnCaseProgressUpdated += HandleProgressUpdated;
                CaseManager.Instance.OnCaseSolved += HandleCaseSolved;
                CaseManager.Instance.OnCaseFailed += HandleCaseFailed;

                RefreshFullNotebook();
            }

            if (solveCaseButton != null)
            {
                solveCaseButton.onClick.AddListener(OnSolveButtonClicked);
            }
        }

        private void OnDisable()
        {
            if (CaseManager.Instance != null)
            {
                CaseManager.Instance.OnClueDiscovered -= HandleClueDiscovered;
                CaseManager.Instance.OnCaseProgressUpdated -= HandleProgressUpdated;
                CaseManager.Instance.OnCaseSolved -= HandleCaseSolved;
                CaseManager.Instance.OnCaseFailed -= HandleCaseFailed;
            }

            if (solveCaseButton != null)
            {
                solveCaseButton.onClick.RemoveListener(OnSolveButtonClicked);
            }
        }

        public void ToggleVisibility()
        {
            if (notebookCanvasGroup == null)
            {
                gameObject.SetActive(!gameObject.activeSelf);
                return;
            }

            bool isVisible = notebookCanvasGroup.alpha > 0.5f;
            notebookCanvasGroup.alpha = isVisible ? 0f : 1f;
            notebookCanvasGroup.interactable = !isVisible;
            notebookCanvasGroup.blocksRaycasts = !isVisible;
        }

        private void RefreshFullNotebook()
        {
            if (CaseManager.Instance == null)
                return;

            if (caseTitleText != null)
                caseTitleText.text = CaseManager.Instance.CaseTitle;

            ClearEntries();

            foreach (ClueData clue in CaseManager.Instance.DiscoveredClues)
            {
                CreateClueEntry(clue);
            }

            UpdateProgressDisplay(
                CaseManager.Instance.TrueCluesCount,
                CaseManager.Instance.RequiredTrueCluesToSolve,
                CaseManager.Instance.FalseCluesCount);
        }

        private void HandleClueDiscovered(ClueData clue)
        {
            CreateClueEntry(clue);
        }

        private void HandleProgressUpdated(int currentTrue, int requiredTrue, int falseCount)
        {
            UpdateProgressDisplay(currentTrue, requiredTrue, falseCount);
        }

        private void UpdateProgressDisplay(int currentTrue, int requiredTrue, int falseCount)
        {
            if (progressText != null)
            {
                progressText.text = $"Pistas Clave: {currentTrue}/{requiredTrue} | Pistas Erradas: {falseCount}/{CaseManager.Instance.MaxAllowedFalseClues}";
            }

            if (solveCaseButton != null)
            {
                solveCaseButton.interactable = currentTrue >= requiredTrue && !CaseManager.Instance.IsCaseResolved;
            }
        }

        private void CreateClueEntry(ClueData clue)
        {
            if (clueListContainer == null || clue == null)
                return;

            GameObject entry;
            if (clueEntryPrefab != null)
            {
                entry = Instantiate(clueEntryPrefab, clueListContainer);
            }
            else
            {
                entry = CreateDefaultClueEntry();
                entry.transform.SetParent(clueListContainer, false);
            }

            TMP_Text textComponent = entry.GetComponentInChildren<TMP_Text>();
            if (textComponent != null)
            {
                string statusTag = clue.IsTrueClue ? "[CLAVE]" : "[DISTRACCIÓN]";
                textComponent.text = $"<b>{statusTag} {clue.ClueName}</b> ({clue.Category})\n<size=80%>{clue.Description}</size>";
                textComponent.color = clue.IsTrueClue ? trueClueColor : falseClueColor;
            }

            activeEntries.Add(entry);
        }

        private GameObject CreateDefaultClueEntry()
        {
            GameObject entry = new GameObject("ClueEntry");
            RectTransform rect = entry.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300f, 60f);

            TextMeshProUGUI text = entry.AddComponent<TextMeshProUGUI>();
            text.fontSize = 14f;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.color = Color.white;
            return entry;
        }

        private void ClearEntries()
        {
            for (int i = activeEntries.Count - 1; i >= 0; i--)
            {
                if (activeEntries[i] != null)
                    Destroy(activeEntries[i]);
            }
            activeEntries.Clear();
        }

        private void OnSolveButtonClicked()
        {
            if (CaseManager.Instance == null)
                return;

            bool success = CaseManager.Instance.ValidateCaseResolution(out string resultMsg);
            if (statusMessageText != null)
            {
                statusMessageText.text = resultMsg;
                statusMessageText.color = success ? trueClueColor : falseClueColor;
            }
        }

        private void HandleCaseSolved(string caseId)
        {
            if (statusMessageText != null)
            {
                statusMessageText.text = "¡CASO RESUELTO CON ÉXITO! Has resuelto el misterio.";
                statusMessageText.color = trueClueColor;
            }

            if (solveCaseButton != null)
                solveCaseButton.interactable = false;
        }

        private void HandleCaseFailed(string caseId, string reason)
        {
            if (statusMessageText != null)
            {
                statusMessageText.text = $"CASO FALLIDO: {reason}";
                statusMessageText.color = falseClueColor;
            }

            if (solveCaseButton != null)
                solveCaseButton.interactable = false;
        }
    }
}
