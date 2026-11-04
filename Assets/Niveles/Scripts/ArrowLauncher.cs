using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Dispara la flecha: sale horizontal hacia donde mira el jugador, con velocidad fija.
/// Solo se dispara con el jugador en el suelo. F / mando X = disparar; T = mostrar trayectoria prevista.
/// </summary>
public class ArrowLauncher : MonoBehaviour
{
    [SerializeField] private Arrow arrowPrefab;
    [Tooltip("Velocidad horizontal de salida (u/s). Fija: no hay ángulo ni fuerza ajustables.")]
    [SerializeField] private float speed = 6.35f;
    [SerializeField] private Vector2 spawnOffset = new Vector2(0.7f, 0f);
    [SerializeField] private float cooldown = 0.5f;

    [Header("Trayectoria prevista (depuración)")]
    [SerializeField] private bool showPreview = false;
    [SerializeField] private Material previewMaterial;
    [SerializeField] private float previewTime = 2.5f;

    private float lastShot = -10f;
    private LineRenderer line;

    private float Facing => transform.localScale.x >= 0f ? 1f : -1f;
    private Vector2 SpawnPoint => (Vector2)transform.position + new Vector2(spawnOffset.x * Facing, spawnOffset.y);

    private void Update()
    {
        if (Time.timeScale == 0f) return;
        if (LevelManager.Instance != null && LevelManager.Instance.IsFinishing) return;

        var kb = Keyboard.current;
        var gp = Gamepad.current;

        if (kb != null && kb.tKey.wasPressedThisFrame) showPreview = !showPreview;

        bool fire = (kb != null && kb.fKey.wasPressedThisFrame) || (gp != null && gp.buttonWest.wasPressedThisFrame);
        if (fire && Time.time - lastShot >= cooldown && IsGrounded())
        {
            lastShot = Time.time;
            var arrow = Instantiate(arrowPrefab);
            arrow.Launch(SpawnPoint, new Vector2(speed * Facing, 0f), transform);
        }

        UpdatePreview();
    }

    private bool IsGrounded()
    {
        return GravityController.Instance == null || GravityController.Instance.IsPlayerGrounded();
    }

    /// <summary>Puntos de la trayectoria prevista con la gravedad actual (mismo integrador que la flecha).</summary>
    public int Predict(Vector3[] buffer, float h)
    {
        Vector2 p = SpawnPoint;
        Vector2 v = new Vector2(speed * Facing, 0f);
        var method = arrowPrefab != null ? arrowPrefab.Method : Arrow.Integrator.RK4;
        int n = Mathf.Min(buffer.Length, Mathf.CeilToInt(previewTime / h) + 1);
        for (int i = 0; i < n; i++)
        {
            buffer[i] = p;
            Arrow.Step(method, ref p, ref v, Physics2D.gravity, h);
        }
        return n;
    }

    private readonly Vector3[] points = new Vector3[200];

    private void UpdatePreview()
    {
        if (!showPreview)
        {
            if (line != null) line.enabled = false;
            return;
        }
        if (line == null)
        {
            line = gameObject.AddComponent<LineRenderer>();
            line.widthMultiplier = 0.05f;
            line.material = previewMaterial;
            line.startColor = line.endColor = new Color(1f, 0.55f, 0.2f, 0.8f);
            line.sortingOrder = 20;
            line.useWorldSpace = true;
        }
        line.enabled = true;
        int n = Predict(points, Time.fixedDeltaTime);
        line.positionCount = n;
        line.SetPositions(points);
    }

    private void OnDrawGizmos()
    {
        if (arrowPrefab == null) return;
        int n = Predict(points, 0.02f);
        Gizmos.color = new Color(1f, 0.55f, 0.2f);
        for (int i = 1; i < n; i++) Gizmos.DrawLine(points[i - 1], points[i]);
    }
}
