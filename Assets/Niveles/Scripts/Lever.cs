using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Palanca: se acciona al tocarla con el jugador (no necesita botón nuevo).
/// Dispara 'onActivated' una sola vez (por ejemplo, ExitDoor.Activate).
/// </summary>
public class Lever : MonoBehaviour
{
    [Tooltip("Hijo que se gira al accionar la palanca (opcional, solo visual).")]
    [SerializeField] private Transform handle;
    [SerializeField] private float onAngle = -40f;
    [SerializeField] private Color onColor = new Color(0.4f, 1f, 0.4f);

    public UnityEvent onActivated = new UnityEvent();

    public bool IsActivated { get; private set; }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsActivated) return;
        if (other.GetComponentInParent<PlayerController2D>() == null) return;

        IsActivated = true;
        if (handle != null)
        {
            handle.localRotation = Quaternion.Euler(0f, 0f, onAngle);
            var sr = handle.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.color = onColor;
        }
        onActivated.Invoke();
    }
}
