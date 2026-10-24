using UnityEngine;

// Fórmulas del movimiento parabólico (en metros, segundos y kilogramos).
// El modelo es el del taller: gravedad más rozamiento lineal con el aire, relativo al viento:
//   a = -g·ŷ - (c / m)·(v - v_viento)
// Con c = 0 no hay rozamiento y la trayectoria es la parábola ideal.
public static class Ballistics
{
    // Aceleración del proyectil con velocidad v
    public static Vector2 Acceleration(Vector2 velocity, float mass, float drag, Vector2 wind)
    {
        return new Vector2(0f, -PhysicsWorld.Gravity) - drag / mass * (velocity - wind);
    }

    // Un paso de Euler semi-implícito, igual que en el taller: primero la velocidad y con ella la posición
    //   v(t + Δt) = v(t) + a·Δt
    //   x(t + Δt) = x(t) + v(t + Δt)·Δt
    public static void Step(ref Vector2 velocity, ref Vector2 displacement, float mass, float drag, Vector2 wind, float dt)
    {
        velocity += Acceleration(velocity, mass, drag, wind) * dt;
        displacement += velocity * dt;
    }

    // Posición exacta sin rozamiento, medida desde el punto de lanzamiento:
    //   x(t) = v0x·t,   y(t) = v0y·t - ½·g·t²
    public static Vector2 IdealPosition(Vector2 initialVelocity, float time)
    {
        return new Vector2(
            initialVelocity.x * time,
            initialVelocity.y * time - 0.5f * PhysicsWorld.Gravity * time * time);
    }

    // Velocidad inicial a partir de la rapidez v0 y el ángulo θ (grados, medido desde +x)
    public static Vector2 LaunchVelocity(float speed, float angleDegrees)
    {
        float angle = angleDegrees * Mathf.Deg2Rad;
        return new Vector2(speed * Mathf.Cos(angle), speed * Mathf.Sin(angle));
    }

    // Altura máxima sobre el punto de lanzamiento, sin rozamiento: h = v0y² / (2·g)
    public static float MaxHeight(Vector2 initialVelocity)
    {
        return initialVelocity.y > 0f ? initialVelocity.y * initialVelocity.y / (2f * PhysicsWorld.Gravity) : 0f;
    }

    // Alcance horizontal al volver a la altura de lanzamiento, sin rozamiento: R = v0²·sen(2θ) / g = 2·v0x·v0y / g
    public static float Range(Vector2 initialVelocity)
    {
        return initialVelocity.y > 0f ? 2f * initialVelocity.x * initialVelocity.y / PhysicsWorld.Gravity : 0f;
    }

    // Ángulo (grados, desde +x) para que un tiro con rapidez v pase por un punto a (dx, dy) metros, sin rozamiento.
    // Despejando θ de la ecuación de la trayectoria y = x·tanθ - g·x² / (2·v²·cos²θ):
    //   tanθ = (v² ± √(v⁴ - g·(g·x² + 2·y·v²))) / (g·x)
    // El signo - da el tiro bajo (más directo) y el + el tiro alto (bombeado).
    // Devuelve false si el punto está fuera de alcance (raíz de un número negativo).
    public static bool TryGetLaunchAngle(Vector2 delta, float speed, bool highArc, out float angleDegrees)
    {
        float g = PhysicsWorld.Gravity;
        float x = Mathf.Abs(delta.x);
        float y = delta.y;
        float v2 = speed * speed;
        float discriminant = v2 * v2 - g * (g * x * x + 2f * y * v2);

        angleDegrees = 0f;
        if (discriminant < 0f) return false;

        float root = Mathf.Sqrt(discriminant);
        float angle = x < 0.0001f
            ? (y >= 0f ? 90f : -90f)
            : Mathf.Atan2(highArc ? v2 + root : v2 - root, g * x) * Mathf.Rad2Deg;

        // Si el objetivo está a la izquierda se refleja el ángulo
        angleDegrees = delta.x < 0f ? 180f - angle : angle;
        return true;
    }
}
