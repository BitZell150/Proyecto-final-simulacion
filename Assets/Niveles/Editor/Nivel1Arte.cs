using UnityEngine;

/// <summary>
/// Arte y decoración del Nivel 1 (La fortaleza caída). Se aplica al final de Nivel1Builder.Construir().
/// Mismas reglas de legibilidad que el Tutorial (ver NivelArte): azul = sólido, pinchos blancos = letal,
/// la decoración va detrás, apagada y fuera de la trayectoria de la flecha (del nicho a la diana).
///
///   Pasillo de inicio (techo bajo) · pozo con el mirador · foso con el puente de tablones · puerta (ya abierta).
///   La diana lleva un resplandor rojizo para que se vea de lejos desde el nicho.
/// </summary>
public static class Nivel1Arte
{
    private static readonly Vector2 Base = new Vector2(0.5f, 0f);
    private static readonly Vector2 Techo = new Vector2(0.5f, 1f);

    public static void Aplicar(Transform nivel, Transform geo, Transform pel, Transform mec)
    {
        NivelArte.Importar();

        // ---- Reemplazo de los cuadrados de prueba ----
        NivelArte.Solidos(geo);
        NivelArte.Pinchos(pel);
        NivelArte.Puente(mec.Find("PuenteRetractil"));
        NivelArte.Diana(mec.Find("Diana"));
        NivelArte.Puerta(mec.Find("PuertaSalida"));

        NivelArte.ParedFondo(nivel);
        var deco = NivelBuilder.Group("Decoracion", nivel).transform;
        var apagado = new Color(0.72f, 0.72f, 0.8f);
        var tenue = new Color(0.6f, 0.62f, 0.66f);

        // ---- Pasillo de inicio (2.6 u de alto): detalles pequeños y bajos ----
        NivelArte.Antorcha(deco, "AntorchaPasillo", "antorcha_1", new Vector2(3.3f, 2.5f), 1.1f);
        NivelArte.Deco(deco, "Calavera", "calavera", new Vector2(5.0f, 2.0f), 0.5f, Base, color: apagado);
        NivelArte.Deco(deco, "Hueso", "hueso", new Vector2(5.6f, 2.0f), 0.3f, Base, color: apagado);
        NivelArte.Deco(deco, "TelaranaPasillo", "telarana", new Vector2(0.95f, 4.2f), 0.8f, new Vector2(0.5f, 0.5f), color: tenue);
        NivelArte.Deco(deco, "EnredaderaPasillo", "enredadera_2", new Vector2(4.3f, 4.6f), 1.0f, Techo, color: tenue, flipX: true);

        // ---- Pozo y mirador ----
        NivelArte.Deco(deco, "EstandartePozo1", "estandarte_1", new Vector2(6.1f, 12.4f), 2.6f, Techo, color: apagado);
        NivelArte.Deco(deco, "EstandartePozo2", "estandarte_2", new Vector2(10.9f, 12.4f), 2.6f, Techo, color: apagado);
        NivelArte.Deco(deco, "VentanaPozo", "ventana", new Vector2(8.5f, 7.4f), 3.2f, Base, color: apagado);
        NivelArte.Antorcha(deco, "AntorchaPozo1", "antorcha_2", new Vector2(6.9f, 5.3f));
        NivelArte.Antorcha(deco, "AntorchaPozo2", "antorcha_1", new Vector2(10.6f, 7.2f));
        NivelArte.Deco(deco, "VelasMirador", "vela_3", new Vector2(9.1f, 4.2f), 0.6f, Base, color: apagado);

        // ---- Foso: antorchas bajo el puente y ventana alta, fuera de la trayectoria de la flecha ----
        NivelArte.Antorcha(deco, "AntorchaFoso1", "antorcha_2", new Vector2(15.0f, 2.3f));
        NivelArte.Antorcha(deco, "AntorchaFoso2", "antorcha_1", new Vector2(21.0f, 2.3f));
        NivelArte.Deco(deco, "VentanaFoso", "ventana", new Vector2(16.4f, 11.6f), 3.0f, Base, color: apagado);
        NivelArte.Deco(deco, "TelaranaViga", "telarana", new Vector2(19.95f, 12.35f), 1.0f, new Vector2(0.5f, 0.5f), color: tenue);

        // ---- Zona de la puerta ----
        NivelArte.Antorcha(deco, "AntorchaPuerta", "antorcha_2", new Vector2(23.7f, 7.3f));
        NivelArte.Deco(deco, "EstandartePuerta", "estandarte_1", new Vector2(24.9f, 10.9f), 2.4f, Techo, color: apagado);
        NivelArte.Luz(deco, "LuzPuerta", new Vector2(25f, 5.3f), 4.2f, new Color(1f, 0.75f, 0.35f, 0.28f));

        // ---- La diana: resplandor rojizo ----
        NivelArte.Luz(deco, "LuzDiana", new Vector2(20.6f, 11.12f), 2.6f, new Color(1f, 0.3f, 0.25f, 0.32f));
    }
}
