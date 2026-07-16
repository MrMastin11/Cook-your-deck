using UnityEngine;

[CreateAssetMenu(fileName = "JokersData", menuName = "Scriptable Objects/JokersData")]
public class JokersData : ScriptableObject
{
    public enum ConditionType
    {
        Solo,
        Pair
    }
    public enum CardType
    {
        Meat,
        Sweet,
        Salty,
        Sour,
        Spicy
    }

    public enum ValueType
    {
        Score,
        Multiplier
    }
    public enum operationType
    {
       Add,
       Multiply
    }
    public int RevardValue;

}
