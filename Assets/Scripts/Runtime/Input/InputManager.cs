#nullable enable

using UnityEngine;

namespace CityGenerator.Runtime.Input
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; } = null!;
        public InputSystem_Actions Actions { get; private set; } = null!;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            Actions = new InputSystem_Actions();
        }

        private void OnEnable() => Actions.Enable();
        private void OnDisable() => Actions.Disable();
    }
}