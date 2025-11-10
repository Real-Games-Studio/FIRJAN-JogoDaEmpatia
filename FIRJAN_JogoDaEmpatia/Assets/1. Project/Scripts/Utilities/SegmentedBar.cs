using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SegmentedBar : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] RectTransform container;
    [SerializeField] GameObject segmentPrefab;      // GameObject modelo para copiar
    [SerializeField] int segments = 16;
    [SerializeField] float spacing = 4f;
    [SerializeField] float fillSpeed, fillDelay;
    [SerializeField] Vector2 padding = new Vector2(6, 6);

    [Header("Visual")]
    [SerializeField] Color onColor = new Color(0.42f, 0.17f, 0.58f); // roxo
    [SerializeField] Color offColor = new Color(0.42f, 0.17f, 0.58f, 0.15f);

    [Header("Controle (opcional)")]
    [SerializeField] Slider slider;              // se quiser controlar por Slider

    readonly List<Image> _segments = new();

    void Awake()
    {
        if (!container) container = (RectTransform)transform;

        var h = container.GetComponent<HorizontalLayoutGroup>();
        if (!h) h = container.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(Mathf.RoundToInt(padding.x), Mathf.RoundToInt(padding.x),
                                   Mathf.RoundToInt(padding.y), Mathf.RoundToInt(padding.y));
        h.spacing = spacing;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = true;
        h.childForceExpandHeight = true;

        BuildSegments();

        if (slider)
        {
            slider.wholeNumbers = true;
            slider.minValue = 0;
            slider.maxValue = segments;
            slider.onValueChanged.AddListener(v => SetValue((int)v));
            SetValue((int)slider.value);
        }
    }

    public void SetValue(int value)
    {
        value = Mathf.Clamp(value, 0, segments);
        for (int i = 0; i < _segments.Count; i++)
            _segments[i].color = (i < value) ? onColor : offColor;
    }

    void BuildSegments()
    {
        // limpar anteriores
        foreach (Transform t in container) Destroy(t.gameObject);
        _segments.Clear();

        for (int i = 0; i < segments; i++)
        {
            GameObject go;
            if (segmentPrefab)
            {
                // usar o prefab como modelo
                go = Instantiate(segmentPrefab, container);
                go.name = $"seg-{i}";
            }
            else
            {
                // fallback: criar do zero se não tiver prefab
                go = new GameObject($"seg-{i}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                go.transform.SetParent(container, false);

                var img = go.GetComponent<Image>();
                img.type = Image.Type.Simple; // usa a sprite padrão do Unity (quadradinho branco)
                img.preserveAspect = false;
            }

            // garantir que tem o componente Image para controlar a cor
            var segmentImage = go.GetComponent<Image>();
            if (segmentImage)
                _segments.Add(segmentImage);
        }

        // inicializa “vazio”
        SetValue(0);
    }

    /// <summary>
    /// Anima uma barra específica até o valor desejado
    /// </summary>
    public System.Collections.IEnumerator AnimateBar(int targetValue)
    {
        int currentValue = 0;

        while (currentValue < targetValue)
        {
            currentValue++;
            SetValue(currentValue);
            yield return new WaitForSeconds(fillSpeed);
        }

        yield return new WaitForSeconds(fillDelay);
    }
}
