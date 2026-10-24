using UnityEngine;

// Pon este componente en cualquier AudioSource para que respete el volumen de las Opciones.
// El volumen que tenga el AudioSource en el Inspector queda como volumen máximo (100).
[RequireComponent(typeof(AudioSource))]
public class AudioChannelVolume : MonoBehaviour
{
    public enum Channel
    {
        Music,
        Effects,
    }

    [SerializeField] private Channel channel = Channel.Music;

    private AudioSource source;
    private float baseVolume;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        baseVolume = source.volume;
    }

    private void OnEnable()
    {
        GameSettings.Changed += Apply;
        Apply();
    }

    private void OnDisable()
    {
        GameSettings.Changed -= Apply;
    }

    private void Apply()
    {
        int volume = channel == Channel.Music ? GameSettings.MusicVolume : GameSettings.EffectsVolume;
        source.volume = baseVolume * volume / GameSettings.MaxVolume;
    }
}
