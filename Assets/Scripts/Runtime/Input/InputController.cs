#nullable enable
using UnityEngine;
using UnityEngine.InputSystem;

namespace CityGenerator.Runtime.Input
{
    public class InputController : MonoBehaviour
    {
        [SerializeField] private TrafficController _trafficController = null!;
        [SerializeField] private CityGeneratorController _cityController = null!;
        [SerializeField] private Camera _topDownCamera = null!;
        [SerializeField] private Camera _freeCam = null!;

        private InputSystem_Actions _actions = null!;
        private bool _paused = false;
        private bool _topDown = true;

        private void Awake()
        {
            _actions = InputManager.Instance.Actions;
            _actions.Player.ToggleMode.performed += OnToggleMode;
            _actions.Player.TogglePause.performed += OnTogglePause;
            _actions.Player.ToggleCamera.performed += OnToggleCamera;
        }

        private void Start() => ApplyCameraState();

        private void OnToggleMode(InputAction.CallbackContext ctx)
        {
            if (_trafficController.Mode == CityMode.Generation)
                _trafficController.EnterSimulation();
            else
                _trafficController.EnterGeneration();
        }

        private void OnTogglePause(InputAction.CallbackContext ctx)
        {
            if (_trafficController.Mode == CityMode.Generation)
            {
                _cityController.Paused = !_cityController.Paused;
            }
            else if (_trafficController.Mode == CityMode.Simulation)
            {
                _trafficController.Paused = !_trafficController.Paused;
            }
            _paused = !_paused;
        }

        private void OnToggleCamera(InputAction.CallbackContext ctx)
        {
            _topDown = !_topDown;
            ApplyCameraState();
        }

        private void ApplyCameraState()
        {
            _topDownCamera.gameObject.SetActive(_topDown);
            _freeCam.gameObject.SetActive(!_topDown);
        }
    }
}