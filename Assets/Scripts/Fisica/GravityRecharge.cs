using UnityEngine;

// Recarga de usos de gravedad: al tocarla, el jugador recupera inversiones (cuando GravityController las tiene
// limitadas) y la recarga desaparece. El contacto se comprueba a mano (caja contra caja).
public class GravityRecharge : MonoBehaviour
{
    [SerializeField, Min(1)] private int uses = 1;
    [SerializeField] private Vector2 size = new Vector2(0.8f, 0.8f);

    [Header("Flotar")]
    [SerializeField] private float bobHeight = 0.12f;
    [SerializeField] private float bobFrequency = 0.8f; // Hz

    private PlayerController2D player;
    private Vector3 startPosition;

    private void Awake()
    {
        player = FindAnyObjectByType<PlayerController2D>();
        startPosition = transform.position;
    }

    private void Update()
    {
        // Movimiento armónico simple: y = y0 + A·sen(2π·f·t)
        float offset = bobHeight * Mathf.Sin(2f * Mathf.PI * bobFrequency * Time.time);
        transform.position = startPosition + new Vector3(0f, offset, 0f);
    }

    private void FixedUpdate()
    {
        GravityController gravity = GravityController.Instance;
        if (gravity == null || player == null || player.Body == null) return;
        if (!gravity.LimitedInversions || gravity.InversionsLeft >= gravity.MaxInversions) return;

        if (PhysicsWorld.BoxesOverlap(startPosition, size, player.Body.WorldCenter, player.Body.WorldSize))
        {
            gravity.AddInversions(uses);
            GameSfx.PlayGravityUp();
            gameObject.SetActive(false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Application.isPlaying ? startPosition : transform.position, size);
    }
}
