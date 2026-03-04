using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class DeckManager : MonoBehaviour
{
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private DragCard dragcard;
    [SerializeField] private DropZone dropZone;

    [Header("Zones")]
    [SerializeField] private DropZone handZone;
    [SerializeField] private DropZone tableZone;

    [Header("Deck Data")]
    [SerializeField] private CardData[] startingDeck;

    private List<CardData> deck = new List<CardData>();
    private List<CardData> discardPile = new List<CardData>();

    [Header("Score")]
    public int score = 0;
    public int minimumScore = 100;

    public TMPro.TextMeshProUGUI ValueText;
    public TMPro.TextMeshProUGUI MultText;
    public TMPro.TextMeshProUGUI scoreText;
    public TMPro.TextMeshProUGUI minimumScoreText;


    private void Awake()
    {
        deck.AddRange(startingDeck);
        ShuffleDeck();
        for (int i = 0; i < 5; i++)
        {
            DrawCard();
        }
    }

    public void DrawCard()
    {
        if (deck.Count == 0)
        {
            if (discardPile.Count == 0)
                return;

            RefillDeck();
        }

        CardData data = deck[deck.Count - 1];
        deck.RemoveAt(deck.Count - 1);

        SpawnCardInHand(data);
    }

    public IEnumerator WaitSecond()
    {
        yield return new WaitForSeconds(0.3f);
    }
    public void EndTurn()
    {
        StartCoroutine(EndTurnCoroutine());
    }
    public IEnumerator EndTurnCoroutine()
    {
        dropZone.endButton.SetActive(false);
        int value = 1;
        int multiplier = 1;

        var cardsOnTable = new List<DragCard>(tableZone.cards);

        foreach (var dragCard in cardsOnTable)
        {
            var instance = dragCard.GetComponent<CardInstance>();

            if (instance != null && instance.data != null)
            {
                yield return StartCoroutine(WaitSecond());
                dragCard.countingUP();
                value += instance.data.value;
                ValueText.text = value.ToString();
                multiplier += instance.data.multiplier;
                MultText.text = multiplier.ToString();
                yield return StartCoroutine(WaitSecond());
                discardPile.Add(instance.data);
                yield return StartCoroutine(WaitSecond());
            }
        }
        yield return StartCoroutine(WaitSecond());
        yield return StartCoroutine(WaitSecond());
        yield return StartCoroutine(WaitSecond());
        score += value * multiplier;
        scoreText.text = score.ToString();
        foreach (var dragCard in cardsOnTable)
        {
            tableZone.RemoveCard(dragCard);
            Destroy(dragCard.gameObject);
        }
        if (score >= minimumScore)
        {
            EndDay();
        }
        value = 0;
        ValueText.text = value.ToString();
        multiplier = 0;
        MultText.text = multiplier.ToString();
        yield return StartCoroutine(WaitSecond());
        for (int i = 0; i < 5; i++)
        {
            RefillDeck();
            DrawCard();
            yield return StartCoroutine(WaitSecond());
        }
    }


    private void EndDay()
    {
        score = 0;
        minimumScore *= 2;

        scoreText.text = score.ToString();
        minimumScoreText.text = "Need score:\n" + minimumScore.ToString();
    }


    private void SpawnCardInHand(CardData data)
    {
        CardView view = Instantiate(cardPrefab);

        var instance = view.GetComponent<CardInstance>();
        instance.Init(data);

        view.Init(instance);

        var dragCard = view.GetComponent<DragCard>();
        handZone.AttachCardAtPosition(dragCard, handZone.cards.Count);
    }

    private void RefillDeck()
    {
        deck.AddRange(discardPile);
        discardPile.Clear();
        ShuffleDeck();
    }


    private void ShuffleDeck()
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);

            var temp = deck[i];
            deck[i] = deck[j];
            deck[j] = temp;
        }
    }
}