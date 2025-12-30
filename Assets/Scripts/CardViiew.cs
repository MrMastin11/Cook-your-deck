using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text tasteText;
    [SerializeField] private Image artworkImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private TMP_Text multiplierText;

    private CardInstance instance;

    public void Init(CardInstance card)
    {
        instance = card;
        Refresh();
    }

    public void Refresh()
    {
        nameText.text = instance.data.cardName;
        artworkImage.sprite = instance.data.artwork;
        valueText.text = instance.value.ToString();
        multiplierText.text = "x" + instance.multiplier;
    }
}
