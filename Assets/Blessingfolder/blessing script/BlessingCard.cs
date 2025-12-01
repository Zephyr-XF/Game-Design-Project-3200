using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems; // Required for hover events

public class BlessingCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image godImage;
    public TMP_Text godNameText;
    public TMP_Text descriptionText;
    public Button selectButton;

    private BlessingData data;
    private Vector3 originalScale;

    private void Start()
    {
        originalScale = transform.localScale;
    }

    public void Setup(BlessingData newData)
    {
        data = newData;
        godImage.sprite = data.godImage;
        godNameText.text = data.godName;
        descriptionText.text = data.description;

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => BlessingManager.Instance.ChooseBlessing(data));
    }

    // Mouse Enter: Scale Up
    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = originalScale * 1.1f;
    }

    // Mouse Exit: Scale Back
    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = originalScale;
    }
}
