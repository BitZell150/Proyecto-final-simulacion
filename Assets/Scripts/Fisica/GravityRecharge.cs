using UnityEngine;

// Recarga de usos de gravedad: al tocarla el jugador recupera usos y la recarga desaparece hasta que reaparezca.
// El contacto se comprueba a mano (caja contra caja), igual que KillZone.
public class GravityRecharge : MonoBehaviour
{
    [SerializeField, Min(1)] private int uses = 1;
    [SerializeField] private Vector2 size = new Vector2(0.8f, 0.8f); // unidades de Unity

    [Header("Flotar")]
    [SerializeField] private float bobHeight = 0.15f;   // unidades de Unity
    [SerializeField] private float bobFrequency = 0.8f; // Hz

    private PlayerController2D player;
    private GravityInverter inverter;
    private Renderer[] renderers;
    private Vector3 startPosition;
    private bool collected;

    private void Awake()
    {
        player = FindAnyObjectByType<PlayerController2D>();
        inverter = player != null ? player.GetComponent<GravityInverter>() : null;
        renderers = GetComponentsInChildren<Renderer>();
        startPosition = transform.position;
    }

    private void OnEnable()
    {
        PlayerController2D.Respawned += Restore;
    }

    private void OnDisable()
    {
        PlayerController2D.Respawned -= Restore;
    }

    private void Update()
    {
        // Movimiento armónico simple: y = y0 + A·sen(2π·f·t)
        float offset = bobHeight * Mathf.Sin(2f * Mathf.PI * bobFrequency * Time.time);
        transform.position = startPosition + new Vector3(0f, offset, 0f);
    }

    private void FixedUpdate()
    {
        if (collected || inverter == null || player.IsDead) return;
        if (inverter.UsesLeft >= inverter.MaxUses) return;

        if (PhysicsWorld.BoxesOverlap(startPosition, size, player.Body.WorldCenter, player.Body.WorldSize))
        {
            inverter.AddUses(uses);
            SetCollected(true);
        }
    }

    private void Restore()
    {
        SetCollected(false);
    }

    private void SetCollected(bool value)
    {
        collected = value;
        foreach (Renderer r in renderers) r.enabled = !value;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Application.isPlaying ? startPosition : transform.position, size);
    }
}
