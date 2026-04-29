using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class DeckHoverUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private DeckManager deckManager;
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private TMPro.TextMeshProUGUI deckCountText;
    private bool isHovering = false;

    private void Awake()
    {
        tooltipPanel.SetActive(false);
    }

    private void Update()
    {
        if (isHovering && tooltipPanel.activeSelf)
        {
            tooltipText.text = deckManager.GetDeckTooltipText();
        }
        if (deckManager != null && deckCountText != null)
        {
            deckCountText.text = deckManager.DeckCount.ToString();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        tooltipPanel.SetActive(true);

        // одразу показуємо, щоб не було 1 кадру пустоти
        tooltipText.text = deckManager.GetDeckTooltipText();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        tooltipPanel.SetActive(false);
    }
}
