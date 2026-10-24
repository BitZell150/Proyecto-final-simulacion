using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Opción de menú sin caja. Mientras está seleccionada (mando/teclado) o señalada con el mouse
// muestra una flecha pixel art a la izquierda del texto y su contenido crece ligeramente.
// La flecha no se escala (siempre se ve con píxeles nítidos); solo se desliza con el texto.
public class MenuOption : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Contenido")]
    [SerializeField] private RectTransform content; // lo que crece al resaltar (texto o fila completa)
    [SerializeField] private Text label;            // texto junto al que se coloca la flecha

    [Header("Flecha")]
    [SerializeField] private RectTransform arrow;
    [SerializeField] private float arrowGap = 24f;  // separación entre la flecha y el texto
    [SerializeField] private float arrowOffsetY;

    [Header("Animación")]
    [SerializeField] private float highlightedScale = 1.12f;
    [SerializeField] private float animationSpeed = 16f; // más alto = más rápido

    private bool hovered;
    private float scale = 1f;
    private Image arrowImage;

    private void Awake()
    {
        arrowImage = arrow.GetComponent<Image>();
    }

    private void OnEnable()
    {
        Apply(false, 1f);
    }

    private void OnDisable()
    {
        // Un objeto desactivado no recibe OnPointerExit: se limpia aquí
        hovered = false;
        Apply(false, 1f);
    }

    private void Update()
    {
        bool highlighted = IsHighlighted();
        float target = highlighted ? highlightedScale : 1f;

        // Tiempo sin escala: el menú de pausa deja Time.timeScale en 0
        scale = Mathf.Lerp(scale, target, 1f - Mathf.Exp(-animationSpeed * Time.unscaledDeltaTime));
        if (Mathf.Abs(scale - target) < 0.001f) scale = target;

        Apply(highlighted, scale);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
    }

    private bool IsHighlighted()
    {
        EventSystem eventSystem = EventSystem.current;
        bool selected = eventSystem != null && eventSystem.currentSelectedGameObject == gameObject;
        return selected || hovered;
    }

    private void Apply(bool highlighted, float newScale)
    {
        scale = newScale;
        content.localScale = new Vector3(newScale, newScale, 1f);

        if (arrowImage.enabled != highlighted) arrowImage.enabled = highlighted;
        if (highlighted)
        {
            // La flecha toma el color del texto (por ejemplo, rojo en "Salir del juego")
            arrowImage.color = label.color;
            PlaceArrow();
        }
    }

    // Deja la flecha pegada al borde izquierdo del texto, esté alineado a la izquierda o centrado
    private void PlaceArrow()
    {
        RectTransform labelRect = label.rectTransform;
        Rect rect = labelRect.rect;
        float textWidth = Mathf.Min(label.preferredWidth, rect.width);

        float left;
        switch (label.alignment)
        {
            case TextAnchor.UpperCenter:
            case TextAnchor.MiddleCenter:
            case TextAnchor.LowerCenter:
                left = rect.center.x - textWidth * 0.5f;
                break;
            case TextAnchor.UpperRight:
            case TextAnchor.MiddleRight:
            case TextAnchor.LowerRight:
                left = rect.xMax - textWidth;
                break;
            default:
                left = rect.xMin;
                break;
        }

        // Borde izquierdo del texto en el espacio de esta opción (ya incluye la escala del contenido)
        Vector3 textEdge = labelRect.TransformPoint(new Vector3(left, rect.center.y, 0f));
        Vector3 local = transform.InverseTransformPoint(textEdge);

        // El pivote de la flecha es su borde derecho
        arrow.localPosition = new Vector3(local.x - arrowGap, local.y + arrowOffsetY, 0f);
    }
}
