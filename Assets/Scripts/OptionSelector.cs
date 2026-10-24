using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Fila de opciones con "< valor >". Va junto a un Button, que es el que recibe la selección
// (mando/teclado); este componente solo cambia el valor con izquierda/derecha o con las flechas.
public class OptionSelector : MonoBehaviour, IMoveHandler
{
    [SerializeField] private Text valueText;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;

    private string[] options = Array.Empty<string>();
    private int index;

    // Se dispara solo cuando el usuario cambia el valor (no con Setup/SetIndex(notify: false))
    public event Action<int> IndexChanged;

    public void Setup(string[] labels, int selectedIndex)
    {
        options = labels;
        SetIndex(selectedIndex, false);
    }

    public void SetIndex(int newIndex, bool notify)
    {
        if (options.Length == 0) return;

        newIndex = Mathf.Clamp(newIndex, 0, options.Length - 1);
        bool changed = newIndex != index;
        index = newIndex;

        valueText.text = options[index];
        previousButton.interactable = index > 0;
        nextButton.interactable = index < options.Length - 1;

        if (changed && notify) IndexChanged?.Invoke(index);
    }

    // Botones "<" y ">"
    public void Previous()
    {
        SetIndex(index - 1, true);
    }

    public void Next()
    {
        SetIndex(index + 1, true);
    }

    public void OnMove(AxisEventData eventData)
    {
        if (eventData.moveDir == MoveDirection.Left)
        {
            Previous();
            eventData.Use();
        }
        else if (eventData.moveDir == MoveDirection.Right)
        {
            Next();
            eventData.Use();
        }
    }
}
