using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BlessingUI : MonoBehaviour
{
    public GameObject blessingPanel;
    public BlessingCard[] cards; // Assign 3 card slots in Inspector

    private void Start()
    {
        // Ensure the panel is hidden when the game starts
        Hide();
    }

    public void ShowBlessings(BlessingData[] options)
    {
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

    public void Hide()
    {
        blessingPanel.SetActive(false);
    }
}


