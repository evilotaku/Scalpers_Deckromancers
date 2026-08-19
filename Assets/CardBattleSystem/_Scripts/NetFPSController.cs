using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class NetFPSController : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _sprintSpeed = 8f;
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private float _gravity = -9.81f;

    [Header("Rotation Settings")]
    [SerializeField] private float _rotationSpeed = 0.5f;

    [Header("References")]
    [SerializeField] private CinemachineCamera _playerCamera;
    
    private CharacterController _characterController;
    private PlayerInput _playerInput;
    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _jumpAction;
    private InputAction _sprintAction;

    private Vector3 _velocity;
    private bool _isGrounded;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _playerInput = GetComponent<PlayerInput>();
        
        _moveAction = _playerInput.actions["Move"];
        _lookAction = _playerInput.actions["Look"];
        _jumpAction = _playerInput.actions["Jump"];
        _sprintAction = _playerInput.actions["Sprint"];
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            if (_playerCamera != null)
            {
                _playerCamera.Priority = 10;
                _playerCamera.gameObject.SetActive(true);
            }
            _playerInput.enabled = true;
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            if (_playerCamera != null)
            {
                _playerCamera.Priority = 0;
                _playerCamera.gameObject.SetActive(false);
            }
            _playerInput.enabled = false;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        HandleRotation();
        HandleMovement();
    }

    private void HandleRotation()
    {
        Vector2 lookInput = _lookAction.ReadValue<Vector2>();
        // Rotate player horizontally
        transform.Rotate(Vector3.up * lookInput.x * _rotationSpeed);
    }

    private void HandleMovement()
    {
        _isGrounded = _characterController.isGrounded;
        if (_isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f;
        }

        Vector2 input = _moveAction.ReadValue<Vector2>();
        float currentSpeed = _sprintAction.ReadValue<float>() > 0.5f ? _sprintSpeed : _moveSpeed;

        Vector3 move = transform.right * input.x + transform.forward * input.y;
        _characterController.Move(move * currentSpeed * Time.deltaTime);

        if (_jumpAction.triggered && _isGrounded)
        {
            _velocity.y = Mathf.Sqrt(_jumpForce * -2f * _gravity);
        }

        _velocity.y += _gravity * Time.deltaTime;
        _characterController.Move(_velocity * Time.deltaTime);
    }
}
