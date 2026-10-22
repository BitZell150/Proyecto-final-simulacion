using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Menú de opciones compartido por el menú principal y el menú de pausa.
// Quien lo use llama a Open() y escucha Closed; B (mando) y Esc (teclado) lo cierran.
public class OptionsMenu : MonoBehaviour
{
    [Header("Selectores")]
    [SerializeField] private OptionSelector musicSelector;
    [SerializeField] private OptionSelector effectsSelector;
    [SerializeField] private OptionSelector resolutionSelector;
    [SerializeField] private OptionSelector windowModeSelector;
    [SerializeField] private OptionSelector languageSelector;

    [Header("Selección inicial (mando/teclado)")]
    [SerializeField] private GameObject firstSelected;

    public event Action Closed;

    public bool IsOpen => gameObject.activeSelf;

    // Frame en el que se cerró por última vez; sirve para que B/Esc no cuenten dos veces
    public int ClosedFrame { get; private set; } = -1;

    // Se ejecuta la primera vez que se abre el menú
    private void Awake()
    {
        musicSelector.Setup(VolumeLabels(), 0);
        effectsSelector.Setup(VolumeLabels(), 0);

        string[] resolutionLabels = new string[GameSettings.Resolutions.Length];
        for (int i = 0; i < resolutionLabels.Length; i++)
        {
            resolutionLabels[i] = GameSettings.ResolutionLabel(i);
        }

        resolutionSelector.Setup(resolutionLabels, 0);
        windowModeSelector.Setup(GameSettings.WindowModeNames, 0);
        languageSelector.Setup(Localization.LanguageNames, 0);

        musicSelector.IndexChanged += index => GameSettings.MusicVolume = index * GameSettings.VolumeStep;
        effectsSelector.IndexChanged += index => GameSettings.EffectsVolume = index * GameSettings.VolumeStep;
        resolutionSelector.IndexChanged += index => GameSettings.ResolutionIndex = index;
        windowModeSelector.IndexChanged += index => GameSettings.WindowModeIndex = index;
        languageSelector.IndexChanged += index => Localization.Current = (Language)index;
    }

    // Cada vez que se abre muestra los valores guardados
    private void OnEnable()
    {
        musicSelector.SetIndex(GameSettings.MusicVolume / GameSettings.VolumeStep, false);
        effectsSelector.SetIndex(GameSettings.EffectsVolume / GameSettings.VolumeStep, false);
        resolutionSelector.SetIndex(GameSettings.ResolutionIndex, false);
        languageSelector.SetIndex((int)Localization.Current, false);
        RefreshWindowModeLabels();

        Localization.Changed += RefreshWindowModeLabels;
    }

    private void OnDisable()
    {
        Localization.Changed -= RefreshWindowModeLabels;
    }

    // Los nombres de los modos de ventana cambian con el idioma
    private void RefreshWindowModeLabels()
    {
        windowModeSelector.Setup(GameSettings.WindowModeNames, GameSettings.WindowModeIndex);
    }

    private void Update()
    {
        // B en mando o Esc en teclado: retroceder
        bool backPressed = Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
        bool escapePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

        if (backPressed || escapePressed) Close();
    }

    public void Open()
    {
        gameObject.SetActive(true);
        MenuNavigation.Select(firstSelected);
    }

    // Botón "Volver"
    public void Close()
    {
        if (!IsOpen) return;

        gameObject.SetActive(false);
        ClosedFrame = Time.frameCount;
        Closed?.Invoke();
    }

    // De 0 a 100 en intervalos de 10
    private static string[] VolumeLabels()
    {
        int count = GameSettings.MaxVolume / GameSettings.VolumeStep + 1;
        string[] labels = new string[count];
        for (int i = 0; i < count; i++)
        {
            labels[i] = (i * GameSettings.VolumeStep).ToString();
        }

        return labels;
    }
}
