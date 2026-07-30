using UnityEngine;
using UnityEngine.EventSystems;

public class JokerRewardChoice : MonoBehaviour, IPointerClickHandler
{
    private DeckManager deckManager;
    private JokersData data;
    private bool selected;

    public void Init(DeckManager deckManager, JokersData data)
    {
        this.deckManager = deckManager;
        this.data = data;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (selected || DragCard.inputLocked) return;

        selected = true;
        DragCard.inputLocked = true;

        if (deckManager != null && data != null)
            deckManager.SelectRewardJoker(this, data);
    }
}
