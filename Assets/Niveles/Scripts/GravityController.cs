using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gravedad ajustable por el jugador (Nivel 1 y 2).
/// Cambia Physics2D.gravity, así que afecta a todo: jugador, caja y flecha.
///   - Dirección: normal (abajo) / invertida (arriba).
///   - Magnitud: |g| = k·g0 con k en {0.5, 0.75, 1, 1.25, 1.5} (nunca 0).
/// Reglas: solo se cambia con el jugador en el suelo; al cambiar, la velocidad horizontal se pone a 0.
///
/// Controles: G / mando Y = invertir; Q / LB = menos |g|; E / RB = más |g|.
/// </summary>
public class GravityController : MonoBehaviour
{
    public const float DefaultG = 9.81f;

    public static GravityController Instance { get; private set; }

    [SerializeField] private float g0 = DefaultG;
    [SerializeField] private float[] multipliers = { 0.5f, 0.75f, 1f, 1.25f, 1.5f };
    [SerializeField] private int startIndex = 2;

    [Header("Jugador")]
    [SerializeField] private Rigidbody2D player;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer = 1 << 3;
    [SerializeField] private float groundCheckRadius = 0.2f;

    [Header("HUD de prueba")]
    [SerializeField] private bool showHud = true;

    private int index;
    private bool inverted;

    public bool Inverted => inverted;
    public float Multiplier => multipliers[index];
    public float Magnitude => g0 * Multiplier;

    private void Awake()
    {
        Instance = this;
        index = Mathf.Clamp(startIndex, 0, multipliers.Length - 1);
        inverted = false;
        Apply();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Physics2D.gravity = new Vector2(0f, -DefaultG); // no dejar la gravedad cambiada al salir
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;
        if (LevelManager.Instance != null && LevelManager.Instance.IsFinishing) return;

        var kb = Keyboard.current;
        var gp = Gamepad.current;
        bool invert = (kb != null && kb.gKey.wasPressedThisFrame) || (gp != null && gp.buttonNorth.wasPressedThisFrame);
        bool less = (kb != null && kb.qKey.wasPressedThisFrame) || (gp != null && gp.leftShoulder.wasPressedThisFrame);
        bool more = (kb != null && kb.eKey.wasPressedThisFrame) || (gp != null && gp.rightShoulder.wasPressedThisFrame);
        if (!invert && !less && !more) return;
        if (!IsPlayerGrounded()) return;

        if (invert) inverted = !inverted;
        if (less) index = Mathf.Max(0, index - 1);
        if (more) index = Mathf.Min(multipliers.Length - 1, index + 1);
        Apply();
    }

    public bool IsPlayerGrounded()
    {
        if (groundCheck == null) return true;
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void Apply()
    {
        Physics2D.gravity = new Vector2(0f, (inverted ? 1f : -1f) * Magnitude);

        if (player != null)
        {
            // Velocidad horizontal a 0 al cambiar la gravedad
            player.linearVelocity = new Vector2(0f, player.linearVelocity.y);

            // Voltear al jugador: el sprite y el GroundCheck (hijo) pasan al "techo"
            Vector3 s = player.transform.localScale;
            s.y = inverted ? -Mathf.Abs(s.y) : Mathf.Abs(s.y);
            if (s.y == 0f) s.y = inverted ? -1f : 1f;
            player.transform.localScale = s;
        }
    }

    // ---------- HUD ----------
    // Al entrar al nivel el panel aparece grande en el centro y, tras unos segundos, se desliza arriba a la izquierda.
    // Al morir (se recarga la escena) ya no se repite la presentación.

    [SerializeField] private float introHold = 4f;
    [SerializeField] private float introMove = 0.8f;

    private static string introShownFor;
    private bool playIntro;
    private GUIStyle titleStyle, textStyle, noteStyle;
    private Texture2D panelTex;

    private const float PanelW = 600f, PanelH = 96f;

    private void Start()
    {
        string scene = gameObject.scene.name;
        playIntro = introShownFor != scene;
        introShownFor = scene;
    }

    private void OnGUI()
    {
        if (!showHud) return;
        if (titleStyle == null) CreateStyles();

        float ui = Screen.height / 720f;
        float t = 1f; // 0 = centro, 1 = esquina
        if (playIntro)
            t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Time.timeSinceLevelLoad - introHold) / introMove));

        float bigScale = Mathf.Min(ui * 1.9f, Screen.width * 0.92f / PanelW);
        float s = Mathf.Lerp(bigScale, ui, t);
        var centre = new Vector2((Screen.width - PanelW * s) / 2f, Screen.height * 0.38f - PanelH * s / 2f);
        var corner = new Vector2(12f * ui, 10f * ui);
        var pos = Vector2.Lerp(centre, corner, t);

        var old = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(new Vector3(pos.x, pos.y, 0f), Quaternion.identity, new Vector3(s, s, 1f));

        GUI.DrawTexture(new Rect(0, 0, PanelW, PanelH), panelTex);
        string dir = inverted ? "▲ invertida" : "▼ normal";
        GUI.Label(new Rect(10, 4, PanelW - 20, 26),
            $"GRAVEDAD  {dir}   |g| = {Multiplier:0.00}·g0 = {Magnitude:0.00} m/s²", titleStyle);
        GUI.Label(new Rect(10, 30, PanelW - 20, 22),
            $"{Tecla("G / Y")} invertir     {Tecla("Q / LB")} menos |g|     {Tecla("E / RB")} más |g|", textStyle);
        GUI.Label(new Rect(10, 52, PanelW - 20, 22),
            $"{Tecla("F / X")} disparar     {Tecla("T")} trayectoria     {Tecla("R / Select")} reiniciar", textStyle);
        GUI.Label(new Rect(10, 74, PanelW - 20, 18), "Cambiar la gravedad y disparar: solo con los pies en el suelo", noteStyle);

        GUI.matrix = old;
    }

    private static string Tecla(string k) => $"<b><color=#FFD54A>{k}</color></b>";

    private void CreateStyles()
    {
        panelTex = new Texture2D(1, 1);
        panelTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.65f));
        panelTex.Apply();

        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, richText = true };
        titleStyle.normal.textColor = new Color(1f, 0.84f, 0.3f);
        textStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true };
        textStyle.normal.textColor = Color.white;
        noteStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Italic };
        noteStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
    }
}
