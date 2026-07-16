using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

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
    private List<CardData> playerDeck = new List<CardData>();
    private List<CardData> deck = new List<CardData>();
    private List<CardData> discardPile = new List<CardData>();
    private HashSet<CardData> mergedCards = new HashSet<CardData>();

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

    [Header("Round Plays")]
    [SerializeField] private int maxCardPlaysPerRound = 3;
    [SerializeField] private int cardPlaysRemaining = 3;
    [SerializeField] private TMPro.TextMeshProUGUI cardPlaysText;

    [Header("Timing")]
    [SerializeField] private float fibStepWait = 0.01f;
    [SerializeField] private float smallWait = 0.3f;

    public GameObject WinPanel;
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private TMPro.TextMeshProUGUI deathMaxScoreText;
    [SerializeField] private TMPro.TextMeshProUGUI deathDayCompleteText;

    public int Day = 1;
    public TMPro.TextMeshProUGUI dayText;

    [SerializeField] private Transform rewardZone;

    // ✅ стан вибору нагороди
    private bool isChoosingReward = false;
    private int startingMinimumScore;
    private int maxScoreThisRun = 0;

    public void Start()
    {
        if (WinPanel != null)
            WinPanel.SetActive(false);

        if (deathPanel != null)
            deathPanel.SetActive(false);
    }

    private void Awake()
    {
        startingMinimumScore = minimumScore;
        InitializePlayerDeck();
        deck.AddRange(playerDeck);
        ShuffleDeck();

        ResetRoundCardPlays();

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
        if (handZone != null && handZone.cards.Count >= handZone.maxCards) return;

        if (deck.Count == 0)
        {
            if (discardPile.Count == 0) return;
            RefillDeck();
        }

        CardData data = deck[deck.Count - 1];
        deck.RemoveAt(deck.Count - 1);
        CheckAndRefillDeck();
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

        int turnScore = value * multiplier;

        yield return StartCoroutine(CountScoreFibonacci(turnScore));

        yield return StartCoroutine(WaitSecond(smallWait));

        score += countScore;
        maxScoreThisRun = Mathf.Max(maxScoreThisRun, score);
        UpdateScoreUI();

        yield return StartCoroutine(WaitSecond(smallWait));

        countScore = 0;
        UpdateCountScoreUI();
        SpendCardPlay();

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

        if (cardPlaysRemaining <= 0 && score < minimumScore)
        {
            ShowDeathPanel();
            yield break;
        }

        yield return StartCoroutine(WaitSecond(smallWait));

        for (int i = 0; i < 3; i++)
        {
            if (handZone != null && handZone.cards.Count >= handZone.maxCards) break;
            DrawCard();
            yield return StartCoroutine(WaitSecond(smallWait));
        }
        CheckAndRefillDeck();
        DragCard.inputLocked = false;
    }

    private IEnumerator DealStartingHand(int count = 5)
    {
        DragCard.inputLocked = true;

        for (int i = 0; i < count; i++)
        {
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

        ReturnAllCardsToDeck();

        minimumScore = Mathf.CeilToInt(minimumScore * 1.2f / 100f) * 100;
        countScore = 0;
        score = 0;

        UpdateCountScoreUI();
        UpdateScoreUI();
        UpdateMinimumScoreUI();
    }

    public void RevardGeted()
    {
        WinPanel.SetActive(false);

        Day++;
        UpdateDayUI();

        ResetDeckForNewDay();
        ResetRoundCardPlays();

        StartCoroutine(DealStartingHand(5));

        // 🔓 розблок тільки тут
        DragCard.inputLocked = false;
    }

    public void NewRun()
    {
        StopAllCoroutines();

        if (WinPanel != null)
            WinPanel.SetActive(false);

        if (deathPanel != null)
            deathPanel.SetActive(false);

        Day = 1;
        score = 0;
        countScore = 0;
        maxScoreThisRun = 0;
        minimumScore = startingMinimumScore;

        if (ValueText != null)
            ValueText.text = "1";

        if (MultText != null)
            MultText.text = "1";

        ClearZoneCards(rewardZone != null ? rewardZone.GetComponent<DropZone>() : null);
        ClearZoneCards(handZone);
        ClearZoneCards(tableZone);

        InitializePlayerDeck();
        ResetDeckForNewDay();
        ResetRoundCardPlays();

        UpdateCountScoreUI();
        UpdateScoreUI();
        UpdateMinimumScoreUI();
        UpdateDayUI();

        if (packController != null)
            packController.ResetPack();

        DragCard.inputLocked = false;
        StartCoroutine(DealStartingHand(5));
    }

    private void ShowDeathPanel()
    {
        DragCard.inputLocked = true;

        if (deathPanel == null)
        {
            Debug.LogWarning("Death Panel is not assigned in DeckManager.");
            return;
        }

        if (deathMaxScoreText != null)
            deathMaxScoreText.text = "MAX SCORE: " + maxScoreThisRun.ToString();

        if (deathDayCompleteText != null)
            deathDayCompleteText.text = "DAYS COMPLETE: " + Mathf.Max(0, Day - 1).ToString();

        deathPanel.SetActive(true);
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
            pool.RemoveAt(index);
        }

        return result;
    }

    public void ThreeCards()
    {
        isChoosingReward = true;

        // 🔒 поки спавняться карти
        DragCard.inputLocked = true;

        StartCoroutine(SpawnRewardCardsCoroutine());
    }

    private IEnumerator SpawnRewardCardsCoroutine()
    {
        List<CardData> choices = GetThreeRandomCards();

        foreach (var data in choices)
        {
            SpawnRewardCard(data);
            yield return new WaitForSeconds(smallWait);
        }

        // 🔓 тепер можна клікати
        DragCard.inputLocked = false;
    }

    private void SpawnRewardCard(CardData data)
    {
        CardView view = Instantiate(cardPrefab);

        var instance = view.GetComponent<CardInstance>();
        instance.Init(data);
        view.Init(instance);
        view.SetMergeLabelVisible(CanMergeRewardCard(data));

        var drag = view.GetComponent<DragCard>();
        drag.isRewardCard = true;

        DropZone zone = rewardZone.GetComponent<DropZone>();
        zone.AttachCardAtPosition(drag, zone.cards.Count);

        view.transform.localScale = Vector3.one * 0.6f;

        RevardText.text = "Choose one:";
    }

    public void SelectRewardCard(DragCard selectedCard, CardData data)
    {
        StartCoroutine(SelectRewardCardCoroutine(selectedCard, data));
    }

    private IEnumerator SelectRewardCardCoroutine(DragCard selectedCard, CardData data)
    {
        DropZone zone = rewardZone.GetComponent<DropZone>();

        foreach (var card in zone.cards)
        {
            if (card == null) continue;

            var cg = card.GetComponent<CanvasGroup>();
            if (cg != null)
                cg.blocksRaycasts = false;
        }

        List<DragCard> cards = new List<DragCard>(zone.cards);

        foreach (DragCard card in cards)
        {
            if (card == null || card == selectedCard)
                continue;

            zone.RemoveCard(card);
            Destroy(card.gameObject);
        }

        if (selectedCard != null)
            zone.RemoveCard(selectedCard);

        yield return new WaitForSeconds(1f);

        bool merged = AddOrMergeRewardCard(data, out CardData resultCard, out CardData mergePreviewCard);

        if (merged && selectedCard != null)
        {
            yield return StartCoroutine(ShowRewardMerge(selectedCard, mergePreviewCard, resultCard));

            while (TryMergeAgain(resultCard, out CardData nextMergePreviewCard))
            {
                yield return StartCoroutine(ShowRewardMerge(selectedCard, nextMergePreviewCard, resultCard));
            }
        }

        if (selectedCard != null)
            Destroy(selectedCard.gameObject);

        zone.cards.Clear();

        isChoosingReward = false;

        // ❗ НЕ розблоковуємо тут

        RevardGeted();
    }

    private void SpawnCardInHand(CardData data)
    {
        //  гарантія що не вилізе за ліміт
        if (handZone != null && handZone.cards.Count >= handZone.maxCards)
            return;

        CardView view = Instantiate(cardPrefab, handZone.transform, false);
        view.transform.localScale = Vector3.one * 0.6f;

        var instance = view.GetComponent<CardInstance>();
        instance.Init(data);

        view.Init(instance);
        view.SetLevelLabelVisible(ShouldShowLevelLabel(data));

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

        deck.AddRange(playerDeck);
        ShuffleDeck();
    }

    private void ReturnAllCardsToDeck()
    {
        ClearZoneCards(handZone);
        ClearZoneCards(tableZone);

        deck.Clear();
        discardPile.Clear();

        deck.AddRange(playerDeck);
        ShuffleDeck();
    }

    private void ClearZoneCards(DropZone zone)
    {
        if (zone == null) return;

        for (int i = zone.cards.Count - 1; i >= 0; i--)
        {
            DragCard dragCard = zone.cards[i];
            zone.RemoveCard(dragCard);

            if (dragCard != null)
                Destroy(dragCard.gameObject);
        }

        zone.cards.Clear();
    }

    private void InitializePlayerDeck()
    {
        playerDeck.Clear();

        foreach (var card in startingDeck)
        {
            if (card == null) continue;
            playerDeck.Add(CreateRuntimeCardCopy(card));
        }
    }

    private bool AddOrMergeRewardCard(CardData selectedCard, out CardData resultCard, out CardData mergePreviewCard)
    {
        resultCard = null;
        mergePreviewCard = null;
        if (selectedCard == null) return false;

        CardData existingCard = FindMatchingPlayerDeckCard(selectedCard);

        if (existingCard == null)
        {
            resultCard = CreateRuntimeCardCopy(selectedCard);
            playerDeck.Add(resultCard);
            return false;
        }

        mergePreviewCard = CreateRuntimeCardCopy(existingCard);
        MergeCardStats(existingCard, selectedCard);
        resultCard = existingCard;
        return true;
    }

    private bool TryMergeAgain(CardData targetCard, out CardData mergePreviewCard)
    {
        mergePreviewCard = null;
        if (targetCard == null) return false;

        CardData matchingCard = FindMatchingPlayerDeckCard(targetCard, targetCard);
        if (matchingCard == null) return false;

        mergePreviewCard = CreateRuntimeCardCopy(matchingCard);
        RemoveCardFromPlayerDeck(matchingCard);
        MergeCardStats(targetCard, matchingCard);
        return true;
    }

    private IEnumerator ShowRewardMerge(DragCard selectedCard, CardData mergePreviewCard, CardData mergedCard)
    {
        CardView selectedView = selectedCard.GetComponent<CardView>();
        if (selectedView != null)
            selectedView.SetMergeLabelVisible(false);

        Transform cardTransform = selectedCard.transform;
        RectTransform cardRect = cardTransform as RectTransform;
        Vector3 centerPosition = cardTransform.position;
        Vector2 centerAnchoredPosition = cardRect != null ? cardRect.anchoredPosition : Vector2.zero;
        Vector3 baseScale = cardTransform.localScale;
        Vector3 worldOffset = cardTransform.right * 95f;
        Vector2 anchoredOffset = Vector2.right * 95f;

        CardView previewView = SpawnMergePreviewCard(mergePreviewCard, cardTransform.parent, centerPosition, baseScale);
        Transform previewTransform = previewView != null ? previewView.transform : null;
        RectTransform previewRect = previewTransform as RectTransform;

        Vector3 selectedStart = centerPosition + worldOffset;
        Vector3 previewStart = centerPosition - worldOffset;
        Vector2 selectedStartAnchored = centerAnchoredPosition + anchoredOffset;
        Vector2 previewStartAnchored = centerAnchoredPosition - anchoredOffset;

        if (cardRect != null)
        {
            cardRect.anchoredPosition = selectedStartAnchored;

            if (previewRect != null)
                previewRect.anchoredPosition = previewStartAnchored;
        }
        else
        {
            cardTransform.position = selectedStart;

            if (previewTransform != null)
                previewTransform.position = previewStart;
        }

        float combineDuration = 0.75f;
        float time = 0f;

        while (time < combineDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / combineDuration));

            if (cardRect != null)
            {
                cardRect.anchoredPosition = Vector2.Lerp(selectedStartAnchored, centerAnchoredPosition, t);

                if (previewRect != null)
                    previewRect.anchoredPosition = Vector2.Lerp(previewStartAnchored, centerAnchoredPosition, t);
            }
            else
            {
                cardTransform.position = Vector3.Lerp(selectedStart, centerPosition, t);

                if (previewTransform != null)
                    previewTransform.position = Vector3.Lerp(previewStart, centerPosition, t);
            }

            yield return null;
        }

        if (cardRect != null)
            cardRect.anchoredPosition = centerAnchoredPosition;
        else
            cardTransform.position = centerPosition;

        if (previewView != null)
            Destroy(previewView.gameObject);

        CardInstance instance = selectedCard.GetComponent<CardInstance>();
        CardView view = selectedView;

        if (instance != null && mergedCard != null)
        {
            instance.Init(mergedCard);
            if (view != null)
            {
                view.SetMergeLabelVisible(false);
                view.SetLevelLabelVisible(true);
                view.Refresh();
            }
        }

        if (RevardText != null)
            RevardText.text = "Merged!";

        Vector3 targetScale = baseScale * 1.18f;

        yield return new WaitForSeconds(0.25f);

        float duration = 0.3f;
        time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / duration));
            cardTransform.localScale = Vector3.Lerp(baseScale, targetScale, t);
            yield return null;
        }

        yield return new WaitForSeconds(0.7f);

        time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / duration));
            cardTransform.localScale = Vector3.Lerp(targetScale, baseScale, t);
            yield return null;
        }

        cardTransform.localScale = baseScale;
    }

    private CardView SpawnMergePreviewCard(CardData data, Transform parent, Vector3 position, Vector3 scale)
    {
        if (data == null) return null;

        CardView view = Instantiate(cardPrefab, parent, false);
        view.transform.position = position;
        view.transform.localScale = scale;

        CardInstance instance = view.GetComponent<CardInstance>();
        instance.Init(data);
        view.Init(instance);
        view.SetLevelLabelVisible(ShouldShowLevelLabel(data));
        view.SetMergeLabelVisible(false);

        DragCard drag = view.GetComponent<DragCard>();
        if (drag != null)
            drag.isRewardCard = true;

        CanvasGroup canvasGroup = view.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;

        return view;
    }

    private CardData FindMatchingPlayerDeckCard(CardData selectedCard)
    {
        return FindMatchingPlayerDeckCard(selectedCard, null);
    }

    private CardData FindMatchingPlayerDeckCard(CardData selectedCard, CardData ignoredCard)
    {
        foreach (var card in playerDeck)
        {
            if (card == null) continue;
            if (card == ignoredCard) continue;

            if (IsSameCardAndLevel(card, selectedCard))
                return card;
        }

        return null;
    }

    private void RemoveCardFromPlayerDeck(CardData card)
    {
        playerDeck.Remove(card);
        deck.Remove(card);
        discardPile.Remove(card);
        mergedCards.Remove(card);
    }

    private void MergeCardStats(CardData existingCard, CardData selectedCard)
    {
        existingCard.value += selectedCard.value;
        existingCard.multiplier += selectedCard.multiplier;
        existingCard.level = selectedCard.level + 1;
        mergedCards.Add(existingCard);

        RefreshVisibleCardStats(existingCard);
    }

    private void RefreshVisibleCardStats(CardData changedCard)
    {
        RefreshMatchingCardsInZone(handZone, changedCard);
        RefreshMatchingCardsInZone(tableZone, changedCard);
    }

    private void RefreshMatchingCardsInZone(DropZone zone, CardData changedCard)
    {
        if (zone == null) return;

        foreach (var dragCard in zone.cards)
        {
            if (dragCard == null) continue;

            CardInstance instance = dragCard.GetComponent<CardInstance>();
            if (instance == null || instance.data == null) continue;
            if (!IsSameCardAndLevel(instance.data, changedCard)) continue;

            instance.Init(changedCard);

            CardView view = dragCard.GetComponent<CardView>();
            if (view != null)
            {
                view.SetLevelLabelVisible(ShouldShowLevelLabel(changedCard));
                view.Refresh();
            }
        }
    }

    private bool ShouldShowLevelLabel(CardData card)
    {
        return card != null && (card.level > 1 || mergedCards.Contains(card));
    }

    private bool CanMergeRewardCard(CardData rewardCard)
    {
        return FindMatchingPlayerDeckCard(rewardCard) != null;
    }

    private bool IsSameCardAndLevel(CardData first, CardData second)
    {
        return first.level == second.level && GetCardKey(first) == GetCardKey(second);
    }

    private string GetCardKey(CardData card)
    {
        string key = string.IsNullOrWhiteSpace(card.cardName) ? card.name : card.cardName;
        return key.Trim().ToLowerInvariant();
    }

    private CardData CreateRuntimeCardCopy(CardData source)
    {
        CardData copy = Instantiate(source);
        copy.name = source.name;
        return copy;
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

    private void ResetRoundCardPlays()
    {
        cardPlaysRemaining = maxCardPlaysPerRound;
        UpdateCardPlaysUI();
    }

    private void SpendCardPlay()
    {
        cardPlaysRemaining = Mathf.Max(0, cardPlaysRemaining - 1);
        UpdateCardPlaysUI();
    }

    private void UpdateCardPlaysUI()
    {
        if (cardPlaysText != null)
            cardPlaysText.text = cardPlaysRemaining.ToString() + "/" + maxCardPlaysPerRound.ToString();
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
    public int DeckCount => deck.Count;

    public string GetDeckTooltipText()
    {
        if (deck.Count == 0)
            return "Deck is empty";

        Dictionary<string, int> counts = new Dictionary<string, int>();

        foreach (var card in deck)
        {
            if (card == null) continue;

            string displayName = string.IsNullOrWhiteSpace(card.cardName) ? card.name : card.cardName;
            if (counts.TryGetValue(displayName, out int value))
                counts[displayName] = value + 1;
            else
                counts[displayName] = 1;
        }

        StringBuilder sb = new StringBuilder();
        foreach (var pair in counts)
            sb.AppendLine($"{pair.Key} x{pair.Value}");

        return sb.ToString().TrimEnd();
    }
    private void CheckAndRefillDeck()
    {
        if (deck.Count == 0 && discardPile.Count > 0)
        {
            RefillDeck();
        }
    }
}
