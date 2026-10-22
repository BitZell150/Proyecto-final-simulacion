using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Escenas")]
    [SerializeField] private string tutorialSceneName = "Tutorial";

    // Botón "Iniciar juego"
    public void PlayGame()
    {
        SceneManager.LoadScene(tutorialSceneName);
    }

    // Botón "Opciones" (pendiente: todavía no hay pantalla de opciones)
    public void OpenOptions()
    {
        Debug.Log("Opciones: todavía no implementado.");
    }

    // Botón "Salir del juego"
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
