using UnityEngine;

// Cuerpo con física propia: no usa la gravedad ni las fuerzas de Unity (el Rigidbody2D queda Kinematic).
// En cada paso aplica la segunda ley de Newton y la integra con Euler semi-implícito, como en los talleres:
//   a = k·g + ΣF / m - (c / m)·v        (g = gravedad del juego; k = escala de gravedad del cuerpo)
//   v(t + Δt) = v(t) + a·Δt
//   x(t + Δt) = x(t) + v(t + Δt)·Δt
// Unity solo se usa para detectar contra qué choca la caja del collider (BoxCast); la respuesta al choque
// se calcula aquí. Quien controla el cuerpo (jugador, caja...) llama a Step una vez por FixedUpdate.
// La masa y la escala de gravedad son las del Rigidbody2D, así se siguen editando donde siempre.
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class PhysicsBody2D : MonoBehaviour
{
    private const int MaxHits = 8;

    // Por debajo de esta rapidez un choque no rebota, para que el cuerpo pueda quedar en reposo
    private const float MinBounceSpeed = 0.5f;

    [Header("Aire")]
    [SerializeField, Min(0f)] private float airDrag = 0f;   // c en kg/s: fuerza del aire F = -c·v

    [Header("Choques")]
    [SerializeField] private LayerMask solidLayers = Physics2D.DefaultRaycastLayers;
    [SerializeField, Range(0f, 1f)] private float restitution = 0f; // e: 0 = no rebota, 1 = rebote perfecto

    private Rigidbody2D rb;
    private BoxCollider2D box;
    private ContactFilter2D solidFilter;
    private readonly RaycastHit2D[] hits = new RaycastHit2D[MaxHits];

    private float mass;
    private float gravityScale;
    private Vector2 netForce;
    private Vector2 targetPosition;

    public float Mass => mass;

    // Velocidad en u/s
    public Vector2 Velocity { get; set; }

    // Aceleración de la gravedad sobre este cuerpo (u/s²)
    public Vector2 GravityAcceleration => PhysicsWorld.Gravity * gravityScale;

    // Vector unitario hacia donde tira la gravedad, y su opuesto ("arriba" para este cuerpo)
    public Vector2 GravityDirection => new Vector2(0f, PhysicsWorld.Gravity.y > 0f ? 1f : -1f);
    public Vector2 Up => -GravityDirection;

    // Apoyado sobre una superficie en el sentido de la gravedad (suelo, o techo si está invertida)
    public bool IsGrounded { get; private set; }
    public PhysicsBody2D GroundBody { get; private set; }

    // Pared (u otro cuerpo) que frenó el último movimiento horizontal; WallSide es -1 (izquierda) o +1 (derecha)
    public Collider2D WallCollider { get; private set; }
    public PhysicsBody2D WallBody { get; private set; }
    public int WallSide { get; private set; }

    // Caja del collider en el mundo, en la posición física actual
    public Vector2 WorldCenter => rb.position + Vector2.Scale(box.offset, transform.lossyScale);
    public Vector2 WorldSize => Vector2.Scale(box.size, Abs(transform.lossyScale));

    // Lo que este cuerpo ya decidió moverse en el paso actual y Unity aplicará al terminar los FixedUpdate.
    // Sirve para que otro cuerpo que lo empuja en el mismo paso no se quede un paso atrás.
    public Vector2 PendingDisplacement => targetPosition - rb.position;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        box = GetComponent<BoxCollider2D>();

        // Se leen antes de volver cinemático el cuerpo
        mass = Mathf.Max(0.01f, rb.mass);
        gravityScale = rb.gravityScale;

        // Unity no debe aplicar gravedad ni fuerzas: el movimiento lo decide este script.
        // Los contactos completos hacen que los triggers (pinchos, palanca, puerta) sigan detectando al cuerpo.
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;
        rb.useFullKinematicContacts = true;

        solidFilter = new ContactFilter2D();
        solidFilter.SetLayerMask(solidLayers);
        solidFilter.useTriggers = false;

        targetPosition = rb.position;
    }

    // Suma una fuerza (N) que actuará solo en el próximo paso
    public void AddForce(Vector2 force)
    {
        netForce += force;
    }

    // Avanza la simulación un paso de dt segundos
    public void Step(float dt)
    {
        // Segunda ley de Newton: gravedad + fuerzas aplicadas + rozamiento lineal con el aire
        Vector2 acceleration = GravityAcceleration + netForce / mass - airDrag / mass * Velocity;
        netForce = Vector2.zero;

        Velocity += acceleration * dt;
        Vector2 displacement = Velocity * dt;

        IsGrounded = false;
        GroundBody = null;
        WallCollider = null;
        WallBody = null;
        WallSide = 0;

        // Primero en horizontal y luego en vertical: así el cuerpo se desliza por suelos y paredes
        Vector2 position = rb.position;
        position = MoveAxis(position, new Vector2(displacement.x, 0f), true);
        position = MoveAxis(position, new Vector2(0f, displacement.y), false);

        targetPosition = position;
        rb.MovePosition(position);
    }

    // Mueve el cuerpo en un eje hasta donde lo deje el primer obstáculo y devuelve la nueva posición
    private Vector2 MoveAxis(Vector2 position, Vector2 move, bool horizontal)
    {
        float distance = move.magnitude;
        if (distance <= 0f) return position;

        Vector2 direction = move / distance;

        // La caja se lanza encogida SkinWidth por cada lado: así no detecta las superficies por las que se
        // desliza (el suelo al caminar) y un contacto real queda a SkinWidth de distancia
        Vector2 castSize = WorldSize - 2f * PhysicsWorld.SkinWidth * Vector2.one;
        Vector2 castOrigin = position + (WorldCenter - rb.position);
        int count = Physics2D.BoxCast(castOrigin, castSize, 0f, direction, solidFilter, hits, distance + PhysicsWorld.SkinWidth);

        float allowed = distance;
        RaycastHit2D blockingHit = default;
        PhysicsBody2D blockingBody = null;
        bool blocked = false;

        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = hits[i];
            if (hit.collider == box || (hit.rigidbody != null && hit.rigidbody == rb)) continue;

            // Si ya empezaba solapado lo ignora, para poder salir en vez de quedar atascado
            if (hit.distance <= 0f) continue;

            // Solo frenan las superficies que miran contra el movimiento
            if (Vector2.Dot(hit.normal, direction) > -0.01f) continue;

            PhysicsBody2D other = hit.rigidbody != null ? hit.rigidbody.GetComponent<PhysicsBody2D>() : null;

            // Si el otro cuerpo ya se apartó en este mismo paso, se puede avanzar eso de más
            float otherAdvance = other != null ? Mathf.Max(0f, Vector2.Dot(other.PendingDisplacement, direction)) : 0f;
            float free = Mathf.Max(0f, hit.distance - PhysicsWorld.SkinWidth + otherAdvance);

            if (free < allowed)
            {
                allowed = free;
                blockingHit = hit;
                blockingBody = other;
                blocked = true;
            }
        }

        if (blocked) ResolveContact(blockingHit, blockingBody, direction, horizontal);

        return position + direction * allowed;
    }

    // Respuesta al choque en la dirección del movimiento. Con la velocidad relativa al obstáculo:
    //   v' = v_obstáculo - e·(v - v_obstáculo)
    // con e = 0 el cuerpo queda a la velocidad del obstáculo (0 si está quieto, o la de una caja que empuja).
    private void ResolveContact(RaycastHit2D hit, PhysicsBody2D other, Vector2 direction, bool horizontal)
    {
        float speed = Vector2.Dot(Velocity, direction);
        float otherSpeed = other != null ? Vector2.Dot(other.Velocity, direction) : 0f;

        if (speed > otherSpeed)
        {
            float relative = speed - otherSpeed;
            float bounce = relative >= MinBounceSpeed ? restitution : 0f;
            float newSpeed = otherSpeed - bounce * relative;
            Velocity += (newSpeed - speed) * direction;
        }

        if (horizontal)
        {
            WallCollider = hit.collider;
            WallBody = other;
            WallSide = direction.x > 0f ? 1 : -1;
        }
        else if (Vector2.Dot(direction, GravityDirection) > 0f)
        {
            IsGrounded = true;
            GroundBody = other;
        }
    }

    private static Vector2 Abs(Vector3 v)
    {
        return new Vector2(Mathf.Abs(v.x), Mathf.Abs(v.y));
    }
}
