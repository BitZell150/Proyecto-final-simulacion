using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Alterna entre dos modos de entrada para que los menús nunca muestren dos opciones resaltadas:
// - Modo puntero (mouse): el cursor se ve y solo se resalta lo que el mouse señala.
// - Modo navegación (mando o teclado): el cursor se oculta y los menús dejan de reaccionar al
//   mouse, así que solo se resalta la opción seleccionada. Al mover el mouse se vuelve al modo puntero.
// Se crea solo al iniciar el juego; no hay que poner nada en las escenas.
public class InputModeManager : MonoBehaviour
{
    private const float MouseMoveThreshold = 2f; // píxeles por frame
    private const float StickThreshold = 0.5f;
    private const int ReapplyEveryFrames = 60;   // por si aparece un Canvas nuevo en modo navegación

    private static InputModeManager instance;

    private IDisposable buttonSubscription;
    private bool pointerMode = true;
    private bool navigationButtonPressed;
    private bool pointerButtonPressed;

    // Verdadero mientras se usa el mouse (también antes de que el jugador toque cualquier cosa)
    public static bool IsPointerMode => instance == null || instance.pointerMode;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (instance != null) return;

        var managerObject = new GameObject("InputModeManager");
        DontDestroyOnLoad(managerObject);
        instance = managerObject.AddComponent<InputModeManager>();
    }

    private void OnEnable()
    {
        buttonSubscription = InputSystem.onAnyButtonPress.Call(OnAnyButtonPress);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        buttonSubscription?.Dispose();
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // Deja todo como estaba al salir (por ejemplo, al terminar Play en el editor)
        pointerMode = true;
        Cursor.visible = true;
        SetRaycasters(true);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) ApplyInputMode();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyInputMode();
    }

    private void Update()
    {
        EventSystem eventSystem = EventSystem.current;
        GameObject selected = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
        if (selected != null) MenuNavigation.Preferred = selected;

        bool mouseActive = pointerButtonPressed || MouseMoved();
        bool navigationActive = navigationButtonPressed || StickMoved();
        pointerButtonPressed = false;
        navigationButtonPressed = false;

        if (mouseActive)
        {
            pointerMode = true;
            ApplyInputMode();

            // Con el mouse solo se resalta lo que señala el cursor
            if (selected != null) eventSystem.SetSelectedGameObject(null);
        }
        else if (navigationActive)
        {
            pointerMode = false;
            ApplyInputMode();

            // Al volver al mando/teclado se recupera la última opción seleccionada
            GameObject preferred = MenuNavigation.Preferred;
            if (eventSystem != null && selected == null && preferred != null && preferred.activeInHierarchy)
            {
                eventSystem.SetSelectedGameObject(preferred);
            }
        }
        else if (!pointerMode && Time.frameCount % ReapplyEveryFrames == 0)
        {
            SetRaycasters(false);
        }
    }

    private void OnAnyButtonPress(InputControl control)
    {
        InputDevice device = control.device;
        if (device is Mouse || device is Pen || device is Touchscreen) pointerButtonPressed = true;
        else navigationButtonPressed = true;
    }

    private static bool MouseMoved()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return false;

        return mouse.delta.ReadValue().sqrMagnitude >= MouseMoveThreshold * MouseMoveThreshold
            || mouse.scroll.ReadValue() != Vector2.zero;
    }

    private static bool StickMoved()
    {
        Gamepad gamepad = Gamepad.current;
        return gamepad != null && gamepad.leftStick.ReadValue().sqrMagnitude >= StickThreshold * StickThreshold;
    }

    private void ApplyInputMode()
    {
        if (Cursor.visible != pointerMode) Cursor.visible = pointerMode;
        SetRaycasters(pointerMode);
    }

    // Sin raycasters el módulo de UI no detecta qué señala el mouse (ni hover ni clics)
    private static void SetRaycasters(bool enabled)
    {
        foreach (GraphicRaycaster raycaster in FindObjectsByType<GraphicRaycaster>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (raycaster.enabled != enabled) raycaster.enabled = enabled;
        }
    }
}
