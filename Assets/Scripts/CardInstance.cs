[System.Serializable]
//конкретні карти в грі
public class CardInstance
{
    public CardData data;
    public string type;
    public int value;
    public int multiplier;

    public CardInstance(CardData data)
    {
        this.data = data;
        type = data.type;
        value = data.value;
        multiplier = data.multiplier;
    }
}
