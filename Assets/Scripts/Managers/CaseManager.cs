using System;
using System.Collections.Generic;
using CrimeVR.Evidence;
using UnityEngine;

namespace CrimeVR.Managers
{
    [DisallowMultipleComponent]
    public class CaseManager : MonoBehaviour
    {
        public static CaseManager Instance { get; private set; }

        [Header("Configuración del Caso")]
        [SerializeField] private string caseId = "case.expediente506";
        [SerializeField] private string caseTitle = "Caso: El Misterio Urbano";
        [TextArea(2, 5)]
        [SerializeField] private string caseSynopsis = "Investiga la escena del crimen, reúne las pistas clave y evita las pistas falsas para resolver el misterio.";

        [Header("Condiciones de Resolución")]
        [Tooltip("Cantidad de pistas verdaderas necesarias para considerar el caso resuelto")]
        [SerializeField] private int requiredTrueCluesToSolve = 3;

        [Tooltip("Número máximo de pistas falsas permitidas antes de fallar la investigación")]
        [SerializeField] private int maxAllowedFalseClues = 2;

        [Header("Audio Feedback")]
        [SerializeField] private AudioClip caseSolvedAudio;
        [SerializeField] private AudioClip caseFailedAudio;
        [SerializeField] private AudioClip clueFoundAudio;

        // Estado en tiempo de ejecución
        private readonly HashSet<string> discoveredClueIds = new HashSet<string>();
        private readonly List<ClueData> discoveredClues = new List<ClueData>();
        private int trueCluesCount;
        private int falseCluesCount;
        private bool isCaseResolved;
        private bool isCaseFailed;
        private AudioSource globalAudioSource;

        // Eventos C#
        public event Action<ClueData> OnClueDiscovered;
        public event Action<int, int, int> OnCaseProgressUpdated; // (trueClues, requiredTrueClues, falseClues)
        public event Action<string> OnCaseSolved;
        public event Action<string, string> OnCaseFailed; // (caseId, reason)

        // Propiedades públicas
        public string CaseId => caseId;
        public string CaseTitle => caseTitle;
        public string CaseSynopsis => caseSynopsis;
        public int RequiredTrueCluesToSolve => requiredTrueCluesToSolve;
        public int MaxAllowedFalseClues => maxAllowedFalseClues;
        public int TrueCluesCount => trueCluesCount;
        public int FalseCluesCount => falseCluesCount;
        public bool IsCaseResolved => isCaseResolved;
        public bool IsCaseFailed => isCaseFailed;
        public IReadOnlyList<ClueData> DiscoveredClues => discoveredClues;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            globalAudioSource = GetComponent<AudioSource>();
            if (globalAudioSource == null)
            {
                globalAudioSource = gameObject.AddComponent<AudioSource>();
                globalAudioSource.playOnAwake = false;
                globalAudioSource.spatialBlend = 0f; // Audio 2D global para eventos del sistema
            }
        }

        private void Start()
        {
            NotifyProgress();
        }

        /// <summary>
        /// Registra una pista cuando el jugador la encuentra o interactúa con ella.
        /// </summary>
        public bool RegisterDiscoveredClue(ClueData clue)
        {
            if (clue == null || isCaseResolved || isCaseFailed)
                return false;

            if (discoveredClueIds.Contains(clue.ClueId))
                return false; // Ya fue registrada

            discoveredClueIds.Add(clue.ClueId);
            discoveredClues.Add(clue);

            if (clue.IsTrueClue)
            {
                trueCluesCount++;
                Debug.Log($"[CaseManager] Pista VERDADERA descubierta: {clue.ClueName} ({trueCluesCount}/{requiredTrueCluesToSolve})");
            }
            else
            {
                falseCluesCount++;
                Debug.LogWarning($"[CaseManager] Pista FALSA descubierta (Red Herring): {clue.ClueName} ({falseCluesCount}/{maxAllowedFalseClues} fallos permitidos)");
            }

            if (clueFoundAudio != null && globalAudioSource != null)
                globalAudioSource.PlayOneShot(clueFoundAudio);

            OnClueDiscovered?.Invoke(clue);
            NotifyProgress();

            // Verificar si excede el límite de pistas falsas
            if (falseCluesCount > maxAllowedFalseClues)
            {
                FailCase("Has acumulado demasiadas pistas falsas y la investigación se ha desviado.");
            }
            // Verificar si ya completó todas las pistas verdaderas
            else if (trueCluesCount >= requiredTrueCluesToSolve)
            {
                SolveCase();
            }

            return true;
        }

        /// <summary>
        /// Valida explícitamente si el jugador puede concluir el caso en base a las pistas recolectadas.
        /// </summary>
        public bool ValidateCaseResolution(out string resultMessage)
        {
            if (isCaseResolved)
            {
                resultMessage = "El caso ya ha sido resuelto con éxito.";
                return true;
            }

            if (isCaseFailed)
            {
                resultMessage = "El caso ha fracasado.";
                return false;
            }

            if (trueCluesCount >= requiredTrueCluesToSolve && falseCluesCount <= maxAllowedFalseClues)
            {
                SolveCase();
                resultMessage = "¡Felicidades! Has reunido todas las evidencias clave para resolver el caso.";
                return true;
            }

            if (falseCluesCount > maxAllowedFalseClues)
            {
                FailCase("La evidencia recolectada incluye demasiados elementos contradictorios.");
                resultMessage = "Investigación fallida por exceso de pistas erróneas.";
                return false;
            }

            resultMessage = $"Aún faltan evidencias. ({trueCluesCount}/{requiredTrueCluesToSolve} pistas clave encontradas).";
            return false;
        }

        private void SolveCase()
        {
            if (isCaseResolved || isCaseFailed)
                return;

            isCaseResolved = true;
            Debug.Log($"[CaseManager] ¡CASO RESUELTO CON ÉXITO!: {caseTitle}");

            if (caseSolvedAudio != null && globalAudioSource != null)
                globalAudioSource.PlayOneShot(caseSolvedAudio);

            OnCaseSolved?.Invoke(caseId);
        }

        private void FailCase(string reason)
        {
            if (isCaseResolved || isCaseFailed)
                return;

            isCaseFailed = true;
            Debug.LogError($"[CaseManager] CASO FALLIDO: {reason}");

            if (caseFailedAudio != null && globalAudioSource != null)
                globalAudioSource.PlayOneShot(caseFailedAudio);

            OnCaseFailed?.Invoke(caseId, reason);
        }

        private void NotifyProgress()
        {
            OnCaseProgressUpdated?.Invoke(trueCluesCount, requiredTrueCluesToSolve, falseCluesCount);
        }

        public void ResetCase()
        {
            discoveredClueIds.Clear();
            discoveredClues.Clear();
            trueCluesCount = 0;
            falseCluesCount = 0;
            isCaseResolved = false;
            isCaseFailed = false;
            NotifyProgress();
        }

        public void Configure(string newCaseId, string newTitle, int requiredTrue, int maxFalse)
        {
            caseId = newCaseId;
            caseTitle = newTitle;
            requiredTrueCluesToSolve = requiredTrue;
            maxAllowedFalseClues = maxFalse;
            ResetCase();
        }
    }
}
