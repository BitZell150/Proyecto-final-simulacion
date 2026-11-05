using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Construye la escena Tutorial (Patio de entrenamiento) según Boceto_Tutorial.png.
/// Menú: Niveles > Construir Tutorial. Borra y vuelve a crear el objeto raíz "Nivel";
/// la cámara, la luz y el PauseMenu de la escena se conservan.
///
/// Medidas (1 bloque = 1 u). Salto real del PlayerController2D:
///   g efectiva = 9.81 * 5 = 49.05 u/s²;  v salto = 12 u/s;  v horizontal = 8 u/s
///   altura máx = v²/2g ≈ 1.47 u;  tiempo en el aire ≈ 0.49 s;  largo ≈ 3.9 u
/// Regla: subidas requeridas ≤ 1.1 u, subidas prohibidas ≥ 2.1 u.
/// </summary>
public static class TutorialBuilder
{
    private const string ScenePath = "Assets/Scenes/Tutorial.unity";

    [MenuItem("Niveles/Construir Tutorial")]
    public static void Construir()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        foreach (var go in scene.GetRootGameObjects())
            if (go.name == "Nivel" || go.name == "GestorNivel") Object.DestroyImmediate(go);

        var nivel = new GameObject("Nivel").transform;
        var geo = NivelBuilder.Group("Geometria", nivel).transform;
        var pel = NivelBuilder.Group("Peligros", nivel).transform;
        var mec = NivelBuilder.Group("Mecanismos", nivel).transform;

        // ---- Marco: suelo, paredes, techo (pantalla de 26.7 x 15 u) ----
        NivelBuilder.Solid("ParedIzquierda", geo, -2f, -1f, 1f, 16f);
        NivelBuilder.Solid("ParedDerecha", geo, 25f, -1f, 28f, 16f);
        NivelBuilder.Solid("Techo", geo, -2f, 14f, 28f, 16f);

        // ---- 1. Inicio + foso de pinchos pequeño (caminar y salto básico) ----
        NivelBuilder.Solid("SueloInicio", geo, 1f, -1f, 4f, 1f);
        NivelBuilder.Solid("FondoFoso1", geo, 4f, -1f, 6f, 0.2f);
        NivelBuilder.Spikes("PinchosFoso1", pel, 4f, 6f, 0.2f);
        NivelBuilder.Solid("SueloCaja", geo, 6f, -1f, 13f, 1f);

        // ---- 2. Caja: empujarla contra el muro y usarla de escalón ----
        // Muro de 2.1 u: imposible de subir sin la caja; desde la caja (1 u) faltan 1.1 u.
        NivelBuilder.Box(mec, new Vector2(8f, 1.5f));
        NivelBuilder.Solid("MesetaBaja", geo, 13f, -1f, 16.5f, 3.1f);

        // ---- 3. Bloque con pinchos colgante que se mueve en horizontal ----
        // Caminando por la meseta se pasa por debajo (cabeza a 4.73, zona letal desde 5.1);
        // al saltar al escalón (cabeza hasta ~6.2) hay que esperar a que el bloque se aleje.
        // Recorrido x 14.2 → 15.7: no toca el borde de la meseta (13) ni el escalón (16.5).
        CrearBloqueColgante(pel, new Vector2(14.2f, 5.9f), 1.5f, 2.4f);

        // ---- 4. Escalón con la palanca ----
        NivelBuilder.Solid("MesetaPalanca", geo, 16.5f, -1f, 19.5f, 4.2f);
        var palanca = CrearPalanca(mec, new Vector2(18.4f, 4.2f));

        // ---- 5. Saltos sobre el foso de pinchos hasta la puerta ----
        NivelBuilder.Solid("FondoFoso2", geo, 19.5f, -1f, 25f, 0.2f);
        NivelBuilder.Spikes("PinchosFoso2", pel, 19.5f, 25f, 0.2f);
        NivelBuilder.Solid("Plataforma1", geo, 20.5f, 3.2f, 21.4f, 3.6f);
        NivelBuilder.Solid("Plataforma2", geo, 22.1f, 4.3f, 22.9f, 4.7f);
        // Desde la meseta de la palanca (4.2) al rellano (5.8) hay 1.6 u: no se puede saltar directo.
        NivelBuilder.Solid("RellanoPuerta", geo, 23.6f, 5.4f, 25f, 5.8f);
        var puerta = CrearPuerta(mec, new Vector2(24.4f, 5.8f));

        UnityEventTools.AddPersistentListener(palanca.onActivated, puerta.Activate);

        // ---- Jugador, gestor y cámara ----
        var jugador = NivelBuilder.Player(nivel, new Vector2(2.5f, 1f));
        CrearGestor(jugador.transform, "Nivel1");
        NivelBuilder.SetupCamera(new Vector2(13f, 7.5f), 7.5f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Niveles] Tutorial construido y guardado.");
    }

    /// <summary>Para ejecutar desde línea de comandos (-batchmode -executeMethod).</summary>
    public static void ConstruirBatch()
    {
        Construir();
    }

    /// <summary>
    /// Bloque con pinchos colgado de una cadena que oscila en horizontal.
    /// Zona letal: de center.y − 0.8 a center.y + 0.4.
    /// </summary>
    internal static void CrearBloqueColgante(Transform parent, Vector2 center, float range, float period,
        float width = 1.6f, float top = 14f, float phase = 0f, string name = "BloqueColgante")
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = center;

        // cuerpo width x 0.8 + pinchos debajo
        NivelBuilder.Sprite("Cuerpo", root.transform, center, new Vector2(width, 0.8f), NivelBuilder.ColorPeligro, 3);
        var pinchos = NivelBuilder.Spikes("PinchosColgantes", root.transform,
            center.x - width / 2f, center.x + width / 2f, center.y - 0.4f, up: false);
        Object.DestroyImmediate(pinchos.GetComponent<Hazard>());
        Object.DestroyImmediate(pinchos.GetComponent<BoxCollider2D>());

        NivelBuilder.Sprite("Cadena", root.transform, new Vector2(center.x, (top + center.y + 0.4f) / 2f),
            new Vector2(0.15f, top - center.y - 0.4f), NivelBuilder.ColorCadena, 1);

        // zona letal: cuerpo + pinchos (de y-0.8 a y+0.4)
        var col = root.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(width - 0.1f, 1.2f);
        col.offset = new Vector2(0f, -0.2f);
        root.AddComponent<Hazard>();

        var rb = root.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var osc = root.AddComponent<Oscillator>();
        var so = new SerializedObject(osc);
        so.FindProperty("offset").vector2Value = new Vector2(range, 0f);
        so.FindProperty("period").floatValue = period;
        so.FindProperty("phase").floatValue = phase;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Lever CrearPalanca(Transform parent, Vector2 basePos)
    {
        var root = new GameObject("Palanca");
        root.transform.SetParent(parent, false);
        root.transform.position = basePos;

        NivelBuilder.Sprite("Base", root.transform, basePos + new Vector2(0f, 0.15f), new Vector2(0.8f, 0.3f),
            new Color(0.3f, 0.3f, 0.3f), 3);

        var mango = new GameObject("Mango");
        mango.transform.SetParent(root.transform, false);
        mango.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        mango.transform.localRotation = Quaternion.Euler(0f, 0f, 40f);
        var palo = NivelBuilder.Sprite("Palo", mango.transform, basePos, new Vector2(0.15f, 0.9f),
            new Color(0.95f, 0.8f, 0.2f), 4);
        palo.transform.localPosition = new Vector3(0f, 0.45f, 0f); // gira alrededor de la base
        palo.transform.localRotation = Quaternion.identity;

        var col = root.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1f, 1.5f);
        col.offset = new Vector2(0f, 0.75f);

        var lever = root.AddComponent<Lever>();
        var so = new SerializedObject(lever);
        so.FindProperty("handle").objectReferenceValue = mango.transform;
        so.ApplyModifiedPropertiesWithoutUndo();
        return lever;
    }

    internal static ExitDoor CrearPuerta(Transform parent, Vector2 basePos, int activacionesRequeridas = 1)
    {
        var root = new GameObject("PuertaSalida");
        root.transform.SetParent(parent, false);
        root.transform.position = basePos;

        var marco = NivelBuilder.Sprite("Marco", root.transform, basePos + new Vector2(0f, 1.1f),
            new Vector2(1.3f, 2.2f), new Color(0.35f, 0.25f, 0.15f), 1);
        var hoja = NivelBuilder.Sprite("Hoja", root.transform, basePos + new Vector2(0f, 1f),
            new Vector2(1f, 2f), new Color(0.55f, 0.35f, 0.15f), 2);

        var col = root.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.8f, 1.8f);
        col.offset = new Vector2(0f, 0.9f);

        var door = root.AddComponent<ExitDoor>();
        var so = new SerializedObject(door);
        so.FindProperty("requiredActivations").intValue = activacionesRequeridas;
        so.FindProperty("closedVisual").objectReferenceValue = hoja;
        so.FindProperty("frame").objectReferenceValue = marco.GetComponent<SpriteRenderer>();
        so.ApplyModifiedPropertiesWithoutUndo();
        return door;
    }

    internal static GameObject CrearGestor(Transform jugador, string siguiente)
    {
        var go = new GameObject("GestorNivel");
        var lm = go.AddComponent<LevelManager>();
        var so = new SerializedObject(lm);
        so.FindProperty("player").objectReferenceValue = jugador;
        so.FindProperty("nextScene").stringValue = siguiente;
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }
}
