using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeckManager : MonoBehaviour
{
    public enum DayDifficulty
    {
        Easy,
        Normal,
        Hard,
        VeryHard,
        Boss
    }

    public enum DayRewardType
    {
        Card,
        Joker,
        CardAndJoker
    }

    [System.Serializable]
    public class DifficultyOption
    {
        public DayDifficulty difficulty = DayDifficulty.Easy;
        public int cardPlaysPerRound = 3;
    }

    private class BossPreferenceSet
    {
        public List<string> liked = new List<string>();
        public List<string> disliked = new List<string>();
    }

    [System.Serializable]
    public class WeekdayDifficultyConfig
    {
        public string weekdayName;
        public DifficultyOption optionOne = new DifficultyOption();
        public DifficultyOption optionTwo = new DifficultyOption();
    }

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
    private List<CardData> rewardCardPool = new List<CardData>();

    [Header("Joker Data")]
    [SerializeField] private JokerInstance jokerPrefab;
    [SerializeField] private Transform jokerZone;
    [SerializeField] private JokersData[] allJokers;
    [SerializeField] private List<JokersData> currentJokers = new List<JokersData>();
    private List<JokersData> rewardJokerPool = new List<JokersData>();

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
    private Coroutine cardPlaysBlinkCoroutine;

    private readonly Color tastePopupColor = new Color(1f, 0.95f, 0.05f);
    private readonly Color multiplierPopupColor = new Color(1f, 0.05f, 0.03f);
    [SerializeField] private TMP_FontAsset floatingNumberFont;

    [Header("Redraw")]
    [SerializeField] private Button redrawButton;

    [Header("Day Win Particles")]
    [SerializeField] private Canvas particleCanvas;
    [SerializeField] private float winParticleDuration = 1.5f;
    [SerializeField] private int winParticlesPerCorner = 90;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip dealSoundOne;
    [SerializeField] private AudioClip dealSoundTwo;
    [SerializeField] private AudioClip shuffleSoundOne;
    [SerializeField] private AudioClip shuffleSoundTwo;
    [SerializeField] private AudioClip cardSoundOne;
    [SerializeField] private AudioClip cardSoundTwo;
    [SerializeField] private AudioClip jokerSoundOne;
    [SerializeField] private AudioClip jokerSoundTwo;
    [SerializeField] private AudioClip buttonSoundOne;
    [SerializeField] private AudioClip buttonSoundTwo;
    private readonly HashSet<Button> audioButtons = new HashSet<Button>();

    [Header("Difficulty Selection")]
    [SerializeField] private GameObject difficultySelectionPanel;
    [SerializeField] private TMPro.TextMeshProUGUI difficultyDayText;
    [SerializeField] private TMPro.TextMeshProUGUI difficultyOptionOneText;
    [SerializeField] private TMPro.TextMeshProUGUI difficultyOptionTwoText;
    [SerializeField] private TMPro.TextMeshProUGUI bossPreferenceText;
    [SerializeField] private Button difficultyOptionOneButton;
    [SerializeField] private Button difficultyOptionTwoButton;
    [SerializeField] private Button skipRewardButton;
    [SerializeField] private WeekdayDifficultyConfig[] weekdayDifficultyConfigs = new WeekdayDifficultyConfig[7];
    private bool waitingForDifficultyChoice = false;
    private BossPreferenceSet optionOneBossPreferences;
    private BossPreferenceSet optionTwoBossPreferences;
    private BossPreferenceSet activeBossPreferences;

    [Header("Timing")]
    [SerializeField] private float fibStepWait = 0.1f;
    [SerializeField] private float smallWait = 0.5f; // single canonical effect delay used everywhere

    public GameObject WinPanel;
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private TMPro.TextMeshProUGUI deathMaxScoreText;
    [SerializeField] private TMPro.TextMeshProUGUI deathDayCompleteText;

    // New: panel shown after completing 28 days (same behavior as death but different game objects)
    [Header("End of 28-day Cycle UI")]
    [SerializeField] private GameObject endOfCyclePanel;
    [SerializeField] private TMPro.TextMeshProUGUI endCycleMaxScoreText;
    [SerializeField] private TMPro.TextMeshProUGUI endCycleDaysCompleteText;

    public int Day = 1;
    public TMPro.TextMeshProUGUI dayText;
    [SerializeField] private Slider dayProgressBar;
    private const int TotalDays = 28;

    [SerializeField] private Transform rewardZone;

    // ✅ стан вибору нагороди
    private bool isChoosingReward = false;
    private int startingMinimumScore;
    private int baseMinimumScore;
    private int maxScoreThisRun = 0;
    private JokersData[] startingAllJokers;
    private DayRewardType currentRewardType = DayRewardType.Card;
    private int rewardPicksRemaining = 1;
    private bool comboCardCompleted = false;
    private JokersData pendingJokerData;
    private JokerRewardChoice pendingRewardJoker;
    private bool waitingForJokerReplacement = false;

    private const int MaxJokers = 6;

    [Header("Week Day Objects (assign in Inspector)")]
    [SerializeField] private GameObject MondayObject;
    [SerializeField] private GameObject TuesdayObject;
    [SerializeField] private GameObject WednesdayObject;
    [SerializeField] private GameObject ThursdayObject;
    [SerializeField] private GameObject FridayObject;
    [SerializeField] private GameObject SaturdayObject;
    [SerializeField] private GameObject SundayObject;

    public void Start()
    {
        if (WinPanel != null)
            WinPanel.SetActive(false);

        if (deathPanel != null)
        {
            SetDeathPanelAlphaOne();
            deathPanel.SetActive(false);
        }

        // ensure endOfCyclePanel is hidden on start
        if (endOfCyclePanel != null)
        {
            CanvasGroup cg = endOfCyclePanel.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
            endOfCyclePanel.SetActive(false);
        }

        if (difficultySelectionPanel != null)
            difficultySelectionPanel.SetActive(false);

        SetupDifficultyButtons();
        SetupRedrawButton();
        SetupButtonSounds();
        SetSkipRewardVisible(false);
        SetRedrawVisible(false);

        // disable the seven weekday GameObjects assigned in the inspector
        DisableAssignedWeekDayObjects();

        // The first day begins with the Monday difficulty choice.
        ShowDifficultySelectionForDay(1);
    }

    // Make sure inspector-assigned weekday GameObjects are inactive at game start.
    private void DisableAssignedWeekDayObjects()
    {
        GameObject[] objs = new GameObject[]
        {
            MondayObject,
            TuesdayObject,
            WednesdayObject,
            ThursdayObject,
            FridayObject,
            SaturdayObject,
            SundayObject
        };

        foreach (var o in objs)
        {
            if (o == null) continue;
            if (o.activeSelf)
                o.SetActive(false);
        }
    }

    private void Awake()
    {
        startingMinimumScore = minimumScore;
        baseMinimumScore = minimumScore;
        startingAllJokers = (JokersData[])allJokers.Clone();
        ResetRewardPools();
        InitializeDifficultyConfigs();
        InitializePlayerDeck();
        deck.AddRange(playerDeck);
        ShuffleDeck();

        ResetRoundCardPlays();

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

    public void Redraw()
    {
        if (redrawButton == null || !redrawButton.gameObject.activeSelf)
            return;

        SetRedrawVisible(false);
        DragCard.inputLocked = true;

        int cardsToDraw = handZone != null ? handZone.cards.Count : 0;
        if (handZone != null)
        {
            List<DragCard> handCards = new List<DragCard>(handZone.cards);
            foreach (DragCard card in handCards)
            {
                if (card == null) continue;

                CardInstance instance = card.GetComponent<CardInstance>();
                if (instance != null && instance.data != null)
                    discardPile.Add(instance.data);

                handZone.RemoveCard(card);
                Destroy(card.gameObject);
            }
        }

        if (cardsToDraw > 0)
            PlayRandomSound(dealSoundOne, dealSoundTwo);

        for (int i = 0; i < cardsToDraw; i++)
            DrawCard();

        CheckAndRefillDeck();
        DragCard.inputLocked = false;
    }

    private void SetRedrawVisible(bool visible)
    {
        if (redrawButton != null)
            redrawButton.gameObject.SetActive(visible);
    }

    private void PlayDayWinParticles()
    {
        Canvas canvas = particleCanvas != null ? particleCanvas : GetComponentInParent<Canvas>();
        if (canvas == null) return;

        RectTransform canvasRect = canvas.transform as RectTransform;
        if (canvasRect == null) return;

        Vector3[] corners = new Vector3[4];
        canvasRect.GetWorldCorners(corners);
        Vector3 topLeft = corners[1];
        Vector3 topRight = corners[2];
        Vector3 center = (topLeft + topRight) * 0.5f;

        CreateWinParticleEmitter(topLeft, (center - topLeft).normalized, canvas);
        CreateWinParticleEmitter(topRight, (center - topRight).normalized, canvas);
    }

    private void CreateWinParticleEmitter(Vector3 position, Vector3 direction, Canvas canvas)
    {
        GameObject emitterObject = new GameObject("DayWinParticles");
        emitterObject.transform.SetParent(canvas.transform, true);
        emitterObject.transform.position = position;
        emitterObject.transform.rotation = Quaternion.LookRotation(direction, canvas.transform.forward);

        ParticleSystem particles = emitterObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        float effectDuration = Mathf.Min(winParticleDuration, 1.5f);
        main.duration = effectDuration;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.0015f, 0.004f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.25f, 0.25f), new Color(1f, 0.85f, 0.15f));
        main.gravityModifier = 0.15f;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = Mathf.Max(1f, winParticlesPerCorner / effectDuration);

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 8f;
        shape.radius = 0.05f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = gradient;

        ParticleSystemRenderer renderer = emitterObject.GetComponent<ParticleSystemRenderer>();
        Shader particleShader = Shader.Find("Particles/Standard Unlit");
        if (particleShader == null)
            particleShader = Shader.Find("Sprites/Default");
        if (particleShader != null)
            renderer.material = new Material(particleShader);

        particles.Play();
        Destroy(emitterObject, effectDuration + 1.5f);
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

        // canonical delay used for every visual "effect" (card or joker)
        float effectDelay = smallWait;

        // Process each card: apply card effect, then yield exactly one delay.
        // After that apply each matching Solo joker — each joker is an effect with exactly one delay.
        foreach (var dragCard in cardsOnTable)
        {
            var instance = dragCard.GetComponent<CardInstance>();
            if (instance == null || instance.data == null) continue;

            PlayRandomSound(cardSoundOne, cardSoundTwo);
            dragCard.countingUP();

            value += GetBossAdjustedCardEffect(instance.data.value, instance.data.type);
            if (ValueText != null) ValueText.text = value.ToString();
            ShowFloatingNumber(dragCard.transform, instance.data.value, tastePopupColor, Vector3.up * 0.25f);

            multiplier += GetBossAdjustedCardEffect(instance.data.multiplier, instance.data.type);
            if (MultText != null) MultText.text = multiplier.ToString();
            ShowFloatingNumber(dragCard.transform, instance.data.multiplier, multiplierPopupColor, Vector3.down * 0.25f);

            // one delay for the card effect
            yield return StartCoroutine(WaitSecond(effectDelay));

            // apply Solo jokers for this card — one delay after each joker effect
            foreach (var joker in GetJokersInActivationOrder(JokersData.ConditionType.Solo))
            {
                if (joker == null) continue;
                if (joker.conditionType != JokersData.ConditionType.Solo) continue;
                if (string.IsNullOrWhiteSpace(instance.data.type)) continue;
                if (!System.Enum.TryParse<JokersData.CardType>(instance.data.type, true, out var parsedCardType)) continue;

                if (joker.cardTypes != null && joker.cardTypes.Contains(parsedCardType))
                {
                    var ji = FindJokerInstance(joker);

                    if (joker.scoreReward != null)
                    {
                        if (joker.scoreReward.operationType == JokersData.OperationType.Add)
                            value += joker.scoreReward.value;
                        else
                            value *= joker.scoreReward.value;

                        if (ji != null)
                            ShowFloatingNumber(ji.transform, joker.scoreReward.value, tastePopupColor, Vector3.up * 0.25f,
                                joker.scoreReward.operationType == JokersData.OperationType.Multiply);
                    }

                    if (joker.multiplierReward != null)
                    {
                        if (joker.multiplierReward.operationType == JokersData.OperationType.Add)
                            multiplier += joker.multiplierReward.value;
                        else
                            multiplier *= joker.multiplierReward.value;

                        if (ji != null)
                            ShowFloatingNumber(ji.transform, joker.multiplierReward.value, multiplierPopupColor, Vector3.down * 0.25f,
                                joker.multiplierReward.operationType == JokersData.OperationType.Multiply);
                    }

                    if (ValueText != null) ValueText.text = value.ToString();
                    if (MultText != null) MultText.text = multiplier.ToString();

                    //Debug.Log($"Applied Solo Joker '{joker.jokerName}' to card '{instance.data.name}'");
                    if (ji != null)
                    {
                        PlayRandomSound(jokerSoundOne, jokerSoundTwo);
                        ji.PlayActivateAnimation(effectDelay, 1.4f);
                    }

                    // one delay for this joker effect
                    yield return StartCoroutine(WaitSecond(effectDelay));
                }
            }

            // move card to discard (no extra visual delay here; card+jokers already consumed effect slots)
            discardPile.Add(instance.data);
        }

        // Process Pair jokers: each successful pair-joker is treated as one effect (apply + one delay)
        foreach (var joker in GetJokersInActivationOrder(JokersData.ConditionType.Pair))
        {
            if (joker == null) continue;
            if (joker.conditionType != JokersData.ConditionType.Pair) continue;

            bool hasAllRequiredCards = true;
            foreach (var requiredType in joker.cardTypes)
            {
                bool found = false;
                foreach (var dragCard in cardsOnTable)
                {
                    var instance = dragCard.GetComponent<CardInstance>();
                    if (instance == null || instance.data == null) continue;
                    if (string.IsNullOrWhiteSpace(instance.data.type)) continue;
                    if (!System.Enum.TryParse(instance.data.type, true, out JokersData.CardType parsedType)) continue;
                    if (parsedType == requiredType) { found = true; break; }
                }
                if (!found) { hasAllRequiredCards = false; break; }
            }

            if (!hasAllRequiredCards) continue;

            var pairJi = FindJokerInstance(joker);

            // apply pair joker reward
            if (joker.scoreReward != null)
            {
                if (joker.scoreReward.operationType == JokersData.OperationType.Add)
                    value += joker.scoreReward.value;
                else
                    value *= joker.scoreReward.value;

                if (pairJi != null)
                    ShowFloatingNumber(pairJi.transform, joker.scoreReward.value, tastePopupColor, Vector3.up * 0.25f,
                        joker.scoreReward.operationType == JokersData.OperationType.Multiply);
            }

            if (joker.multiplierReward != null)
            {
                if (joker.multiplierReward.operationType == JokersData.OperationType.Add)
                    multiplier += joker.multiplierReward.value;
                else
                    multiplier *= joker.multiplierReward.value;

                if (pairJi != null)
                    ShowFloatingNumber(pairJi.transform, joker.multiplierReward.value, multiplierPopupColor, Vector3.down * 0.25f,
                        joker.multiplierReward.operationType == JokersData.OperationType.Multiply);
            }

            if (ValueText != null) ValueText.text = value.ToString();
            if (MultText != null) MultText.text = multiplier.ToString();

            //Debug.Log($"Applied Pair Joker '{joker.jokerName}'");
            if (pairJi != null)
            {
                PlayRandomSound(jokerSoundOne, jokerSoundTwo);
                pairJi.PlayActivateAnimation(effectDelay);
            }

            // one delay for this pair-joker effect
            yield return StartCoroutine(WaitSecond(effectDelay));
        }

        // finalize turn scoring
        int turnScore = value * multiplier;

        yield return StartCoroutine(CountScoreFibonacci(turnScore));
        yield return StartCoroutine(WaitSecond(effectDelay));

        score += countScore;
        maxScoreThisRun = Mathf.Max(maxScoreThisRun, score);
        UpdateScoreUI();

        yield return StartCoroutine(WaitSecond(effectDelay));

        countScore = 0;
        UpdateCountScoreUI();
        SpendCardPlay();

        foreach (var dragCard in cardsOnTable)
        {
            tableZone.RemoveCard(dragCard);
            Destroy(dragCard.gameObject);
        }

        value = 1;
        if (ValueText != null) ValueText.text = value.ToString();

        multiplier = 1;
        if (MultText != null) MultText.text = multiplier.ToString();

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

        yield return StartCoroutine(WaitSecond(effectDelay));

        PlayRandomSound(dealSoundOne, dealSoundTwo);
        for (int i = 0; i < 3; i++)
        {
            if (handZone != null && handZone.cards.Count >= handZone.maxCards) break;
            DrawCard();
            yield return StartCoroutine(WaitSecond(effectDelay));
        }
        CheckAndRefillDeck();
        DragCard.inputLocked = false;
    }

    private List<JokersData> GetJokersInActivationOrder(JokersData.ConditionType conditionType)
    {
        List<JokersData> ordered = new List<JokersData>();

        foreach (JokersData joker in currentJokers)
        {
            if (joker != null && joker.conditionType == conditionType)
                ordered.Add(joker);
        }

        ordered.Sort((first, second) =>
        {
            bool firstMultiplies = IsMultiplyingJoker(first);
            bool secondMultiplies = IsMultiplyingJoker(second);
            return firstMultiplies.CompareTo(secondMultiplies);
        });

        return ordered;
    }

    private bool IsMultiplyingJoker(JokersData joker)
    {
        if (joker == null) return false;

        return (joker.scoreReward != null && joker.scoreReward.operationType == JokersData.OperationType.Multiply)
            || (joker.multiplierReward != null && joker.multiplierReward.operationType == JokersData.OperationType.Multiply);
    }

    private IEnumerator DealStartingHand(int count = 5)
    {
        DragCard.inputLocked = true;

        if (count > 0)
            PlayRandomSound(dealSoundOne, dealSoundTwo);

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
        StopCardPlaysBlink();
        SetRedrawVisible(false);
        PlayDayWinParticles();

        if (WinPanel != null)
        {
            WinPanel.SetActive(true);
            if (packController != null)
                packController.ResetPack();
            RevardText.text = "Reward:";
        }

        if (turnScoreText != null)
            turnScoreText.text = "SCORE:\n" + score.ToString();

        ReturnAllCardsToDeck();

        countScore = 0;
        score = 0;

        UpdateCountScoreUI();
        UpdateScoreUI();
        // NOTE: difficulty is chosen after reward, before the next day starts.
    }

    public void RevardGeted()
    {
        WinPanel.SetActive(false);

        Day++;
        IncreaseBaseScoreForDay(Day);
        UpdateDayUI();

        // If player just completed the 28th day, show end-of-cycle panel (same behavior as ShowDeathPanel)
        if (Day > 28)
        {
            ShowEndOfCyclePanel();
            return;
        }

        ShowDifficultySelectionForDay(Day);
    }

    // Hide all inspector-assigned weekday objects
    private void HideAllWeekDayObjects()
    {
        GameObject[] objs = new GameObject[]
        {
            MondayObject,
            TuesdayObject,
            WednesdayObject,
            ThursdayObject,
            FridayObject,
            SaturdayObject,
            SundayObject
        };

        foreach (var o in objs)
        {
            if (o == null) continue;
            if (o.activeSelf)
                o.SetActive(false);
        }
    }

    // Show and move the weekday object corresponding to `day` (1-based) to (0,0,0).
    // Uses switch-case to select the right GameObject.
    private void ShowWeekdayForDay(int day)
    {
        // don't show anything for invalid day values or during end-of-cycle
        if (day <= 0) return;

        // normalize to 1..7 cycle
        int index = ((day - 1) % 7 + 7) % 7;

        // hide any previously shown weekday objects
        HideAllWeekDayObjects();

        GameObject target = null;
        switch (index)
        {
            case 0: target = MondayObject; break;
            case 1: target = TuesdayObject; break;
            case 2: target = WednesdayObject; break;
            case 3: target = ThursdayObject; break;
            case 4: target = FridayObject; break;
            case 5: target = SaturdayObject; break;
            case 6: target = SundayObject; break;
        }

        if (target == null) return;

        // move to origin and make visible
        target.transform.position = Vector3.zero;
        target.SetActive(true);
    }

    private void SetupDifficultyButtons()
    {
        if (difficultyOptionOneButton != null)
        {
            difficultyOptionOneButton.onClick.RemoveListener(OnDifficultyOptionOneClick);
            difficultyOptionOneButton.onClick.AddListener(OnDifficultyOptionOneClick);
        }

        if (difficultyOptionTwoButton != null)
        {
            difficultyOptionTwoButton.onClick.RemoveListener(OnDifficultyOptionTwoClick);
            difficultyOptionTwoButton.onClick.AddListener(OnDifficultyOptionTwoClick);
        }
    }

    private void ShowDifficultySelectionForDay(int day)
    {
        waitingForDifficultyChoice = true;
        DragCard.inputLocked = true;
        HideAllWeekDayObjects();

        WeekdayDifficultyConfig config = GetDifficultyConfigForDay(day);

        if (difficultyDayText != null)
            difficultyDayText.text = config.weekdayName;

        optionOneBossPreferences = CreateBossPreferences(config.optionOne.difficulty);
        optionTwoBossPreferences = CreateBossPreferences(config.optionTwo.difficulty);

        if (config.optionOne.difficulty == DayDifficulty.Boss && config.optionTwo.difficulty == DayDifficulty.Boss)
            EnsureDifferentBossPreferences();

        if (difficultyOptionOneText != null)
        {
            difficultyOptionOneText.text = GetDifficultyOptionText(config.optionOne, optionOneBossPreferences);
            difficultyOptionOneText.color = GetDifficultyColor(config.optionOne.difficulty);
        }

        if (difficultyOptionTwoText != null)
        {
            difficultyOptionTwoText.text = GetDifficultyOptionText(config.optionTwo, optionTwoBossPreferences);
            difficultyOptionTwoText.color = GetDifficultyColor(config.optionTwo.difficulty);
        }

        if (bossPreferenceText != null)
            bossPreferenceText.text = "";

        if (difficultySelectionPanel != null)
            difficultySelectionPanel.SetActive(true);
    }

    public void OnDifficultyOptionOneClick()
    {
        ChooseDifficultyOption(0);
    }

    public void OnDifficultyOptionTwoClick()
    {
        ChooseDifficultyOption(1);
    }

    public void ChooseDifficultyOption(int optionIndex)
    {
        if (!waitingForDifficultyChoice) return;

        WeekdayDifficultyConfig config = GetDifficultyConfigForDay(Day);
        DifficultyOption option = optionIndex == 0 ? config.optionOne : config.optionTwo;

        minimumScore = GetRequiredScoreForOption(option);
        maxCardPlaysPerRound = Mathf.Max(1, option.cardPlaysPerRound);
        currentRewardType = GetRewardTypeForDifficulty(option.difficulty);
        rewardPicksRemaining = GetRewardPickCountForDifficulty(option.difficulty);
        comboCardCompleted = false;
        activeBossPreferences = optionIndex == 0 ? optionOneBossPreferences : optionTwoBossPreferences;

        if (bossPreferenceText != null)
        {
            bossPreferenceText.text = activeBossPreferences != null
                ? GetBossPreferenceText(activeBossPreferences)
                : GetDifficultyName(option.difficulty).ToUpperInvariant();
        }

        if (difficultySelectionPanel != null)
            difficultySelectionPanel.SetActive(false);

        SetRedrawVisible(false);

        waitingForDifficultyChoice = false;

        StartNextDayAfterDifficultyChoice();
    }

    private void StartNextDayAfterDifficultyChoice()
    {
        UpdateDayUI();
        ResetDeckForNewDay();
        ResetRoundCardPlays();
        UpdateMinimumScoreUI();
        SetRedrawVisible(true);

        StartCoroutine(DealStartingHand(5));

        DragCard.inputLocked = false;
    }

    private void InitializeDifficultyConfigs()
    {
        if (weekdayDifficultyConfigs == null || weekdayDifficultyConfigs.Length != 7)
        {
            WeekdayDifficultyConfig[] resizedConfigs = new WeekdayDifficultyConfig[7];

            if (weekdayDifficultyConfigs != null)
            {
                int count = Mathf.Min(weekdayDifficultyConfigs.Length, resizedConfigs.Length);
                for (int i = 0; i < count; i++)
                    resizedConfigs[i] = weekdayDifficultyConfigs[i];
            }

            weekdayDifficultyConfigs = resizedConfigs;
        }

        for (int i = 0; i < weekdayDifficultyConfigs.Length; i++)
        {
            if (weekdayDifficultyConfigs[i] == null)
                weekdayDifficultyConfigs[i] = new WeekdayDifficultyConfig();

            if (weekdayDifficultyConfigs[i].optionOne == null)
                weekdayDifficultyConfigs[i].optionOne = new DifficultyOption();

            if (weekdayDifficultyConfigs[i].optionTwo == null)
                weekdayDifficultyConfigs[i].optionTwo = new DifficultyOption();

            EnsureDifficultyConfigDefaults(weekdayDifficultyConfigs[i], i);
        }
    }

    private WeekdayDifficultyConfig GetDifficultyConfigForDay(int day)
    {
        int index = GetWeekdayIndex(day);

        if (weekdayDifficultyConfigs != null &&
            index >= 0 &&
            index < weekdayDifficultyConfigs.Length &&
            weekdayDifficultyConfigs[index] != null)
        {
            EnsureDifficultyConfigDefaults(weekdayDifficultyConfigs[index], index);
            return weekdayDifficultyConfigs[index];
        }

        WeekdayDifficultyConfig fallback = new WeekdayDifficultyConfig();
        EnsureDifficultyConfigDefaults(fallback, index);
        return fallback;
    }

    private void EnsureDifficultyConfigDefaults(WeekdayDifficultyConfig config, int weekdayIndex)
    {
        if (config == null) return;

        if (string.IsNullOrWhiteSpace(config.weekdayName))
            config.weekdayName = GetWeekdayNameByIndex(weekdayIndex);

        EnsureDifficultyOptionDefaults(config.optionOne);
        EnsureDifficultyOptionDefaults(config.optionTwo);
    }

    private void EnsureDifficultyOptionDefaults(DifficultyOption option)
    {
        if (option == null) return;

        if (option.cardPlaysPerRound <= 0)
            option.cardPlaysPerRound = 3;
    }

    private string GetDifficultyOptionText(DifficultyOption option, BossPreferenceSet bossPreferences = null)
    {
        if (option == null)
            return "";

        if (option.difficulty == DayDifficulty.Boss && bossPreferences != null)
        {
            return "BOSS\nScore: " + GetRequiredScoreForOption(option).ToString()
                + "\n\n<color=#218739>" + FormatBossTastes(bossPreferences.liked) + " x1.5</color>"
                + "\n\n<color=#B3261E>" + FormatBossTastes(bossPreferences.disliked) + " x0.5</color>"
                + "\n\nReward: " + GetRewardTextForDifficulty(option.difficulty);
        }

        string text = GetDifficultyName(option.difficulty) + "\n\nScore: " + GetRequiredScoreForOption(option).ToString() + "\n\nReward: "
             + GetRewardTextForDifficulty(option.difficulty);

        return text;
    }

    private BossPreferenceSet CreateBossPreferences(DayDifficulty difficulty)
    {
        if (difficulty != DayDifficulty.Boss)
            return null;

        List<string> tastes = new List<string>();
        if (allCards != null)
        {
            foreach (CardData card in allCards)
            {
                if (card == null || string.IsNullOrWhiteSpace(card.type)) continue;
                if (!tastes.Contains(card.type)) tastes.Add(card.type);
            }
        }

        ShuffleStrings(tastes);
        int bossNumber = Mathf.Clamp(Day / 7, 1, 4);
        int likedCount = bossNumber == 1 || bossNumber == 3 ? 2 : 1;
        int dislikedCount = bossNumber == 3 || bossNumber == 4 ? 2 : 1;

        BossPreferenceSet result = new BossPreferenceSet();
        for (int i = 0; i < tastes.Count && result.liked.Count < likedCount; i++)
            result.liked.Add(tastes[i]);

        for (int i = result.liked.Count; i < tastes.Count && result.disliked.Count < dislikedCount; i++)
            result.disliked.Add(tastes[i]);

        return result;
    }

    private void EnsureDifferentBossPreferences()
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            if (!AreSameBossPreferences(optionOneBossPreferences, optionTwoBossPreferences))
                return;

            optionTwoBossPreferences = CreateBossPreferences(DayDifficulty.Boss);
        }
    }

    private bool AreSameBossPreferences(BossPreferenceSet first, BossPreferenceSet second)
    {
        if (first == null || second == null) return false;
        return JoinPreferences(first.liked) == JoinPreferences(second.liked)
            && JoinPreferences(first.disliked) == JoinPreferences(second.disliked);
    }

    private string JoinPreferences(List<string> preferences)
    {
        return preferences == null || preferences.Count == 0 ? "-" : string.Join(", ", preferences);
    }

    private void ShuffleStrings(List<string> values)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            string temp = values[i];
            values[i] = values[j];
            values[j] = temp;
        }
    }

    private int GetBossAdjustedCardEffect(int effect, string taste)
    {
        if (activeBossPreferences == null || string.IsNullOrWhiteSpace(taste))
            return effect;

        if (activeBossPreferences.liked.Contains(taste))
            return Mathf.RoundToInt(effect * 1.5f);

        if (activeBossPreferences.disliked.Contains(taste))
            return Mathf.RoundToInt(effect * 0.5f);

        return effect;
    }

    private string GetBossPreferenceText(BossPreferenceSet preferences)
    {
        if (preferences == null)
            return "";

        return "<color=#218739>" + FormatBossTastes(preferences.liked) + " x1.5</color>"
            + "\n<color=#B3261E>" + FormatBossTastes(preferences.disliked) + " x0.5</color>";
    }

    private void SetupButtonSounds()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        foreach (Button button in buttons)
        {
            if (button == null || audioButtons.Contains(button)) continue;

            button.onClick.AddListener(PlayButtonSound);
            audioButtons.Add(button);
        }
    }

    public void PlayButtonSound()
    {
        // Opening the reward panel should stay silent.
        if (isChoosingReward || waitingForJokerReplacement)
            return;

        PlayRandomSound(buttonSoundOne, buttonSoundTwo);
    }

    private void PlayRandomSound(AudioClip first, AudioClip second)
    {
        if (audioSource == null) return;

        AudioClip selected = Random.value < 0.5f ? first : second;
        if (selected != null)
            audioSource.PlayOneShot(selected);
    }

    private string FormatBossTastes(List<string> tastes)
    {
        if (tastes == null || tastes.Count == 0)
            return "-";

        StringBuilder result = new StringBuilder();
        for (int i = 0; i < tastes.Count; i++)
        {
            if (i > 0)
                result.Append(" ");

            result.Append(tastes[i].ToUpperInvariant());
        }

        return result.ToString();
    }
    private int GetRequiredScoreForOption(DifficultyOption option)
    {
        if (option == null)
            return baseMinimumScore;

        return Mathf.CeilToInt(baseMinimumScore * GetScoreMultiplierForDifficulty(option.difficulty));
    }

    private float GetScoreMultiplierForDifficulty(DayDifficulty difficulty)
    {
        switch (difficulty)
        {
            case DayDifficulty.Normal:
                return 2f;
            case DayDifficulty.Hard:
                return 3f;
            case DayDifficulty.VeryHard:
                return 4f;
            case DayDifficulty.Boss:
                return 5f;
            default:
                return 1f;
        }
    }

    private DayRewardType GetRewardTypeForDifficulty(DayDifficulty difficulty)
    {
        // VeryHard now gives only cards (3 cards) instead of card+joker combo
        if (difficulty == DayDifficulty.Hard || difficulty == DayDifficulty.Boss)
            return DayRewardType.Joker;

        return DayRewardType.Card;
    }

    private int GetRewardPickCountForDifficulty(DayDifficulty difficulty)
    {
        // Normal and Boss give 2 picks, VeryHard gives 3 card picks, others 1
        if (difficulty == DayDifficulty.Normal || difficulty == DayDifficulty.Boss)
            return 2;

        if (difficulty == DayDifficulty.VeryHard)
            return 3;

        return 1;
    }

    private string GetRewardTextForDifficulty(DayDifficulty difficulty)
    {
        if (difficulty == DayDifficulty.Normal)
            return "2 cards";

        if (difficulty == DayDifficulty.VeryHard)
            return "3 cards";

        if (difficulty == DayDifficulty.Hard)
            return "1 recept";

        if (difficulty == DayDifficulty.Boss)
            return "2 recept";

        return "1 card";
    }

    private string GetDifficultyName(DayDifficulty difficulty)
    {
        return difficulty.ToString();
    }

    private Color GetDifficultyColor(DayDifficulty difficulty)
    {
        switch (difficulty)
        {
            case DayDifficulty.Easy:
                return new Color(0.13f, 0.52f, 0.22f);
            case DayDifficulty.Normal:
                return new Color(0.68f, 0.52f, 0.02f);
            case DayDifficulty.Hard:
                return new Color(0.78f, 0.30f, 0.02f);
            case DayDifficulty.VeryHard:
                return new Color(0.72f, 0.08f, 0.05f);
            case DayDifficulty.Boss:
                return new Color(0.40f, 0.12f, 0.62f);
            default:
                return Color.white;
        }
    }

    private void IncreaseBaseScoreForDay(int day)
    {
        baseMinimumScore = Mathf.CeilToInt(baseMinimumScore * 1.2f);
    }

    private void SetupRedrawButton()
    {
        if (redrawButton == null) return;

        redrawButton.onClick.RemoveListener(Redraw);
        redrawButton.onClick.AddListener(Redraw);
    }
    private int GetWeekdayIndex(int day)
    {
        int index = (day - 1) % 7;
        if (index < 0) index += 7;
        return index;
    }

    private string GetWeekdayName(int day)
    {
        return GetWeekdayNameByIndex(GetWeekdayIndex(day));
    }

    private string GetWeekdayNameByIndex(int index)
    {
        string[] dayNames = new string[]
        {
            "Monday",
            "Tuesday",
            "Wednesday",
            "Thursday",
            "Friday",
            "Saturday",
            "Sunday"
        };

        if (index < 0 || index >= dayNames.Length)
            return "";

        return dayNames[index];
    }

    public void NewRun()
    {
        StopAllCoroutines();

        if (WinPanel != null)
            WinPanel.SetActive(false);

        if (deathPanel != null)
        {
            SetDeathPanelAlphaOne();
            deathPanel.SetActive(false);
        }

        if (difficultySelectionPanel != null)
            difficultySelectionPanel.SetActive(false);

        waitingForDifficultyChoice = false;

        // hide end-of-cycle panel on new run
        if (endOfCyclePanel != null)
        {
            CanvasGroup cg = endOfCyclePanel.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
            endOfCyclePanel.SetActive(false);
        }

        Day = 1;
        score = 0;
        countScore = 0;
        maxScoreThisRun = 0;
        minimumScore = startingMinimumScore;
        baseMinimumScore = startingMinimumScore;
        currentRewardType = DayRewardType.Card;
        rewardPicksRemaining = 1;
        comboCardCompleted = false;
        pendingJokerData = null;
        pendingRewardJoker = null;
        waitingForJokerReplacement = false;

        if (ValueText != null)
            ValueText.text = "1";

        if (MultText != null)
            MultText.text = "1";

        ClearZoneCards(rewardZone != null ? rewardZone.GetComponent<DropZone>() : null);
        ClearZoneCards(handZone);
        ClearZoneCards(tableZone);

        InitializePlayerDeck();

        // Restore all jokers data back to allJokers and clear current jokers
        if (startingAllJokers != null)
            allJokers = (JokersData[])startingAllJokers.Clone();
        else
            allJokers = new JokersData[0];

        currentJokers.Clear();
        ResetRewardPools();

        // Remove any JokerInstance GameObjects from joker zone and reward zone
        if (jokerZone != null)
        {
            for (int i = jokerZone.childCount - 1; i >= 0; i--)
            {
                Transform child = jokerZone.GetChild(i);
                if (child.GetComponent<JokerInstance>() != null || child.GetComponent<JokerRewardChoice>() != null)
                    Destroy(child.gameObject);
            }
        }

        if (rewardZone != null)
        {
            for (int i = rewardZone.childCount - 1; i >= 0; i--)
            {
                Transform child = rewardZone.GetChild(i);
                if (child.GetComponent<JokerInstance>() != null || child.GetComponent<JokerRewardChoice>() != null)
                    Destroy(child.gameObject);
            }
        }

        ResetDeckForNewDay();
        ResetRoundCardPlays();

        UpdateCountScoreUI();
        UpdateScoreUI();
        UpdateMinimumScoreUI();
        UpdateDayUI();

        if (packController != null)
            packController.ResetPack();

        ShowDifficultySelectionForDay(1);
    }

    private void ShowDeathPanel()
    {
        StopCardPlaysBlink();
        SetRedrawVisible(false);
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

        SetDeathPanelAlphaOne();
        deathPanel.SetActive(true);
    }

    // New: show end-of-cycle panel (same behavior as ShowDeathPanel but different game objects)
    private void ShowEndOfCyclePanel()
    {
        DragCard.inputLocked = true;

        if (endOfCyclePanel == null)
        {
            Debug.LogWarning("End-of-cycle Panel is not assigned in DeckManager.");
            return;
        }

        if (endCycleMaxScoreText != null)
            endCycleMaxScoreText.text = "MAX SCORE: " + maxScoreThisRun.ToString();

        if (endCycleDaysCompleteText != null)
            endCycleDaysCompleteText.text = "DAYS COMPLETE: " + Mathf.Max(0, Day - 1).ToString();

        CanvasGroup canvasGroup = endOfCyclePanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        endOfCyclePanel.SetActive(true);
    }

    private void SetDeathPanelAlphaOne()
    {
        if (deathPanel == null) return;

        CanvasGroup canvasGroup = deathPanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    public List<CardData> GetThreeRandomCards()
    {
        List<CardData> result = new List<CardData>();

        for (int i = 0; i < 3; i++)
        {
            if (rewardCardPool.Count == 0)
                RefillRewardCardPool();
            for (int j = rewardCardPool.Count - 1; j >= 0; j--)
            {
                if (result.Contains(rewardCardPool[j]))
                    rewardCardPool.RemoveAt(j);
            }
            if (rewardCardPool.Count == 0) break;

            int index = Random.Range(0, rewardCardPool.Count);
            result.Add(rewardCardPool[index]);
            rewardCardPool.RemoveAt(index);
        }

        return result;
    }

    public void ThreeCards()
    {
        isChoosingReward = true;
        SetSkipRewardVisible(false);

        DropZone rewardDropZone = rewardZone != null ? rewardZone.GetComponent<DropZone>() : null;
        if (rewardDropZone != null)
            rewardDropZone.maxCards = Mathf.Max(rewardDropZone.maxCards, 3);

        if (RevardText != null)
            RevardText.text = "Reward:";

        // 🔒 поки спавняться карти
        DragCard.inputLocked = true;

        StartCoroutine(SpawnRewardCardsCoroutine());
    }

    private IEnumerator SpawnRewardCardsCoroutine()
    {
        if (currentRewardType == DayRewardType.Joker)
        {
            yield return StartCoroutine(SpawnRewardJokersCoroutine());
            yield break;
        }

        List<CardData> choices = GetThreeRandomCards();

        foreach (var data in choices)
        {
            SpawnRewardCard(data);
            yield return new WaitForSeconds(smallWait);
        }

        // 🔓 тепер можна клікати
        DragCard.inputLocked = false;
        SetSkipRewardVisible(true);
    }

    private IEnumerator SpawnRewardJokersCoroutine()
    {
        List<JokersData> choices = GetThreeRandomJokers();

        foreach (var data in choices)
        {
            SpawnRewardJoker(data);
            yield return new WaitForSeconds(smallWait);
        }

        DragCard.inputLocked = false;
        SetSkipRewardVisible(true);
    }

    private void SetSkipRewardVisible(bool visible)
    {
        if (skipRewardButton != null)
            skipRewardButton.gameObject.SetActive(visible);
    }
    private List<JokersData> GetThreeRandomJokers()
    {
        List<JokersData> result = new List<JokersData>();

        for (int i = 0; i < 3; i++)
        {
            if (rewardJokerPool.Count == 0)
                RefillRewardJokerPool();
            for (int j = rewardJokerPool.Count - 1; j >= 0; j--)
            {
                if (result.Contains(rewardJokerPool[j]))
                    rewardJokerPool.RemoveAt(j);
            }
            if (rewardJokerPool.Count == 0) break;

            int index = Random.Range(0, rewardJokerPool.Count);
            result.Add(rewardJokerPool[index]);
            rewardJokerPool.RemoveAt(index);
        }

        return result;
    }

    private void ResetRewardPools()
    {
        rewardCardPool.Clear();
        if (allCards != null)
            rewardCardPool.AddRange(allCards);
        ShuffleCards(rewardCardPool);

        rewardJokerPool.Clear();
        RefillRewardJokerPool();
    }

    private void RefillRewardCardPool()
    {
        rewardCardPool.Clear();
        if (allCards != null)
            rewardCardPool.AddRange(allCards);
        ShuffleCards(rewardCardPool);
    }

    private void RefillRewardJokerPool()
    {
        rewardJokerPool.Clear();
        HashSet<JokersData> excluded = new HashSet<JokersData>(currentJokers);

        if (allJokers != null)
        {
            foreach (JokersData joker in allJokers)
            {
                if (joker != null && !excluded.Contains(joker))
                    rewardJokerPool.Add(joker);
            }
        }

        ShuffleJokers(rewardJokerPool);
    }

    private void ShuffleCards(List<CardData> values)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            CardData temp = values[i];
            values[i] = values[j];
            values[j] = temp;
        }
    }

    private void ShuffleJokers(List<JokersData> values)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            JokersData temp = values[i];
            values[i] = values[j];
            values[j] = temp;
        }
    }

    private void SpawnRewardJoker(JokersData data)
    {
        if (jokerPrefab == null || rewardZone == null || data == null) return;

        int index = GetRewardJokerCount();
        JokerInstance joker = Instantiate(jokerPrefab, rewardZone, false);
        joker.Init(data);

        Image image = joker.GetComponent<Image>();
        if (image != null && data.jokerSprite != null)
            image.sprite = data.jokerSprite;

        JokerRewardChoice choice = joker.GetComponent<JokerRewardChoice>();
        if (choice == null)
            choice = joker.gameObject.AddComponent<JokerRewardChoice>();

        choice.Init(this, data);

        RectTransform rect = joker.transform as RectTransform;
        if (rect != null)
            rect.anchoredPosition = Vector2.right * 95f * index;
        else
            joker.transform.position = rewardZone.position + Vector3.right * 95f * index;

        joker.transform.localScale = Vector3.one;

        SetRewardChoiceText();
    }

    private int GetRewardJokerCount()
    {
        if (rewardZone == null) return 0;

        int count = 0;

        for (int i = 0; i < rewardZone.childCount; i++)
        {
            if (rewardZone.GetChild(i).GetComponent<JokerInstance>() != null)
                count++;
        }

        return count;
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

        SetRewardChoiceText();
    }

    private void SetRewardChoiceText()
    {
        if (RevardText != null)
            RevardText.text = "Choose one:";
    }
    public void SelectRewardCard(DragCard selectedCard, CardData data)
    {
        PlayRandomSound(cardSoundOne, cardSoundTwo);
        SetSkipRewardVisible(false);
        StartCoroutine(SelectRewardCardCoroutine(selectedCard, data));
    }

    public void SelectRewardJoker(JokerRewardChoice selectedJoker, JokersData data)
    {
        PlayRandomSound(jokerSoundOne, jokerSoundTwo);
        SetSkipRewardVisible(false);
        StartCoroutine(SelectRewardJokerCoroutine(selectedJoker, data));
    }

    private IEnumerator SelectRewardJokerCoroutine(JokerRewardChoice selectedJoker, JokersData data)
    {
        List<JokerRewardChoice> choices = new List<JokerRewardChoice>();

        if (rewardZone != null)
        {
            for (int i = rewardZone.childCount - 1; i >= 0; i--)
            {
                JokerRewardChoice choice = rewardZone.GetChild(i).GetComponent<JokerRewardChoice>();
                if (choice == null) continue;

                choices.Add(choice);

                CanvasGroup canvasGroup = choice.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                    canvasGroup.blocksRaycasts = false;
            }
        }

        foreach (var choice in choices)
        {
            if (choice == null || choice == selectedJoker)
                continue;

            Destroy(choice.gameObject);
        }

        yield return new WaitForSeconds(0.3f);

        if (currentJokers.Count >= MaxJokers)
        {
            pendingJokerData = data;
            pendingRewardJoker = selectedJoker;
            waitingForJokerReplacement = true;
            DragCard.inputLocked = false;
            SetSkipRewardVisible(true);

            if (RevardText != null)
                RevardText.text = "Choose a recept to replace:";

            yield break;
        }

        MoveJokerDataToCurrent(data);
        if (selectedJoker != null && jokerZone != null)
        {
            selectedJoker.transform.SetParent(jokerZone, false);
            selectedJoker.transform.localScale = Vector3.one;

            RectTransform rect = selectedJoker.transform as RectTransform;
            if (rect != null)
                rect.anchoredPosition = Vector2.right * 95f * Mathf.Max(0, currentJokers.Count - 1);
        }

        CompleteRewardPick();
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

        yield return StartCoroutine(WaitSecond(1f));

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

        CompleteRewardPick();
    }

    private void CompleteRewardPick()
    {
        if (currentRewardType == DayRewardType.CardAndJoker && !comboCardCompleted)
        {
            comboCardCompleted = true;
            currentRewardType = DayRewardType.Joker;
            rewardPicksRemaining = 1;
            isChoosingReward = false;
            DragCard.inputLocked = false;

            if (packController != null)
                packController.ResetPack();

            if (RevardText != null)
                RevardText.text = "Reward:";

            return;
        }

        rewardPicksRemaining = Mathf.Max(0, rewardPicksRemaining - 1);

        if (rewardPicksRemaining > 0)
        {
            isChoosingReward = false;
            DragCard.inputLocked = false;

            if (RevardText != null)
                RevardText.text = "Reward:";

            if (packController != null)
                packController.ResetPack();

            return;
        }

        isChoosingReward = false;
        UpdateMinimumScoreUI();
        RevardGeted();
    }

    public void SkipReward()
    {
        if (!isChoosingReward && !waitingForJokerReplacement)
            return;

        SetSkipRewardVisible(false);

        StopCoroutine(nameof(SelectRewardCardCoroutine));
        StopCoroutine(nameof(SelectRewardJokerCoroutine));

        if (rewardZone != null)
        {
            DropZone zone = rewardZone.GetComponent<DropZone>();
            if (zone != null)
                zone.cards.Clear();

            for (int i = rewardZone.childCount - 1; i >= 0; i--)
                Destroy(rewardZone.GetChild(i).gameObject);
        }

        pendingJokerData = null;
        pendingRewardJoker = null;
        waitingForJokerReplacement = false;
        SetSkipRewardVisible(false);
        CompleteRewardPick();
    }

    public void SelectJokerToReplace(JokerInstance oldJoker)
    {
        if (!waitingForJokerReplacement || oldJoker == null || pendingJokerData == null)
            return;

        JokersData oldData = oldJoker.Data;
        if (oldData != null)
        {
            currentJokers.Remove(oldData);
            ReturnJokerToRewardPool(oldData);
        }

        Destroy(oldJoker.gameObject);
        MoveJokerDataToCurrent(pendingJokerData);

        if (pendingRewardJoker != null && jokerZone != null)
        {
            pendingRewardJoker.transform.SetParent(jokerZone, false);
            pendingRewardJoker.transform.localScale = Vector3.one;

            RectTransform rect = pendingRewardJoker.transform as RectTransform;
            if (rect != null)
                rect.anchoredPosition = Vector2.right * 95f * Mathf.Max(0, currentJokers.Count - 1);
        }

        pendingJokerData = null;
        pendingRewardJoker = null;
        waitingForJokerReplacement = false;
        SetSkipRewardVisible(false);
        CompleteRewardPick();
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
        PlayRandomSound(shuffleSoundOne, shuffleSoundTwo);

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

        SetRewardChoiceText();

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
        {
            cardPlaysText.text = cardPlaysRemaining.ToString() + "/" + maxCardPlaysPerRound.ToString();

            if (cardPlaysRemaining == 1)
            {
                if (cardPlaysBlinkCoroutine == null)
                    cardPlaysBlinkCoroutine = StartCoroutine(BlinkCardPlaysText());
            }
            else
            {
                StopCardPlaysBlink();
            }
        }
    }

    private void StopCardPlaysBlink()
    {
        if (cardPlaysBlinkCoroutine != null)
        {
            StopCoroutine(cardPlaysBlinkCoroutine);
            cardPlaysBlinkCoroutine = null;
        }

        if (cardPlaysText != null)
            cardPlaysText.color = Color.white;
    }

    private IEnumerator BlinkCardPlaysText()
    {
        bool red = false;
        while (cardPlaysRemaining == 1 && cardPlaysText != null)
        {
            red = !red;
            cardPlaysText.color = red ? Color.red : Color.white;
            yield return new WaitForSeconds(0.35f);
        }

        if (cardPlaysText != null)
            cardPlaysText.color = Color.white;

        cardPlaysBlinkCoroutine = null;
    }

    private void ShowFloatingNumber(Transform source, int amount, Color color, Vector3 worldOffset, bool useMultiplySymbol = false)
    {
        if (source == null || amount == 0) return;

        Canvas canvas = source.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        GameObject popupObject = new GameObject("FloatingNumber");
        popupObject.transform.SetParent(canvas.rootCanvas.transform, true);
        popupObject.transform.position = source.position + worldOffset;

        float sourceScale = source.lossyScale.x;
        float canvasScale = canvas.rootCanvas.transform.lossyScale.x;
        if (canvasScale > 0f)
        {
            float popupScale = (sourceScale / canvasScale) * 1.35f;
            popupObject.transform.localScale = Vector3.one * Mathf.Clamp(popupScale, 0.4f, 1.35f);
        }

        TextMeshProUGUI text = popupObject.AddComponent<TextMeshProUGUI>();
        RectTransform popupRect = popupObject.transform as RectTransform;
        if (popupRect != null)
            popupRect.sizeDelta = new Vector2(180f, 60f);
        text.font = floatingNumberFont != null ? floatingNumberFont : TMP_Settings.defaultFontAsset;
        text.text = useMultiplySymbol ? "x" + amount.ToString() : (amount > 0 ? "+" + amount.ToString() : amount.ToString());
        text.color = Color.white;
        text.fontSize = 42f;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.outlineWidth = 0.25f;
        text.outlineColor = color;

        CanvasGroup group = popupObject.AddComponent<CanvasGroup>();
        StartCoroutine(AnimateFloatingNumber(popupObject, text, group));
    }

    private IEnumerator AnimateFloatingNumber(GameObject popupObject, TextMeshProUGUI text, CanvasGroup group)
    {
        RectTransform rect = popupObject.transform as RectTransform;
        Vector3 start = popupObject.transform.position;
        Vector3 end = start + Vector3.up * 0.45f;
        float duration = 0.8f;
        float time = 0f;

        while (time < duration && popupObject != null)
        {
            time += Time.deltaTime;
            float progress = Mathf.Clamp01(time / duration);
            popupObject.transform.position = Vector3.Lerp(start, end, progress);
            group.alpha = 1f - progress;
            yield return null;
        }

        if (popupObject != null)
            Destroy(popupObject);
    }

    private void UpdateMinimumScoreUI()
    {
        if (minimumScoreText != null)
            minimumScoreText.text = "Need score:\n" + minimumScore.ToString();
    }

    private void UpdateDayUI()
    {
        string[] dayNames = new string[]
        {
            "Monday",
            "Tuesday",
            "Wednesday",
            "Thursday",
            "Friday",
            "Saturday",
            "Sunday"
        };

        int index = (Day - 1) % 7;
        if (index < 0) index += 7;

        if (dayText != null)
            dayText.text = $"Day {Day}/{TotalDays}\n{dayNames[index]}";

        if (dayProgressBar != null)
        {
            dayProgressBar.minValue = 0f;
            dayProgressBar.maxValue = 1f;
            dayProgressBar.value = Mathf.Clamp01((float)Day / TotalDays);
        }
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

    // helper: find JokerInstance GameObject that corresponds to JokersData
    private JokerInstance FindJokerInstance(JokersData data)
    {
        if (data == null) return null;

        // check joker zone (active equipped jokers)
        if (jokerZone != null)
        {
            for (int i = 0; i < jokerZone.childCount; i++)
            {
                var ji = jokerZone.GetChild(i).GetComponent<JokerInstance>();
                if (ji != null && ji.Data == data)
                    return ji;
            }
        }

        // also check reward zone (when joker was just spawned as a reward)
        if (rewardZone != null)
        {
            for (int i = 0; i < rewardZone.childCount; i++)
            {
                var ji = rewardZone.GetChild(i).GetComponent<JokerInstance>();
                if (ji != null && ji.Data == data)
                    return ji;
            }
        }

        return null;
    }

    private void MoveJokerDataToCurrent(JokersData data)
    {
        if (data == null) return;

        rewardJokerPool.Remove(data);

        if (!currentJokers.Contains(data))
            currentJokers.Add(data);

        // remove the chosen joker from allJokers so it will never be offered again
        if (allJokers != null && allJokers.Length > 0)
        {
            var list = new List<JokersData>(allJokers);
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] == data)
                    list.RemoveAt(i);
            }
            allJokers = list.ToArray();
        }
    }

    private void ReturnJokerToRewardPool(JokersData data)
    {
        if (data == null) return;

        if (!rewardJokerPool.Contains(data))
            rewardJokerPool.Add(data);

        List<JokersData> all = allJokers != null ? new List<JokersData>(allJokers) : new List<JokersData>();
        if (!all.Contains(data))
        {
            all.Add(data);
            allJokers = all.ToArray();
        }
    }
    public void OnEasyClick()
    {

    }
    public void OnNormalClick()
    {

    }
    public void OnHardClick()
    {

    }
}

