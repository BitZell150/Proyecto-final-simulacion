using UnityEngine;

// Sonidos sueltos del juego (salto y muerte del personaje). Se crea solo al iniciar el juego y
// sobrevive a los cambios de escena: así la muerte sigue sonando aunque el nivel se reinicie enseguida.
// Los archivos están en Resources/Audio/SFX y el volumen sigue la opción "Volumen efectos".
public class GameSfx : MonoBehaviour
{
    private const string JumpPath = "Audio/SFX/saltar";
    private const string DeathPath = "Audio/SFX/muerte";

    // Dos rutas del controlador pueden pedir el mismo salto en frames seguidos: se suena una sola vez
    private const float JumpCooldown = 0.15f;

    private static GameSfx instance;

    private AudioSource source;
    private AudioClip jumpClip;
    private AudioClip deathClip;
    private float lastJumpTime = -10f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (instance != null) return;

        var sfxObject = new GameObject("GameSfx");
        DontDestroyOnLoad(sfxObject);
        instance = sfxObject.AddComponent<GameSfx>();
    }

    // El personaje salta
    public static void PlayJump()
    {
        if (instance == null || Time.unscaledTime < instance.lastJumpTime + JumpCooldown) return;

        instance.lastJumpTime = Time.unscaledTime;
        instance.Play(instance.jumpClip, 0.7f);
    }

    // El personaje muere
    public static void PlayDeath()
    {
        if (instance != null) instance.Play(instance.deathClip, 1f);
    }

    private void Awake()
    {
        jumpClip = Resources.Load<AudioClip>(JumpPath);
        deathClip = Resources.Load<AudioClip>(DeathPath);

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
    }

    // 'volume' es el volumen máximo de ese sonido, antes de aplicar "Volumen efectos"
    private void Play(AudioClip clip, float volume)
    {
        if (clip == null) return;

        float effects = GameSettings.EffectsVolume / (float)GameSettings.MaxVolume;
        if (effects <= 0f) return;

        source.PlayOneShot(clip, volume * effects);
    }
}
