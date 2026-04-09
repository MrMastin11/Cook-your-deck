using UnityEngine;
using UnityEngine.EventSystems;

public class DragCard : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerClickHandler
{
    private Vector3 originalPosition;
    private Transform originalParent;
    private CanvasGroup canvasGroup;
    private DropZone currentZone;

    [Header("Scale Settings")]
    private Vector3 baseScale;
    private float hoverMultiplier = 1.1f;
    private bool isEnlarged = false;

    [Header("Hover Settings")]
    private float hoverOffsetY = 40f;
    private bool isDragging = false;

    public bool isRewardCard = false; // ✅ тепер НЕ static
    public static bool inputLocked = false;

    private Canvas canvas;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        baseScale = transform.localScale;
        canvas = GetComponentInParent<Canvas>();
    }

    private void ScaleUp()
    {
        if (!isEnlarged)
        {
            transform.localScale = baseScale * hoverMultiplier;
            isEnlarged = true;
        }
    }

    private void ScaleDown()
    {
        if (isEnlarged)
        {
            transform.localScale = baseScale;
            isEnlarged = false;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (inputLocked || isRewardCard) return; // ❗ блок для reward

        isDragging = true;
        originalPosition = transform.position;
        originalParent = transform.parent;

        canvasGroup.blocksRaycasts = false;
        transform.SetParent(canvas != null ? canvas.transform : transform.root);

        ScaleUp();

        if (currentZone != null) currentZone.RemoveCard(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (inputLocked || isRewardCard) return;

        if (canvas != null)
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector3 worldPoint
            );
            transform.position = worldPoint;
        }
        else
        {
            transform.position = Input.mousePosition;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (inputLocked || isRewardCard) return;

        isDragging = false;
        canvasGroup.blocksRaycasts = true;

        ScaleDown();

        DropZone targetZone = null;
        if (eventData.pointerEnter != null)
            targetZone = eventData.pointerEnter.GetComponentInParent<DropZone>();

        if (targetZone == null) targetZone = currentZone;

        if (!targetZone.CanAcceptCard(this))
        {
            ReturnToPrevious();
            return;
        }

        if (currentZone != null && currentZone.isTableZone)
            currentZone.OnCardRemovedFromTable(this);

        int index = targetZone.GetInsertIndex(transform.position);
        targetZone.AttachCardAtPosition(this, index);

        if (targetZone.isTableZone)
            targetZone.OnCardAddedToTable(this);
    }

    private void ReturnToPrevious()
    {
        transform.SetParent(originalParent);
        transform.position = originalPosition;
        ScaleDown();
        if (currentZone != null) currentZone.UpdateCardPositions();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isRewardCard)
        {
            CardInstance instance = GetComponent<CardInstance>();

            if (instance != null)
            {
                DeckManager deck = Object.FindFirstObjectByType<DeckManager>();
                deck.SelectRewardCard(this, instance.data);
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isDragging || Input.GetMouseButton(0)) return;
        ScaleUp();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isDragging) return;
        ScaleDown();
    }

    public void SetCurrentZone(DropZone zone)
    {
        currentZone = zone;
        originalParent = zone.transform;
    }

    public void countingUP()
    {
        inputLocked = true;
        canvasGroup.blocksRaycasts = false;

        RectTransform rect = transform as RectTransform;
        originalPosition = rect.anchoredPosition;
        rect.anchoredPosition += new Vector2(0, hoverOffsetY);
    }
}