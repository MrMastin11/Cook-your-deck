using UnityEngine;

public class DropZone : MonoBehaviour
{
    public void AttachCard(DragCard card)
    {
        card.transform.SetParent(transform);
        card.transform.position = transform.position;
        card.SetCurrentZone(this);
    }
}
