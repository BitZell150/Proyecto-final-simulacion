using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Música de fondo. Se crea sola al iniciar el juego y sobrevive a los cambios de escena.
// Cada escena puede tener su pista (tabla SceneTracks); al cambiar de pista la anterior baja
// poco a poco y la nueva sube. Si la nueva escena usa la misma pista, sigue sonando sin cortarse.
// Las escenas que no están en la tabla no tienen música. El volumen sigue la opción "Volumen música".
// Los archivos están en Resources/Audio/Music.
public class MusicManager : MonoBehaviour
{
    // Escena -> pista (ruta dentro de Resources, sin extensión). Para música en un nivel, añadir una línea.
    private static readonly Dictionary<string, string> SceneTracks = new Dictionary<string, string>
    {
        { "MainMenu", "Audio/Music/goblins_den" },
    };

    private const float BaseVolume = 0.5f; // volumen máximo de la música, antes de aplicar "Volumen música"
    private const float FadeSeconds = 0.6f;

    private static MusicManager instance;

    private AudioSource source;
    private AudioClip wanted;  // pista que debe sonar en la escena actual (null = ninguna)
    private float fade;        // 0 = en silencio, 1 = a todo volumen

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

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true; // la música sigue en pausa
        source.volume = 0f;
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
        wanted = SceneTracks.TryGetValue(sceneName, out string path) ? Resources.Load<AudioClip>(path) : null;
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
        }

        float music = GameSettings.MusicVolume / (float)GameSettings.MaxVolume;
        source.volume = BaseVolume * music * fade;
    }
}
