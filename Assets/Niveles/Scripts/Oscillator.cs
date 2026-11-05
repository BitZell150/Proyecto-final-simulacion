using UnityEngine;

/// <summary>
/// Mueve un objeto de ida y vuelta entre su posición inicial y (inicial + offset).
/// Movimiento suave (coseno). Con 'phase' se ponen dos bloques en contrafase (0 y 0.5).
/// Usa Rigidbody2D cinemático para que los triggers detecten bien al jugador.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Oscillator : MonoBehaviour
{
    [SerializeField] private Vector2 offset = new Vector2(3f, 0f);
    [SerializeField] private float period = 3f;
    [Range(0f, 1f)]
    [SerializeField] private float phase = 0f;

    private Rigidbody2D rb;
    private Vector2 start;
    private float time;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        start = rb.position;
    }

    private void FixedUpdate()
    {
        time += Time.fixedDeltaTime;
        rb.MovePosition(start + offset * Evaluate(time));
    }

    // 0 en el inicio, 1 en el extremo
    private float Evaluate(float t)
    {
        return 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * (t / period + phase));
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 a = Application.isPlaying ? (Vector3)start : transform.position;
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(a, a + (Vector3)offset);
        Gizmos.DrawWireSphere(a, 0.15f);
        Gizmos.DrawWireSphere(a + (Vector3)offset, 0.15f);
    }
}
