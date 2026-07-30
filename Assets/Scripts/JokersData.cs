using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "JokersData", menuName = "Scriptable Objects/JokersData")]
public class JokersData : ScriptableObject
{
    [Header("General")]
    public string jokerName;
    public Sprite jokerSprite;

    public enum ConditionType { Solo, Pair }
    public enum CardType { Meat, Sweet, Salty, Sour, Spicy }
    public enum OperationType { Add, Multiply }

    [System.Serializable]
    public class Reward
    {
        public OperationType operationType;
        public int value;
    }

    [Header("Condition")]
    public ConditionType conditionType;
    public List<CardType> cardTypes = new List<CardType>();

    [Header("Reward")]
    public Reward scoreReward = new Reward();
    public Reward multiplierReward = new Reward();

    [Header("Description")]
    [TextArea(3, 5)]
    public string description;
}