using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Crea los prefabs de las mecánicas de física (jugador, caja, lanzador, proyectil, recarga) y la escena
// "PruebasFisica" para probarlas juntas. Menú: Herramientas > Física > Crear escena de pruebas.
// También se puede ejecutar desde la línea de comandos: -executeMethod PhysicsTestSceneBuilder.Build
public static class PhysicsTestSceneBuilder
{
    private const string SpritesFolder = "Assets/Sprites/Fisica";
    private const string PrefabsFolder = "Assets/Prefabs/Fisica";
    private const string ScenePath = "Assets/Scenes/PruebasFisica.unity";

    // Escena base: ya trae la cámara, la luz 2D y el menú de pausa
    private const string BaseScenePath = "Assets/Scenes/Nivel1.unity";

    private const string KnightTexturePath = "Assets/Personaje.png";
    private const string KnightSpriteName = "Personaje_0";
    private const string FontPath = "Assets/Fonts/PerfectDOSVGA437.ttf";

    // Medidas del escenario (unidades de Unity; 1,5 unidades = 1 m)
    private const float FloorTop = -5f;
    private const float CeilingBottom = 5f;
    private const float InnerWall = 12.75f;
    private const float Thickness = 1.5f;
    private const float PitLeft = 3f;
    private const float PitRight = 6f;

    private struct Prefabs
    {
        public GameObject Player;
        public GameObject Box;
        public GameObject Launcher;
        public GameObject Recharge;
    }

    [MenuItem("Herramientas/Física/Crear escena de pruebas")]
    private static void BuildFromMenu()
    {
        if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog(
                "Escena de pruebas de física",
                "Ya existe " + ScenePath + ". Se van a reemplazar la escena y los prefabs de " + PrefabsFolder + ". ¿Continuar?",
                "Reemplazar", "Cancelar"))
        {
            return;
        }

        Build();
    }

    public static void Build()
    {
        ConfigureSprites();
        Prefabs prefabs = BuildPrefabs();
        BuildScene(prefabs);
        AssetDatabase.SaveAssets();
        Debug.Log("Física: escena de pruebas creada en " + ScenePath);
    }

    // ---------------------------------------------------------------- Sprites

    private static void ConfigureSprites()
    {
        Vector2 center = new Vector2(0.5f, 0.5f);
        ConfigureSprite("Caja", 16, center);
        ConfigureSprite("Bala", 20, center);
        ConfigureSprite("CanonTubo", 16, new Vector2(0.15f, 0.5f)); // gira desde la recámara
        ConfigureSprite("CanonBase", 16, center);
        ConfigureSprite("RecargaGravedad", 16, center);
        ConfigureSprite("Bloque", 16, center);
    }

    // Pixel art: sin filtrado ni compresión, y malla rectangular para poder dibujarlo en mosaico
    private static void ConfigureSprite(string name, float pixelsPerUnit, Vector2 pivot)
    {
        string path = SpritesFolder + "/" + name + ".png";
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        importer.SetTextureSettings(settings);

        importer.SaveAndReimport();
    }

    private static Sprite LoadSprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpritesFolder + "/" + name + ".png");
    }

    // ---------------------------------------------------------------- Prefabs

    private static Prefabs BuildPrefabs()
    {
        EnsureFolder(PrefabsFolder);

        var prefabs = new Prefabs();
        prefabs.Player = BuildPlayerPrefab();
        prefabs.Box = BuildBoxPrefab();
        GameObject projectile = BuildProjectilePrefab();
        prefabs.Launcher = BuildLauncherPrefab(projectile.GetComponent<Projectile>());
        prefabs.Recharge = BuildRechargePrefab();
        return prefabs;
    }

    private static GameObject BuildPlayerPrefab()
    {
        Sprite knight = AssetDatabase.LoadAllAssetsAtPath(KnightTexturePath).OfType<Sprite>().First(s => s.name == KnightSpriteName);

        // El caballero mide 2,4 unidades de alto (1,6 m)
        var go = new GameObject("Jugador");
        go.transform.localScale = Vector3.one * (2.4f / knight.bounds.size.y);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = knight;
        renderer.sortingOrder = 10;

        AddKinematicBody(go);
        var collider = go.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(knight.bounds.size.x * 0.62f, knight.bounds.size.y * 0.98f);

        var body = go.AddComponent<PhysicsBody2D>();
        SetFloat(body, "mass", 70f);

        go.AddComponent<GravityInverter>();
        go.AddComponent<PlayerController2D>();

        return SavePrefab(go, "Jugador");
    }

    private static GameObject BuildBoxPrefab()
    {
        // Caja de 1 m de lado (1,5 unidades)
        var go = new GameObject("Caja");
        go.transform.localScale = Vector3.one * 1.5f;

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite("Caja");
        renderer.sortingOrder = 5;

        AddKinematicBody(go);
        go.AddComponent<BoxCollider2D>().size = Vector2.one;

        var body = go.AddComponent<PhysicsBody2D>();
        SetFloat(body, "mass", 35f);
        go.AddComponent<PushableBox>();

        return SavePrefab(go, "Caja");
    }

    private static GameObject BuildProjectilePrefab()
    {
        var go = new GameObject("Proyectil");

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite("Bala");
        renderer.sortingOrder = 20;

        var projectile = go.AddComponent<Projectile>();
        SetFloat(projectile, "radius", 0.2f);

        return SavePrefab(go, "Proyectil");
    }

    private static GameObject BuildLauncherPrefab(Projectile projectilePrefab)
    {
        var go = new GameObject("Lanzador");
        go.transform.localScale = Vector3.one * 1.5f;

        var baseRenderer = go.AddComponent<SpriteRenderer>();
        baseRenderer.sprite = LoadSprite("CanonBase");
        baseRenderer.sortingOrder = 7;

        // La base es sólida: el jugador no la atraviesa
        go.AddComponent<BoxCollider2D>().size = new Vector2(1f, 0.75f);

        var barrel = new GameObject("Tubo");
        barrel.transform.SetParent(go.transform, false);
        barrel.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        var barrelRenderer = barrel.AddComponent<SpriteRenderer>();
        barrelRenderer.sprite = LoadSprite("CanonTubo");
        barrelRenderer.sortingOrder = 6;

        var muzzle = new GameObject("Boca");
        muzzle.transform.SetParent(barrel.transform, false);
        muzzle.transform.localPosition = new Vector3(0.95f, 0f, 0f);

        var launcher = go.AddComponent<ProjectileLauncher>();
        var so = new SerializedObject(launcher);
        so.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
        so.FindProperty("muzzle").objectReferenceValue = muzzle.transform;
        so.FindProperty("barrel").objectReferenceValue = barrel.transform;
        so.ApplyModifiedPropertiesWithoutUndo();
        barrel.transform.rotation = Quaternion.Euler(0f, 0f, 135f);

        return SavePrefab(go, "Lanzador");
    }

    private static GameObject BuildRechargePrefab()
    {
        var go = new GameObject("RecargaGravedad");

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite("RecargaGravedad");
        renderer.sortingOrder = 8;

        go.AddComponent<GravityRecharge>();
        return SavePrefab(go, "RecargaGravedad");
    }

    private static void AddKinematicBody(GameObject go)
    {
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    // ---------------------------------------------------------------- Escena

    private static void BuildScene(Prefabs prefabs)
    {
        AssetDatabase.DeleteAsset(ScenePath);
        AssetDatabase.CopyAsset(BaseScenePath, ScenePath);
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Camera camera = Object.FindAnyObjectByType<Camera>();
        camera.orthographicSize = 7.5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.13f, 0.19f);
        camera.transform.position = new Vector3(0f, 0f, -10f);

        // Paredes, suelo (con un hueco) y techo
        var level = new GameObject("Escenario");
        float wallCenter = InnerWall + Thickness * 0.5f;
        float floorY = FloorTop - Thickness * 0.5f;
        float ceilingY = CeilingBottom + Thickness * 0.5f;
        CreateBlock("Suelo izquierdo", level, new Vector2((-InnerWall + PitLeft) * 0.5f, floorY), new Vector2(PitLeft + InnerWall, Thickness));
        CreateBlock("Suelo derecho", level, new Vector2((PitRight + InnerWall) * 0.5f, floorY), new Vector2(InnerWall - PitRight, Thickness));
        CreateBlock("Techo", level, new Vector2(0f, ceilingY), new Vector2(InnerWall * 2f, Thickness));
        CreateBlock("Pared izquierda", level, new Vector2(-wallCenter, 0f), new Vector2(Thickness, 16f));
        CreateBlock("Pared derecha", level, new Vector2(wallCenter, 0f), new Vector2(Thickness, 16f));

        var pit = new GameObject("Vacío");
        pit.transform.SetParent(level.transform);
        pit.transform.position = new Vector3((PitLeft + PitRight) * 0.5f, -8.5f, 0f);
        var pitArea = pit.AddComponent<BoxCollider2D>();
        pitArea.size = new Vector2(PitRight - PitLeft + 0.4f, 2f);
        pitArea.isTrigger = true;
        pit.AddComponent<KillZone>();

        // Jugador
        GameObject player = Place(prefabs.Player, "Jugador", new Vector2(-11f, FloorTop + 1.25f));

        // Cajas: liviana (15 kg), normal (35 kg) y muy pesada (150 kg, el jugador no la mueve)
        var boxes = new GameObject("Cajas");
        GameObject light = Place(prefabs.Box, "Caja liviana (15 kg)", new Vector2(-8f, FloorTop + 0.75f), boxes);
        SetFloat(light.GetComponent<PhysicsBody2D>(), "mass", 15f);
        light.GetComponent<SpriteRenderer>().color = new Color(1f, 0.95f, 0.8f);

        Place(prefabs.Box, "Caja normal (35 kg)", new Vector2(-4.5f, FloorTop + 0.75f), boxes);

        GameObject heavy = Place(prefabs.Box, "Caja pesada (150 kg)", new Vector2(-0.5f, FloorTop + 1.1f), boxes);
        heavy.transform.localScale = Vector3.one * 2.2f;
        SetFloat(heavy.GetComponent<PhysicsBody2D>(), "mass", 150f);
        heavy.GetComponent<SpriteRenderer>().color = new Color(0.7f, 0.6f, 0.55f);

        // Lanzador a la derecha, apuntando al jugador cuando está a su alcance
        GameObject launcher = Place(prefabs.Launcher, "Lanzador", new Vector2(11.5f, FloorTop + 0.5625f));
        var launcherSo = new SerializedObject(launcher.GetComponent<ProjectileLauncher>());
        launcherSo.FindProperty("launchSpeed").floatValue = 10f;
        launcherSo.FindProperty("launchAngle").floatValue = 135f;
        launcherSo.FindProperty("fireInterval").floatValue = 2.5f;
        launcherSo.FindProperty("aimAtPlayer").boolValue = true;
        launcherSo.ApplyModifiedPropertiesWithoutUndo();

        // Recargas: una en el techo (sobre la caja pesada) y otra pasando el hueco
        var recharges = new GameObject("Recargas");
        Place(prefabs.Recharge, "Recarga techo", new Vector2(-0.5f, CeilingBottom - 1f), recharges);
        Place(prefabs.Recharge, "Recarga suelo", new Vector2(8.5f, FloorTop + 1.2f), recharges);

        BuildHud(player.GetComponent<GravityInverter>());

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    private static void CreateBlock(string name, GameObject parent, Vector2 center, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.position = center;

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite("Bloque");
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.tileMode = SpriteTileMode.Continuous;
        renderer.size = size;

        go.AddComponent<BoxCollider2D>().size = size;
    }

    private static GameObject Place(GameObject prefab, string name, Vector2 position, GameObject parent = null)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene());
        instance.name = name;
        if (parent != null) instance.transform.SetParent(parent.transform);
        instance.transform.position = position;
        return instance;
    }

    private static void BuildHud(GravityInverter inverter)
    {
        Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);

        var canvasObject = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -1; // debajo de los menús

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Text uses = CreateText("Usos de gravedad", canvasObject, font, 40, TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(900f, 60f));
        var hud = uses.gameObject.AddComponent<GravityUsesHud>();
        var hudSo = new SerializedObject(hud);
        hudSo.FindProperty("inverter").objectReferenceValue = inverter;
        hudSo.ApplyModifiedPropertiesWithoutUndo();

        Text controls = CreateText("Controles", canvasObject, font, 26, TextAnchor.LowerCenter,
            new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(1800f, 40f));
        controls.color = new Color(1f, 1f, 1f, 0.75f);
        var localized = controls.gameObject.AddComponent<LocalizedText>();
        var localizedSo = new SerializedObject(localized);
        localizedSo.FindProperty("key").stringValue = "hud.controles";
        localizedSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Text CreateText(string name, GameObject parent, Font font, int size, TextAnchor alignment,
        Vector2 anchor, Vector2 position, Vector2 dimensions)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent.transform, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;

        var text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.raycastTarget = false;

        go.GetComponent<Shadow>().effectDistance = new Vector2(3f, -3f);
        return text;
    }

    // ---------------------------------------------------------------- Utilidades

    private static GameObject SavePrefab(GameObject go, string name)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabsFolder + "/" + name + ".prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static void SetFloat(Object target, string property, float value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(property).floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
