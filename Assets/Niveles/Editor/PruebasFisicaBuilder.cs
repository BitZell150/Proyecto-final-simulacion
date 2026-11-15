using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Construye la escena PruebasFisica: una sala para probar juntas las mecánicas de física por código.
/// Menú: Niveles > Construir Pruebas de física. No forma parte de la ruta del juego (ningún menú lleva a ella).
///
/// Qué hay (1 bloque = 1 u; suelo en y = 1, techo seguro en y = 9):
///   · Tres cajas con distinta masa. Con F_max = 250 N y g efectiva = |g|·5 (rozamiento μs = 0.5, μk = 0.4):
///       1 kg  → se empuja a 8·(1 − 0.4·1·49.05/250) ≈ 7.4 u/s
///       4 kg  → se empuja a ≈ 5.5 u/s
///       15 kg → no arranca con g0 (μs·m·g = 368 N > 250 N) pero sí con |g| = 0.5·g0 (184 N)
///   · Un cañón que apunta al jugador con la ecuación del tiro parabólico (v0 = 12 u/s, alcance ≈ 14.7 u con g0).
///   · Gravedad con 3 inversiones y dos recargas.
///   · La puerta de salida (abierta) detrás del cañón.
/// </summary>
public static class PruebasFisicaBuilder
{
    private const string ScenePath = "Assets/Scenes/PruebasFisica.unity";

    // Escena base: ya trae la cámara, la luz 2D, el PauseMenu y el menú "Nivel completado"
    private const string BaseScenePath = "Assets/Scenes/Nivel1.unity";

    private const string SpritesDir = "Assets/Sprites/Fisica";

    [MenuItem("Niveles/Construir Pruebas de física")]
    public static void Construir()
    {
        // Se copia el archivo (y no el asset) para conservar el .meta de la escena
        File.Copy(BaseScenePath, ScenePath, true);
        AssetDatabase.ImportAsset(ScenePath);

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (var go in scene.GetRootGameObjects())
            if (go.name == "Nivel" || go.name == "GestorNivel" || go.name == "Gravedad") Object.DestroyImmediate(go);

        var nivel = new GameObject("Nivel").transform;
        var geo = NivelBuilder.Group("Geometria", nivel).transform;
        var pel = NivelBuilder.Group("Peligros", nivel).transform;
        var mec = NivelBuilder.Group("Mecanismos", nivel).transform;

        // ---- Marco: paredes, suelo y techo seguro (se puede caminar por él con la gravedad invertida) ----
        NivelBuilder.Solid("ParedIzquierda", geo, -2f, -1f, 0.5f, 16f);
        NivelBuilder.Solid("ParedDerecha", geo, 26f, -1f, 28f, 16f);
        NivelBuilder.Solid("Suelo", geo, 0.5f, -1f, 26f, 1f);
        NivelBuilder.Solid("Techo_Seguro", geo, 0.5f, 9f, 26f, 16f);

        // ---- Cajas de 1, 4 y 15 kg ----
        var liviana = CrearCaja(mec, "Caja", new Vector2(5f, 1.5f), 1f, 1f);
        var mediana = CrearCaja(mec, "CajaMediana", new Vector2(8.5f, 1.5f), 4f, 1f);
        var pesada = CrearCaja(mec, "CajaPesada", new Vector2(12.5f, 1.8f), 15f, 1.6f);

        // ---- Cañón que apunta al jugador ----
        var lanzador = (GameObject)PrefabUtility.InstantiatePrefab(LanzadorPrefab());
        lanzador.transform.SetParent(pel, false);
        lanzador.transform.position = new Vector3(22.5f, 1f + 0.5625f, 0f);
        var so = new SerializedObject(lanzador.GetComponent<ProjectileLauncher>());
        so.FindProperty("aimAtPlayer").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();

        // ---- Recargas de gravedad: una pegada al techo y otra en el suelo ----
        CrearRecarga(mec, "RecargaTecho", new Vector2(10.5f, 8.2f));
        CrearRecarga(mec, "RecargaSuelo", new Vector2(17f, 1.8f));

        // ---- Puerta de salida, abierta, detrás del cañón ----
        var puerta = TutorialBuilder.CrearPuerta(mec, new Vector2(24.8f, 1f), 0);

        // ---- Jugador, gravedad (3 inversiones), arco, gestor y cámara ----
        var jugador = NivelBuilder.Player(nivel, new Vector2(2f, 1f));
        Nivel1Builder.AddGravityAndBow(jugador, Nivel1Builder.ArrowSpeed);
        so = new SerializedObject(GameObject.Find("Gravedad").GetComponent<GravityController>());
        so.FindProperty("maxInversions").intValue = 3;
        so.ApplyModifiedPropertiesWithoutUndo();
        TutorialBuilder.CrearGestor(jugador.transform, "MainMenu");
        NivelBuilder.SetupCamera(new Vector2(13f, 7.5f), 7.5f);

        // ---- Arte de los niveles ----
        NivelArte.Importar();
        NivelArte.Solidos(geo);
        NivelArte.Caja(liviana.transform);
        NivelArte.Caja(mediana.transform);
        NivelArte.Caja(pesada.transform);
        NivelArte.Puerta(puerta.transform);
        NivelArte.ParedFondo(nivel);
        var deco = NivelBuilder.Group("Decoracion", nivel).transform;
        NivelArte.Antorcha(deco, "AntorchaInicio", "antorcha_1", new Vector2(2.2f, 3.6f));
        NivelArte.Antorcha(deco, "AntorchaCanon", "antorcha_2", new Vector2(20.2f, 3.6f));
        NivelArte.Luz(deco, "LuzPuerta", new Vector2(24.8f, 2.2f), 4.4f, new Color(1f, 0.75f, 0.35f, 0.28f));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Niveles] PruebasFisica construida y guardada.");
    }

    /// <summary>Para ejecutar desde línea de comandos (-batchmode -executeMethod).</summary>
    public static void ConstruirBatch() => Construir();

    // ---------- piezas ----------

    /// <summary>Caja empujable de 'lado' unidades y 'masa' kg (la masa vive en el Rigidbody2D).</summary>
    private static GameObject CrearCaja(Transform parent, string name, Vector2 center, float masa, float lado)
    {
        var caja = NivelBuilder.Box(parent, center);
        caja.name = name;
        caja.transform.localScale = new Vector3(lado, lado, 1f);
        caja.GetComponent<Rigidbody2D>().mass = masa;
        return caja;
    }

    private static void CrearRecarga(Transform parent, string name, Vector2 center)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(RecargaPrefab());
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = center;
    }

    // ---------- prefabs (se crean la primera vez; después no se sobreescriben) ----------

    private static GameObject ProyectilPrefab()
    {
        string path = NivelBuilder.Root + "/Prefabs/Proyectil.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var go = new GameObject("Proyectil");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PixelSprite("Bala", 20f, new Vector2(0.5f, 0.5f)); // 8 px = 0.4 u de diámetro
        sr.sortingOrder = 8;
        go.AddComponent<Projectile>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static GameObject LanzadorPrefab()
    {
        string path = NivelBuilder.Root + "/Prefabs/Lanzador.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        // Base de 1.5 x 1.125 u, sólida y pisable (capa Ground)
        var go = new GameObject("Lanzador");
        go.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
        go.layer = NivelBuilder.GroundLayer;
        go.tag = "Ground";
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PixelSprite("CanonBase", 16f, new Vector2(0.5f, 0.5f));
        sr.sortingOrder = 5;
        go.AddComponent<BoxCollider2D>().size = new Vector2(1f, 0.75f);

        var tubo = new GameObject("Tubo");
        tubo.transform.SetParent(go.transform, false);
        tubo.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        var tuboSr = tubo.AddComponent<SpriteRenderer>();
        tuboSr.sprite = PixelSprite("CanonTubo", 16f, new Vector2(0.15f, 0.5f)); // gira desde la recámara
        tuboSr.sortingOrder = 4;

        var boca = new GameObject("Boca");
        boca.transform.SetParent(tubo.transform, false);
        boca.transform.localPosition = new Vector3(0.95f, 0f, 0f);

        var launcher = go.AddComponent<ProjectileLauncher>();
        var so = new SerializedObject(launcher);
        so.FindProperty("projectilePrefab").objectReferenceValue = ProyectilPrefab().GetComponent<Projectile>();
        so.FindProperty("muzzle").objectReferenceValue = boca.transform;
        so.FindProperty("barrel").objectReferenceValue = tubo.transform;
        so.ApplyModifiedPropertiesWithoutUndo();
        tubo.transform.rotation = Quaternion.Euler(0f, 0f, 135f);

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static GameObject RecargaPrefab()
    {
        string path = NivelBuilder.Root + "/Prefabs/RecargaGravedad.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var go = new GameObject("RecargaGravedad");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PixelSprite("RecargaGravedad", 16f, new Vector2(0.5f, 0.5f)); // 12 px = 0.75 u
        sr.sortingOrder = 6;
        go.AddComponent<GravityRecharge>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    /// <summary>Sprite pixel art de Assets/Sprites/Fisica: sin filtrado ni compresión.</summary>
    private static Sprite PixelSprite(string name, float pixelsPerUnit, Vector2 pivot)
    {
        string path = $"{SpritesDir}/{name}.png";
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = pixelsPerUnit;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;

        var settings = new TextureImporterSettings();
        imp.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        imp.SetTextureSettings(settings);
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
