using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CrimeVR.Interaction
{
    public class InspectableObject : MonoBehaviour
    {
        [SerializeField] private Vector3 inspectionLocalPosition = new Vector3(0f, 0f, 0.5f);
        [SerializeField] private Vector3 inspectionLocalEulerAngles = new Vector3(0f, 180f, 0f);
        [SerializeField] private Vector3 inspectionLocalScale = Vector3.one;
        [SerializeField] private bool freezePhysicsDuringInspection = true;
        [SerializeField] private string clueId = "clue.inspectable.default";
        [SerializeField] private string displayName = "Evidence";
        [SerializeField] private string category = "Clue";
        [TextArea]
        [SerializeField] private string description = "Pista inspeccionable.";
        [SerializeField] private bool registerOnInspect = true;
        [SerializeField] private Color highlightColor = new Color(0.3f, 0.75f, 1f, 1f);
        [SerializeField] private float highlightIntensity = 0.45f;

        private Transform originalParent;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private Vector3 originalScale;
        private Rigidbody cachedRigidbody;
        private XRGrabInteractable grabInteractable;
        private Renderer[] cachedRenderers;
        private Color[] originalColors;
        private bool originalUseGravity;
        private bool originalIsKinematic;
        private bool hasSnapshot;
        private bool isInspecting;
        private bool isHighlighted;

        public bool IsInspecting => isInspecting;
        public string ClueId => clueId;
        public string DisplayName => displayName;
        public string Category => category;
        public string Description => description;
        public bool RegisterOnInspect => registerOnInspect;

        private void Awake()
        {
            cachedRigidbody = GetComponent<Rigidbody>();
            grabInteractable = GetComponent<XRGrabInteractable>();
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
            originalColors = new Color[cachedRenderers.Length];
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                Material material = cachedRenderers[i] != null ? cachedRenderers[i].sharedMaterial : null;
                originalColors[i] = material != null && material.HasProperty("_BaseColor")
                    ? material.GetColor("_BaseColor")
                    : Color.white;
            }
        }

        public void BeginInspection(Transform inspectionAnchor)
        {
            if (inspectionAnchor == null || isInspecting)
                return;

            if (!hasSnapshot)
            {
                originalParent = transform.parent;
                originalPosition = transform.position;
                originalRotation = transform.rotation;
                originalScale = transform.localScale;
                hasSnapshot = true;
            }

            if (freezePhysicsDuringInspection && cachedRigidbody != null)
            {
                originalUseGravity = cachedRigidbody.useGravity;
                originalIsKinematic = cachedRigidbody.isKinematic;
                cachedRigidbody.linearVelocity = Vector3.zero;
                cachedRigidbody.angularVelocity = Vector3.zero;
                cachedRigidbody.useGravity = false;
                cachedRigidbody.isKinematic = true;
            }

            if (grabInteractable != null)
                grabInteractable.enabled = false;

            transform.SetParent(inspectionAnchor, false);
            transform.localPosition = inspectionLocalPosition;
            transform.localEulerAngles = inspectionLocalEulerAngles;
            transform.localScale = inspectionLocalScale;
            isInspecting = true;
        }

        public void EndInspection()
        {
            if (!hasSnapshot || !isInspecting)
                return;

            transform.SetParent(originalParent, true);
            transform.position = originalPosition;
            transform.rotation = originalRotation;
            transform.localScale = originalScale;

            if (grabInteractable != null)
                grabInteractable.enabled = true;

            if (freezePhysicsDuringInspection && cachedRigidbody != null)
            {
                cachedRigidbody.useGravity = originalUseGravity;
                cachedRigidbody.isKinematic = originalIsKinematic;
            }

            isInspecting = false;
        }

        public void SetMetadata(string newClueId, string newDisplayName, string newCategory, string newDescription, bool shouldRegisterOnInspect)
        {
            clueId = newClueId;
            displayName = newDisplayName;
            category = newCategory;
            description = newDescription;
            registerOnInspect = shouldRegisterOnInspect;
        }

        public void SetHighlighted(bool highlighted)
        {
            if (isHighlighted == highlighted)
                return;

            isHighlighted = highlighted;
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                Renderer renderer = cachedRenderers[i];
                if (renderer == null || renderer.sharedMaterial == null || !renderer.sharedMaterial.HasProperty("_BaseColor"))
                    continue;

                Color targetColor = highlighted
                    ? Color.Lerp(originalColors[i], highlightColor, highlightIntensity)
                    : originalColors[i];

                renderer.material.SetColor("_BaseColor", targetColor);
            }
        }
    }
}
