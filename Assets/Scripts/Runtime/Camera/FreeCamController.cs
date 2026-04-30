#nullable enable
using CityGenerator.Runtime.Input;
using UnityEngine;

namespace CityGen.Runtime.Camera
{
    public class FreeCamController : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 20f;
        [SerializeField] private float _lookSpeed = 0.15f;

        private InputSystem_Actions _actions = null!;
        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private float _pitch;
        private float _yaw;

        private void Awake()
        {
            _actions = InputManager.Instance.Actions;
            _actions.Player.Move.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
            _actions.Player.Move.canceled += ctx => _moveInput = Vector2.zero;
            _actions.Player.Look.performed += ctx => _lookInput = ctx.ReadValue<Vector2>();
            _actions.Player.Look.canceled += ctx => _lookInput = Vector2.zero;
        }

        private void OnEnable()
        {
            // Initialise euler angles from current transform so there's no snap on enable
            _pitch = transform.eulerAngles.x;
            _yaw = transform.eulerAngles.y;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
        }

        private void Update()
        {
            // Look
            _yaw += _lookInput.x * _lookSpeed;
            _pitch -= _lookInput.y * _lookSpeed; // subtract so mouse-up looks up
            _pitch = Mathf.Clamp(_pitch, -89f, 89f);
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

            // Move
            var move = new Vector3(_moveInput.x, 0f, _moveInput.y);
            transform.Translate(move * _moveSpeed * Time.deltaTime, Space.Self);
        }
    }
}