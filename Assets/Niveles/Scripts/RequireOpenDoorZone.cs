using UnityEngine;

/// <summary>
/// Zona sin retorno: si el jugador entra con la puerta todavía cerrada, el nivel se reinicia.
/// Sirve para "bajar al patio sin las dos dianas → reinicio".
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RequireOpenDoorZone : MonoBehaviour
{
    [SerializeField] private ExitDoor door;

    private void OnTriggerEnter2D(Collider2D other) => Check(other);
    private void OnTriggerStay2D(Collider2D other) => Check(other);

    private void Check(Collider2D other)
    {
        if (door == null || door.IsOpen || LevelManager.Instance == null) return;
        if (other.GetComponentInParent<PlayerController2D>() != null)
        {
            LevelManager.Instance.KillPlayer();
        }
    }
}
