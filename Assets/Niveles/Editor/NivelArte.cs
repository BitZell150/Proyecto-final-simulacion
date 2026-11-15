using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Arte de los niveles (solo editor): importa los sprites de Assets/Niveles/Arte y reemplaza los
/// cuadrados de prueba de un nivel ya construido por arte, SIN tocar colliders, scripts ni nombres.
///
/// Reglas de legibilidad (el jugador debe distinguir de un vistazo qué es qué):
///   · Azul brillante = sólido (suelo, paredes, plataformas). Nada de decoración lo usa.
///   · Pinchos blancos = letal. Ninguna decoración es blanca y puntiaguda.
///   · Caja marrón = empujable. La decoración no usa cajas ni barriles.
///   · La decoración va detrás (órdenes negativos), atenuada y lejos de la ruta del jugador.
/// </summary>
public static class NivelArte
{
    public const string Dir = "Assets/Niveles/Arte";

    // Órdenes de dibujo
    public const int OrdenFondo = -20, OrdenDeco = -10, OrdenLuz = -9, OrdenSolido = 0;

    // ---------- Importación ----------

    /// <summary>Configura los PNG de Arte como sprites (se puede llamar las veces que sea).</summary>
    public static void Importar()
    {
        AssetDatabase.Refresh();
        foreach (var file in Directory.GetFiles(Dir, "*.png"))
        {
            string path = file.Replace('\\', '/');
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;

            string n = Path.GetFileNameWithoutExtension(path);
            float ppu = 100f;
            var borde = Vector4.zero;
            bool repetido = false;
            switch (n)
            {
                case "piedra": ppu = 72f; repetido = true; break;   // 144 px = 2 u
                case "fondo": ppu = 64f; repetido = true; break;    // 128 px = 2 u
                case "losa": ppu = 185f; repetido = true; borde = new Vector4(8f, 0f, 8f, 0f); break; // 74 px = 0.4 u
                case "cadena": ppu = 130f; repetido = true; break;
                case "tablon": ppu = 92.5f; repetido = true; break; // 37 px = 0.4 u de alto
                case "rim": ppu = 72f; repetido = true; break;      // 8 px = 0.11 u de alto  // 39 px = 0.3 u de ancho
            }

            var s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            var meshType = repetido ? SpriteMeshType.FullRect : SpriteMeshType.Tight;

            bool cambia = imp.textureType != TextureImporterType.Sprite
                || imp.spriteImportMode != SpriteImportMode.Single
                || !Mathf.Approximately(imp.spritePixelsPerUnit, ppu)
                || imp.spriteBorder != borde
                || imp.filterMode != FilterMode.Bilinear
                || s.spriteMeshType != meshType;
            if (!cambia) continue;

            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = ppu;
            imp.spriteBorder = borde;
            imp.filterMode = FilterMode.Bilinear;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.ReadTextureSettings(s);
            s.spriteMeshType = meshType;
            s.spriteAlignment = (int)SpriteAlignment.Center;
            imp.SetTextureSettings(s);
            imp.SaveAndReimport();
        }
    }

    public static Sprite Cargar(string nombre)
    {
        var sp = AssetDatabase.LoadAssetAtPath<Sprite>($"{Dir}/{nombre}.png");
        if (sp == null) Debug.LogWarning($"[Niveles] Falta el sprite '{nombre}' en {Dir}");
        return sp;
    }

    // ---------- Piezas visuales ----------

    /// <summary>
    /// Crea un SpriteRenderer hijo de 'raiz' que ocupa exactamente 'tam' unidades de mundo centrado en 'centro',
    /// aunque la raíz esté escalada (los colliders de la raíz no se tocan).
    /// </summary>
    public static SpriteRenderer Visual(Transform raiz, string nombre, Sprite sprite, Vector2 centro, Vector2 tam,
        int orden, Color color, SpriteDrawMode modo = SpriteDrawMode.Simple, bool flipX = false, bool flipY = false)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(raiz, false);
        go.transform.position = new Vector3(centro.x, centro.y, 0f);

        var esc = raiz.lossyScale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = orden;
        sr.color = color;
        sr.flipX = flipX;
        sr.flipY = flipY;

        if (modo == SpriteDrawMode.Simple)
        {
            var nat = sprite.bounds.size;
            go.transform.localScale = new Vector3(tam.x / nat.x / esc.x, tam.y / nat.y / esc.y, 1f);
        }
        else
        {
            sr.drawMode = modo;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = tam;
            go.transform.localScale = new Vector3(1f / esc.x, 1f / esc.y, 1f);
        }
        return sr;
    }

    /// <summary>Sprite de decoración anclado por 'pivote' (0.5,0 = centro de la base; 0.5,1 = centro del borde superior).</summary>
    public static SpriteRenderer Deco(Transform raiz, string nombre, string sprite, Vector2 ancla, float alto,
        Vector2 pivote, int orden = OrdenDeco, Color? color = null, bool flipX = false)
    {
        var sp = Cargar(sprite);
        var nat = sp.bounds.size;
        var tam = new Vector2(alto * nat.x / nat.y, alto);
        var centro = ancla + new Vector2((0.5f - pivote.x) * tam.x, (0.5f - pivote.y) * tam.y);
        return Visual(raiz, nombre, sp, centro, tam, orden, color ?? Color.white, SpriteDrawMode.Simple, flipX);
    }

    /// <summary>Resplandor cálido suave (guía la vista hacia antorchas, palanca y puerta).</summary>
    public static void Luz(Transform raiz, string nombre, Vector2 centro, float diametro, Color color)
    {
        Visual(raiz, nombre, Cargar("luz"), centro, new Vector2(diametro, diametro), OrdenLuz, color);
    }

    // ---------- Reemplazo de los cuadrados de prueba ----------

    private const float Teja = 2f; // lado de la baldosa de piedra en unidades

    /// <summary>
    /// Suelo, paredes y plataformas: piedra azul; losas finas; fondo de foso oscuro.
    /// La piedra se tesela anclada al borde visible (arriba en suelos y mesetas, abajo en el techo, junto al
    /// hueco del nivel en las paredes) y la parte sobrante queda fuera de pantalla: así el borde que se ve
    /// siempre empieza con una hilada completa de ladrillos.
    /// </summary>
    public static void Solidos(Transform geo)
    {
        var piedra = Cargar("piedra");
        var losa = Cargar("losa");
        var fondo = Cargar("fondo");
        var rim = Cargar("rim");
        var paredes = new Color(0.78f, 0.78f, 0.9f); // marco del nivel un poco más oscuro que las plataformas

        foreach (Transform t in geo)
        {
            var sr = t.GetComponent<SpriteRenderer>();
            if (sr == null) continue;
            var tam = new Vector2(t.lossyScale.x, t.lossyScale.y);
            var centro = (Vector2)t.position;
            Object.DestroyImmediate(sr);

            if (t.name.StartsWith("Fondo"))
            {
                Visual(t, "Arte", fondo, centro, tam, OrdenSolido, new Color(0.55f, 0.5f, 0.65f), SpriteDrawMode.Tiled);
            }
            else if (tam.y <= 0.5f)
            {
                Visual(t, "Arte", losa, centro, tam, OrdenSolido, Color.white, SpriteDrawMode.Sliced);
            }
            else
            {
                bool izq = t.name.EndsWith("Izquierda"), der = t.name.EndsWith("Derecha");
                // "colgante": llega al borde superior de la pantalla sin tocar el inferior (techos, vigas, columnas, tabiques)
                bool colgante = centro.y + tam.y / 2f >= 15.5f && centro.y - tam.y / 2f > 0.5f;
                float h = Mathf.Ceil(tam.y / Teja) * Teja;
                var c = centro; var s = new Vector2(tam.x, h);
                if (colgante) c.y = centro.y - tam.y / 2f + h / 2f;    // ancla abajo, sobra hacia arriba
                else c.y = centro.y + tam.y / 2f - h / 2f;             // ancla arriba, sobra hacia abajo
                if (izq || der)
                {
                    float w = Mathf.Ceil(tam.x / Teja) * Teja;
                    s.x = w;
                    c.x = izq ? centro.x + tam.x / 2f - w / 2f : centro.x - tam.x / 2f + w / 2f;
                }
                bool marco = izq || der || colgante;
                if (t.name == "PosteDiana")
                {
                    // poste de madera: escala uniforme por altura
                    var viga = Cargar("viga");
                    float alto = tam.y;
                    Visual(t, "Arte", viga, centro, new Vector2(alto * viga.bounds.size.x / viga.bounds.size.y, alto), OrdenSolido, Color.white);
                    continue;
                }
                Visual(t, "Arte", piedra, c, s, OrdenSolido, marco ? paredes : Color.white, SpriteDrawMode.Tiled);

                // reborde claro en las superficies donde se pisa
                if (!marco)
                    Visual(t, "Reborde", rim, new Vector2(centro.x, centro.y + tam.y / 2f - 0.055f), new Vector2(tam.x, 0.11f),
                        OrdenSolido + 1, Color.white, SpriteDrawMode.Tiled);
            }
        }
    }

    /// <summary>
    /// Pinchos de los fosos y del bloque colgante: se rehace cada fila con la punta del sprite SIN deformarla
    /// (escala uniforme). El paso entre puntas se ajusta para llenar justo el ancho de la zona letal.
    /// </summary>
    public static void Pinchos(Transform peligros)
    {
        var sp = Cargar("pincho");
        var nat = sp.bounds.size;

        // filas = padres directos de los "Pincho" de prueba
        var filas = new System.Collections.Generic.List<Transform>();
        foreach (var sr in peligros.GetComponentsInChildren<SpriteRenderer>(true))
            if (sr.gameObject.name == "Pincho" && !filas.Contains(sr.transform.parent)) filas.Add(sr.transform.parent);

        foreach (var fila in filas)
        {
            float xMin = float.MaxValue, xMax = float.MinValue, yBase = 0f, dir = 1f;
            var viejos = new System.Collections.Generic.List<GameObject>();
            foreach (Transform h in fila)
            {
                var sr = h.GetComponent<SpriteRenderer>();
                if (sr == null || h.name != "Pincho") continue;
                float semi = h.localScale.x / 2f;
                xMin = Mathf.Min(xMin, h.position.x - semi);
                xMax = Mathf.Max(xMax, h.position.x + semi);
                dir = sr.flipY ? -1f : 1f;
                yBase = h.position.y - dir * 0.25f; // los de prueba miden 0.5 de alto
                viejos.Add(h.gameObject);
            }
            foreach (var v in viejos) Object.DestroyImmediate(v);

            int n = Mathf.Max(1, Mathf.RoundToInt((xMax - xMin) / 0.68f));
            float paso = (xMax - xMin) / n;
            float k = paso / nat.x;               // escala uniforme: la punta mantiene su proporción
            float alto = nat.y * k;
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject("Pincho");
                go.transform.SetParent(fila, false);
                go.transform.position = new Vector3(xMin + paso * (i + 0.5f), yBase + dir * alto / 2f, 0f);
                go.transform.localScale = new Vector3(k, k, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                sr.flipY = dir < 0f;
                sr.sortingOrder = 2;
            }
        }
    }

    /// <summary>Caja empujable: caja de madera (marrón, contrasta con el azul y el morado).</summary>
    public static void Caja(Transform caja)
    {
        var sr = caja.GetComponent<SpriteRenderer>();
        if (sr == null) return;
        var centro = (Vector2)caja.position;
        var sp = Cargar("caja");
        float alto = caja.lossyScale.y;                       // 1 u; el ancho sale de la proporción del sprite
        var tam = new Vector2(alto * sp.bounds.size.x / sp.bounds.size.y, alto);
        Object.DestroyImmediate(sr);
        Visual(caja, "Arte", sp, centro, tam, 3, Color.white);
    }

    /// <summary>
    /// Puerta: la hoja es la puerta de madera; el marco es la misma puerta con el hueco vacío,
    /// que ExitDoor tiñe de verde al abrirse (y se oculta la hoja).
    /// </summary>
    public static void Puerta(Transform puerta)
    {
        // 2.1 u de alto; el ancho sale de la proporción del sprite (no se aplasta); la base queda sobre el rellano
        const float alto = 2.1f;
        var centro = (Vector2)puerta.position + new Vector2(0f, alto / 2f);
        Reemplazar(puerta.Find("Marco"), "puerta_abierta", 1, alto: alto, centro: centro);
        Reemplazar(puerta.Find("Hoja"), "puerta", 2, alto: alto, centro: centro);
    }

    /// <summary>Palanca: base metálica del sprite; el mango sigue siendo amarillo para verse bien.</summary>
    public static void Palanca(Transform palanca)
    {
        Reemplazar(palanca.Find("Base"), "placa", 3, ancho: 0.8f);
        var palo = palanca.Find("Mango/Palo");
        if (palo != null) palo.localScale = new Vector3(0.24f, palo.localScale.y, 1f); // más gruesa: se ve de lejos
    }

    /// <summary>
    /// Cambia el sprite de un objeto de prueba con escala UNIFORME: se indica el ancho o el alto en unidades
    /// y la otra medida sale de la proporción original del sprite.
    /// </summary>
    private static void Reemplazar(Transform t, string sprite, int orden, float ancho = 0f, float alto = 0f, Vector2? centro = null)
    {
        if (t == null) return;
        var sr = t.GetComponent<SpriteRenderer>();
        if (sr == null) return;
        var sp = Cargar(sprite);
        var nat = sp.bounds.size;
        float k = ancho > 0f ? ancho / nat.x : alto / nat.y;
        if (centro.HasValue) t.position = new Vector3(centro.Value.x, centro.Value.y, 0f);
        sr.sprite = sp;
        sr.color = Color.white;
        sr.sortingOrder = orden;
        t.localScale = new Vector3(k, k, 1f);
    }

    /// <summary>Pared del fondo: ladrillo morado oscuro que cubre toda la vista (la cámara ve x −0.33…26.33, y 0…15).</summary>
    public static void ParedFondo(Transform nivel)
    {
        var g = NivelBuilder.Group("Fondo", nivel).transform;
        Visual(g, "ParedFondo", Cargar("fondo"), new Vector2(13f, 7.5f), new Vector2(29f, 17f),
            OrdenFondo, new Color(0.5f, 0.47f, 0.62f), SpriteDrawMode.Tiled);
    }

    /// <summary>Antorcha con su resplandor (la llama queda en el tercio superior del sprite).</summary>
    public static void Antorcha(Transform deco, string nombre, string sprite, Vector2 baseAncla, float alto = 1.3f)
    {
        Deco(deco, nombre, sprite, baseAncla, alto, new Vector2(0.5f, 0f));
        Luz(deco, nombre + "_Luz", baseAncla + new Vector2(0f, alto * 0.75f), alto * 2.3f, new Color(1f, 0.65f, 0.25f, 0.3f));
    }

    /// <summary>Puente retráctil: tablones de madera en mosaico (el puente se esconde tras el muro, orden −1).</summary>
    public static void Puente(Transform puente)
    {
        var sr = puente.GetComponent<SpriteRenderer>();
        if (sr == null) return;
        var centro = (Vector2)puente.position;
        var tam = new Vector2(puente.lossyScale.x, puente.lossyScale.y);
        int orden = sr.sortingOrder;
        Object.DestroyImmediate(sr);
        Visual(puente, "Arte", Cargar("tablon"), centro, tam, orden, Color.white, SpriteDrawMode.Tiled);
    }

    /// <summary>
    /// Diana: el aro es el sprite rojo y blanco; el centro es el mismo sprite en grises teñido de rojo
    /// (Target.Hit lo tiñe de verde al acertar). La cuerda pasa a ser una cadena.
    /// </summary>
    public static void Diana(Transform diana)
    {
        var aro = diana.Find("Aro");
        var centro = diana.Find("Centro");
        var cuerda = diana.Find("Cuerda");
        if (aro != null) Reemplazar(aro, "diana", 5, ancho: aro.lossyScale.x);
        if (centro != null)
        {
            Reemplazar(centro, "diana_gris", 6, ancho: centro.lossyScale.x);
            centro.GetComponent<SpriteRenderer>().color = new Color(1f, 0.28f, 0.28f);
        }
        if (cuerda != null)
        {
            var c = (Vector2)cuerda.position;
            float largo = cuerda.lossyScale.y;
            Object.DestroyImmediate(cuerda.gameObject);
            Visual(diana, "Cuerda", Cargar("cadena"), c, new Vector2(0.3f, largo), 1, Color.white, SpriteDrawMode.Tiled);
        }
    }

    /// <summary>
    /// Bloque con pinchos colgante (CrearBloqueColgante): cuerpo de dos hiladas de piedra + cadena de eslabones.
    /// Los pinchos los cambia Pinchos().
    /// </summary>
    public static void BloqueColgante(Transform bloque)
    {
        var cuerpo = bloque.Find("Cuerpo");
        if (cuerpo != null)
        {
            var c = (Vector2)cuerpo.position;
            var tam = new Vector2(cuerpo.lossyScale.x, cuerpo.lossyScale.y);
            Object.DestroyImmediate(cuerpo.gameObject);
            var tinte = new Color(0.92f, 0.52f, 0.66f); // más oscuro y rojizo que las plataformas: es letal
            Visual(bloque, "Cuerpo", Cargar("losa"), c + new Vector2(0f, 0.2f), new Vector2(tam.x, 0.4f), 3, tinte, SpriteDrawMode.Sliced);
            Visual(bloque, "Cuerpo2", Cargar("losa"), c - new Vector2(0f, 0.2f), new Vector2(tam.x, 0.4f), 3, tinte, SpriteDrawMode.Sliced);
        }

        var cadena = bloque.Find("Cadena");
        if (cadena != null)
        {
            var c = (Vector2)cadena.position;
            float largo = cadena.lossyScale.y;
            Object.DestroyImmediate(cadena.gameObject);
            Visual(bloque, "Cadena", Cargar("cadena"), c, new Vector2(0.3f, largo), 1, Color.white, SpriteDrawMode.Tiled);
        }
    }
}
