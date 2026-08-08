using System.Collections;
using UnityEngine;

public class JokerInstance : MonoBehaviour
{
    [Header("Scriptable Object")]
    [SerializeField] private JokersData jokerData;

    public JokersData Data { get; set; }

    [Header("Runtime Data")]
    public string jokerName;
    public Sprite jokerSprite;
    public JokersData.ConditionType conditionType;
    public JokersData.CardType cardType;
    public JokersData.Reward scoreReward;
    public JokersData.Reward multiplierReward;
    public string description;

    // animation coroutine handle
    private Coroutine activateCoroutine;

    private void Awake()
    {
        // preserve existing behavior if Init is used elsewhere
        Init(jokerData);
    }

    public void Init(JokersData data)
    {
        if (data == null) return;

        Data = data;

        jokerName = data.jokerName;
        jokerSprite = data.jokerSprite;
        conditionType = data.conditionType;
        // note: JokersData may contain list of cardTypes; JokerInstance stores a single cardType for display
        if (data.cardTypes != null && data.cardTypes.Count > 0)
            cardType = data.cardTypes[0];

        scoreReward = data.scoreReward;
        multiplierReward = data.multiplierReward;
        description = data.description;
    }

    // Public API: play a brief activate animation that scales up and back over totalDuration seconds.
    public void PlayActivateAnimation(float totalDuration, float scaleMultiplier = 1.18f)
    {
        if (activateCoroutine != null)
            StopCoroutine(activateCoroutine);
        activateCoroutine = StartCoroutine(ActivateCoroutine(totalDuration, scaleMultiplier));
    }

    private IEnumerator ActivateCoroutine(float totalDuration, float scaleMultiplier)
    {
        if (totalDuration <= 0f)
            yield break;

        Transform t = transform;
        Vector3 original = t.localScale;
        Vector3 target = original * scaleMultiplier;

        float half = totalDuration * 0.5f;
        float time = 0f;

        // scale up
        while (time < half)
        {
            time += Time.deltaTime;
            float k = Mathf.Clamp01(time / half);
            t.localScale = Vector3.Lerp(original, target, Mathf.SmoothStep(0f, 1f, k));
            yield return null;
        }
        t.localScale = target;

        // scale down
        time = 0f;
        while (time < half)
        {
            time += Time.deltaTime;
            float k = Mathf.Clamp01(time / half);
            t.localScale = Vector3.Lerp(target, original, Mathf.SmoothStep(0f, 1f, k));
            yield return null;
        }
        t.localScale = original;

        activateCoroutine = null;
    }
}