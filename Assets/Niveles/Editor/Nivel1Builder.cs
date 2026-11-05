using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Construye la escena Nivel1 (La fortaleza caída) según Boceto_Nivel1.png.
/// Menú: Niveles > Construir Nivel 1.
///
/// Medidas (1 bloque = 1 u; jugador 1.16 x 1.63; g efectiva del jugador = |g|·5):
///   salto con |g| = g0:      alto 1.47, largo 3.9
///   salto con |g| = 0.5·g0:  alto 2.94, largo 7.8   → el foso mide 10 (hueco efectivo ≈ 9.2): imposible
///   salto con |g| = 1.5·g0:  alto 0.98
///
/// Disparo (flecha): v = 6.35 u/s horizontal; y = y0 + (gy/2v²)·x².
///   Desde el techo del nicho (jugador invertido, centro y ≈ 5.99) hasta la diana (20.6, 11.12):
///   con |g| = 0.75·g0 sube ≈ 5.1 u en ~7.5 u → acierta. Con 0.5·g0 se queda corta;
///   con 1·g0 pasa por encima (solo roza desde el borde derecho del nicho); con g normal cae al foso.
/// </summary>
public static class Nivel1Builder
{
    private const string ScenePath = "Assets/Scenes/Nivel1.unity";
    public const float ArrowSpeed = 6.35f;

    [MenuItem("Niveles/Construir Nivel 1")]
    public static void Construir()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (var go in scene.GetRootGameObjects())
            if (go.name == "Nivel" || go.name == "GestorNivel" || go.name == "Gravedad") Object.DestroyImmediate(go);

        var nivel = new GameObject("Nivel").transform;
        var geo = NivelBuilder.Group("Geometria", nivel).transform;
        var pel = NivelBuilder.Group("Peligros", nivel).transform;
        var mec = NivelBuilder.Group("Mecanismos", nivel).transform;

        // ---- Marco ----
        NivelBuilder.Solid("ParedIzquierda", geo, -2f, -1f, 0.5f, 16f);
        NivelBuilder.Solid("ParedDerecha", geo, 26f, -1f, 28f, 16f);

        // ---- 1. Pasillo de práctica: techo bajo y seguro (2.6 u sobre el suelo) ----
        NivelBuilder.Solid("SueloPasillo", geo, 0.5f, -1f, 6.5f, 2f);
        // Termina 1.5 u antes del escalón: si llegara hasta él, la cabeza chocaría al saltar y no se podría subir.
        NivelBuilder.Solid("TechoPasillo_Seguro", geo, 0.5f, 4.6f, 5f, 16f);

        // ---- 2. Escalones hasta el mirador; encima, pozo con techo de pinchos ----
        NivelBuilder.Solid("Escalon", geo, 6.5f, -1f, 8f, 3.1f);
        NivelBuilder.Solid("Mirador", geo, 8f, -1f, 13f, 4.2f);
        NivelBuilder.Solid("TechoPozo", geo, 5f, 13.5f, 11.8f, 16f);
        NivelBuilder.Spikes("PinchosTechoPozo", pel, 5f, 11.8f, 13.5f, up: false);

        // ---- 3. Nicho: columna colgante con el techo bajo (2.6 u sobre el mirador) ----
        // Estrecha a propósito: fija desde dónde se dispara, y con ello el rango de |g| que acierta.
        NivelBuilder.Solid("ColumnaNicho_Seguro", geo, 11.8f, 6.8f, 13f, 16f);

        // ---- 5. Foso de pinchos (10 u) y puente retráctil que sale del muro derecho ----
        NivelBuilder.Solid("FondoFoso", geo, 13f, -1f, 23f, 0.5f);
        NivelBuilder.Spikes("PinchosFoso", pel, 13f, 23f, 0.5f);
        var puente = CrearPuente(mec, 13f, 23f, 4.2f);
        // Sobre el foso el techo está derrumbado (x 13 → 19.5): salir por arriba = reinicio (límites del LevelManager)

        // ---- 4. Viga rota con la diana colgada ----
        NivelBuilder.Solid("VigaRota", geo, 19.5f, 13f, 23f, 16f);
        NivelBuilder.Spikes("PinchosViga", pel, 19.5f, 23f, 13f, up: false); // invertido desde el puente = muerte
        var diana = CrearDiana(mec, new Vector2(20.6f, 11.12f), 0.35f, 13f);
        UnityEventTools.AddPersistentListener(diana.onHit, puente.Activate);

        // ---- 6. Zona de la puerta: techo con pinchos ----
        NivelBuilder.Solid("SueloPuerta", geo, 23f, -1f, 26f, 4.2f);
        NivelBuilder.Solid("TechoPuerta", geo, 23f, 12f, 26f, 16f);
        NivelBuilder.Spikes("PinchosTechoPuerta", pel, 23f, 26f, 12f, up: false);
        TutorialBuilder.CrearPuerta(mec, new Vector2(25f, 4.2f), 0); // abierta: el reto es el puente

        // ---- Jugador, gravedad, arco, gestor y cámara ----
        var jugador = NivelBuilder.Player(nivel, new Vector2(1.8f, 2f));
        AddGravityAndBow(jugador, ArrowSpeed);
        var gestor = TutorialBuilder.CrearGestor(jugador.transform, "Nivel2");
        SetBounds(gestor, new Rect(-2f, -3f, 30f, 19f)); // arriba termina en y = 16
        NivelBuilder.SetupCamera(new Vector2(13f, 7.5f), 7.5f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Niveles] Nivel1 construido y guardado.");
    }

    public static void ConstruirBatch() => Construir();

    // ---------- piezas reutilizables (también para el Nivel 2) ----------

    internal static void SetBounds(GameObject gestor, Rect bounds)
    {
        var so = new SerializedObject(gestor.GetComponent<LevelManager>());
        so.FindProperty("levelBounds").rectValue = bounds;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>Añade el control de gravedad (objeto "Gravedad") y el arco al jugador.</summary>
    internal static void AddGravityAndBow(GameObject jugador, float arrowSpeed)
    {
        var check = jugador.transform.Find("GroundCheck");

        var grav = new GameObject("Gravedad").AddComponent<GravityController>();
        var so = new SerializedObject(grav);
        so.FindProperty("player").objectReferenceValue = jugador.GetComponent<Rigidbody2D>();
        so.FindProperty("groundCheck").objectReferenceValue = check;
        so.FindProperty("groundLayer").intValue = 1 << NivelBuilder.GroundLayer;
        so.ApplyModifiedPropertiesWithoutUndo();

        var bow = jugador.AddComponent<ArrowLauncher>();
        so = new SerializedObject(bow);
        so.FindProperty("arrowPrefab").objectReferenceValue = ArrowPrefab();
        so.FindProperty("speed").floatValue = arrowSpeed;
        so.FindProperty("previewMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Arrow ArrowPrefab()
    {
        string path = NivelBuilder.Root + "/Prefabs/Flecha.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing.GetComponent<Arrow>();

        Directory.CreateDirectory(NivelBuilder.Root + "/Prefabs");
        var go = new GameObject("Flecha");
        var visual = NivelBuilder.Sprite("Asta", go.transform, Vector2.zero, new Vector2(0.8f, 0.08f),
            new Color(0.85f, 0.75f, 0.55f), 8);
        visual.transform.localPosition = new Vector3(-0.4f, 0f, 0f); // la punta está en el origen
        NivelBuilder.Sprite("Punta", go.transform, Vector2.zero, new Vector2(0.15f, 0.15f), Color.white, 9);
        go.AddComponent<Arrow>();
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab.GetComponent<Arrow>();
    }

    internal static Target CrearDiana(Transform parent, Vector2 center, float radius, float anchorY)
    {
        var root = new GameObject("Diana");
        root.transform.SetParent(parent, false);
        root.transform.position = center;

        NivelBuilder.Sprite("Cuerda", root.transform, new Vector2(center.x, (anchorY + center.y) / 2f),
            new Vector2(0.08f, anchorY - center.y), NivelBuilder.ColorCadena, 1);
        var aro = NivelBuilder.Sprite("Aro", root.transform, center, Vector2.one * (radius * 2f + 0.2f),
            new Color(0.9f, 0.9f, 0.85f), 5);
        aro.GetComponent<SpriteRenderer>().sprite = NivelBuilder.SpriteCirculo;
        var centro = NivelBuilder.Sprite("Centro", root.transform, center, Vector2.one * radius * 2f,
            new Color(0.8f, 0.15f, 0.15f), 6);
        centro.GetComponent<SpriteRenderer>().sprite = NivelBuilder.SpriteCirculo;

        var col = root.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = radius;

        var target = root.AddComponent<Target>();
        var so = new SerializedObject(target);
        so.FindProperty("visual").objectReferenceValue = centro.GetComponent<SpriteRenderer>();
        so.ApplyModifiedPropertiesWithoutUndo();
        return target;
    }

    /// <summary>Puente de xMin a xMax con la cara superior en topY; empieza escondido dentro del muro derecho.</summary>
    private static Mechanism CrearPuente(Transform parent, float xMin, float xMax, float topY)
    {
        float length = xMax - xMin;
        var go = NivelBuilder.Sprite("PuenteRetractil", parent,
            new Vector2((xMin + xMax) / 2f + length, topY - 0.2f), new Vector2(length, 0.4f),
            new Color(0.5f, 0.35f, 0.2f), -1); // detrás del muro mientras está recogido
        go.layer = NivelBuilder.GroundLayer;
        go.tag = "Ground";
        go.AddComponent<BoxCollider2D>();
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var mech = go.AddComponent<Mechanism>();
        var so = new SerializedObject(mech);
        so.FindProperty("offset").vector2Value = new Vector2(-length, 0f);
        so.FindProperty("duration").floatValue = 1.5f;
        so.ApplyModifiedPropertiesWithoutUndo();
        return mech;
    }
}
