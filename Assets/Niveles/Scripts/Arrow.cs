using UnityEngine;

/// <summary>
/// Flecha con trayectoria calculada a mano (no usa Rigidbody2D): es lo que se evalúa en Simulación.
///
/// Modelo: sale horizontal con velocidad v (fija) y solo la afecta la gravedad g = Physics2D.gravity.
///   Ecuaciones:  dx/dt = vx,  dy/dt = vy,  dvx/dt = 0,  dvy/dt = gy
///   Solución analítica:  y = y0 + (gy / 2v²)·x²   (con g normal gy = −|g|: la curva baja; invertida: sube)
///
/// Integración numérica en FixedUpdate (paso h = Time.fixedDeltaTime), a elegir:
///   - Euler semi-implícito:  v(n+1) = v(n) + g·h ;  p(n+1) = p(n) + v(n+1)·h
///     (error de posición ≈ g·h·t/2, unos centímetros con h = 0.02 s)
///   - RK4: con aceleración constante coincide con la solución analítica.
/// La colisión se detecta con un Linecast entre la posición anterior y la nueva.
/// </summary>
public class Arrow : MonoBehaviour
{
    public enum Integrator { EulerSemiImplicito, RK4 }

    [SerializeField] private Integrator integrator = Integrator.RK4;
    [SerializeField] private float lifetime = 6f;
    [SerializeField] private float stuckLifetime = 2f;

    private Vector2 position;
    private Vector2 velocity;
    private Transform owner;
    private bool stuck;
    private float age;

    public Integrator Method => integrator;

    public void Launch(Vector2 start, Vector2 startVelocity, Transform shooter)
    {
        position = start;
        velocity = startVelocity;
        owner = shooter;
        transform.position = start;
        UpdateRotation();
    }

    private void FixedUpdate()
    {
        age += Time.fixedDeltaTime;
        if (stuck)
        {
            if (age > stuckLifetime) Destroy(gameObject);
            return;
        }
        if (age > lifetime) { Destroy(gameObject); return; }

        Vector2 previous = position;
        Step(integrator, ref position, ref velocity, Physics2D.gravity, Time.fixedDeltaTime);

        if (CheckHit(previous, position, out Vector2 hitPoint))
        {
            position = hitPoint;
            stuck = true;
            age = 0f;
        }

        transform.position = position;
        UpdateRotation();
    }

    /// <summary>Un paso de integración. Lo usa también la vista previa de la trayectoria.</summary>
    public static void Step(Integrator method, ref Vector2 p, ref Vector2 v, Vector2 g, float h)
    {
        if (method == Integrator.EulerSemiImplicito)
        {
            v += g * h;
            p += v * h;
            return;
        }

        // RK4 sobre el estado (p, v) con derivadas (v, g)
        Vector2 k1p = v;            Vector2 k1v = g;
        Vector2 k2p = v + 0.5f * h * k1v; Vector2 k2v = g;
        Vector2 k3p = v + 0.5f * h * k2v; Vector2 k3v = g;
        Vector2 k4p = v + h * k3v;        Vector2 k4v = g;
        p += h / 6f * (k1p + 2f * k2p + 2f * k3p + k4p);
        v += h / 6f * (k1v + 2f * k2v + 2f * k3v + k4v);
    }

    private bool CheckHit(Vector2 from, Vector2 to, out Vector2 point)
    {
        point = to;
        var hits = Physics2D.LinecastAll(from, to); // ordenados por distancia
        foreach (var hit in hits)
        {
            if (owner != null && hit.collider.transform.IsChildOf(owner)) continue;

            var target = hit.collider.GetComponentInParent<Target>();
            if (target != null)
            {
                target.Hit();
                point = hit.point;
                return true;
            }
            if (hit.collider.isTrigger) continue; // pinchos, palancas, zonas: la flecha pasa
            point = hit.point;
            return true;
        }
        return false;
    }

    private void UpdateRotation()
    {
        if (velocity.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
