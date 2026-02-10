using UnityEngine;

public class CardSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private DropZone tableZone;

    [Header("Card Pool")]
    [SerializeField] private CardData[] aviableCards;

    public void SpawnRandomCard()
    {
        int randomIndex = Random.Range(0, aviableCards.Length);
        SpawnCard(aviableCards[randomIndex]);
    }

    public void SpawnCard(CardData data)
    {
        CardView view = Instantiate(cardPrefab);

        CardInstance instance = view.GetComponent<CardInstance>();
        instance.Init(data);

        view.Init(instance);

        DragCard dragCard = view.GetComponent<DragCard>();
        tableZone.AttachCardAtPosition(dragCard, tableZone.cards.Count);
    }

}
