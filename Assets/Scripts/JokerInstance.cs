using UnityEngine;

public class JokerInstance : MonoBehaviour
{
    [Header("Scriptable Object")]
    [SerializeField] private JokersData jokerData;

    public JokersData Data { get; private set; }

    [Header("Runtime Data")]
    public string jokerName;
    public Sprite jokerSprite;

    public JokersData.ConditionType conditionType;
    public JokersData.CardType cardType;

    public JokersData.Reward scoreReward;
    public JokersData.Reward multiplierReward;

    public string description;

    private void Awake()
    {
        Init(jokerData);
    }

    public void Init(JokersData data)
    {
        if (data == null)
        {
            Debug.LogError($"{name}: JokersData не призначено!");
            return;
        }

        Data = data;

        jokerName = data.jokerName;
        jokerSprite = data.jokerSprite;

        conditionType = data.conditionType;

        if (data.cardTypes != null && data.cardTypes.Count > 0)
            cardType = data.cardTypes[0];
        else
            cardType = default;

        scoreReward = data.scoreReward;
        multiplierReward = data.multiplierReward;

        description = data.description;
    }
}