using System;
using System.Collections.Generic;
using UnityEngine;

public enum Language
{
    Spanish,
    English,
}

// Textos de la interfaz en español e inglés. El idioma elegido se guarda con PlayerPrefs.
// Para añadir un texto: una línea en Texts (español, inglés) y usar su clave con LocalizedText o Get().
public static class Localization
{
    private const string LanguageKey = "Options.Language";

    // Nombres de los idiomas, siempre en su propio idioma y en el orden de Language
    public static readonly string[] LanguageNames = { "Español", "English" };

    // Cada clave lleva un texto por idioma, en el orden de Language
    private static readonly Dictionary<string, string[]> Texts = new Dictionary<string, string[]>
    {
        ["titulo.juego"] = new[] { "Proyecto Final Simulación", "Simulation Final Project" },

        ["menu.iniciar"] = new[] { "Iniciar juego", "Start game" },
        ["menu.opciones"] = new[] { "Opciones", "Options" },
        ["menu.salir"] = new[] { "Salir del juego", "Quit game" },
        ["menu.salir.confirmar"] = new[] { "¿Seguro que quieres salir?", "Are you sure you want to quit?" },

        ["pausa.titulo"] = new[] { "Pausa", "Pause" },
        ["pausa.reanudar"] = new[] { "Reanudar juego", "Resume game" },
        ["pausa.reiniciar"] = new[] { "Reiniciar nivel", "Restart level" },
        ["pausa.menu"] = new[] { "Volver al menú principal", "Back to main menu" },
        ["pausa.menu.confirmar"] = new[] { "¿Seguro que quieres volver al menú principal?", "Are you sure you want to go back to the main menu?" },

        ["opciones.titulo"] = new[] { "Opciones", "Options" },
        ["opciones.musica"] = new[] { "Volumen música", "Music volume" },
        ["opciones.efectos"] = new[] { "Volumen efectos", "Effects volume" },
        ["opciones.resolucion"] = new[] { "Resolución", "Resolution" },
        ["opciones.ventana"] = new[] { "Modo de ventana", "Window mode" },
        ["opciones.ventana.completa"] = new[] { "Pantalla completa", "Fullscreen" },
        ["opciones.ventana.ventana"] = new[] { "Ventana", "Windowed" },
        ["opciones.ventana.sinbordes"] = new[] { "Ventana sin bordes", "Borderless window" },
        ["opciones.idioma"] = new[] { "Idioma", "Language" },
        ["opciones.volver"] = new[] { "Volver", "Back" },

        ["confirmar.si"] = new[] { "Sí", "Yes" },
        ["confirmar.no"] = new[] { "No", "No" },
    };

    // Se dispara cuando cambia el idioma
    public static event Action Changed;

    public static Language Current
    {
        get => (Language)Mathf.Clamp(PlayerPrefs.GetInt(LanguageKey, (int)Language.Spanish), 0, LanguageNames.Length - 1);
        set
        {
            if (value == Current) return;

            PlayerPrefs.SetInt(LanguageKey, (int)value);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }

    public static string Get(string key)
    {
        if (!Texts.TryGetValue(key, out string[] translations))
        {
            Debug.LogWarning("Localization: falta la clave '" + key + "'");
            return key;
        }

        return translations[(int)Current];
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        // Con "Reload Domain" desactivado los eventos estáticos sobrevivirían entre sesiones de Play
        Changed = null;
    }
}
