using UnityEngine;

// Constantes y fórmulas comunes de la física del juego.
// Toda la física se calcula en unidades del SI (m, s, kg, N) y se pasa a unidades de Unity solo al mover
// los objetos, igual que el factor de escala del taller de movimiento parabólico.
public static class PhysicsWorld
{
    // Aceleración de la gravedad (m/s²)
    public const float Gravity = 9.81f;

    // Unidades de Unity por metro: el caballero mide 2,4 unidades, es decir, 1,6 m
    public const float UnitsPerMeter = 1.5f;

    // Holgura (en unidades de Unity) entre cuerpos en contacto, para que no queden incrustados
    public const float SkinWidth = 0.02f;

    // ¿Se solapan dos cajas alineadas con los ejes (AABB)? Es el mismo criterio del taller de colisiones
    // (|x1 - x2| <= distancia de contacto), aplicado en los dos ejes.
    public static bool BoxesOverlap(Vector2 centerA, Vector2 sizeA, Vector2 centerB, Vector2 sizeB)
    {
        return Mathf.Abs(centerA.x - centerB.x) <= (sizeA.x + sizeB.x) * 0.5f
            && Mathf.Abs(centerA.y - centerB.y) <= (sizeA.y + sizeB.y) * 0.5f;
    }

    // ¿Toca un círculo a una caja alineada con los ejes? Se busca el punto de la caja más cercano al centro
    // del círculo y se compara su distancia con el radio.
    public static bool CircleOverlapsBox(Vector2 circleCenter, float radius, Vector2 boxCenter, Vector2 boxSize)
    {
        Vector2 half = boxSize * 0.5f;
        Vector2 closest = new Vector2(
            Mathf.Clamp(circleCenter.x, boxCenter.x - half.x, boxCenter.x + half.x),
            Mathf.Clamp(circleCenter.y, boxCenter.y - half.y, boxCenter.y + half.y));

        return (circleCenter - closest).sqrMagnitude <= radius * radius;
    }

    // Velocidades tras un choque elástico en una dimensión (fórmula del taller de colisiones):
    //   v1' = ((m1 - m2) / (m1 + m2))·v1 + (2·m2 / (m1 + m2))·v2
    //   v2' = (2·m1 / (m1 + m2))·v1 + ((m2 - m1) / (m1 + m2))·v2
    public static void ElasticCollision1D(float m1, float v1, float m2, float v2, out float v1After, out float v2After)
    {
        float total = m1 + m2;
        v1After = (m1 - m2) / total * v1 + 2f * m2 / total * v2;
        v2After = 2f * m1 / total * v1 + (m2 - m1) / total * v2;
    }
}
