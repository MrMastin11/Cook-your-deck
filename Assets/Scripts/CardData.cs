using UnityEngine;

[CreateAssetMenu(fileName = "Card", menuName = "Scriptable Objects/Card")]
public class CardData : ScriptableObject
{
    [Header("Base Info")]
    public string cardName;
    public Sprite artwork;

    [Header("Gameplay")]
    public int value;        // очки
    public int multiplier;   // множник

    [TextArea]
    public string description;
}


