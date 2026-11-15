using UnityEngine;

// Proyectil del cañón con movimiento parabólico calculado por código (ver Ballistics), sin Rigidbody2D.
// En cada paso de física:
//   1. Integra velocidad y desplazamiento con la gravedad del juego y el rozamiento con el aire.
//   2. Comprueba a mano si el círculo del proyectil toca la caja del jugador (y lo mata: el nivel se reinicia).
//   3. Lanza un CircleCast entre la posición anterior y la nueva para ver si choca con algo sólido.
//      Si choca con una caja empujable le transmite la velocidad con la fórmula del choque elástico.
// Entre pasos de física la imagen se interpola para que el movimiento se vea suave.
public class Projectile : MonoBehaviour
{
    private const int MaxHits = 4;

    [SerializeField, Min(0.01f)] private float radius = 0.2f;
    [SerializeField, Min(0.1f)] private float lifetime = 8f;  // s
    [SerializeField] private LayerMask solidLayers = Physics2D.DefaultRaycastLayers;

    private readonly RaycastHit2D[] hits = new RaycastHit2D[MaxHits];
    private ContactFilter2D solidFilter;

    private Vector2 origin;        // punto de lanzamiento
    private Vector2 velocity;
    private Vector2 displacement;  // desde el punto de lanzamiento
    private float mass;
    private float drag;
    private Vector2 wind;
    private float age;
    private bool launched;

    private Vector2 previousPosition;
    private Vector2 currentPosition;
    private PlayerController2D player;

    public Vector2 Velocity => velocity;

    // Lo llama el lanzador justo después de crearlo
    public void Launch(Vector2 initialVelocity, float projectileMass, float airDrag, Vector2 windVelocity)
    {
        origin = transform.position;
        velocity = initialVelocity;
        displacement = Vector2.zero;
        mass = Mathf.Max(0.01f, projectileMass);
        drag = Mathf.Max(0f, airDrag);
        wind = windVelocity;
        age = 0f;
        launched = true;

        previousPosition = origin;
        currentPosition = origin;
    }

    private void Awake()
    {
        solidFilter = new ContactFilter2D();
        solidFilter.SetLayerMask(solidLayers);
        solidFilter.useTriggers = false;

        player = FindAnyObjectByType<PlayerController2D>();
    }

    private void FixedUpdate()
    {
        if (!launched) return;

        float dt = Time.fixedDeltaTime;
        age += dt;
        if (age > lifetime)
        {
            Destroy(gameObject);
            return;
        }

        Ballistics.Step(ref velocity, ref displacement, PhysicsWorld.Gravity, mass, drag, wind, dt);
        Vector2 next = origin + displacement;

        previousPosition = currentPosition;

        // Choque con algo sólido entre la posición anterior y la nueva
        Vector2 path = next - currentPosition;
        float distance = path.magnitude;
        if (distance > 0f && TryGetSolidHit(currentPosition, path / distance, distance, out RaycastHit2D hit))
        {
            currentPosition = hit.centroid;
            if (HitsPlayer(currentPosition)) KillPlayer();
            else TransferToBox(hit);

            Destroy(gameObject);
            return;
        }

        currentPosition = next;
        if (HitsPlayer(currentPosition))
        {
            KillPlayer();
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (!launched) return;

        // Interpolación entre los dos últimos pasos de física, solo para dibujar
        float alpha = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);
        Vector2 drawn = Vector2.Lerp(previousPosition, currentPosition, alpha);
        transform.position = new Vector3(drawn.x, drawn.y, transform.position.z);
    }

    private bool TryGetSolidHit(Vector2 from, Vector2 direction, float distance, out RaycastHit2D closest)
    {
        closest = default;
        bool found = false;

        int count = Physics2D.CircleCast(from, radius, direction, solidFilter, hits, distance);
        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = hits[i];

            // Lo que ya estaba solapado al salir (por ejemplo, el propio cañón) no cuenta
            if (hit.distance <= 0f) continue;

            // El jugador se comprueba aparte, con su caja de física
            if (player != null && hit.collider.GetComponentInParent<PlayerController2D>() == player) continue;

            if (!found || hit.distance < closest.distance)
            {
                closest = hit;
                found = true;
            }
        }

        return found;
    }

    // Círculo del proyectil contra la caja del jugador (AABB), calculado a mano
    private bool HitsPlayer(Vector2 position)
    {
        if (player == null || player.Body == null) return false;
        if (LevelManager.Instance != null && LevelManager.Instance.IsFinishing) return false;

        PhysicsBody2D body = player.Body;
        return PhysicsWorld.CircleOverlapsBox(position, radius, body.WorldCenter, body.WorldSize);
    }

    private void KillPlayer()
    {
        if (LevelManager.Instance == null) return;

        Debug.Log($"[Niveles] Muerte por proyectil en {currentPosition}");
        LevelManager.Instance.KillPlayer();
    }

    // Choque elástico en una dimensión (eje x) entre el proyectil y una caja empujable
    private void TransferToBox(RaycastHit2D hit)
    {
        PushableBox box = hit.collider.GetComponentInParent<PushableBox>();
        if (box == null) return;

        PhysicsBody2D boxBody = box.Body;
        PhysicsWorld.ElasticCollision1D(mass, velocity.x, boxBody.Mass, boxBody.Velocity.x, out _, out float boxVelocityX);
        boxBody.Velocity = new Vector2(boxVelocityX, boxBody.Velocity.y);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
