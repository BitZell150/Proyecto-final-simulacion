using UnityEngine;

/// <summary>
/// Arte y decoración del Tutorial (Patio de entrenamiento). Se aplica al final de TutorialBuilder.Construir().
///
/// Vista de la cámara: x −0.33…26.33, y 0…15. La acción ocurre por debajo de y ≈ 8; todo el tercio
/// superior es pared del fondo vacía, que es donde va la decoración (así nunca tapa ni confunde la ruta).
///
///   Sólidos ........ piedra azul (suelo, paredes, plataformas)
///   Fondo .......... ladrillo morado oscuro y apagado
///   Peligros ....... pinchos blancos + bloque colgante con cadena
///   Interactivos ... caja de madera, palanca con placa, puerta de madera (verde al abrirse), cada uno con su luz cálida
///   Decoración ..... ventanas, estandartes, antorchas, faroles, enredaderas, telarañas, calaveras, velas
/// </summary>
public static class TutorialArte
{
    private static readonly Vector2 Base = new Vector2(0.5f, 0f);   // ancla en el centro de la base
    private static readonly Vector2 Techo = new Vector2(0.5f, 1f);  // ancla en el centro del borde superior

    public static void Aplicar(Transform nivel, Transform geo, Transform pel, Transform mec)
    {
        NivelArte.Importar();

        // ---- Reemplazo de los cuadrados de prueba ----
        NivelArte.Solidos(geo);
        NivelArte.Pinchos(pel);
        NivelArte.BloqueColgante(pel.Find("BloqueColgante"));
        NivelArte.Caja(mec.Find("Caja"));
        NivelArte.Puerta(mec.Find("PuertaSalida"));
        NivelArte.Palanca(mec.Find("Palanca"));

        // ---- Pared del fondo ----
        NivelArte.ParedFondo(nivel);

        var deco = NivelBuilder.Group("Decoracion", nivel).transform;
        var apagado = new Color(0.72f, 0.72f, 0.8f); // la decoración del fondo se ve más apagada que lo jugable
        var tenue = new Color(0.6f, 0.62f, 0.66f);

        // ---- Arriba: estandartes, ventanas, faroles, enredaderas y telarañas (lejos de toda la ruta) ----
        NivelArte.Deco(deco, "Estandarte1", "estandarte_1", new Vector2(3.6f, 13.9f), 3.0f, Techo, color: apagado);
        NivelArte.Deco(deco, "Estandarte2", "estandarte_2", new Vector2(22.4f, 13.9f), 3.0f, Techo, color: apagado);
        NivelArte.Deco(deco, "Ventana1", "ventana", new Vector2(7.6f, 7.2f), 3.0f, Base, color: apagado);
        NivelArte.Deco(deco, "Ventana2", "ventana", new Vector2(18.8f, 8.4f), 3.0f, Base, color: apagado);
        NivelArte.Deco(deco, "Farol1", "farol", new Vector2(11.6f, 13.9f), 2.4f, Techo, color: apagado);
        NivelArte.Deco(deco, "Farol2", "farol", new Vector2(20.4f, 13.9f), 2.4f, Techo, color: apagado);
        NivelArte.Deco(deco, "Enredadera1", "enredadera_1", new Vector2(9.3f, 14.0f), 2.2f, Techo, color: tenue);
        NivelArte.Deco(deco, "Enredadera2", "enredadera_2", new Vector2(17.8f, 14.0f), 2.4f, Techo, color: tenue);
        NivelArte.Deco(deco, "Enredadera3", "enredadera_2", new Vector2(24.3f, 14.0f), 1.9f, Techo, color: tenue, flipX: true);
        NivelArte.Deco(deco, "Telarana1", "telarana", new Vector2(1.65f, 13.4f), 1.2f, new Vector2(0.5f, 0.5f), color: tenue);
        NivelArte.Deco(deco, "Telarana2", "telarana", new Vector2(24.35f, 13.4f), 1.2f, new Vector2(0.5f, 0.5f), color: tenue, flipX: true);

        // ---- Antorchas: puntos de luz que van guiando la vista de izquierda a derecha ----
        NivelArte.Antorcha(deco, "AntorchaInicio", "antorcha_1", new Vector2(2.4f, 3.7f));
        NivelArte.Antorcha(deco, "AntorchaCaja", "antorcha_2", new Vector2(10.2f, 4.4f));
        NivelArte.Antorcha(deco, "AntorchaPalanca", "antorcha_1", new Vector2(17.0f, 7.3f));
        NivelArte.Antorcha(deco, "AntorchaPuerta", "antorcha_2", new Vector2(22.4f, 7.4f));

        // ---- Suelo: detalles pequeños, apagados y sin forma de pincho ni de caja ----
        NivelArte.Deco(deco, "Calavera", "calavera", new Vector2(9.0f, 1.0f), 0.6f, Base, color: apagado);
        NivelArte.Deco(deco, "Hueso", "hueso", new Vector2(9.7f, 1.0f), 0.3f, Base, color: apagado);
        NivelArte.Deco(deco, "VelasPalanca", "vela_3", new Vector2(17.0f, 4.2f), 0.7f, Base, color: apagado);

        // ---- Luz sobre lo importante: palanca y puerta ----
        // (la palanca es amarilla: un halo anaranjado detrás le quitaría contraste, así que solo lleva las velas)
        NivelArte.Luz(deco, "LuzPuerta", new Vector2(24.4f, 7.0f), 4.4f, new Color(1f, 0.75f, 0.35f, 0.28f));
    }
}
