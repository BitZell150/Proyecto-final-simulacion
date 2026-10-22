using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Botones seleccionados al abrir cada panel")]
    [SerializeField] private GameObject resumeButton;
    [SerializeField] private GameObject optionsButton;
    [SerializeField] private GameObject optionsBackButton;

    [Header("Escenas")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool isPaused;
    private bool optionsOpen;
    private Coroutine restoreTimeRoutine;

    private void Start()
    {
        EnsureEventSystem();
        SetPanels(false, false);
    }

    private void Update()
    {
        Gamepad gamepad = Gamepad.current;
        Keyboard keyboard = Keyboard.current;

        bool startPressed = gamepad != null && gamepad.startButton.wasPressedThisFrame;
        bool backPressed = gamepad != null && gamepad.buttonEast.wasPressedThisFrame;
        bool escapePressed = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;

        if (!isPaused)
        {
            if (startPressed || escapePressed) Pause();
            return;
        }

        if (startPressed)
        {
            Resume();
        }
        else if (escapePressed)
        {
            if (optionsOpen) CloseOptions();
            else Resume();
        }
        else if (backPressed && optionsOpen)
        {
            // B solo retrocede dentro de los menús, nunca saca del juego ni de la pausa
            CloseOptions();
        }
    }

    private void OnDisable()
    {
        // Evita que el juego quede congelado si se sale de Play estando en pausa
        if (isPaused || restoreTimeRoutine != null) Time.timeScale = 1f;
    }

    // Botón "Reanudar juego"
    public void Resume()
    {
        isPaused = false;
        optionsOpen = false;
        SetPanels(false, false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        // El tiempo se restaura un frame después para que el mismo botón que aceptó
        // "Reanudar" (A) no cuente también como salto del personaje
        restoreTimeRoutine = StartCoroutine(RestoreTimeNextFrame());
    }

    // Botón "Opciones"
    public void OpenOptions()
    {
        optionsOpen = true;
        SetPanels(false, true);
        Select(optionsBackButton);
    }

    // Botón "Volver" de Opciones (o botón B / Esc)
    public void CloseOptions()
    {
        optionsOpen = false;
        SetPanels(true, false);
        Select(optionsButton);
    }

    // Botón "Volver al menú principal"
    public void BackToMainMenu()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void Pause()
    {
        if (restoreTimeRoutine != null)
        {
            StopCoroutine(restoreTimeRoutine);
            restoreTimeRoutine = null;
        }

        isPaused = true;
        optionsOpen = false;
        Time.timeScale = 0f;
        SetPanels(true, false);
        Select(resumeButton);
    }

    private IEnumerator RestoreTimeNextFrame()
    {
        yield return null;
        yield return null;
        Time.timeScale = 1f;
        restoreTimeRoutine = null;
    }

    private void SetPanels(bool showPause, bool showOptions)
    {
        pausePanel.SetActive(showPause);
        optionsPanel.SetActive(showOptions);
    }

    private static void Select(GameObject target)
    {
        if (EventSystem.current == null) return;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(target);
    }

    // Si el nivel no trae EventSystem propio, se crea uno para poder navegar con mando y teclado
    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;

        var eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }
}
