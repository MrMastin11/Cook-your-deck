using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "JokersData", menuName = "Scriptable Objects/JokersData")]
public class JokersData : ScriptableObject
{
    [Header("General")]
    public string jokerName;

    public enum ConditionType { Solo, Pair }
    public enum CardType { Meat, Sweet, Salty, Sour, Spicy }
    public enum ValueType { Score, Multiplier }
    public enum OperationType { Add, Multiply }

    [Header("Condition")]
    public ConditionType conditionType;
    public List<CardType> cardTypes = new List<CardType>();

    [Header("Reward")]
    public ValueType valueType;
    public OperationType operationType;
    public int rewardValue;

    [Header("Description")]
    [TextArea(3, 5)]
    public string description;
}