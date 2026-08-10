using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CrimeVR.UI
{
    [DisallowMultipleComponent]
    public class CaseSelectionMenu : MonoBehaviour
    {
        [System.Serializable]
        public class CaseEntry
        {
            public string caseId = "case.default";
            public string displayName = "Caso";
            [TextArea] public string summary = "Descripcion del caso.";
            public string sceneName = "CrimeScene_Prototype";
            public Button button;
        }

        [SerializeField] private List<CaseEntry> cases = new List<CaseEntry>();
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private TMP_Text hintText;
        [SerializeField] private Color normalColor = new Color(0.16f, 0.18f, 0.22f, 0.94f);
        [SerializeField] private Color selectedColor = new Color(0.54f, 0.12f, 0.12f, 0.98f);
        [SerializeField] private Vector3 normalScale = Vector3.one;
        [SerializeField] private Vector3 selectedScale = new Vector3(1.03f, 1.03f, 1f);
        [SerializeField] private Key confirmKey = Key.Enter;

        private int selectedIndex;

        private void Awake()
        {
            for (int i = 0; i < cases.Count; i++)
            {
                int capturedIndex = i;
                if (cases[i].button != null)
                    cases[i].button.onClick.AddListener(() => SelectAndLoad(capturedIndex));
            }

            RefreshSelection();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || cases.Count == 0)
                return;

            bool previousPressed = keyboard.leftArrowKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame;
            bool nextPressed = keyboard.rightArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame;

            if (previousPressed)
                MoveSelection(-1);
            else if (nextPressed)
                MoveSelection(1);
            else if (keyboard[confirmKey].wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
                LoadSelectedCase();
        }

        public void Configure(List<CaseEntry> entries, TMP_Text targetTitle, TMP_Text targetSummary, TMP_Text targetHint)
        {
            cases = entries;
            titleText = targetTitle;
            summaryText = targetSummary;
            hintText = targetHint;
            selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, cases.Count - 1));
            RefreshSelection();
        }

        public void SelectAndLoad(int caseIndex)
        {
            if (caseIndex < 0 || caseIndex >= cases.Count)
                return;

            selectedIndex = caseIndex;
            RefreshSelection();
            LoadSelectedCase();
        }

        public void LoadSelectedCase()
        {
            if (cases.Count == 0)
                return;

            string targetScene = cases[selectedIndex].sceneName;
            if (!string.IsNullOrWhiteSpace(targetScene))
                SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
        }

        private void MoveSelection(int direction)
        {
            if (cases.Count == 0)
                return;

            selectedIndex += direction;
            if (selectedIndex < 0)
                selectedIndex = cases.Count - 1;
            else if (selectedIndex >= cases.Count)
                selectedIndex = 0;

            RefreshSelection();
        }

        private void RefreshSelection()
        {
            if (cases.Count == 0)
                return;

            selectedIndex = Mathf.Clamp(selectedIndex, 0, cases.Count - 1);

            for (int i = 0; i < cases.Count; i++)
            {
                if (cases[i].button == null)
                    continue;

                Image buttonImage = cases[i].button.GetComponent<Image>();
                if (buttonImage != null)
                    buttonImage.color = i == selectedIndex ? selectedColor : normalColor;

                RectTransform buttonRect = cases[i].button.transform as RectTransform;
                if (buttonRect != null)
                    buttonRect.localScale = i == selectedIndex ? selectedScale : normalScale;
            }

            if (titleText != null)
                titleText.text = cases[selectedIndex].displayName;

            if (summaryText != null)
                summaryText.text = cases[selectedIndex].summary;

            if (hintText != null)
                hintText.text = "Flechas: seleccionar  |  Enter: ingresar";
        }
    }
}
