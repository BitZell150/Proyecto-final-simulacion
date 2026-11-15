using UnityEngine;

/// <summary>
/// Si este objeto (p. ej. la caja) cae por debajo de 'minY', el nivel se reinicia.
/// Sirve para "caja al foso → reinicio".
/// </summary>
public class ResetIfFallen : MonoBehaviour
{
    [SerializeField] private float minY = 0.7f;

    private void FixedUpdate()
    {
        if (transform.position.y < minY && LevelManager.Instance != null)
        {
            Debug.Log($"[Niveles] '{name}' cayó al foso: reinicio");
            LevelManager.Instance.KillPlayer();
            enabled = false;
        }
    }
}
