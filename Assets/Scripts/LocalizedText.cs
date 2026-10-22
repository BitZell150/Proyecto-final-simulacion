using UnityEngine;
using UnityEngine.UI;

// Pone en el Text el texto de su clave en el idioma actual y lo cambia cuando cambia el idioma.
[RequireComponent(typeof(Text))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string key;

    private Text text;

    private void Awake()
    {
        text = GetComponent<Text>();
    }

    // También se refresca al activarse: los menús cerrados no oyen el cambio de idioma
    private void OnEnable()
    {
        Localization.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        Localization.Changed -= Refresh;
    }

    private void Refresh()
    {
        text.text = Localization.Get(key);
    }
}
