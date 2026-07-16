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
    private bool showLevelLabel;
    private bool showMergeLabel;

    public void Init(CardInstance card)
    {
        instance = card;
        Refresh();
    }

    public void Refresh()
    {
        string cardName = instance.data.cardName;

        if (showLevelLabel)
            cardName += " lvl " + instance.level.ToString();

        nameText.text = cardName;
        if (descriptionText != null)
            descriptionText.text = showMergeLabel ? "Merge" : "";

        tasteText.text = instance.type;
        artworkImage.sprite = instance.data.artwork;
        valueText.text = instance.value.ToString();
        multiplierText.text = instance.multiplier.ToString();
    }

    public void SetLevelLabelVisible(bool value)
    {
        showLevelLabel = value;
        Refresh();
    }

    public void SetMergeLabelVisible(bool value)
    {
        showMergeLabel = value;
        Refresh();
    }
}
