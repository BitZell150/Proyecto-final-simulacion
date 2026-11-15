using UnityEngine;

// Pasos del personaje: mientras el jugador lo mueve sobre el suelo suena un paso cada cierto tiempo.
// Cada paso es uno de los clips de Resources/Audio/SFX/Pasos elegido al azar (nunca el mismo dos
// veces seguidas) y con un tono y volumen ligeramente distintos, para que no suene repetitivo.
// PlayerController2D lo añade solo al personaje; no hay que ponerlo en las escenas.
// Para tener más variantes basta con soltar más .wav en esa carpeta.
[RequireComponent(typeof(PlayerController2D))]
public class PlayerFootsteps : MonoBehaviour
{
    private const string ClipsFolder = "Audio/SFX/Pasos";

    [SerializeField] private float stepInterval = 0.25f;    // segundos entre pasos (la animación de correr da 2 pasos cada 0,5 s)
    [SerializeField] [Range(0f, 1f)] private float volume = 0.6f; // volumen máximo, antes de aplicar "Volumen efectos"
    [SerializeField] private float pitchVariation = 0.06f;  // el tono varía entre 1 - x y 1 + x
    [SerializeField] private float volumeVariation = 0.15f; // el volumen baja hasta un x% al azar

    private PlayerController2D controller;
    private Rigidbody2D body;
    private AudioSource source;
    private AudioClip[] clips;
    private int lastClip = -1;
    private float nextStepTime;

    private void Awake()
    {
        controller = GetComponent<PlayerController2D>();
        body = GetComponent<Rigidbody2D>();
        clips = Resources.LoadAll<AudioClip>(ClipsFolder);

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
    }

    private void Update()
    {
        // En pausa, con el personaje congelado (al morir o terminar el nivel) o en el aire no hay pasos
        if (clips.Length == 0 || Time.timeScale == 0f || !controller.enabled || !body.simulated || !controller.IsGrounded) return;

        // Suena mientras el jugador mantiene el movimiento, aunque el personaje choque contra una pared
        // y no avance (sigue "caminando" en el sitio); al soltar el movimiento se detiene
        if (Mathf.Abs(controller.HorizontalInput) < 0.1f) return;

        // Si no ha pasado el tiempo de un paso desde el último, se espera (así no se acumulan al parar y arrancar)
        if (Time.time < nextStepTime) return;

        nextStepTime = Time.time + stepInterval;
        PlayStep();
    }

    private void PlayStep()
    {
        float effects = GameSettings.EffectsVolume / (float)GameSettings.MaxVolume;
        if (effects <= 0f) return;

        int index = Random.Range(0, clips.Length);
        if (clips.Length > 1 && index == lastClip) index = (index + 1 + Random.Range(0, clips.Length - 1)) % clips.Length;
        lastClip = index;

        source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        source.PlayOneShot(clips[index], volume * effects * (1f - Random.Range(0f, volumeVariation)));
    }
}
