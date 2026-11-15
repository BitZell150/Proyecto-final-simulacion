using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Diana: al recibir la flecha dispara 'onHit' una sola vez (abrir puente, puerta, etc.).
/// El radio del CircleCollider2D define la tolerancia (y con ello el rango de |g| que acierta).
/// </summary>
public class Target : MonoBehaviour
{
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private Color hitColor = new Color(0.4f, 1f, 0.4f);

    public UnityEvent onHit = new UnityEvent();

    public bool IsHit { get; private set; }

    public void Hit()
    {
        if (IsHit) return;
        IsHit = true;
        if (visual != null) visual.color = hitColor;
        onHit.Invoke();
    }
}
