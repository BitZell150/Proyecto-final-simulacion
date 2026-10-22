using System;
using UnityEngine;

// Opciones del juego (volumen de música y efectos, resolución y modo de ventana).
// Se guardan con PlayerPrefs, así que se conservan al cerrar el juego.
public static class GameSettings
{
    public const int MinVolume = 0;
    public const int MaxVolume = 100;
    public const int VolumeStep = 10;

    public static readonly Vector2Int[] Resolutions =
    {
        new Vector2Int(1920, 1080),
        new Vector2Int(1600, 900),
        new Vector2Int(1366, 768),
        new Vector2Int(1280, 720),
        new Vector2Int(1024, 768),
        new Vector2Int(800, 600),
    };

    // Nombres de los modos de ventana en el idioma actual (mismo orden que WindowModes)
    public static string[] WindowModeNames
    {
        get
        {
            var names = new string[WindowModeKeys.Length];
            for (int i = 0; i < names.Length; i++)
            {
                names[i] = Localization.Get(WindowModeKeys[i]);
            }

            return names;
        }
    }

    private static readonly string[] WindowModeKeys =
    {
        "opciones.ventana.completa",
        "opciones.ventana.ventana",
        "opciones.ventana.sinbordes",
    };

    private static readonly FullScreenMode[] WindowModes =
    {
        FullScreenMode.ExclusiveFullScreen,
        FullScreenMode.Windowed,
        FullScreenMode.FullScreenWindow,
    };

    private const string MusicKey = "Options.MusicVolume";
    private const string EffectsKey = "Options.EffectsVolume";
    private const string ResolutionKey = "Options.Resolution";
    private const string WindowModeKey = "Options.WindowMode";

    // Se dispara cada vez que cambia cualquier opción
    public static event Action Changed;

    public static int MusicVolume
    {
        get => GetVolume(MusicKey);
        set => SetVolume(MusicKey, value);
    }

    public static int EffectsVolume
    {
        get => GetVolume(EffectsKey);
        set => SetVolume(EffectsKey, value);
    }

    public static int ResolutionIndex
    {
        get
        {
            if (PlayerPrefs.HasKey(ResolutionKey))
            {
                return Mathf.Clamp(PlayerPrefs.GetInt(ResolutionKey), 0, Resolutions.Length - 1);
            }

            return ClosestResolutionIndex(Screen.width, Screen.height);
        }
        set => SaveDisplay(Mathf.Clamp(value, 0, Resolutions.Length - 1), WindowModeIndex);
    }

    public static int WindowModeIndex
    {
        get
        {
            if (PlayerPrefs.HasKey(WindowModeKey))
            {
                return Mathf.Clamp(PlayerPrefs.GetInt(WindowModeKey), 0, WindowModes.Length - 1);
            }

            return IndexOfCurrentWindowMode();
        }
        set => SaveDisplay(ResolutionIndex, Mathf.Clamp(value, 0, WindowModes.Length - 1));
    }

    public static string ResolutionLabel(int index)
    {
        Vector2Int resolution = Resolutions[index];
        return resolution.x + " x " + resolution.y;
    }

    // Aplica la resolución y el modo de ventana guardados.
    // En el editor Screen.SetResolution no cambia la vista Game, por eso solo se aplica en un build.
    public static void ApplyDisplay()
    {
        if (Application.isEditor) return;

        Vector2Int resolution = Resolutions[ResolutionIndex];
        Screen.SetResolution(resolution.x, resolution.y, WindowModes[WindowModeIndex]);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        // Con "Reload Domain" desactivado los eventos estáticos sobrevivirían entre sesiones de Play
        Changed = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySavedDisplayOnStartup()
    {
        if (PlayerPrefs.HasKey(ResolutionKey) && PlayerPrefs.HasKey(WindowModeKey))
        {
            ApplyDisplay();
        }
    }

    private static int GetVolume(string key)
    {
        return SnapVolume(PlayerPrefs.GetInt(key, MaxVolume));
    }

    private static void SetVolume(string key, int value)
    {
        PlayerPrefs.SetInt(key, SnapVolume(value));
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    // Deja el volumen en múltiplos de 10 entre 0 y 100
    private static int SnapVolume(int value)
    {
        int snapped = Mathf.RoundToInt(value / (float)VolumeStep) * VolumeStep;
        return Mathf.Clamp(snapped, MinVolume, MaxVolume);
    }

    private static void SaveDisplay(int resolutionIndex, int windowModeIndex)
    {
        PlayerPrefs.SetInt(ResolutionKey, resolutionIndex);
        PlayerPrefs.SetInt(WindowModeKey, windowModeIndex);
        PlayerPrefs.Save();

        ApplyDisplay();
        if (Application.isEditor)
        {
            Debug.Log("Opciones: " + ResolutionLabel(resolutionIndex) + ", " + WindowModeNames[windowModeIndex]
                + " (guardado; en el editor la vista Game no cambia, se ve en un build)");
        }

        Changed?.Invoke();
    }

    private static int ClosestResolutionIndex(int width, int height)
    {
        int best = 0;
        long bestDistance = long.MaxValue;
        for (int i = 0; i < Resolutions.Length; i++)
        {
            long dx = Resolutions[i].x - width;
            long dy = Resolutions[i].y - height;
            long distance = dx * dx + dy * dy;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }

        return best;
    }

    private static int IndexOfCurrentWindowMode()
    {
        switch (Screen.fullScreenMode)
        {
            case FullScreenMode.ExclusiveFullScreen: return 0;
            case FullScreenMode.Windowed: return 1;
            default: return 2; // FullScreenWindow y MaximizedWindow
        }
    }
}
