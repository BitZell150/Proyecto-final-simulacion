using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Base de las pruebas de niveles: un "bot" que juega con un teclado virtual del Input System.
/// Las pruebas viven en su propio ensamblado y no pueden referenciar Assembly-CSharp,
/// así que leen el estado del juego por nombres de objetos, colores y reflexión.
/// Ejecutar: Window > General > Test Runner > PlayMode > Run All.
/// </summary>
public abstract class NivelBot
{
    protected const float FeetToCenter = 1.63f / 2f + 0.02f;

    protected Keyboard kb;
    protected GameObject player;
    protected Rigidbody2D rb;
    protected Transform groundCheck;
    private readonly HashSet<Key> held = new HashSet<Key>();

    private InputSettings originalSettings;

    [UnitySetUp]
    public IEnumerator SetUpBot()
    {
        // Ajustes temporales (no se guardan en el proyecto): sin interfaz el juego "no tiene foco"
        // y el Input System descartaría las teclas del teclado virtual.
        originalSettings = InputSystem.settings;
        var testSettings = ScriptableObject.CreateInstance<InputSettings>();
        testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings = testSettings;

        kb = InputSystem.AddDevice<Keyboard>();
        kb.MakeCurrent();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDownBot()
    {
        held.Clear();
        if (kb != null) InputSystem.RemoveDevice(kb);
        if (originalSettings != null) InputSystem.settings = originalSettings;
        Physics2D.gravity = new Vector2(0f, -9.81f);
        yield return null;
    }

    // ---------- escena y estado ----------

    protected IEnumerator Load(string scene)
    {
        SceneManager.LoadScene(scene);
        yield return null;
        yield return null;
        player = GameObject.Find("Jugador");
        Assert.IsNotNull(player, $"No hay 'Jugador' en {scene}");
        rb = player.GetComponent<Rigidbody2D>();
        groundCheck = player.transform.Find("GroundCheck");
        yield return new WaitForSeconds(0.4f);
    }

    protected static string ActiveScene => SceneManager.GetActiveScene().name;
    protected Vector2 Pos => player != null ? (Vector2)player.transform.position : Vector2.zero;
    protected bool Grounded => groundCheck != null && Physics2D.OverlapCircle(groundCheck.position, 0.2f, 1 << 3);
    protected bool Inverted => player != null && player.transform.localScale.y < 0f;

    /// <summary>LevelManager.KillPlayer tiñe al jugador de rojo antes de reiniciar.</summary>
    protected bool Dead
    {
        get
        {
            if (player == null) return true;
            var c = player.GetComponent<SpriteRenderer>().color;
            return c.r > 0.9f && c.g < 0.5f;
        }
    }

    protected static Transform Find(string path)
    {
        var parts = path.Split('/');
        var root = GameObject.Find(parts[0]);
        if (root == null) return null;
        return parts.Length == 1 ? root.transform : root.transform.Find(string.Join("/", parts.Skip(1)));
    }

    /// <summary>La diana se pinta de verde al recibir la flecha.</summary>
    protected static bool TargetHit(string path)
    {
        var centro = Find(path + "/Centro");
        Assert.IsNotNull(centro, "No se encontró " + path);
        return centro.GetComponent<SpriteRenderer>().color.g > 0.9f;
    }

    /// <summary>La puerta abierta oculta su "Hoja".</summary>
    protected static bool DoorOpen()
    {
        var hoja = Find("Nivel/Mecanismos/PuertaSalida/Hoja");
        return hoja != null && !hoja.gameObject.activeSelf;
    }

    // ---------- teclado virtual ----------

    protected void Hold(Key k) { held.Add(k); Push(); }
    protected void Release(Key k) { held.Remove(k); Push(); }
    protected void ReleaseAll() { held.Clear(); Push(); }
    private void Push() => InputSystem.QueueStateEvent(kb, new KeyboardState(held.ToArray()));

    protected IEnumerator Tap(Key k)
    {
        Hold(k);
        yield return null;
        yield return null;
        Release(k);
        yield return null;
    }

    protected IEnumerator TapTimes(Key k, int n)
    {
        for (int i = 0; i < n; i++)
        {
            yield return Tap(k);
            yield return new WaitForSeconds(0.05f);
        }
    }

    // ---------- movimiento ----------

    protected IEnumerator Teleport(float x, float feetY)
    {
        ReleaseAll();
        var p = new Vector2(x, feetY + FeetToCenter);
        rb.linearVelocity = Vector2.zero;
        rb.position = p;
        player.transform.position = p;
        yield return new WaitForFixedUpdate();
        yield return new WaitForSeconds(0.4f);
    }

    protected IEnumerator WalkTo(float x, float timeout = 4f)
    {
        float dir = Mathf.Sign(x - Pos.x);
        Key k = dir > 0 ? Key.D : Key.A;
        Hold(k);
        float t = 0f;
        while (t < timeout && !Dead && (x - Pos.x) * dir > 0.05f)
        {
            t += Time.deltaTime;
            yield return null;
        }
        Release(k);
        yield return new WaitForSeconds(0.15f);
    }

    /// <summary>Salta y se mueve en el aire hasta targetX; espera a aterrizar.</summary>
    protected IEnumerator JumpTo(float targetX, float timeout = 2f)
    {
        float dir = Mathf.Sign(targetX - Pos.x);
        Key k = dir > 0 ? Key.D : Key.A;
        // dirección y salto en el mismo fotograma, como haría un jugador
        held.Add(k);
        held.Add(Key.Space);
        Push();
        yield return null;
        yield return null;
        Release(Key.Space);
        float t = 0f;
        bool holding = true;
        while (t < timeout && !Dead)
        {
            if (holding && (targetX - Pos.x) * dir <= 0.05f) { Release(k); holding = false; }
            t += Time.deltaTime;
            if (t > 0.15f && Grounded) break;
            yield return null;
        }
        if (holding) Release(k);
        yield return new WaitForSeconds(0.15f);
    }

    protected IEnumerator WaitUntilOrTimeout(Func<bool> condition, float timeout)
    {
        float t = 0f;
        while (!condition() && t < timeout)
        {
            t += Time.deltaTime;
            yield return null;
        }
    }

    // ---------- bloques oscilantes (para medir el tiempo de los saltos) ----------

    /// <summary>Posición x del Oscillator dentro de 'ahead' segundos (lee sus campos privados).</summary>
    protected static float PredictOscillatorX(GameObject go, float ahead)
    {
        var osc = go.GetComponent("Oscillator");
        var type = osc.GetType();
        const BindingFlags f = BindingFlags.NonPublic | BindingFlags.Instance;
        float time = (float)type.GetField("time", f).GetValue(osc);
        float period = (float)type.GetField("period", f).GetValue(osc);
        float phase = (float)type.GetField("phase", f).GetValue(osc);
        Vector2 start = (Vector2)type.GetField("start", f).GetValue(osc);
        Vector2 offset = (Vector2)type.GetField("offset", f).GetValue(osc);
        float s = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * ((time + ahead) / period + phase));
        return start.x + offset.x * s;
    }

    /// <summary>
    /// ¿Es seguro saltar ahora desde c0 hasta c1 (suelo en feetY, g normal)?
    /// Simula el salto y comprueba que no toca la zona letal de los bloques (ancho 'blockHalf', y de yMin a yMax).
    /// </summary>
    protected bool SafeToJump(float c0, float c1, float feetY, GameObject[] blocks, float blockHalf, float yMin, float yMax)
    {
        // Saltar en el centro de la ventana, no en su borde: también debe ser seguro 0.15 s antes y después.
        return SafeToJumpAt(-0.15f, c0, c1, feetY, blocks, blockHalf, yMin, yMax)
            && SafeToJumpAt(0f, c0, c1, feetY, blocks, blockHalf, yMin, yMax)
            && SafeToJumpAt(0.15f, c0, c1, feetY, blocks, blockHalf, yMin, yMax);
    }

    private bool SafeToJumpAt(float launchDelay, float c0, float c1, float feetY, GameObject[] blocks, float blockHalf, float yMin, float yMax)
    {
        float g = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
        float dir = Mathf.Sign(c1 - c0);
        for (float tau = 0f; tau < 0.6f; tau += 0.01f)
        {
            // el avance horizontal empieza en el siguiente paso de física (~0.03 s después del salto)
            float run = Mathf.Max(0f, tau - 0.03f);
            float x = dir > 0 ? Mathf.Min(c1, c0 + 8f * run) : Mathf.Max(c1, c0 - 8f * run);
            float feet = feetY + 12f * tau - 0.5f * g * tau * tau;
            if (tau > 0.05f && feet < feetY) break;
            foreach (var b in blocks)
            {
                float bx = PredictOscillatorX(b, launchDelay + tau + 0.05f);
                bool overlapX = Mathf.Abs(x - bx) < 0.58f + blockHalf + 0.1f;
                bool overlapY = feet + 1.63f > yMin - 0.1f && feet < yMax;
                if (overlapX && overlapY) return false;
            }
        }
        return true;
    }
}
