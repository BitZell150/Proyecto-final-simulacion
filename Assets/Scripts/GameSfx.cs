using UnityEngine;

// Sonidos sueltos del juego (salto, muerte, victoria, flecha y cambios de gravedad). Se crea solo al iniciar el juego y
// sobrevive a los cambios de escena: así la muerte sigue sonando aunque el nivel se reinicie enseguida.
// Los archivos están en Resources/Audio/SFX y el volumen sigue la opción "Volumen efectos".
public class GameSfx : MonoBehaviour
{
    private const string JumpPath = "Audio/SFX/saltar";
    private const string DeathPath = "Audio/SFX/muerte";
    private const string VictoryPath = "Audio/SFX/victoria";
    private const string ArrowPath = "Audio/SFX/flecha";
    private const string GravityFlipPath = "Audio/SFX/gravedad_cambio";
    private const string GravityDownPath = "Audio/SFX/gravedad_abajo";
    private const string GravityUpPath = "Audio/SFX/gravedad_arriba";

    // Dos rutas del controlador pueden pedir el mismo salto en frames seguidos: se suena una sola vez
    private const float JumpCooldown = 0.15f;

    private static GameSfx instance;

    private AudioSource source;
    private AudioClip jumpClip;
    private AudioClip deathClip;
    private AudioClip victoryClip;
    private AudioClip arrowClip;
    private AudioClip gravityFlipClip;
    private AudioClip gravityDownClip;
    private AudioClip gravityUpClip;
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
        if (instance != null) instance.Play(instance.deathClip, 0.8f);
    }

    // El personaje llega al final del nivel
    public static void PlayVictory()
    {
        if (instance != null) instance.Play(instance.victoryClip, 1f);
    }

    // El personaje dispara una flecha
    public static void PlayArrow()
    {
        if (instance != null) instance.Play(instance.arrowClip, 0.8f);
    }

    // La gravedad cambia de dirección (normal <-> invertida)
    public static void PlayGravityFlip()
    {
        if (instance != null) instance.Play(instance.gravityFlipClip, 0.8f);
    }

    // La gravedad se reduce (menos |g|)
    public static void PlayGravityDown()
    {
        if (instance != null) instance.Play(instance.gravityDownClip, 1f);
    }

    // La gravedad aumenta (más |g|)
    public static void PlayGravityUp()
    {
        if (instance != null) instance.Play(instance.gravityUpClip, 1f);
    }

    private void Awake()
    {
        jumpClip = Resources.Load<AudioClip>(JumpPath);
        deathClip = Resources.Load<AudioClip>(DeathPath);
        victoryClip = Resources.Load<AudioClip>(VictoryPath);
        arrowClip = Resources.Load<AudioClip>(ArrowPath);
        gravityFlipClip = Resources.Load<AudioClip>(GravityFlipPath);
        gravityDownClip = Resources.Load<AudioClip>(GravityDownPath);
        gravityUpClip = Resources.Load<AudioClip>(GravityUpPath);

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
