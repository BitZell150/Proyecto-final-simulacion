using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Música de fondo. Se crea sola al iniciar el juego y sobrevive a los cambios de escena.
// Cada escena puede tener su pista (tabla SceneTracks); al cambiar de pista la anterior baja
// poco a poco y la nueva sube. Si la nueva escena usa la misma pista, sigue sonando sin cortarse.
// Las escenas que no están en la tabla no tienen música. El volumen sigue la opción "Volumen música".
// Con el juego en pausa (SetMuffled) la música baja de volumen y se oye "a través de una pared".
// Los archivos están en Resources/Audio/Music.
public class MusicManager : MonoBehaviour
{
    private const string MenuTrack = "Audio/Music/goblins_den";
    private const string LevelTrack = "Audio/Music/lost_in_the_maze";

    // Escena -> (pista, volumen máximo de esa pista antes de aplicar "Volumen música").
    // La ruta es dentro de Resources, sin extensión. Para música en otra escena, añadir una línea.
    private static readonly Dictionary<string, (string path, float volume)> SceneTracks =
        new Dictionary<string, (string path, float volume)>
        {
            { "MainMenu", (MenuTrack, 0.5f) },
            // Esta pista está masterizada muy fuerte (casi 10 veces más que la del menú): por eso su volumen es bajo
            { "Tutorial", (LevelTrack, 0.12f) },
            { "Nivel1", (LevelTrack, 0.12f) },
            { "Nivel2", (LevelTrack, 0.12f) },
        };

    private const float FadeSeconds = 0.6f;

    // En pausa: la música baja al 40 % y se le quitan los agudos (filtro paso bajo) como si sonara tras una pared
    private const float MuffledVolume = 0.4f;
    private const float MuffledCutoff = 600f;   // Hz
    private const float NormalCutoff = 22000f;  // Hz (sin filtrar)
    private const float MuffleSeconds = 0.25f;

    private static MusicManager instance;

    private AudioSource source;
    private AudioLowPassFilter lowPass;
    private AudioClip wanted;       // pista que debe sonar en la escena actual (null = ninguna)
    private float wantedVolume;     // volumen máximo de esa pista
    private float trackVolume;      // volumen máximo de la pista que suena ahora
    private float fade;             // 0 = en silencio, 1 = a todo volumen
    private bool muffled;           // el juego está en pausa
    private float muffle;           // 0 = normal, 1 = amortiguada del todo

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (instance != null) return;

        var musicObject = new GameObject("MusicManager");
        DontDestroyOnLoad(musicObject);
        instance = musicObject.AddComponent<MusicManager>();

        // La primera escena ya cargó antes de crear este objeto
        instance.ChooseTrack(SceneManager.GetActiveScene().name);
    }

    // Pausa del juego: la música baja y se amortigua (true) o vuelve a la normalidad (false)
    public static void SetMuffled(bool value)
    {
        if (instance != null) instance.muffled = value;
    }

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true; // la música sigue en pausa
        source.volume = 0f;

        // El filtro se activa solo mientras la música está amortiguada
        lowPass = gameObject.AddComponent<AudioLowPassFilter>();
        lowPass.cutoffFrequency = NormalCutoff;
        lowPass.enabled = false;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ChooseTrack(scene.name);
    }

    private void ChooseTrack(string sceneName)
    {
        // Una escena nueva empieza sin pausa (por ejemplo, al reiniciar el nivel desde el menú de pausa)
        muffled = false;

        if (SceneTracks.TryGetValue(sceneName, out var track))
        {
            wanted = Resources.Load<AudioClip>(track.path);
            wantedVolume = track.volume;
        }
        else
        {
            wanted = null;
        }
    }

    private void Update()
    {
        float step = Time.unscaledDeltaTime / FadeSeconds;

        if (source.clip != wanted)
        {
            // Pista distinta: primero baja la actual y, en silencio, se cambia
            fade = Mathf.MoveTowards(fade, 0f, step);
            if (fade <= 0f)
            {
                source.Stop();
                source.clip = wanted;
                if (wanted != null) source.Play();
            }
        }
        else if (wanted != null)
        {
            if (!source.isPlaying) source.Play();
            fade = Mathf.MoveTowards(fade, 1f, step);
            trackVolume = wantedVolume; // mientras baja la pista anterior se conserva su propio volumen
        }

        // Pausa: la música baja de volumen y se le quitan los agudos poco a poco
        muffle = Mathf.MoveTowards(muffle, muffled ? 1f : 0f, Time.unscaledDeltaTime / MuffleSeconds);
        lowPass.enabled = muffle > 0.001f;
        if (lowPass.enabled)
        {
            // El corte baja de forma exponencial: así la transición suena pareja
            lowPass.cutoffFrequency = NormalCutoff * Mathf.Pow(MuffledCutoff / NormalCutoff, muffle);
        }

        float music = GameSettings.MusicVolume / (float)GameSettings.MaxVolume;
        source.volume = trackVolume * music * fade * Mathf.Lerp(1f, MuffledVolume, muffle);
    }
}
