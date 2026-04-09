using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class DeckManager : MonoBehaviour
{
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private DragCard dragcard;
    [SerializeField] private DropZone dropZone;
    [SerializeField] private PackController packController;

    [Header("Zones")]
    [SerializeField] private DropZone handZone;
    [SerializeField] private DropZone tableZone;
 

[Header("Deck Data")]
    [SerializeField] private CardData[] startingDeck;
    [SerializeField] private List<CardData> allCards = new List<CardData>();
    private List<CardData> deck = new List<CardData>();
    private List<CardData> discardPile = new List<CardData>();
    

    [Header("Score")]
    public int score = 0;
    public int minimumScore = 50;
    public TMPro.TextMeshProUGUI ValueText;
    public TMPro.TextMeshProUGUI MultText;
    public TMPro.TextMeshProUGUI scoreText;
    public TMPro.TextMeshProUGUI minimumScoreText;
    public TMPro.TextMeshProUGUI turnScoreText;
    public TMPro.TextMeshProUGUI RevardText;

    private int countScore = 0;
    [SerializeField] private TMPro.TextMeshProUGUI CountScoreText;

    [Header("Timing")]
    [SerializeField] private float fibStepWait = 0.01f;
    [SerializeField] private float smallWait = 0.3f;

    public GameObject WinPanel;
    
    public int Day = 1;
    public TMPro.TextMeshProUGUI dayText;

    public void Start()
    {
        WinPanel.SetActive(false);
    }

    private void Awake()
    {
        deck.AddRange(startingDeck);
        ShuffleDeck();

        // initial deal for game start => 5 cards
        StartCoroutine(DealStartingHand(5));

        UpdateScoreUI();
        UpdateMinimumScoreUI();
        UpdateCountScoreUI();
        UpdateDayUI();

        if (dropZone != null && dropZone.endButton != null)
            dropZone.endButton.SetActive(true);
    }

    public void DrawCard()
    {
        // don't draw if hand is already full
        if (handZone != null && handZone.cards.Count >= handZone.maxCards) return;

        if (deck.Count == 0)
        {
            if (discardPile.Count == 0) return;
            RefillDeck();
        }

        CardData data = deck[deck.Count - 1];
        deck.RemoveAt(deck.Count - 1);
        SpawnCardInHand(data);
    }

    public IEnumerator WaitSecond(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }

    public void EndTurn()
    {
        StartCoroutine(EndTurnCoroutine());
    }

    public IEnumerator EndTurnCoroutine()
    {
        if (dropZone != null && dropZone.endButton != null)
            dropZone.endButton.SetActive(false);

        DragCard.inputLocked = true;

        int value = 1;
        int multiplier = 1;

        var cardsOnTable = new List<DragCard>(tableZone.cards);

        foreach (var dragCard in cardsOnTable)
        {
            var instance = dragCard.GetComponent<CardInstance>();

            if (instance != null && instance.data != null)
            {
                yield return StartCoroutine(WaitSecond(smallWait));

                dragCard.countingUP();

                value += instance.data.value;
                ValueText.text = value.ToString();

                multiplier += instance.data.multiplier;
                MultText.text = multiplier.ToString();

                yield return StartCoroutine(WaitSecond(smallWait));

                discardPile.Add(instance.data);

                yield return StartCoroutine(WaitSecond(smallWait));
            }
        }

        yield return StartCoroutine(WaitSecond(smallWait));
        yield return StartCoroutine(WaitSecond(smallWait));
        yield return StartCoroutine(WaitSecond(smallWait));

        int turnScore = value * multiplier;

        yield return StartCoroutine(CountScoreFibonacci(turnScore));

        yield return StartCoroutine(WaitSecond(smallWait));

        score += countScore;
        UpdateScoreUI();

        yield return StartCoroutine(WaitSecond(smallWait));

        countScore = 0;
        UpdateCountScoreUI();

        foreach (var dragCard in cardsOnTable)
        {
            tableZone.RemoveCard(dragCard);
            Destroy(dragCard.gameObject);
        }

        value = 1;
        ValueText.text = value.ToString();

        multiplier = 1;
        MultText.text = multiplier.ToString();

        if (score >= minimumScore)
        {
            EndDay();
            yield break;
        }

        yield return StartCoroutine(WaitSecond(smallWait));

        // draw up to 3 cards after a normal turn, but stop if hand is full
        for (int i = 0; i < 3; i++)
        {
            if (handZone != null && handZone.cards.Count >= handZone.maxCards) break;
            DrawCard();
            yield return StartCoroutine(WaitSecond(smallWait));
        }

        DragCard.inputLocked = false;

        if (dropZone != null && dropZone.endButton != null)
            dropZone.endButton.SetActive(true);
    }

    // now accepts count (default 5) so new-day deals 5, other callers can request 3
    private IEnumerator DealStartingHand(int count = 5)
    {
        DragCard.inputLocked = true;

        for (int i = 0; i < count; i++)
        {
            // stop if hand reached its max capacity
            if (handZone != null && handZone.cards.Count >= handZone.maxCards) break;
            DrawCard();
            yield return StartCoroutine(WaitSecond(smallWait));
        }

        DragCard.inputLocked = false;
    }

    private IEnumerator CountScoreFibonacci(int turnTarget)
    {
        if (turnTarget <= 0) yield break;

        int a = 1;
        int b = 1;
        int added = 0;

        while (added < turnTarget)
        {
            int fib = a;

            if (added + fib > turnTarget)
                fib = turnTarget - added;

            countScore += fib;
            added += fib;

            UpdateCountScoreUI();

            yield return StartCoroutine(WaitSecond(fibStepWait));

            int next = a + b;
            a = b;
            b = next;
        }
    }

    private void EndDay()
    {
        if (WinPanel != null)
        {
            WinPanel.SetActive(true);
            if (packController != null)
                packController.ResetPack();
            RevardText.text = "Revard:";
        }
        
        if (turnScoreText != null)
            turnScoreText.text = "SCORE:\n" + score.ToString();

        for (int i = handZone.cards.Count - 1; i >= 0; i--)
        {
            var dragCard = handZone.cards[i];
            handZone.RemoveCard(dragCard);
            Destroy(dragCard.gameObject);
        }
        minimumScore *= 2;
        countScore = 0;
        score = 0;
        

        UpdateCountScoreUI();
        UpdateScoreUI();
        UpdateMinimumScoreUI();
        UpdateDayUI();
    }

    public void RevardGeted()
    {
        WinPanel.SetActive(false);
        Day++;
        ResetDeckForNewDay();

        // new day => deal 5 cards
        StartCoroutine(DealStartingHand(5));
        DragCard.inputLocked = false;
    }

    private void SpawnCardInHand(CardData data)
    {
        CardView view = Instantiate(cardPrefab, handZone.transform, false);
        view.transform.localScale = Vector3.one * 0.6f;

        var instance = view.GetComponent<CardInstance>();
        instance.Init(data);

        view.Init(instance);

        var dragCard = view.GetComponent<DragCard>();

        handZone.AttachCardAtPosition(dragCard, handZone.cards.Count);

        Canvas.ForceUpdateCanvases();
    }

    private void RefillDeck()
    {
        if (discardPile.Count == 0) return;

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

    private void ResetDeckForNewDay()
    {
        deck.Clear();
        discardPile.Clear();

        deck.AddRange(startingDeck);
        ShuffleDeck();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score.ToString();
    }

    private void UpdateCountScoreUI()
    {
        if (CountScoreText != null)
            CountScoreText.text = countScore.ToString();
    }

    private void UpdateMinimumScoreUI()
    {
        if (minimumScoreText != null)
            minimumScoreText.text = "Need score:\n" + minimumScore.ToString();
    }

    private void UpdateDayUI()
    {
        if (dayText != null)
            dayText.text = "Day: " + Day.ToString();
    }
    public List<CardData> GetThreeRandomCards()
    {
        List<CardData> pool = new List<CardData>(allCards);
        List<CardData> result = new List<CardData>();

        for (int i = 0; i < 3; i++)
        {
            if (pool.Count == 0) break;

            int index = Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index); //щоб не було повторів
        }

        return result;
    }
    public void ThreeCards()
    {
        StartCoroutine(SpawnRewardCardsCoroutine());
    }
    private IEnumerator SpawnRewardCardsCoroutine()
    {
        List<CardData> choices = GetThreeRandomCards();

        foreach (var data in choices)
        {
            SpawnRewardCard(data);
            yield return new WaitForSeconds(smallWait); // ← затримка
        }
    }
    [SerializeField] private Transform rewardZone;

    private void SpawnRewardCard(CardData data)
    {
        CardView view = Instantiate(cardPrefab);

        var instance = view.GetComponent<CardInstance>();
        instance.Init(data);
        view.Init(instance);

        var drag = view.GetComponent<DragCard>();
        drag.isRewardCard = true;

        DropZone zone = rewardZone.GetComponent<DropZone>();
        zone.AttachCardAtPosition(drag, zone.cards.Count);

        view.transform.localScale = Vector3.one * 0.6f;

        RevardText.text = "Choose one:";
    }
    public void AddCardToDeck(CardData data)
    {
        List<CardData> list = new List<CardData>(startingDeck);
        list.Add(data);
        startingDeck = list.ToArray();

        ClearRewardCards();
    }
    private void ClearRewardCards()
    {
        DropZone zone = rewardZone.GetComponent<DropZone>();

        foreach (Transform child in rewardZone)
        {
            DragCard card = child.GetComponent<DragCard>();

            if (card != null)
            {
                zone.RemoveCard(card); // ✅ remove first
            }

            Destroy(child.gameObject);
        }
    }
    public void SelectRewardCard(DragCard selectedCard, CardData data)
    {
        StartCoroutine(SelectRewardCardCoroutine(selectedCard, data));
    }

    private IEnumerator SelectRewardCardCoroutine(DragCard selectedCard, CardData data)
    {
        DragCard.inputLocked = true;

        DropZone zone = rewardZone.GetComponent<DropZone>();

        // Зберігаємо список, щоб безпечно пройтись по ньому
        List<DragCard> cards = new List<DragCard>(zone.cards);

        // 1) Одразу прибираємо всі інші карти
        foreach (DragCard card in cards)
        {
            if (card == null || card == selectedCard)
                continue;

            zone.RemoveCard(card);
            Destroy(card.gameObject);
        }

        // 2) Забираємо вибрану карту зі списку, але не знищуємо її одразу
        if (selectedCard != null)
            zone.RemoveCard(selectedCard);

        // 3) Чекаємо 1 секунду, карта ще видима
        yield return new WaitForSeconds(1f);

        // 4) Додаємо карту в колоду
        List<CardData> list = new List<CardData>(startingDeck);
        list.Add(data);
        startingDeck = list.ToArray();

        // 5) Тепер знищуємо вибрану карту
        if (selectedCard != null)
            Destroy(selectedCard.gameObject);

        // 6) Гарантовано очищаємо список зони
        zone.cards.Clear();

        DragCard.inputLocked = false;

        RevardGeted();
    }
}