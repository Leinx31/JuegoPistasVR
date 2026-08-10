using UnityEngine;

namespace CrimeVR.Tools
{
    [RequireComponent(typeof(Renderer))]
    public class UVReactiveSurface : MonoBehaviour
    {
        [SerializeField] private Color hiddenColor = new Color(0f, 0f, 0f, 0f);
        [SerializeField] private Color revealedColor = new Color(0.25f, 0.85f, 1f, 1f);
        [SerializeField] private float revealDistance = 2.25f;
        [SerializeField] private float revealSharpness = 6f;
        [SerializeField] private string colorPropertyName = "_BaseColor";
        [SerializeField] private string emissionPropertyName = "_EmissionColor";

        private Renderer cachedRenderer;
        private MaterialPropertyBlock propertyBlock;
        private int colorPropertyId;
        private int emissionPropertyId;
        private float revealAmount;

        private void Awake()
        {
            cachedRenderer = GetComponent<Renderer>();
            propertyBlock = new MaterialPropertyBlock();
            colorPropertyId = Shader.PropertyToID(colorPropertyName);
            emissionPropertyId = Shader.PropertyToID(emissionPropertyName);
            ApplyReveal(0f);
        }

        public void UpdateUVReveal(Vector3 flashlightPosition, Vector3 flashlightForward, float intensity, bool isActive)
        {
            if (!isActive || intensity <= 0f)
            {
                revealAmount = Mathf.MoveTowards(revealAmount, 0f, Time.deltaTime * revealSharpness);
                ApplyReveal(revealAmount);
                return;
            }

            Vector3 toSurface = transform.position - flashlightPosition;
            float distance = toSurface.magnitude;
            if (distance > revealDistance)
            {
                revealAmount = Mathf.MoveTowards(revealAmount, 0f, Time.deltaTime * revealSharpness);
                ApplyReveal(revealAmount);
                return;
            }

            float angleDot = Mathf.Max(0f, Vector3.Dot(flashlightForward.normalized, toSurface.normalized));
            float distanceFactor = 1f - Mathf.Clamp01(distance / revealDistance);
            float targetReveal = intensity * angleDot * distanceFactor;
            revealAmount = Mathf.MoveTowards(revealAmount, targetReveal, Time.deltaTime * revealSharpness);
            ApplyReveal(revealAmount);
        }

        private void ApplyReveal(float amount)
        {
            if (cachedRenderer == null)
                return;

            Color surfaceColor = Color.Lerp(hiddenColor, revealedColor, Mathf.Clamp01(amount));
            Color emissionColor = revealedColor * Mathf.LinearToGammaSpace(amount * 2f);

            cachedRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(colorPropertyId, surfaceColor);
            propertyBlock.SetColor(emissionPropertyId, emissionColor);
            cachedRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
