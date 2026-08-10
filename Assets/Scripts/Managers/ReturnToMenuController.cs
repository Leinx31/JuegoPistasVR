using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace CrimeVR.Managers
{
    [DisallowMultipleComponent]
    public class ReturnToMenuController : MonoBehaviour
    {
        [SerializeField] private string menuSceneName = "CaseSelection_Map";
        [SerializeField] private Key returnKey = Key.Escape;
        [SerializeField] private Key alternateReturnKey = Key.Backquote;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard[returnKey].wasPressedThisFrame || keyboard[alternateReturnKey].wasPressedThisFrame)
                ReturnToMenu();
        }

        public void ReturnToMenu()
        {
            if (string.IsNullOrWhiteSpace(menuSceneName))
                return;

            SceneManager.LoadScene(menuSceneName, LoadSceneMode.Single);
        }
    }
}
