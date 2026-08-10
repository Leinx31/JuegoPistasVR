using UnityEngine;

namespace CrimeVR.Player
{
    [DisallowMultipleComponent]
    public class InvestigatorViewRig : MonoBehaviour
    {
        [SerializeField] private bool desktopOnly = true;
        [SerializeField] private Transform visualsRoot;
        [SerializeField] private float positionLerpSpeed = 12f;
        [SerializeField] private float rotationLerpSpeed = 12f;

        private Transform targetCamera;
        private Vector3 initialLocalPosition;
        private Quaternion initialLocalRotation;

        private void Awake()
        {
            if (visualsRoot == null)
                visualsRoot = transform;

            targetCamera = GetComponentInParent<Camera>()?.transform;
            initialLocalPosition = visualsRoot.localPosition;
            initialLocalRotation = visualsRoot.localRotation;
        }

        private void OnEnable()
        {
            UpdateVisibility();
        }

        private void LateUpdate()
        {
            if (visualsRoot == null)
                return;

            UpdateVisibility();

            visualsRoot.localPosition = Vector3.Lerp(
                visualsRoot.localPosition,
                initialLocalPosition,
                Time.deltaTime * positionLerpSpeed);

            visualsRoot.localRotation = Quaternion.Slerp(
                visualsRoot.localRotation,
                initialLocalRotation,
                Time.deltaTime * rotationLerpSpeed);
        }

        private void UpdateVisibility()
        {
            if (visualsRoot == null)
                return;

            bool shouldShow = true;
            if (desktopOnly)
                shouldShow = GetComponentInParent<DesktopDebugController>() != null;

            if (visualsRoot.gameObject.activeSelf != shouldShow)
                visualsRoot.gameObject.SetActive(shouldShow);
        }
    }
}
