using UnityEngine;
using UnityEngine.EventSystems;

public class DragCard : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Vector3 originalPosition;
    private Transform originalParent;
    private CanvasGroup canvasGroup;

    private DropZone currentZone;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
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
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Move smoothly towards the mouse position with a delay
        Vector3 targetPosition = Input.mousePosition;
        float smoothSpeed = 10f * Time.deltaTime; // Adjust for more/less delay
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        if (eventData.pointerEnter != null &&
            eventData.pointerEnter.TryGetComponent(out DropZone zone))
        {
            zone.AttachCard(this);
        }
        else
        {
            transform.SetParent(originalParent);
            transform.position = originalPosition;
        }
    }
}
