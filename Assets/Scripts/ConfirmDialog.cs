using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Cartel de confirmación con botones "Sí" y "No", reutilizable en cualquier menú.
// Quien lo use llama a Show(mensaje, siAcepta, siCancela); "No", B (mando) y Esc (teclado) cancelan.
public class ConfirmDialog : MonoBehaviour
{
    [SerializeField] private Text messageText;
    [SerializeField] private GameObject noButton;

    private Action onYes;
    private Action onNo;

    public bool IsOpen => gameObject.activeSelf;

    // Frame en el que se cerró por última vez; sirve para que B/Esc no cuenten dos veces
    public int ClosedFrame { get; private set; } = -1;

    public void Show(string message, Action yes, Action no)
    {
        messageText.text = message;
        onYes = yes;
        onNo = no;

        gameObject.SetActive(true);

        // "No" queda seleccionado por defecto para evitar aceptar por error
        MenuNavigation.Select(noButton);
    }

    // Botón "Sí"
    public void Confirm()
    {
        Action callback = onYes;
        Hide();
        callback?.Invoke();
    }

    // Botón "No", B o Esc
    public void Cancel()
    {
        Action callback = onNo;
        Hide();
        callback?.Invoke();
    }

    // Cierra el cartel sin avisar a nadie (por ejemplo, al reanudar con Start)
    public void Close()
    {
        if (IsOpen) Hide();
    }

    private void Update()
    {
        bool backPressed = Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
        bool escapePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

        if (backPressed || escapePressed) Cancel();
    }

    private void Hide()
    {
        gameObject.SetActive(false);
        ClosedFrame = Time.frameCount;
        onYes = null;
        onNo = null;
    }
}
