using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Escenas")]
    [SerializeField] private string tutorialSceneName = "Tutorial";

    [Header("Opciones")]
    [SerializeField] private OptionsMenu optionsMenu;
    [SerializeField] private GameObject optionsButton;

    [Header("Confirmación al salir")]
    [SerializeField] private ConfirmDialog confirmDialog;
    [SerializeField] private GameObject quitButton;

    private void OnEnable()
    {
        if (optionsMenu != null) optionsMenu.Closed += OnOptionsClosed;
    }

    private void OnDisable()
    {
        if (optionsMenu != null) optionsMenu.Closed -= OnOptionsClosed;
    }

    // Botón "Iniciar juego"
    public void PlayGame()
    {
        SceneManager.LoadScene(tutorialSceneName);
    }

    // Botón "Opciones"
    public void OpenOptions()
    {
        optionsMenu.Open();
    }

    // Botón "Salir del juego": primero pide confirmación
    public void QuitGame()
    {
        confirmDialog.Show(Localization.Get("menu.salir.confirmar"), ConfirmQuit, OnQuitCancelled);
    }

    private void ConfirmQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Al cancelar, la selección vuelve al botón "Salir del juego"
    private void OnQuitCancelled()
    {
        MenuNavigation.Select(quitButton);
    }

    // Al cerrar Opciones, la selección vuelve al botón "Opciones"
    private void OnOptionsClosed()
    {
        MenuNavigation.Select(optionsButton);
    }
}
