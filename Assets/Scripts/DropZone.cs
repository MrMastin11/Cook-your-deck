using System.Collections.Generic;
using UnityEngine;

public class DropZone : MonoBehaviour
{
    public List<DragCard> cards = new List<DragCard>();
    public float spacing = 150f;

    public bool isTableZone = false;
    public int maxCards = 5;

    public GameObject endButton;

    public void Start()
    {
        if (endButton != null)
            endButton.SetActive(false);
    }

    public void AttachCardAtPosition(DragCard card, int index)
    {
        if (!CanAcceptCard(card)) return;

        if (cards.Contains(card))
            cards.Remove(card);

        index = Mathf.Clamp(index, 0, cards.Count);
        cards.Insert(index, card);

        card.transform.SetParent(transform);
        card.SetCurrentZone(this);
        UpdateCardPositions();

        if (endButton != null)
        {
            bool shouldShow = (cards.Count == 3 && isTableZone);
            endButton.SetActive(shouldShow);
        }
    }

    public void RemoveCard(DragCard card)
    {
        if (cards.Contains(card))
        {
            cards.Remove(card);
            UpdateCardPositions();
        }
    }

    public void UpdateCardPositions()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].transform.position = transform.position + Vector3.right * spacing * i;
            cards[i].transform.SetSiblingIndex(i);
        }
    }

    public int GetInsertIndex(Vector3 pointerPosition)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (pointerPosition.x < cards[i].transform.position.x)
                return i;
        }
        return cards.Count;
    }

    public void ClearZone()
    {
        foreach (var card in cards)
            Destroy(card.gameObject);

        cards.Clear();
        if (endButton != null) endButton.SetActive(false);
    }

    public bool CanAcceptCard(DragCard card)
    {
        if (cards.Contains(card)) return true;
        return cards.Count < maxCards;
    }

    public void OnCardAddedToTable(DragCard card) { }
    public void OnCardRemovedFromTable(DragCard card) { }
}