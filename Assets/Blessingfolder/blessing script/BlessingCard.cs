using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BlessingCard : MonoBehaviour
{
    public Image godImage;
    public TMP_Text godNameText;
    public TMP_Text descriptionText;
    public Button selectButton;

    private BlessingData data;

    public void Setup(BlessingData newData)
    {
        data = newData;
        godImage.sprite = data.godImage;
        godNameText.text = data.godName;
        descriptionText.text = data.description;

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => BlessingManager.Instance.ChooseBlessing(data));
    }
}
