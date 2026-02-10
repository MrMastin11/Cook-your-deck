using UnityEngine;
using UnityEngine.EventSystems;

public class DragCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 originalPosition;
    private Transform originalParent;
    private CanvasGroup canvasGroup;
    private DropZone currentZone;

    private Vector3 originalScale;
    private bool isPointerOver = false;
    private float hoverScale = 1f;
    private float hoverOffsetY = 40f;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        originalScale = transform.localScale;
    }

    public void SetCurrentZone(DropZone zone)
    {
        currentZone = zone;
        originalParent = zone.transform;
        originalPosition = transform.position;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalPosition = transform.position;
        originalParent = transform.parent;

        canvasGroup.blocksRaycasts = false;
        transform.SetParent(transform.root);

        originalScale = transform.localScale;
        transform.localScale = originalScale * hoverScale;

        if (currentZone != null)
        {
            currentZone.RemoveCard(this);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        DropZone targetZone = null;

        transform.localScale = originalScale;

        if (eventData.pointerEnter != null)
            targetZone = eventData.pointerEnter.GetComponentInParent<DropZone>();

        if (targetZone == null)
            targetZone = currentZone;

        if (!targetZone.CanAcceptCard(this))
        {
            transform.SetParent(originalParent);
            transform.position = originalPosition;
            currentZone.UpdateCardPositions();
            return;
        }

        if (currentZone != null && currentZone.isTableZone)
            currentZone.OnCardRemovedFromTable(this);

        int index = targetZone.GetInsertIndex(transform.position);
        targetZone.AttachCardAtPosition(this, index);

        if (targetZone.isTableZone)
            targetZone.OnCardAddedToTable(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Prevent hover effect if currently dragging this card
        if (!isPointerOver && !Input.GetMouseButton(0))
        {
            isPointerOver = true;
            originalPosition = transform.position;
            transform.position = originalPosition + new Vector3(0, hoverOffsetY, 0);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isPointerOver)
        {
            isPointerOver = false;
            transform.position = originalPosition;
        }
    }
}
