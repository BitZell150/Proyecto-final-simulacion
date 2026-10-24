using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Muestra en el HUD los usos de gravedad que le quedan al jugador ("Gravedad: 2/3").
// Parpadea en rojo si se intenta invertir sin usos o en el aire.
[RequireComponent(typeof(Text))]
public class GravityUsesHud : MonoBehaviour
{
    [SerializeField] private GravityInverter inverter;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color emptyColor = new Color(1f, 0.4f, 0.4f);
    [SerializeField] private Color invertedColor = new Color(0.55f, 0.85f, 1f);

    private Text text;
    private Coroutine flashRoutine;

    private void Awake()
    {
        text = GetComponent<Text>();
        if (inverter == null) inverter = FindAnyObjectByType<GravityInverter>();
    }

    private void OnEnable()
    {
        Localization.Changed += Refresh;
        if (inverter != null)
        {
            inverter.UsesChanged += Refresh;
            inverter.GravityChanged += Refresh;
            inverter.Denied += Flash;
        }

        Refresh();
    }

    // Se repite en Start por si el GravityInverter aún no había hecho su Awake en OnEnable
    private void Start()
    {
        Refresh();
    }

    private void OnDisable()
    {
        Localization.Changed -= Refresh;
        if (inverter != null)
        {
            inverter.UsesChanged -= Refresh;
            inverter.GravityChanged -= Refresh;
            inverter.Denied -= Flash;
        }
    }

    private void Refresh()
    {
        if (inverter == null)
        {
            text.text = string.Empty;
            return;
        }

        string label = Localization.Get(inverter.IsInverted ? "hud.gravedad.invertida" : "hud.gravedad");
        text.text = label + ": " + inverter.UsesLeft + "/" + inverter.MaxUses;
        text.color = CurrentColor();
    }

    private Color CurrentColor()
    {
        if (inverter.UsesLeft == 0) return emptyColor;
        return inverter.IsInverted ? invertedColor : normalColor;
    }

    private void Flash()
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        for (int i = 0; i < 3; i++)
        {
            text.color = emptyColor;
            yield return new WaitForSecondsRealtime(0.08f);
            text.color = CurrentColor();
            yield return new WaitForSecondsRealtime(0.08f);
        }

        flashRoutine = null;
    }
}
