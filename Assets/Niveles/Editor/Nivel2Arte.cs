using UnityEngine;

/// <summary>
/// Arte y decoración del Nivel 2 (La cripta maldita). Se aplica al final de Nivel2Builder.Construir().
/// Mismas reglas de legibilidad que el Tutorial (ver NivelArte). Es el nivel examen: la zona de los pilares y los
/// bloques colgantes se deja limpia (todo lo que se ve ahí es peligro o suelo) y la decoración va en la cámara
/// de la caja, el hueco, el patio y la bóveda de la puerta.
/// </summary>
public static class Nivel2Arte
{
    private static readonly Vector2 Base = new Vector2(0.5f, 0f);
    private static readonly Vector2 Techo = new Vector2(0.5f, 1f);

    public static void Aplicar(Transform nivel, Transform geo, Transform pel, Transform mec)
    {
        NivelArte.Importar();

        // ---- Reemplazo de los cuadrados de prueba ----
        NivelArte.Solidos(geo);
        NivelArte.Pinchos(pel);
        NivelArte.BloqueColgante(pel.Find("BloqueColganteA"));
        NivelArte.BloqueColgante(pel.Find("BloqueColganteB"));
        NivelArte.Caja(mec.Find("Caja"));
        NivelArte.Puerta(mec.Find("PuertaSalida"));
        NivelArte.Diana(mec.Find("DianaPatio"));
        NivelArte.Diana(mec.Find("DianaLosa"));

        NivelArte.ParedFondo(nivel);
        var deco = NivelBuilder.Group("Decoracion", nivel).transform;
        var apagado = new Color(0.72f, 0.72f, 0.8f);
        var tenue = new Color(0.6f, 0.62f, 0.66f);

        // ---- Cámara de la caja ----
        NivelArte.Deco(deco, "VentanaCamara", "ventana", new Vector2(11.2f, 3.0f), 2.6f, Base, color: apagado);
        NivelArte.Antorcha(deco, "AntorchaCamara1", "antorcha_1", new Vector2(9.1f, 4.0f));
        NivelArte.Antorcha(deco, "AntorchaCamara2", "antorcha_2", new Vector2(13.0f, 4.0f));
        NivelArte.Deco(deco, "Calavera", "calavera", new Vector2(8.6f, 1.0f), 0.5f, Base, color: apagado);
        NivelArte.Deco(deco, "Hueso", "hueso", new Vector2(9.1f, 1.0f), 0.3f, Base, color: apagado);
        NivelArte.Deco(deco, "TelaranaCamara1", "telarana", new Vector2(8.5f, 6.5f), 1.0f, new Vector2(0.5f, 0.5f), color: tenue);
        NivelArte.Deco(deco, "TelaranaCamara2", "telarana", new Vector2(13.45f, 6.5f), 1.0f, new Vector2(0.5f, 0.5f), color: tenue, flipX: true);

        // ---- Hueco junto al mirador y mirador ----
        NivelArte.Antorcha(deco, "AntorchaHueco", "antorcha_2", new Vector2(15.4f, 4.0f), 1.2f);
        NivelArte.Deco(deco, "VelasMirador", "vela_3", new Vector2(17.2f, 4.4f), 0.6f, Base, color: apagado);

        // ---- Patio ----
        NivelArte.Deco(deco, "VentanaPatio", "ventana", new Vector2(22.2f, 4.0f), 3.2f, Base, color: apagado);
        NivelArte.Antorcha(deco, "AntorchaPatio", "antorcha_1", new Vector2(20.8f, 7.4f));
        NivelArte.Deco(deco, "EstandartePatio", "estandarte_2", new Vector2(21.8f, 12.8f), 2.6f, Techo, color: apagado);
        NivelArte.Deco(deco, "EnredaderaPatio1", "enredadera_1", new Vector2(24.2f, 13.0f), 1.5f, Techo, color: tenue);
        NivelArte.Deco(deco, "EnredaderaPatio2", "enredadera_2", new Vector2(20.9f, 13.0f), 1.3f, Techo, color: tenue);

        // ---- Puerta (en la losa) ----
        NivelArte.Antorcha(deco, "AntorchaPuerta", "antorcha_2", new Vector2(23.5f, 10.4f), 1.1f);
        NivelArte.Luz(deco, "LuzPuerta", new Vector2(25f, 10.4f), 4.2f, new Color(1f, 0.75f, 0.35f, 0.28f));

        // ---- Dianas: resplandor rojizo ----
        NivelArte.Luz(deco, "LuzDianaPatio", new Vector2(25f, 2f), 2.4f, new Color(1f, 0.3f, 0.25f, 0.32f));
        NivelArte.Luz(deco, "LuzDianaLosa", new Vector2(23.4f, 8.2f), 2.4f, new Color(1f, 0.3f, 0.25f, 0.32f));
    }
}
