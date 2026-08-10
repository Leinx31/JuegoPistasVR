using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System;

namespace CrimeVR.Evidence
{
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(Rigidbody))]
    public class EvidenceCollectible : MonoBehaviour
    {
        [SerializeField] private string evidenceId = "evidence.debug.001";
        [SerializeField] private string displayName = "Latex Glove";
        [SerializeField] private string category = "Trace";
        [TextArea]
        [SerializeField] private string description = "Objeto de prueba para validar recoleccion e inventario VR.";

        private XRGrabInteractable grabInteractable;
        private Rigidbody cachedRigidbody;
        private Collider[] cachedColliders;
        private bool isCollected;

        public event Action<EvidenceCollectible> Collected;
        public string EvidenceId => evidenceId;
        public string DisplayName => displayName;
        public string Category => category;
        public string Description => description;
        public bool IsCollected => isCollected;
        public bool IsBeingHeld => grabInteractable != null && grabInteractable.isSelected;
        public XRGrabInteractable GrabInteractable => grabInteractable;

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            cachedRigidbody = GetComponent<Rigidbody>();
            cachedColliders = GetComponentsInChildren<Collider>(true);
        }

        public void Collect()
        {
            if (isCollected)
                return;

            isCollected = true;

            if (grabInteractable != null)
                grabInteractable.enabled = false;

            if (cachedRigidbody != null)
            {
                cachedRigidbody.linearVelocity = Vector3.zero;
                cachedRigidbody.angularVelocity = Vector3.zero;
                cachedRigidbody.isKinematic = true;
                cachedRigidbody.useGravity = false;
            }

            for (int i = 0; i < cachedColliders.Length; i++)
                cachedColliders[i].enabled = false;

            Collected?.Invoke(this);
            gameObject.SetActive(false);
        }
    }
}
