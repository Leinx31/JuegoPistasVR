using System;
using CrimeVR.Managers;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CrimeVR.Evidence
{
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(AudioSource))]
    [DisallowMultipleComponent]
    public class ClueInteractable : MonoBehaviour
    {
        [Header("Datos de la Pista")]
        [Tooltip("ScriptableObject con toda la información de la pista")]
        [SerializeField] private ClueData clueData;

        [Header("Comportamiento")]
        [Tooltip("¿Registrar automáticamente la pista en el CaseManager al agarrarla?")]
        [SerializeField] private bool registerOnGrab = true;

        [Tooltip("¿Destruir o desactivar el objeto al guardarlo/recogerlo?")]
        [SerializeField] private bool hideOnCollected = false;

        [Header("Audio 3D Espacial")]
        [Tooltip("Volumen del clip de audio 3D")]
        [Range(0f, 1f)]
        [SerializeField] private float audioVolume = 1f;

        [Tooltip("Distancia mínima para atenuación de audio 3D")]
        [SerializeField] private float minAudioDistance = 0.5f;

        [Tooltip("Distancia máxima para atenuación de audio 3D")]
        [SerializeField] private float maxAudioDistance = 8f;

        private XRGrabInteractable grabInteractable;
        private AudioSource spatialAudioSource;
        private bool isRegistered;

        public event Action<ClueData> ClueInspected;

        public ClueData ClueData => clueData;
        public bool IsRegistered => isRegistered;

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            spatialAudioSource = GetComponent<AudioSource>();
            ConfigureSpatialAudio();
        }

        private void OnEnable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.AddListener(OnSelectEntered);
            }
        }

        private void OnDisable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            }
        }

        private void ConfigureSpatialAudio()
        {
            if (spatialAudioSource == null)
                return;

            spatialAudioSource.spatialBlend = 1f; // 100% 3D espacial
            spatialAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            spatialAudioSource.minDistance = minAudioDistance;
            spatialAudioSource.maxDistance = maxAudioDistance;
            spatialAudioSource.playOnAwake = false;
            spatialAudioSource.volume = audioVolume;
            spatialAudioSource.dopplerLevel = 0f;
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            TriggerInspection();
        }

        /// <summary>
        /// Activa la inspección del objeto, reproduciendo el audio 3D y registrando la pista en el CaseManager.
        /// </summary>
        public void TriggerInspection()
        {
            if (clueData == null)
                return;

            PlaySpatialAudio();

            if (registerOnGrab && !isRegistered)
            {
                isRegistered = true;
                if (CaseManager.Instance != null)
                {
                    CaseManager.Instance.RegisterDiscoveredClue(clueData);
                }
                else
                {
                    Debug.LogWarning($"[ClueInteractable] CaseManager.Instance no encontrado en la escena al inspeccionar {clueData.ClueName}.");
                }
            }

            ClueInspected?.Invoke(clueData);

            if (hideOnCollected)
            {
                gameObject.SetActive(false);
            }
        }

        private static AudioClip cachedChimeClip;

        private void PlaySpatialAudio()
        {
            if (spatialAudioSource == null)
                return;

            if (clueData != null && clueData.InspectAudioClip != null)
            {
                spatialAudioSource.clip = clueData.InspectAudioClip;
                spatialAudioSource.Play();
            }
            else
            {
                AudioClip fallbackClip = GetOrCreateProceduralChime();
                spatialAudioSource.PlayOneShot(fallbackClip, audioVolume);
            }
        }

        private static AudioClip GetOrCreateProceduralChime()
        {
            if (cachedChimeClip != null)
                return cachedChimeClip;

            int sampleRate = 44100;
            float duration = 0.55f;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-5.5f * t);
                float wave = Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.5f +
                             Mathf.Sin(2f * Mathf.PI * 1320f * t) * 0.35f +
                             Mathf.Sin(2f * Mathf.PI * 1760f * t) * 0.2f;
                samples[i] = wave * envelope * 0.7f;
            }

            cachedChimeClip = AudioClip.Create("Clue_Procedural_Chime", sampleCount, 1, sampleRate, false);
            cachedChimeClip.SetData(samples, 0);
            return cachedChimeClip;
        }

        public void SetClueData(ClueData newClueData)
        {
            clueData = newClueData;
        }
    }
}
