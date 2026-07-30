using System.Collections;
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

    private Coroutine showCoroutine;
    private JokerInstance jokerInstance;

    private void Awake()
    {
        jokerInstance = GetComponentInParent<JokerInstance>();

        if (tooltipText == null && tooltip != null)
            tooltipText = tooltip.GetComponentInChildren<TMP_Text>(true);

        if (tooltip != null)
            tooltip.SetActive(false);
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
            tooltip.SetActive(false);
    }

    private IEnumerator ShowTooltipAfterDelay()
    {
        if (showDelay > 0f)
            yield return new WaitForSeconds(showDelay);

        if (tooltip != null)
            tooltip.SetActive(true);

        showCoroutine = null;
    }

    private void UpdateTooltipText()
    {
        if (tooltipText == null) return;

        JokerInstance joker = jokerInstance != null ? jokerInstance : GetComponentInParent<JokerInstance>();
        if (joker == null || joker.Data == null) return;

        tooltipText.text = joker.Data.jokerName + "\n" + joker.Data.description;
    }
}
