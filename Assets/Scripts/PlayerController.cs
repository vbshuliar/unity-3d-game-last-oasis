using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float sprintSpeed = 6f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundedGravity = -0.5f;

    private CharacterController controller;
    private PlayerInput playerInput;

    private Vector2 moveInput;
    private bool isSprintPressed;
    private Vector3 velocity;
    private bool isGrounded;

    // Public properties for other scripts to access
    public Vector2 MoveInput => moveInput;
    public bool IsMoving => moveInput.magnitude > 0.1f;
    public bool IsSprinting => IsMoving && isSprintPressed;
    public float CurrentSpeed => IsSprinting ? sprintSpeed : walkSpeed;
    public bool IsGrounded => isGrounded;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        if (playerInput != null)
        {
            playerInput.actions["Move"].performed += OnMovePerformed;
            playerInput.actions["Move"].canceled += OnMoveCanceled;
            playerInput.actions["Sprint"].performed += OnSprintPerformed;
            playerInput.actions["Sprint"].canceled += OnSprintCanceled;
        }
    }

    private void OnDisable()
    {
        if (playerInput != null)
        {
            playerInput.actions["Move"].performed -= OnMovePerformed;
            playerInput.actions["Move"].canceled -= OnMoveCanceled;
            playerInput.actions["Sprint"].performed -= OnSprintPerformed;
            playerInput.actions["Sprint"].canceled -= OnSprintCanceled;
        }
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        moveInput = Vector2.zero;
    }

    private void OnSprintPerformed(InputAction.CallbackContext context)
    {
        isSprintPressed = true;
    }

    private void OnSprintCanceled(InputAction.CallbackContext context)
    {
        isSprintPressed = false;
    }

    private void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        // Check if grounded
        isGrounded = controller.isGrounded;

        // Apply grounded gravity when on ground to keep player stuck to ground
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = groundedGravity;
        }

        // Get movement direction from input
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        // Only move and rotate if there's input
        if (moveDirection.magnitude >= 0.1f)
        {
            // Calculate target rotation based on movement direction
            float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            float angle = Mathf.LerpAngle(transform.eulerAngles.y, targetAngle, rotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            // Determine current speed (walk or sprint)
            float currentSpeed = IsSprinting ? sprintSpeed : walkSpeed;

            // Move the character
            Vector3 move = moveDirection.normalized * currentSpeed;
            controller.Move(move * Time.deltaTime);
        }

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
