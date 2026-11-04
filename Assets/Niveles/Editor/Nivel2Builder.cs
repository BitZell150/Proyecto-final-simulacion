using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Construye la escena Nivel2 (La cripta maldita, nivel examen) según Boceto_Nivel2.png.
/// Menú: Niveles > Construir Nivel 2.
///
/// Medidas clave (1 bloque = 1 u; suelo en y = 1):
///   Salto: |g|=g0 → alto 1.47 / largo 3.9;  |g|=0.5·g0 → alto 2.94 / largo 7.8;  |g|=0.75·g0 → alto 1.96.
///   1. Techo de pinchos a 3.6 u sobre los pilares: con |g| ≤ 0.75·g0 cualquier salto ahí es mortal.
///   2. Foso de 2 u: el jugador lo salta, la caja no. Se pasa la caja por el techo seguro de la cámara.
///   3. Mirador a 3.4 u del suelo (> 2.94: imposible sin caja). Desde la caja (2.4 u) solo con |g| = 0.5·g0.
///      Un tabique con pinchos (base a 2.4 u del suelo) impide cruzar por el techo con un salto invertido.
///   4. Diana del patio (g normal) y diana bajo la losa (g invertida): la puerta necesita las 2.
///   5. Patio y bóveda: entrar con la puerta cerrada reinicia el nivel.
/// </summary>
public static class Nivel2Builder
{
    private const string ScenePath = "Assets/Scenes/Nivel2.unity";

    [MenuItem("Niveles/Construir Nivel 2")]
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

        // ---- 1. Dos pilares sobre el vacío + dos bloques colgantes desfasados ----
        // Huecos de 1.63 u (más anchos que el jugador, 1.16 u): quien intente cruzar corriendo cae al vacío.
        // Pilares de 0.7 u con centros en 3.68 y 6.02; saltos de ~2.5 u (el salto con g0 llega a 3.9 u).
        NivelBuilder.Solid("SueloInicio", geo, 0.5f, -1f, 1.7f, 1f);
        float[] centros = { 3.68f, 6.02f };
        foreach (var (c, i) in new[] { (centros[0], 1), (centros[1], 2) })
            NivelBuilder.Solid($"Pilar{i}", geo, c - 0.35f, -1f, c + 0.35f, 1f);
        // Vacío: zona letal invisible bajo el borde de la pantalla (y = 0) para reiniciar enseguida
        var vacio = new GameObject("Vacio");
        vacio.transform.SetParent(pel, false);
        vacio.transform.position = new Vector3((1.7f + 8f) / 2f, -1.2f, 0f);
        var vcol = vacio.AddComponent<BoxCollider2D>();
        vcol.isTrigger = true;
        vcol.size = new Vector2(8f - 1.7f, 1f);
        vacio.AddComponent<Hazard>();

        NivelBuilder.Solid("TechoPilares", geo, 0.5f, 4.6f, 8f, 16f);
        NivelBuilder.Spikes("PinchosTechoPilares", pel, 0.5f, 8f, 4.6f, up: false);
        // Zona letal de los bloques: y 3.2 → 4.4 (de pie en un pilar la cabeza llega a 2.63; saltando, a 4.1).
        // Parámetros elegidos por simulación (con 0.05 s de retraso de reacción, 0.1 u de margen y que los
        // bloques nunca se crucen): cada salto tiene una ventana segura de ~0.9–1.1 s en un ciclo de 5.4 s.
        // B llega como mucho a x = 7.6: así no alcanza al jugador cuando invierte la gravedad en x = 8.6.
        TutorialBuilder.CrearBloqueColgante(pel, new Vector2(2.4f, 4.0f), 2.4f, 5.4f, 0.6f, 4.6f, 0f, "BloqueColganteA");
        TutorialBuilder.CrearBloqueColgante(pel, new Vector2(5.0f, 4.0f), 2.6f, 5.4f, 0.6f, 4.6f, 0.3f, "BloqueColganteB");

        // ---- 2. Cámara de la caja: foso de 2 u y techo seguro ----
        // Al invertir pegado a x = 8 se rozan los pinchos del techo de los pilares: la caja empieza más a la derecha
        // para que el jugador tenga sitio (x 8.6) entre esos pinchos y la caja.
        NivelBuilder.Solid("SueloCamara", geo, 8f, -1f, 10.3f, 1f);
        NivelBuilder.Solid("FondoFoso2", geo, 10.3f, -1f, 12.3f, 0f);
        NivelBuilder.Spikes("PinchosFoso2", pel, 10.3f, 12.3f, 0f);
        NivelBuilder.Solid("SueloMuro", geo, 12.3f, -1f, 16.5f, 1f);
        NivelBuilder.Solid("TechoCamara_Seguro", geo, 8f, 7f, 13.95f, 16f);
        var caja = NivelBuilder.Box(mec, new Vector2(9.75f, 1.5f));
        var reset = caja.AddComponent<ResetIfFallen>();
        var so = new SerializedObject(reset);
        so.FindProperty("minY").floatValue = 0.7f;
        so.ApplyModifiedPropertiesWithoutUndo();

        // ---- 3. Tabique con pinchos (no se cruza por el techo) y hueco junto al mirador ----
        // Delgado a propósito:
        //  - la caja empujada por el techo se detiene en x 12.95–13.95 y el jugador, detrás, cae sobre el suelo (≥ 12.2);
        //  - con la caja contra el mirador, el jugador (a su izquierda, centro ≈ 14.9) no queda bajo el tabique.
        NivelBuilder.Solid("Tabique", geo, 13.95f, 3.4f, 14.3f, 16f);
        NivelBuilder.Spikes("PinchosTabique", pel, 13.95f, 14.3f, 3.4f, up: false);
        NivelBuilder.Solid("TechoHueco", geo, 14.3f, 7.5f, 16.5f, 16f);
        NivelBuilder.Spikes("PinchosTechoHueco", pel, 14.3f, 16.5f, 7.5f, up: false);
        NivelBuilder.Solid("Mirador", geo, 16.5f, -1f, 19f, 4.4f);
        NivelBuilder.Solid("TechoNicho_Seguro", geo, 16.5f, 7f, 19f, 16f);

        // ---- 4/5/6. Patio, losa con la puerta y bóveda segura ----
        NivelBuilder.Solid("FondoFosoPatio", geo, 19f, -1f, 20f, 0f);
        NivelBuilder.Spikes("PinchosFosoPatio", pel, 19f, 20f, 0f);
        NivelBuilder.Solid("SueloPatio", geo, 20f, -1f, 26f, 1f);
        NivelBuilder.Solid("TechoPatio", geo, 19f, 13f, 20.5f, 16f);
        NivelBuilder.Spikes("PinchosTechoPatio", pel, 19f, 20.5f, 13f, up: false);
        NivelBuilder.Solid("Boveda_Segura", geo, 20.5f, 13f, 26f, 16f);
        NivelBuilder.Solid("Losa", geo, 23f, 9f, 26f, 9.4f);
        NivelBuilder.Spikes("PinchosBajoLosa", pel, 23f, 26f, 9f, up: false);

        var puerta = TutorialBuilder.CrearPuerta(mec, new Vector2(25f, 9.4f), 2);
        NivelBuilder.Solid("PosteDiana", geo, 24.9f, 1f, 25.1f, 1.65f);
        var dianaPatio = Nivel1Builder.CrearDiana(mec, new Vector2(25f, 2f), 0.35f, 2f);
        dianaPatio.name = "DianaPatio";
        Object.DestroyImmediate(dianaPatio.transform.Find("Cuerda").gameObject);
        var dianaLosa = Nivel1Builder.CrearDiana(mec, new Vector2(23.4f, 8.2f), 0.35f, 9f);
        dianaLosa.name = "DianaLosa";
        UnityEventTools.AddPersistentListener(dianaPatio.onHit, puerta.Activate);
        UnityEventTools.AddPersistentListener(dianaLosa.onHit, puerta.Activate);

        // Zona sin retorno: patio y bóveda (x ≥ 19.9). Entrar con la puerta cerrada = reinicio.
        var zona = new GameObject("ZonaSinRetorno");
        zona.transform.SetParent(mec, false);
        zona.transform.position = new Vector3((19.9f + 26f) / 2f, 7.5f, 0f);
        var zcol = zona.AddComponent<BoxCollider2D>();
        zcol.isTrigger = true;
        zcol.size = new Vector2(26f - 19.9f, 17f);
        var gate = zona.AddComponent<RequireOpenDoorZone>();
        so = new SerializedObject(gate);
        so.FindProperty("door").objectReferenceValue = puerta;
        so.ApplyModifiedPropertiesWithoutUndo();

        // ---- Jugador, gravedad, arco, gestor y cámara ----
        var jugador = NivelBuilder.Player(nivel, new Vector2(1.1f, 1f));
        Nivel1Builder.AddGravityAndBow(jugador, Nivel1Builder.ArrowSpeed);
        var gestor = TutorialBuilder.CrearGestor(jugador.transform, "MainMenu");
        Nivel1Builder.SetBounds(gestor, new Rect(-2f, -3f, 30f, 19f));
        NivelBuilder.SetupCamera(new Vector2(13f, 7.5f), 7.5f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Niveles] Nivel2 construido y guardado.");
    }

    public static void ConstruirBatch() => Construir();
}
