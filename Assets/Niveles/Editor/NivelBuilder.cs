using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Utilidades compartidas para construir niveles desde código (solo editor).
/// Todo se crea con sprites de prueba (cuadrados y triángulos) con nombres claros
/// para que el artista los cambie sin tocar la lógica.
/// Medida: 1 bloque = 1 unidad de Unity.
/// </summary>
public static class NivelBuilder
{
    public const string Root = "Assets/Niveles";
    public const int GroundLayer = 3; // capa "Ground" (la que usa el groundCheck del jugador)

    public static readonly Color ColorSuelo = new Color(0.36f, 0.36f, 0.42f);
    public static readonly Color ColorPinchos = new Color(0.85f, 0.85f, 0.9f);
    public static readonly Color ColorPeligro = new Color(0.55f, 0.15f, 0.15f);
    public static readonly Color ColorCaja = new Color(0.6f, 0.4f, 0.2f);
    public static readonly Color ColorCadena = new Color(0.25f, 0.25f, 0.25f);

    // ---------- Sprites de prueba ----------

    public static Sprite SpriteBloque => GetOrCreateSprite("Bloque", 8, 8, 8, (x, y) => true);

    // Triángulo de 1 x 0.5 unidades, punta hacia arriba
    public static Sprite SpritePincho => GetOrCreateSprite("Pincho", 16, 8, 16, (x, y) =>
    {
        float half = 8f * (1f - (y + 0.5f) / 8f);
        return Mathf.Abs(x + 0.5f - 8f) <= half;
    });

    // Círculo de 1 u de diámetro (dianas)
    public static Sprite SpriteCirculo => GetOrCreateSprite("Circulo", 32, 32, 32, (x, y) =>
    {
        float dx = x + 0.5f - 16f, dy = y + 0.5f - 16f;
        return dx * dx + dy * dy <= 16f * 16f;
    });

    private static Sprite GetOrCreateSprite(string name, int w, int h, float ppu, System.Func<int, int, bool> fill)
    {
        string dir = Root + "/Sprites";
        string path = $"{dir}/{name}.png";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        Directory.CreateDirectory(dir);
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, fill(x, y) ? Color.white : Color.clear);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = ppu;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ---------- Física ----------

    public static PhysicsMaterial2D Material(string name, float friction)
    {
        string dir = Root + "/Fisica";
        string path = $"{dir}/{name}.physicsMaterial2D";
        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
        if (mat != null) return mat;
        Directory.CreateDirectory(dir);
        mat = new PhysicsMaterial2D(name) { friction = friction, bounciness = 0f };
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // ---------- Piezas ----------

    public static GameObject Group(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    public static GameObject Sprite(string name, Transform parent, Vector2 center, Vector2 size, Color color, int order = 0)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteBloque;
        sr.color = color;
        sr.sortingOrder = order;
        return go;
    }

    /// <summary>Bloque sólido de suelo/pared/techo entre (xMin,yMin) y (xMax,yMax).</summary>
    public static GameObject Solid(string name, Transform parent, float xMin, float yMin, float xMax, float yMax)
    {
        var go = Sprite(name, parent, new Vector2((xMin + xMax) / 2f, (yMin + yMax) / 2f),
            new Vector2(xMax - xMin, yMax - yMin), ColorSuelo);
        go.AddComponent<BoxCollider2D>();
        go.layer = GroundLayer;
        go.tag = "Ground";
        go.isStatic = true;
        return go;
    }

    /// <summary>Fila de pinchos letales de xMin a xMax con la base en y (hacia arriba o colgando hacia abajo).</summary>
    public static GameObject Spikes(string name, Transform parent, float xMin, float xMax, float y, bool up = true)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3((xMin + xMax) / 2f, y, 0f);

        int count = Mathf.Max(1, Mathf.RoundToInt(xMax - xMin));
        float width = (xMax - xMin) / count;
        float dir = up ? 1f : -1f;
        for (int i = 0; i < count; i++)
        {
            var s = new GameObject("Pincho");
            s.transform.SetParent(root.transform, false);
            s.transform.position = new Vector3(xMin + width * (i + 0.5f), y + dir * 0.25f, 0f);
            s.transform.localScale = new Vector3(width, 1f, 1f);
            var sr = s.AddComponent<SpriteRenderer>();
            sr.sprite = SpritePincho;
            sr.color = ColorPinchos;
            sr.flipY = !up;
            sr.sortingOrder = 2;
        }

        // Zona letal algo más baja que las puntas, para que rozar el borde no mate
        var col = root.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(xMax - xMin - 0.1f, 0.3f);
        col.offset = new Vector2(0f, dir * 0.15f);
        root.AddComponent<Hazard>();
        return root;
    }

    /// <summary>Instancia el prefab del jugador (lo crea la primera vez).</summary>
    public static GameObject Player(Transform parent, Vector2 feetPosition)
    {
        var prefab = PlayerPrefab();
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(parent, false);
        go.transform.position = feetPosition + new Vector2(0f, 1.63f / 2f + 0.02f);
        return go;
    }

    private static GameObject PlayerPrefab()
    {
        string dir = Root + "/Prefabs";
        string path = $"{dir}/Jugador.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing; // no se sobreescribe: el artista puede editarlo

        Directory.CreateDirectory(dir);
        var go = new GameObject("Jugador");

        // Mismos valores que el personaje de la escena de prueba ("Proyecto simulación")
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/pesonaje/Firefly (1).png")
            .OfType<Sprite>().FirstOrDefault(s => s.name == "Firefly (1)_0");
        sr.sortingOrder = 10;

        var anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Animations/AnimatorControllerPlayer.controller");

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 5f;
        rb.freezeRotation = true; // que no se vuelque al chocar con esquinas
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.16f, 1.63f);
        col.sharedMaterial = Material("SinFriccion", 0f); // no se queda pegado a las paredes

        var check = new GameObject("GroundCheck");
        check.transform.SetParent(go.transform, false);
        check.transform.localPosition = new Vector3(0f, -1.63f / 2f, 0f);

        var pc = go.AddComponent<PlayerController2D>();
        var so = new SerializedObject(pc);
        so.FindProperty("moveSpeed").floatValue = 8f;
        so.FindProperty("jumpForce").floatValue = 12f;
        so.FindProperty("groundCheck").objectReferenceValue = check.transform;
        so.FindProperty("groundLayer").intValue = 1 << GroundLayer;
        so.FindProperty("groundCheckRadius").floatValue = 0.2f;
        so.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    /// <summary>Caja empujable de 1x1.</summary>
    public static GameObject Box(Transform parent, Vector2 center)
    {
        var go = Sprite("Caja", parent, center, Vector2.one, ColorCaja, 3);
        go.layer = GroundLayer;
        go.tag = "Ground";
        var rb = go.AddComponent<Rigidbody2D>();
        rb.mass = 1f;
        rb.gravityScale = 5f; // misma gravedad efectiva que el jugador
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.98f, 0.98f);
        col.sharedMaterial = Material("Caja", 0.4f);
        return go;
    }

    /// <summary>Configura la cámara fija para ver el nivel entero.</summary>
    public static void SetupCamera(Vector2 center, float orthoSize)
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.transform.position = new Vector3(center.x, center.y, -10f);
        cam.orthographic = true;
        cam.orthographicSize = orthoSize;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.09f, 0.09f, 0.12f);
    }
}
