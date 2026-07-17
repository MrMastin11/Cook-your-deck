using UnityEngine;

public class JokerInstance : MonoBehaviour
{
    [Header("Scriptable Object")]
    [SerializeField] private JokersData jokerData;

    public JokersData Data { get; private set; }

    [Header("Runtime Data")]
    public string jokerName;
    public JokersData.ConditionType conditionType;
    public JokersData.CardType cardType;
    public JokersData.ValueType valueType;
    public JokersData.OperationType operationType;
    public int rewardValue;
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
        conditionType = data.conditionType;

        // Беремо перший тип карти зі списку, якщо він існує
        if (data.cardTypes != null && data.cardTypes.Count > 0)
            cardType = data.cardTypes[0];
        else
            cardType = default;

        valueType = data.valueType;
        operationType = data.operationType;
        rewardValue = data.rewardValue;
        description = data.description;
    }
}