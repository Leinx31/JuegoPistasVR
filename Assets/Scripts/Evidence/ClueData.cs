using UnityEngine;

namespace CrimeVR.Evidence
{
    [CreateAssetMenu(fileName = "NewClueData", menuName = "Crime VR/Clue Data", order = 10)]
    public class ClueData : ScriptableObject
    {
        [Header("Identificación")]
        [Tooltip("Identificador único de la pista para el seguimiento del caso")]
        [SerializeField] private string clueId = "clue.default.001";

        [Tooltip("Nombre visible de la evidencia")]
        [SerializeField] private string clueName = "Nombre de la Pista";

        [Tooltip("Categoría de la evidencia (ej: Arma, Documento, Rastro Forense)")]
        [SerializeField] private string category = "Evidencia General";

        [Header("Detalles de Investigación")]
        [TextArea(3, 8)]
        [Tooltip("Descripción detallada o notas forenses sobre la pista")]
        [SerializeField] private string description = "Descripción forense del objeto encontrado en la escena del crimen.";

        [Tooltip("¿Es una pista verídica requerida para resolver el caso? Si es falso, actúa como distractor (Red Herring)")]
        [SerializeField] private bool isTrueClue = true;

        [Header("Audio y Visual")]
        [Tooltip("Clip de audio narrativo o efecto espacial que se reproduce al recoger/inspeccionar la pista")]
        [SerializeField] private AudioClip inspectAudioClip;

        [Tooltip("Icono representativo para la libreta/HUD")]
        [SerializeField] private Sprite clueIcon;

        // Propiedades públicas de solo lectura
        public string ClueId => clueId;
        public string ClueName => clueName;
        public string Category => category;
        public string Description => description;
        public bool IsTrueClue => isTrueClue;
        public AudioClip InspectAudioClip => inspectAudioClip;
        public Sprite ClueIcon => clueIcon;

        public void Initialize(string id, string name, string desc, bool isTrue, AudioClip audio = null, Sprite icon = null, string cat = "General")
        {
            clueId = id;
            clueName = name;
            description = desc;
            isTrueClue = isTrue;
            inspectAudioClip = audio;
            clueIcon = icon;
            category = cat;
        }
    }
}
