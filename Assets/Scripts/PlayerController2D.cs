using System;
using System.Collections;
using UnityEngine;
// Añade esta línea si usas el nuevo Input System mediante clases directas de teclado
using UnityEngine.InputSystem;

// Personaje con física propia (PhysicsBody2D): caminar, saltar, empujar cajas y reaparecer al morir.
// La gravedad la aplica PhysicsBody2D; GravityInverter (si el personaje lo tiene) puede invertirla.
[RequireComponent(typeof(PhysicsBody2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float walkSpeed = 4f;           // m/s
    [SerializeField] private float groundAcceleration = 30f; // m/s²
    [SerializeField] private float airAcceleration = 15f;    // m/s²

    [Header("Salto")]
    [SerializeField] private float jumpHeight = 1.3f;     // m; rapidez inicial v0 = √(2·g·h)
    [SerializeField] private float coyoteTime = 0.1f;     // s en los que aún se puede saltar tras dejar el suelo
    [SerializeField] private float jumpBufferTime = 0.1f; // s que se recuerda el botón de salto antes de aterrizar

    [Header("Empuje")]
    [SerializeField] private float pushForce = 250f; // N con el personaje quieto; baja a 0 al ir a walkSpeed

    [Header("Reaparecer")]
    [SerializeField] private float respawnDelay = 0.6f; // s

    // Se dispara al reaparecer el jugador: cajas, proyectiles y recargas vuelven a su estado inicial
    public static event Action Respawned;

    private PhysicsBody2D body;
    private SpriteRenderer spriteRenderer;
    private GravityInverter gravityInverter;
    private float horizontalInput;
    private float lastJumpPressTime = float.NegativeInfinity;
    private float lastGroundedTime = float.NegativeInfinity;
    private Vector2 spawnPosition;

    public PhysicsBody2D Body => body;
    public bool IsDead { get; private set; }
    public bool IsPushing { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        // Con "Reload Domain" desactivado los eventos estáticos sobrevivirían entre sesiones de Play
        Respawned = null;
    }

    private void Awake()
    {
        // Las escenas hechas con la versión anterior (Rigidbody2D dinámico) no traen PhysicsBody2D
        body = GetComponent<PhysicsBody2D>();
        if (body == null) body = gameObject.AddComponent<PhysicsBody2D>();

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        gravityInverter = GetComponent<GravityInverter>();
        spawnPosition = transform.position;
    }

    private void OnEnable()
    {
        if (gravityInverter != null) gravityInverter.GravityChanged += OnGravityChanged;
    }

    private void OnDisable()
    {
        if (gravityInverter != null) gravityInverter.GravityChanged -= OnGravityChanged;
    }

    private void Update()
    {
        // Con el juego en pausa (Time.timeScale = 0) no se procesa input del personaje
        if (Time.timeScale == 0f) return;

        if (IsDead)
        {
            horizontalInput = 0f;
            return;
        }

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

        // Se recuerda el momento; el salto se hace en FixedUpdate, junto con el resto de la física
        if (jumpPressed) lastJumpPressTime = Time.time;

        if (spriteRenderer != null && horizontalInput != 0f) spriteRenderer.flipX = horizontalInput < 0f;
    }

    private void FixedUpdate()
    {
        if (IsDead) return;

        float dt = Time.fixedDeltaTime;
        Vector2 velocity = body.Velocity;

        if (body.IsGrounded) lastGroundedTime = Time.time;

        // Caminar: la velocidad horizontal se acerca a la deseada con aceleración constante
        float targetSpeed = horizontalInput * walkSpeed;
        float acceleration = body.IsGrounded ? groundAcceleration : airAcceleration;
        velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, acceleration * dt);

        // Saltar: rapidez inicial para subir jumpHeight metros, v0 = √(2·g·h), en contra de la gravedad.
        // Se acepta un poco antes de aterrizar (buffer) y un poco después de dejar el suelo (coyote time).
        bool jumpBuffered = Time.time - lastJumpPressTime <= jumpBufferTime;
        bool canJump = Time.time - lastGroundedTime <= coyoteTime;
        if (jumpBuffered && canJump)
        {
            float jumpSpeed = Mathf.Sqrt(2f * PhysicsWorld.Gravity * jumpHeight);
            velocity = new Vector2(velocity.x, 0f) + jumpSpeed * body.Up;
            lastJumpPressTime = float.NegativeInfinity;
            lastGroundedTime = float.NegativeInfinity;
        }

        body.Velocity = velocity;
        body.Step(dt);

        PushTouchingBox();
    }

    // Si camina contra una caja empujable estando en el suelo, le aplica una fuerza.
    // La fuerza del personaje es máxima quieto y nula a su rapidez de caminar (como un motor):
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
        float force = pushForce * Mathf.Clamp01(1f - boxSpeed / walkSpeed) * Mathf.Abs(horizontalInput);
        box.Push(force * body.WallSide);
        IsPushing = true;
    }

    // Lo llaman los peligros (proyectiles, vacío...): el personaje parpadea y reaparece en el inicio
    public void Kill()
    {
        if (IsDead) return;

        IsDead = true;
        IsPushing = false;
        body.Velocity = Vector2.zero;
        StartCoroutine(RespawnAfterDelay());
    }

    // Cambia el punto donde reaparece (por ejemplo, al pasar por un punto de control)
    public void SetSpawnPoint(Vector2 position)
    {
        spawnPosition = position;
    }

    private IEnumerator RespawnAfterDelay()
    {
        const float blinkInterval = 0.08f;
        float elapsed = 0f;
        while (elapsed < respawnDelay)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = Mathf.FloorToInt(elapsed / blinkInterval) % 2 == 0;
            yield return null;
            elapsed += Time.deltaTime;
        }

        if (spriteRenderer != null) spriteRenderer.enabled = true;
        Respawn();
    }

    private void Respawn()
    {
        body.Teleport(spawnPosition);
        if (gravityInverter != null) gravityInverter.ResetState();

        IsDead = false;
        lastJumpPressTime = float.NegativeInfinity;
        lastGroundedTime = float.NegativeInfinity;
        Respawned?.Invoke();
    }

    // Al invertir la gravedad no vale el coyote time del suelo anterior
    private void OnGravityChanged()
    {
        lastGroundedTime = float.NegativeInfinity;
    }

    private void OnDrawGizmosSelected()
    {
        if (body == null) return;

        // Velocidad actual (m/s), dibujada a escala
        Gizmos.color = Color.cyan;
        Vector3 center = body.WorldCenter;
        Gizmos.DrawLine(center, center + (Vector3)(body.Velocity * 0.25f * PhysicsWorld.UnitsPerMeter));
    }
}
