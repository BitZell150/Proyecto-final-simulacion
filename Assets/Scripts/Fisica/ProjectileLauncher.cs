using UnityEngine;

// Obstáculo que dispara proyectiles cada cierto tiempo con rapidez v0 y ángulo θ.
// Si "Apuntar al jugador" está activo, calcula θ con la ecuación del tiro parabólico (Ballistics.TryGetLaunchAngle);
// si el jugador está fuera de alcance dispara con el ángulo fijo.
// En el editor dibuja la trayectoria prevista con el mismo integrador que usa el proyectil.
public class ProjectileLauncher : MonoBehaviour
{
    [Header("Proyectil")]
    [SerializeField] private Projectile projectilePrefab;
    [Tooltip("Punto de salida; si está vacío sale del centro del lanzador")]
    [SerializeField] private Transform muzzle;
    [Tooltip("Se gira hacia el ángulo de tiro (opcional)")]
    [SerializeField] private Transform barrel;

    [Header("Disparo")]
    [SerializeField, Min(0f)] private float launchSpeed = 8f;                  // v0 en m/s
    [SerializeField, Range(-180f, 180f)] private float launchAngle = 135f;   // θ en grados desde +x
    [SerializeField, Min(0.1f)] private float fireInterval = 2.5f;           // s
    [SerializeField, Min(0f)] private float firstShotDelay = 1f;             // s
    [SerializeField] private bool aimAtPlayer = false;
    [Tooltip("Al apuntar: tiro alto (bombeado) en vez del tiro bajo")]
    [SerializeField] private bool useHighArc = false;

    [Header("Proyectil y aire (modelo del taller)")]
    [SerializeField, Min(0.01f)] private float projectileMass = 2f;  // kg
    [SerializeField, Min(0f)] private float airDrag = 0f;            // c en kg/s; 0 = parábola ideal
    [SerializeField] private Vector2 wind = Vector2.zero;            // m/s

    [Header("Vista previa en el editor")]
    [SerializeField, Min(0.1f)] private float previewTime = 3f;      // s de trayectoria dibujada

    private PlayerController2D player;
    private float timer;
    private float currentAngle;

    private Vector2 MuzzlePosition => muzzle != null ? (Vector2)muzzle.position : (Vector2)transform.position;

    private void Awake()
    {
        player = FindAnyObjectByType<PlayerController2D>();
        ResetLauncher();
    }

    private void OnEnable()
    {
        PlayerController2D.Respawned += ResetLauncher;
    }

    private void OnDisable()
    {
        PlayerController2D.Respawned -= ResetLauncher;
    }

    private void FixedUpdate()
    {
        currentAngle = ComputeAngle();

        timer -= Time.fixedDeltaTime;
        if (timer <= 0f)
        {
            Fire();
            timer += fireInterval;
        }
    }

    private void Update()
    {
        if (barrel != null) barrel.rotation = Quaternion.Euler(0f, 0f, currentAngle);
    }

    private void Fire()
    {
        if (projectilePrefab == null) return;
        if (player != null && player.IsDead) return;

        Projectile projectile = Instantiate(projectilePrefab, MuzzlePosition, Quaternion.identity);
        projectile.Launch(Ballistics.LaunchVelocity(launchSpeed, currentAngle), projectileMass, airDrag, wind);
    }

    // Ángulo de tiro: hacia el jugador si se puede alcanzar, si no el fijado en el Inspector
    private float ComputeAngle()
    {
        if (!aimAtPlayer || player == null || player.IsDead) return launchAngle;

        Vector2 delta = (player.Body.WorldCenter - MuzzlePosition) / PhysicsWorld.UnitsPerMeter;
        return Ballistics.TryGetLaunchAngle(delta, launchSpeed, useHighArc, out float angle) ? angle : launchAngle;
    }

    private void ResetLauncher()
    {
        timer = firstShotDelay;
        currentAngle = launchAngle;
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            currentAngle = launchAngle;
            if (barrel != null) barrel.rotation = Quaternion.Euler(0f, 0f, launchAngle);
        }
    }

    private void OnDrawGizmos()
    {
        float angle = Application.isPlaying ? currentAngle : launchAngle;
        Vector2 initialVelocity = Ballistics.LaunchVelocity(launchSpeed, angle);
        Vector2 start = MuzzlePosition;

        // Trayectoria simulada con el mismo paso de física que el proyectil (con aire y viento)
        Gizmos.color = new Color(1f, 0.55f, 0.1f);
        Vector2 velocity = initialVelocity;
        Vector2 displacement = Vector2.zero;
        Vector2 previous = start;
        float dt = Time.fixedDeltaTime;
        for (float t = 0f; t < previewTime; t += dt)
        {
            Ballistics.Step(ref velocity, ref displacement, projectileMass, airDrag, wind, dt);
            Vector2 point = start + displacement * PhysicsWorld.UnitsPerMeter;
            Gizmos.DrawLine(previous, point);
            previous = point;
        }

        // Punto más alto de la parábola ideal (sin aire): t = v0y / g
        if (initialVelocity.y > 0f)
        {
            float apexTime = initialVelocity.y / PhysicsWorld.Gravity;
            Vector2 apex = start + Ballistics.IdealPosition(initialVelocity, apexTime) * PhysicsWorld.UnitsPerMeter;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(apex, 0.12f);
        }
    }
}
