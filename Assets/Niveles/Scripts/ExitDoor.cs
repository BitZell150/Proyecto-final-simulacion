using UnityEngine;

/// <summary>
/// Puerta de salida con contador: se abre cuando recibe 'requiredActivations' llamadas a Activate()
/// (palanca, dianas...). Abierta, al tocarla el jugador termina el nivel.
/// </summary>
public class ExitDoor : MonoBehaviour
{
    [SerializeField] private int requiredActivations = 1;
    [Tooltip("Hoja de la puerta: se oculta al abrir (solo visual).")]
    [SerializeField] private GameObject closedVisual;
    [SerializeField] private SpriteRenderer frame;
    [SerializeField] private Color openColor = new Color(0.4f, 1f, 0.4f);

    private int activations;

    public bool IsOpen => activations >= requiredActivations;

    private void Start()
    {
        if (IsOpen) ShowOpen(); // requiredActivations = 0: abierta desde el inicio
    }

    public void Activate()
    {
        if (IsOpen) return;
        activations++;
        if (IsOpen) ShowOpen();
    }

    private void ShowOpen()
    {
        if (closedVisual != null) closedVisual.SetActive(false);
        if (frame != null) frame.color = openColor;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!IsOpen || LevelManager.Instance == null) return;
        if (other.GetComponentInParent<PlayerController2D>() != null)
        {
            LevelManager.Instance.CompleteLevel();
        }
    }
}
