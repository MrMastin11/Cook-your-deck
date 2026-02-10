using UnityEngine;

public class CardInstance : MonoBehaviour
{
    public CardData data;
    public string type;
    public int value;
    public int multiplier;

    public void Init(CardData data)
    {
        this.data = data;
        type = data.type;
        value = data.value;
        multiplier = data.multiplier;
    }
}
 