using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PillMovement2D : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    public float moveSpeed = 5f;
    public float jumpForce = 7f;

    [Header("Referencias de Input")]
    public InputActionReference moveAction;
    public InputActionReference jumpAction;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        // Suscribirse al evento de salto
        jumpAction.action.performed += OnJump;
        
        // Habilitar las acciones
        moveAction.action.Enable();
        jumpAction.action.Enable();
    }

    private void OnDisable()
    {
        // Desuscribirse para evitar fugas de memoria
        jumpAction.action.performed -= OnJump;
        
        moveAction.action.Disable();
        jumpAction.action.Disable();
    }

    private void Update()
    {
        // Leer el valor del joystick izquierdo o D-pad
        moveInput = moveAction.action.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        // Aplicar velocidad en el eje X, manteniendo la velocidad actual en Y (gravedad)
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isGrounded = false;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Detección básica de suelo usando tags
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }
}