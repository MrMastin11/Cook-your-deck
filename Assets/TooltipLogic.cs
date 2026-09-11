using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipLogic : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Assign the tooltip GameObject (UI panel, prefab instance, etc.).")]
    [SerializeField] private GameObject tooltip;
    [SerializeField] private TMP_Text tooltipText;

    [Tooltip("Optional delay before showing the tooltip (seconds).")]
    [SerializeField] private float showDelay = 0f;

    [Tooltip("Sorting order used to keep the tooltip above reward menus and cards.")]
    [SerializeField] private int tooltipSortingOrder = 1000;

    private Coroutine showCoroutine;
    private JokerInstance jokerInstance;
    private Transform tooltipOriginalParent;
    private int tooltipOriginalSiblingIndex;
    private bool tooltipMovedToRoot;

    private void Awake()
    {
        jokerInstance = GetComponentInParent<JokerInstance>();

        if (tooltipText == null && tooltip != null)
            tooltipText = tooltip.GetComponentInChildren<TMP_Text>(true);

        if (tooltip != null)
        {
            tooltipOriginalParent = tooltip.transform.parent;
            tooltipOriginalSiblingIndex = tooltip.transform.GetSiblingIndex();
            tooltip.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (tooltip == null) return;

        tooltip.SetActive(false);
        Destroy(tooltip);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltip == null) return;
        UpdateTooltipText();

        // start show coroutine so we can respect optional delay
        showCoroutine = StartCoroutine(ShowTooltipAfterDelay());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // cancel pending show and hide immediately
        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
            showCoroutine = null;
        }

        if (tooltip != null)
        {
            tooltip.SetActive(false);
            RestoreTooltipParent();
        }
    }

    private IEnumerator ShowTooltipAfterDelay()
    {
        if (showDelay > 0f)
            yield return new WaitForSeconds(showDelay);

        BringTooltipToFront();

        if (tooltip != null)
            tooltip.SetActive(true);

        showCoroutine = null;
    }

    private void BringTooltipToFront()
    {
        if (tooltip == null) return;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            Transform rootCanvasTransform = parentCanvas.rootCanvas.transform;
            if (tooltip.transform.parent != rootCanvasTransform)
            {
                tooltip.transform.SetParent(rootCanvasTransform, true);
                tooltipMovedToRoot = true;
            }
        }

        tooltip.transform.SetAsLastSibling();
        PositionTooltipUnderJoker();

        Canvas tooltipCanvas = tooltip.GetComponent<Canvas>();
        if (tooltipCanvas == null)
            tooltipCanvas = tooltip.AddComponent<Canvas>();

        tooltipCanvas.overrideSorting = true;
        tooltipCanvas.sortingOrder = tooltipSortingOrder;
    }

    private void PositionTooltipUnderJoker()
    {
        RectTransform sourceRect = transform as RectTransform;
        RectTransform tooltipRect = tooltip != null ? tooltip.transform as RectTransform : null;
        if (sourceRect == null || tooltipRect == null)
            return;

        Vector3[] corners = new Vector3[4];
        sourceRect.GetWorldCorners(corners);

        float sourceBottom = (corners[0].y + corners[3].y) * 0.5f;
        float sourceCenterX = (corners[0].x + corners[3].x) * 0.5f;
        // World Space Canvas uses world units, so the gap must scale with the joker.
        float jokerWorldHeight = sourceRect.rect.height * sourceRect.lossyScale.y;
        float verticalGap = Mathf.Max(0.01f, jokerWorldHeight * 0.08f);

        tooltipRect.pivot = new Vector2(0.5f, 1f);
        tooltipRect.position = new Vector3(
            sourceCenterX,
            sourceBottom - verticalGap,
            tooltipRect.position.z);
    }

    private void RestoreTooltipParent()
    {
        if (!tooltipMovedToRoot || tooltip == null || tooltipOriginalParent == null)
            return;

        tooltip.transform.SetParent(tooltipOriginalParent, true);
        tooltip.transform.SetSiblingIndex(tooltipOriginalSiblingIndex);
        tooltipMovedToRoot = false;
    }

    private void UpdateTooltipText()
    {
        if (tooltipText == null) return;

        JokerInstance joker = jokerInstance != null ? jokerInstance : GetComponentInParent<JokerInstance>();
        if (joker == null || joker.Data == null) return;

        tooltipText.text = FormatEffectTerms(joker.Data.jokerName + "\n" + joker.Data.description);
    }

    private string FormatEffectTerms(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return Regex.Replace(text, @"([+xX]?\d+(?:[\.,]\d+)?\s*(taste|mult))\b", match =>
        {
            string term = match.Groups[2].Value.ToLowerInvariant();
            string color = term == "taste" ? "#FFD600" : "#FF2020";
            return "<color=" + color + ">" + match.Value + "</color>";
        }, RegexOptions.IgnoreCase);
    }
}
