using UnityEngine;

/// <summary>
/// Mecanismo de dos estados (puente, plataforma, puerta): se mueve de A (posición inicial)
/// a B (A + offset) al activarse. Rigidbody2D cinemático para que el jugador pueda ir encima.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Mechanism : MonoBehaviour
{
    [SerializeField] private Vector2 offset = new Vector2(-10f, 0f);
    [SerializeField] private float duration = 1.5f;

    private Rigidbody2D rb;
    private Vector2 a;
    private bool active;
    private float t; // 0 = A, 1 = B

    public bool IsActive => active;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        a = rb.position;
    }

    public void Activate() => active = true;
    public void Deactivate() => active = false;
    public void Toggle() => active = !active;

    private void FixedUpdate()
    {
        float goal = active ? 1f : 0f;
        if (Mathf.Approximately(t, goal)) return;
        t = Mathf.MoveTowards(t, goal, Time.fixedDeltaTime / Mathf.Max(0.01f, duration));
        rb.MovePosition(a + offset * Mathf.SmoothStep(0f, 1f, t));
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 start = Application.isPlaying ? (Vector3)a : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(start, start + (Vector3)offset);
        Gizmos.DrawWireCube(start + (Vector3)offset, Vector3.one * 0.3f);
    }
}
