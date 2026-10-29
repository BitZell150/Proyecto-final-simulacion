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
    [SerializeField] private OptionsMenu optionsMenu;

    [Header("Botones seleccionados al abrir cada panel")]
    [SerializeField] private GameObject resumeButton;
    [SerializeField] private GameObject optionsButton;

    [Header("Confirmación al volver al menú principal")]
    [SerializeField] private ConfirmDialog confirmDialog;
    [SerializeField] private GameObject mainMenuButton;

    [Header("Escenas")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool isPaused;
    private Coroutine restoreTimeRoutine;

    private void OnEnable()
    {
        optionsMenu.Closed += OnOptionsClosed;
    }

    private void Start()
    {
        EnsureEventSystem();
        pausePanel.SetActive(false);
    }

    private void Update()
    {
        Gamepad gamepad = Gamepad.current;
        Keyboard keyboard = Keyboard.current;

        bool startPressed = gamepad != null && gamepad.startButton.wasPressedThisFrame;
        bool escapePressed = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;

        if (!isPaused)
        {
            if (startPressed || escapePressed) Pause();
            return;
        }

        if (startPressed)
        {
            Resume();
            return;
        }

        // B y Esc dentro de Opciones o del cartel de confirmación los manejan esos menús; si acaban
        // de cerrarse en este mismo frame, ese Esc no debe contar también para reanudar
        bool optionsBusy = optionsMenu.IsOpen || optionsMenu.ClosedFrame == Time.frameCount;
        bool dialogBusy = confirmDialog.IsOpen || confirmDialog.ClosedFrame == Time.frameCount;
        if (escapePressed && !optionsBusy && !dialogBusy) Resume();

        // B en la pausa principal no hace nada: solo retrocede dentro de Opciones y del cartel
    }

    private void OnDisable()
    {
        optionsMenu.Closed -= OnOptionsClosed;

        // Evita que el juego quede congelado si se sale de Play estando en pausa
        if (isPaused || restoreTimeRoutine != null) Time.timeScale = 1f;
    }

    // Botón "Reanudar juego"
    public void Resume()
    {
        isPaused = false;
        pausePanel.SetActive(false);
        optionsMenu.Close();
        confirmDialog.Close();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        // El tiempo se restaura un par de frames después para que el mismo botón que aceptó
        // "Reanudar" (A) no cuente también como salto del personaje
        restoreTimeRoutine = StartCoroutine(RestoreTimeNextFrame());
    }

    // Botón "Opciones"
    public void OpenOptions()
    {
        pausePanel.SetActive(false);
        optionsMenu.Open();
    }

    // Botón "Reiniciar nivel": vuelve a cargar la escena actual desde cero
    public void RestartLevel()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Botón "Volver al menú principal": primero pide confirmación
    public void BackToMainMenu()
    {
        confirmDialog.Show(Localization.Get("pausa.menu.confirmar"), LoadMainMenu, OnBackToMenuCancelled);
    }

    private void LoadMainMenu()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // Al cancelar, la selección vuelve al botón "Volver al menú principal"
    private void OnBackToMenuCancelled()
    {
        MenuNavigation.Select(mainMenuButton);
    }

    private void Pause()
    {
        if (restoreTimeRoutine != null)
        {
            StopCoroutine(restoreTimeRoutine);
            restoreTimeRoutine = null;
        }

        isPaused = true;
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
        MenuNavigation.Select(resumeButton);
    }

    // Al cerrar Opciones se vuelve a la pausa, con "Opciones" seleccionado
    private void OnOptionsClosed()
    {
        if (!isPaused) return;

        pausePanel.SetActive(true);
        MenuNavigation.Select(optionsButton);
    }

    private IEnumerator RestoreTimeNextFrame()
    {
        yield return null;
        yield return null;
        Time.timeScale = 1f;
        restoreTimeRoutine = null;
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
