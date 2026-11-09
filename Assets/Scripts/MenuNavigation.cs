using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

// Selección de botones para mando y teclado. Mientras se usa el mouse no se selecciona nada
// (solo se resalta lo que el cursor señala); la opción se recupera al volver al mando o teclado.
public static class MenuNavigation
{
    // Opción que debe quedar seleccionada la próxima vez que se use mando o teclado
    public static GameObject Preferred { get; set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Preferred = null;
    }

    // Si la escena no trae EventSystem propio, se crea uno para poder navegar con mando y teclado
    public static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;

        var eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

    public static void Select(GameObject target)
    {
        Preferred = target;
        if (EventSystem.current == null) return;

        EventSystem.current.SetSelectedGameObject(null);
        if (!InputModeManager.IsPointerMode) EventSystem.current.SetSelectedGameObject(target);

        // Esta selección la hace el código (abrir un menú, volver atrás), no el usuario: sin click
        MenuSfx.SyncSelection();
    }
}
