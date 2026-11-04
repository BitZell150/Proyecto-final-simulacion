using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestor de un nivel: muerte del jugador (reinicia la escena completa),
/// salida del nivel y reinicio rápido de depuración.
/// Morir recarga la escena, así que puentes, dianas, palancas y cajas vuelven a su estado inicial.
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Referencias")]
    [SerializeField] private Transform player;

    [Header("Flujo")]
    [Tooltip("Escena que se carga al cruzar la puerta de salida. Vacío = MainMenu.")]
    [SerializeField] private string nextScene = "MainMenu";
    [SerializeField] private float deathDelay = 0.5f;
    [SerializeField] private float exitDelay = 0.5f;

    [Header("Límites (salir de aquí = morir)")]
    [SerializeField] private Rect levelBounds = new Rect(-2f, -3f, 30f, 20f);

    [Header("Depuración")]
    [Tooltip("R en teclado o Select/View en el mando reinicia el nivel.")]
    [SerializeField] private bool quickRestart = true;

    private bool finishing;

    public bool IsFinishing => finishing;

    private void Awake()
    {
        Instance = this;
        // Physics2D.gravity es global: cada nivel empieza con la gravedad normal
        Physics2D.gravity = new Vector2(0f, -9.81f); // g0 (igual a GravityController.DefaultG)
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return; // en pausa no se hace nada

        if (quickRestart && !finishing)
        {
            bool restart = (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                        || (Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame);
            if (restart) Restart();
        }

        if (player != null && !finishing && !levelBounds.Contains(player.position))
        {
            Debug.Log($"[Niveles] Muerte por salir de los límites en {player.position}");
            KillPlayer();
        }
    }

    /// <summary>Mata al jugador y reinicia el nivel tras una pausa corta.</summary>
    public void KillPlayer()
    {
        if (finishing) return;
        finishing = true;
        FreezePlayer(new Color(1f, 0.3f, 0.3f));
        StartCoroutine(LoadAfter(SceneManager.GetActiveScene().name, deathDelay));
    }

    /// <summary>El jugador cruzó la puerta abierta.</summary>
    public void CompleteLevel()
    {
        if (finishing) return;
        finishing = true;
        FreezePlayer(new Color(0.6f, 1f, 0.6f));
        string target = string.IsNullOrEmpty(nextScene) ? "MainMenu" : nextScene;
        StartCoroutine(LoadAfter(target, exitDelay));
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void FreezePlayer(Color tint)
    {
        if (player == null) return;
        var controller = player.GetComponent<PlayerController2D>();
        if (controller != null) controller.enabled = false;
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }
        var sr = player.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = tint;
    }

    private IEnumerator LoadAfter(string sceneName, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        SceneManager.LoadScene(sceneName);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
        Vector3 c = levelBounds.center;
        Gizmos.DrawWireCube(c, new Vector3(levelBounds.width, levelBounds.height, 0f));
    }
}
