using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace CrimeVR.Tools
{
    [RequireComponent(typeof(XRGrabInteractable))]
    public class UVFlashlightTool : MonoBehaviour
    {
        [SerializeField] private Light uvLight;
        [SerializeField] private Transform beamOrigin;
        [SerializeField] private InputActionReference leftActivateAction;
        [SerializeField] private InputActionReference rightActivateAction;
        [SerializeField] private float revealIntensity = 1f;
        [SerializeField] private float maxReactiveDistance = 2.25f;
        [SerializeField] private bool startsEnabled;

        private XRGrabInteractable grabInteractable;
        private readonly List<UVReactiveSurface> reactiveSurfaces = new List<UVReactiveSurface>();
        private bool isHeld;
        private bool isLightOn;
        private bool wasLeftPressedLastFrame;
        private bool wasRightPressedLastFrame;

        public bool IsLightOn => isLightOn;

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            if (uvLight == null)
                uvLight = GetComponentInChildren<Light>(true);
            if (beamOrigin == null)
                beamOrigin = uvLight != null ? uvLight.transform : transform;

            grabInteractable.selectEntered.AddListener(OnSelectEntered);
            grabInteractable.selectExited.AddListener(OnSelectExited);

            SetLightState(startsEnabled);
        }

        private void OnEnable()
        {
            leftActivateAction?.action?.Enable();
            rightActivateAction?.action?.Enable();
            RefreshReactiveCache();
        }

        private void OnDisable()
        {
            leftActivateAction?.action?.Disable();
            rightActivateAction?.action?.Disable();
        }

        private void Update()
        {
            HandleToggleInput();
            UpdateReactiveSurfaces();
        }

        public void Configure(Light targetLight, Transform targetBeamOrigin, InputActionReference leftAction, InputActionReference rightAction)
        {
            uvLight = targetLight;
            beamOrigin = targetBeamOrigin;
            leftActivateAction = leftAction;
            rightActivateAction = rightAction;
            SetLightState(startsEnabled);
            RefreshReactiveCache();
        }

        public void SetDesktopHeldState(bool held)
        {
            isHeld = held;
        }

        public void ToggleLight()
        {
            SetLightState(!isLightOn);
        }

        public void RefreshReactiveCache()
        {
            reactiveSurfaces.Clear();
            reactiveSurfaces.AddRange(Object.FindObjectsByType<UVReactiveSurface>(FindObjectsSortMode.None));
        }

        private void HandleToggleInput()
        {
            bool leftPressed = leftActivateAction != null && leftActivateAction.action != null && leftActivateAction.action.IsPressed();
            bool rightPressed = rightActivateAction != null && rightActivateAction.action != null && rightActivateAction.action.IsPressed();

            if (isHeld && ((leftPressed && !wasLeftPressedLastFrame) || (rightPressed && !wasRightPressedLastFrame)))
                SetLightState(!isLightOn);

            wasLeftPressedLastFrame = leftPressed;
            wasRightPressedLastFrame = rightPressed;
        }

        private void UpdateReactiveSurfaces()
        {
            if (beamOrigin == null)
                return;

            Vector3 origin = beamOrigin.position;
            Vector3 forward = beamOrigin.forward;
            bool canReveal = isHeld && isLightOn;

            for (int i = 0; i < reactiveSurfaces.Count; i++)
            {
                UVReactiveSurface surface = reactiveSurfaces[i];
                if (surface == null)
                    continue;

                surface.UpdateUVReveal(origin, forward, revealIntensity, canReveal);
            }
        }

        private void SetLightState(bool active)
        {
            isLightOn = active;
            if (uvLight != null)
                uvLight.enabled = active;
        }

        private void OnSelectEntered(SelectEnterEventArgs _)
        {
            isHeld = true;
        }

        private void OnSelectExited(SelectExitEventArgs _)
        {
            isHeld = false;
        }
    }
}
