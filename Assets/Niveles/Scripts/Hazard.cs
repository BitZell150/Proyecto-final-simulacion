using UnityEngine;

/// <summary>
/// Cualquier cosa letal (pinchos, bloques con pinchos, techos derrumbados).
/// Si el jugador toca su collider (trigger o sólido), muere y el nivel se reinicia.
/// </summary>
public class Hazard : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other) => TryKill(other);
    private void OnTriggerStay2D(Collider2D other) => TryKill(other);
    private void OnCollisionEnter2D(Collision2D collision) => TryKill(collision.collider);

    private void TryKill(Collider2D other)
    {
        if (LevelManager.Instance == null || LevelManager.Instance.IsFinishing) return;
        if (other.GetComponentInParent<PlayerController2D>() != null)
        {
            Debug.Log($"[Niveles] Muerte por '{name}' en {other.transform.position}");
            LevelManager.Instance.KillPlayer();
        }
    }
}
