using UnityEngine;

[CreateAssetMenu(fileName = "Card", menuName = "Scriptable Objects/Card")]
public class CardData : ScriptableObject
{
    [Header("Base Info")]
    public string cardName;
    public string type;
    public Sprite artwork;

    [Header("Gameplay")]
    public int value;  
    public int multiplier; 
}


