using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BlessingUI : MonoBehaviour
{
    public GameObject blessingPanel;
    public BlessingCard[] cards; // Assign 3 card slots in Inspector
    public TMP_Text quoteText; // 拖拽一个 Text 进来显示台词

    [Header("Animation Settings")]
    public float animationHoldDuration = 1.5f; // Time to hold the chosen card on screen

    private void Start()
    {
        // Ensure the panel is hidden instantly when the game starts
        if (blessingPanel != null)
            blessingPanel.SetActive(false);
        
        if (quoteText != null) quoteText.text = ""; // 一开始清空
    }

    public void UpdateQuote(string text, Vector3 cardPosition)
    {
        if (quoteText != null)
        {
            quoteText.text = text;
            if (!string.IsNullOrEmpty(text))
            {
                // Offset Y to appear above the card. Adjust 150f as needed based on your UI scale.
                quoteText.transform.position = cardPosition + new Vector3(0, 350f, 0); 
                quoteText.gameObject.SetActive(true);
            }
            else
            {
                quoteText.gameObject.SetActive(false);
            }
        }
    }

    public void ClearQuote()
    {
        if (quoteText != null)
        {
            quoteText.text = "";
            quoteText.gameObject.SetActive(false);
        }
    }

    private Coroutine currentAnimation;

    public void ShowBlessings(BlessingData[] options)
    {
        // Stop any running hide animation to prevent conflicts
        if (currentAnimation != null) StopCoroutine(currentAnimation);
        
        // Force reset UI state (Alpha, Scale) in case it was left in a hidden state
        ResetUI();

        // 1. Setup all cards first
        for (int i = 0; i < cards.Length; i++)
        {
            if (i < options.Length)
            {
                cards[i].Setup(options[i]);
                cards[i].gameObject.SetActive(true);
            }
            else
            {
                cards[i].gameObject.SetActive(false);
            }
        }

        // 2. Then show the panel, so the user sees the complete UI instantly
        blessingPanel.SetActive(true);
    }

    private void ResetUI()
    {
        // Reset Panel
        blessingPanel.transform.localScale = Vector3.one;
        CanvasGroup panelGroup = blessingPanel.GetComponent<CanvasGroup>();
        if (panelGroup != null) panelGroup.alpha = 1f;

        // Reset Cards
        foreach (var card in cards)
        {
            card.ResetScale(); // Restore original scale (fixes small image issue)
            CanvasGroup cg = card.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
        }
    }

    public void Hide(BlessingData chosenData, System.Action onComplete = null)
    {
        currentAnimation = StartCoroutine(ChosenOneAnimation(chosenData, onComplete));
    }

    // Overload for simple hiding without callback (if needed)
    public void Hide()
    {
        Hide(null, null);
    }

    private System.Collections.IEnumerator ChosenOneAnimation(BlessingData chosenData, System.Action onComplete)
    {
        float elapsed = 0f;
        
        // Ensure panel is active
        blessingPanel.SetActive(true);
        
        // 1. Identify cards
        BlessingCard chosenCard = null;
        List<BlessingCard> otherCards = new List<BlessingCard>();

        foreach (var card in cards)
        {
            if (card.gameObject.activeSelf)
            {
                if (chosenData != null && card.Data == chosenData)
                {
                    chosenCard = card;
                }
                else
                {
                    otherCards.Add(card);
                }
            }
        }

        // 2. Fade out other cards immediately
        foreach (var card in otherCards)
        {
            CanvasGroup cg = card.GetComponent<CanvasGroup>();
            if (cg == null) cg = card.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f; // Instant vanish or quick fade
        }

        // 3. Animate chosen card
        if (chosenCard != null)
        {
            Vector3 startPos = chosenCard.transform.localPosition;
            Vector3 targetPos = Vector3.zero; // Center of panel (assuming panel is centered)
            Vector3 startScale = chosenCard.transform.localScale;
            Vector3 targetScale = startScale * 1.5f; // Scale up

            while (elapsed < 0.5f) // First half: Move to center and scale up
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / 0.5f);
                float curve = t * t * (3f - 2f * t); // Smooth step

                chosenCard.transform.localPosition = Vector3.Lerp(startPos, targetPos, curve);
                chosenCard.transform.localScale = Vector3.Lerp(startScale, targetScale, curve);

                yield return null;
            }
            
            // Ensure final position
            chosenCard.transform.localPosition = targetPos;
            chosenCard.transform.localScale = targetScale;
            
            // Flash effect (Hold for longer)
            yield return new WaitForSecondsRealtime(animationHoldDuration); // Use public variable
        }

        // 4. Fade out everything (Panel)
        CanvasGroup panelGroup = blessingPanel.GetComponent<CanvasGroup>();
        if (panelGroup == null) panelGroup = blessingPanel.AddComponent<CanvasGroup>();
        float startAlpha = panelGroup.alpha;
        
        elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 0.3f);
            panelGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }

        // Finalize
        blessingPanel.SetActive(false);
        
        // Reset Panel
        panelGroup.alpha = 1f;

        // Reset Cards (important for next time!)
        foreach (var card in cards)
        {
            CanvasGroup cg = card.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
            // Reset position/scale is tricky because LayoutGroup controls position. 
            // We should rely on LayoutGroup to reset positions next time ShowBlessings is called.
            // But scale needs manual reset if we modified it.
            card.transform.localScale = Vector3.one; // Assuming 1 is default
        }

        // Callback
        onComplete?.Invoke();
    }
}


