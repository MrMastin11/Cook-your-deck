[System.Serializable]
public class CardInstance
{
    public CardData data;

    public int value;
    public int multiplier;

    public CardInstance(CardData data)
    {
        this.data = data;
        value = data.value;
        multiplier = data.multiplier;
    }
}
