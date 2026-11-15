using UnityEngine;
using UnityEngine.InputSystem;

// El movimiento no lo hace la física de Unity: este script decide la velocidad (caminar y saltar) y
// PhysicsBody2D la integra por código junto con la gravedad y resuelve los choques.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 12f;

    [Header("Empuje de cajas")]
    [SerializeField] private float pushForce = 250f; // N con la caja quieta; baja a 0 cuando la caja va a moveSpeed

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckRadius = 0.2f;

    private Rigidbody2D rb;
    private PhysicsBody2D body;
    private float horizontalInput;
    private bool isGrounded;
    private bool jumpRequested; // Variable para registrar el salto

    private Animator animator;

    // Lo usa PlayerFootsteps para saber si el personaje pisa el suelo
    public bool IsGrounded => isGrounded;
    public float HorizontalInput => horizontalInput;

    // Cuerpo con la física por código (lo usan la gravedad, los proyectiles y las recargas)
    public PhysicsBody2D Body => body;
    public bool IsPushing { get; private set; }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Física por código (se añade sola, así no hay que tocar las escenas ni el prefab)
        body = GetComponent<PhysicsBody2D>();
        if (body == null) body = gameObject.AddComponent<PhysicsBody2D>();

        // Sonido de pasos (se añade solo, así no hay que tocar las escenas)
        if (GetComponent<PlayerFootsteps>() == null) gameObject.AddComponent<PlayerFootsteps>();
    }

    private void Update()
    {
        checkFlip();
        animator.SetFloat("Horizontal", Mathf.Abs(horizontalInput));
        animator.SetBool("IsGrounded", isGrounded);

        // Lectura de movimiento horizontal
        horizontalInput = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontalInput = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontalInput = 1f;

            // Registramos la pulsación en Update para no perder el 'wasPressedThisFrame'
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                jumpRequested = true;
            }

            // Con el juego en pausa (Time.timeScale = 0) no se procesa input del personaje
            if (Time.timeScale == 0f) return;

            Gamepad gamepad = Gamepad.current;

            // Lectura adaptada al nuevo Input System (Teclas A/D o Flechas)
            float keyboardInput = 0f;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) keyboardInput = -1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) keyboardInput = 1f;
            }

            // Mando: stick izquierdo (analógico, ya con zona muerta) y D-pad
            float gamepadInput = 0f;
            if (gamepad != null)
            {
                gamepadInput = gamepad.leftStick.ReadValue().x + gamepad.dpad.ReadValue().x;
            }

            horizontalInput = Mathf.Clamp(keyboardInput + gamepadInput, -1f, 1f);

            // Salto con la barra espaciadora o con el botón A del mando (buttonSouth)
            bool jumpPressed = (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                            || (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);

            if (jumpPressed && isGrounded)
            {
                Jump();
            }
        }
    }
     

    private void FixedUpdate()
    {
        // 1. Validar si está en el suelo
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }

        // 2. Aplicar movimiento horizontal
        body.Velocity = new Vector2(horizontalInput * moveSpeed, body.Velocity.y);

        // 3. Ejecutar el salto de manera sincronizada con la física
        if (jumpRequested && isGrounded)
        {
            Jump();
        }

        // Resetear la petición de salto
        jumpRequested = false;

        // 4. Avanzar la física por código: gravedad, integración y choques
        body.Step(Time.fixedDeltaTime);

        // 5. Empujar la caja que tenga delante
        PushTouchingBox();
    }

    // Si camina contra una caja empujable estando apoyado, le aplica una fuerza.
    // La fuerza del personaje es máxima con la caja quieta y nula a su rapidez de caminar (como un motor):
    //   F = F_max·(1 - v_caja / v_caminar)
    // así la caja acaba a la rapidez en que esa fuerza iguala al rozamiento cinético.
    private void PushTouchingBox()
    {
        IsPushing = false;

        if (!body.IsGrounded || body.WallBody == null || horizontalInput == 0f) return;
        if ((horizontalInput > 0f ? 1 : -1) != body.WallSide) return;

        PushableBox box = body.WallBody.GetComponent<PushableBox>();
        if (box == null) return;

        float boxSpeed = Mathf.Max(0f, box.Body.Velocity.x * body.WallSide);
        float force = pushForce * Mathf.Clamp01(1f - boxSpeed / moveSpeed) * Mathf.Abs(horizontalInput);
        box.Push(force * body.WallSide);
        IsPushing = true;
    }

    private void Jump()
    {
        animator.SetTrigger("Jump");
        GameSfx.PlayJump();
        // Con gravedad invertida (hacia arriba) se salta hacia abajo
        body.Velocity = new Vector2(body.Velocity.x, jumpForce * body.Up.y);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }

    void checkFlip()
    {
        if (horizontalInput == 0)
            return;

        bool movingRight = horizontalInput > 0;
        bool facingRight = transform.localScale.x > 0;

        if (movingRight != facingRight)
        {
            Vector2 scale = transform.localScale;
            scale.x = movingRight ? 1f : -1f;
            transform.localScale = scale;
        }
    }
}