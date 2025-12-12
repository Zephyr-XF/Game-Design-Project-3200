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

    public BlessingData Data { get; private set; }
    public Transform scaleTarget; // Assign the object you want to scale (e.g., GodImage). If empty, it scales the whole card.
    private Vector3 originalScale = Vector3.one; // Default to 1

    private void Start()
    {
        InitializeScale();
    }

    public void Setup(BlessingData newData)
    {
        if (newData == null)
        {
            Debug.LogError("BlessingCard received null data!");
            gameObject.SetActive(false);
            return;
        }

        InitializeScale();

        Data = newData;
        godImage.sprite = Data.godImage;
        godNameText.text = Data.godName;
        descriptionText.text = Data.description;
        
        // Show Sanity Cost if applicable
        if (Data.sanityCost > 0)
        {
            descriptionText.text += $"\n<color=#FF0000>Sanity Cost: {Data.sanityCost}</color>";
        }

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => BlessingManager.Instance.ChooseBlessing(Data));
    }

    private void InitializeScale()
    {
        if (scaleTarget == null) scaleTarget = transform;
        
        // Try to get base scale from GodAnimation if present
        var anim = scaleTarget.GetComponent<GodAnimation>();
        if (anim != null)
        {
            originalScale = anim.baseScale;
        }
        else
        {
            // Fallback: if we haven't captured it yet or it's zero, try current
            if (originalScale == Vector3.one && scaleTarget.localScale != Vector3.one && scaleTarget.localScale != Vector3.zero)
            {
                originalScale = scaleTarget.localScale;
            }
        }
    }

    // Mouse Enter: Scale Up
    public void OnPointerEnter(PointerEventData eventData)
    {
        if(scaleTarget != null)
        {
            // Notify animation script to stop breathing
            var anim = scaleTarget.GetComponent<GodAnimation>();
            if (anim != null) anim.SetHover(true);

            scaleTarget.localScale = originalScale * 1.1f;
        }

        // Show Quote
        if (Data != null)
        {
            BlessingManager.Instance.blessingUI.UpdateQuote(Data.quote, transform.position);
        }
    }

    // Mouse Exit: Scale Back
    public void OnPointerExit(PointerEventData eventData)
    {
        if(scaleTarget != null)
        {
            scaleTarget.localScale = originalScale;

            // Notify animation script to resume breathing
            var anim = scaleTarget.GetComponent<GodAnimation>();
            if (anim != null) anim.SetHover(false);
        }
        
        // Hide Quote
        BlessingManager.Instance.blessingUI.ClearQuote();
    }

    public void ResetScale()
    {
        InitializeScale(); // Ensure we have the latest correct scale
        if (scaleTarget != null) scaleTarget.localScale = originalScale;
    }
}
