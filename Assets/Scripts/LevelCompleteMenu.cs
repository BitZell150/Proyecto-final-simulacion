using UnityEngine;
using UnityEngine.SceneManagement;

// Menú "Nivel completado": lo abre LevelManager cuando el jugador cruza la puerta de salida.
// Detiene el juego y ofrece pasar al siguiente nivel o volver al menú principal (con cartel de confirmación).
// En el último nivel (la siguiente escena es el menú principal) solo se ofrece volver al menú
// y el título pasa a ser el de fin del juego.
public class LevelCompleteMenu : MonoBehaviour
{
    // Evita que un salto con A/Espacio justo al llegar a la puerta elija una opción sin querer
    private const float InputDelay = 0.4f;

    [SerializeField] private GameObject panel;
    [SerializeField] private LocalizedText titleText;
    [SerializeField] private RectTransform nextButton;
    [SerializeField] private RectTransform mainMenuButton;
    [SerializeField] private ConfirmDialog confirmDialog;

    [Header("Títulos (claves de Localization)")]
    [Tooltip("Título normal; cada nivel puede poner el suyo (por ejemplo, el Tutorial).")]
    [SerializeField] private string titleKey = "nivel.completado";
    [Tooltip("Título cuando no hay siguiente nivel (fin del juego).")]
    [SerializeField] private string lastLevelTitleKey = "nivel.fin";

    [Header("Escenas")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private string nextScene;
    private float openedAt;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Start()
    {
        MenuNavigation.EnsureEventSystem();
    }

    private void OnDisable()
    {
        // Evita que el juego quede congelado si se sale de Play con el menú abierto
        if (isOpen) Time.timeScale = 1f;
    }

    public void Show(string next)
    {
        nextScene = next;
        bool hasNext = !string.IsNullOrEmpty(next) && next != mainMenuSceneName;

        titleText.SetKey(hasNext ? titleKey : lastLevelTitleKey);

        // Sin siguiente nivel, "Volver al menú principal" ocupa el primer lugar de la lista
        nextButton.gameObject.SetActive(hasNext);
        if (!hasNext) mainMenuButton.anchoredPosition = nextButton.anchoredPosition;

        isOpen = true;
        openedAt = Time.unscaledTime;
        Time.timeScale = 0f;
        panel.SetActive(true);
        MenuNavigation.Select((hasNext ? nextButton : mainMenuButton).gameObject);
    }

    // Botón "Siguiente nivel"
    public void NextLevel()
    {
        if (!CanChoose()) return;

        Leave(nextScene);
    }

    // Botón "Volver al menú principal": primero pide confirmación
    public void BackToMainMenu()
    {
        if (!CanChoose()) return;

        confirmDialog.Show(Localization.Get("pausa.menu.confirmar"), LeaveToMainMenu, OnBackToMenuCancelled);
    }

    private void LeaveToMainMenu()
    {
        Leave(mainMenuSceneName);
    }

    // Al cancelar, la selección vuelve al botón "Volver al menú principal"
    private void OnBackToMenuCancelled()
    {
        MenuNavigation.Select(mainMenuButton.gameObject);
    }

    private bool CanChoose()
    {
        return isOpen && Time.unscaledTime >= openedAt + InputDelay;
    }

    private void Leave(string sceneName)
    {
        isOpen = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}
