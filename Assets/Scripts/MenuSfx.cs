using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Sonidos de los menús. Se crea solo al iniciar el juego y sobrevive a los cambios de escena
// (así el sonido no se corta al cargar un nivel); no hay que poner nada en las escenas.
//   click       -> el usuario navega por el menú (mover la selección con mando/teclado o pasar el mouse).
//   seleccionar -> el usuario elige una opción o cambia un valor en Opciones.
//   volver      -> el usuario retrocede (botón Volver, "No", B o Esc). Sustituye a "seleccionar" si ambos
//                  se piden en el mismo frame (el botón Volver también es una opción que se elige).
// El volumen sigue la opción "Volumen efectos". Los archivos están en Resources/Audio/SFX.
public class MenuSfx : MonoBehaviour
{
    private const string ClickPath = "Audio/SFX/click";
    private const string SelectPath = "Audio/SFX/seleccionar";
    private const string BackPath = "Audio/SFX/volver";

    private static MenuSfx instance;

    private AudioSource source;
    private AudioClip clickClip;
    private AudioClip selectClip;
    private AudioClip backClip;
    private GameObject lastSelected;
    private bool selectPending;
    private bool backPending;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (instance != null) return;

        var sfxObject = new GameObject("MenuSfx");
        DontDestroyOnLoad(sfxObject);
        instance = sfxObject.AddComponent<MenuSfx>();
    }

    // Navegar por el menú
    public static void PlayClick()
    {
        if (instance != null) instance.Play(instance.clickClip);
    }

    // Elegir una opción o cambiar un valor (suena al final del frame, ver PlayBack)
    public static void PlaySelect()
    {
        if (instance != null) instance.selectPending = true;
    }

    // Retroceder en el menú. Suena al final del frame: si en ese mismo frame se pidió también
    // "seleccionar" (por ejemplo, al pulsar el botón Volver), solo suena "volver".
    public static void PlayBack()
    {
        if (instance != null) instance.backPending = true;
    }

    // Marca la selección actual como "ya conocida": el cambio no suena como navegación.
    // Se usa cuando el código (no el usuario) mueve la selección, por ejemplo al abrir un menú.
    public static void SyncSelection()
    {
        if (instance != null) instance.lastSelected = CurrentSelected();
    }

    private void Awake()
    {
        clickClip = Resources.Load<AudioClip>(ClickPath);
        selectClip = Resources.Load<AudioClip>(SelectPath);
        backClip = Resources.Load<AudioClip>(BackPath);

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true; // suena aunque el juego esté en pausa
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
        lastSelected = CurrentSelected();
    }

    // Con mando o teclado, cada vez que la selección pasa a otra opción suena el click.
    // Con el mouse la selección no cuenta (el clic la cambia); ahí el click lo da MenuOption al señalar.
    private void Update()
    {
        GameObject selected = CurrentSelected();
        if (selected == lastSelected) return;

        lastSelected = selected;
        if (selected != null && !InputModeManager.IsPointerMode && selected.GetComponent<MenuOption>() != null)
        {
            PlayClick();
        }
    }

    private void LateUpdate()
    {
        if (backPending) Play(backClip);
        else if (selectPending) Play(selectClip);

        backPending = false;
        selectPending = false;
    }

    private void Play(AudioClip clip)
    {
        if (clip == null) return;

        float volume = GameSettings.EffectsVolume / (float)GameSettings.MaxVolume;
        if (volume <= 0f) return;

        source.PlayOneShot(clip, volume);
    }

    private static GameObject CurrentSelected()
    {
        EventSystem eventSystem = EventSystem.current;
        return eventSystem != null ? eventSystem.currentSelectedGameObject : null;
    }
}
