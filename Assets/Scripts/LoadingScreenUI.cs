using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LoadingScreenUI : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private Image progressBarFill;
    private Text loadingText;
    private GameObject loadingPanel;

    void Awake()
    {
        SetupUI();
    }

    private void SetupUI()
    {
        // Create panel
        loadingPanel = new GameObject("LoadingPanel");
        loadingPanel.transform.SetParent(transform);
        RectTransform panelRect = loadingPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = loadingPanel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Create loading text
        GameObject textObj = new GameObject("LoadingText");
        textObj.transform.SetParent(loadingPanel.transform);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchoredPosition = new Vector2(0, 80);
        textRect.sizeDelta = new Vector2(800, 200);

        loadingText = textObj.AddComponent<Text>();
        loadingText.text = "Loading...";
        loadingText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        loadingText.fontSize = 40;
        loadingText.fontStyle = FontStyle.Bold;
        loadingText.alignment = TextAnchor.MiddleCenter;
        loadingText.color = Color.white;

        // Create progress bar background
        GameObject progressBgObj = new GameObject("ProgressBg");
        progressBgObj.transform.SetParent(loadingPanel.transform);
        RectTransform progressBgRect = progressBgObj.AddComponent<RectTransform>();
        progressBgRect.anchoredPosition = new Vector2(0, -50);
        progressBgRect.sizeDelta = new Vector2(500, 50);

        Image progressBgImage = progressBgObj.AddComponent<Image>();
        progressBgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        // Create progress bar fill
        GameObject progressFillObj = new GameObject("ProgressFill");
        progressFillObj.transform.SetParent(progressBgObj.transform);
        RectTransform progressFillRect = progressFillObj.AddComponent<RectTransform>();
        progressFillRect.anchorMin = new Vector2(0, 0.5f);
        progressFillRect.anchorMax = new Vector2(0, 0.5f);
        progressFillRect.pivot = new Vector2(0, 0.5f);
        progressFillRect.anchoredPosition = Vector2.zero;
        progressFillRect.sizeDelta = new Vector2(0, 50);

        progressBarFill = progressFillObj.AddComponent<Image>();
        progressBarFill.color = new Color(0.2f, 0.8f, 0.2f, 1f);

        // Create progress text
        GameObject progressTextObj = new GameObject("ProgressText");
        progressTextObj.transform.SetParent(progressBgObj.transform);
        RectTransform progressTextRect = progressTextObj.AddComponent<RectTransform>();
        progressTextRect.anchorMin = Vector2.zero;
        progressTextRect.anchorMax = Vector2.one;
        progressTextRect.offsetMin = Vector2.zero;
        progressTextRect.offsetMax = Vector2.zero;

        Text progressText = progressTextObj.AddComponent<Text>();
        progressText.text = "0%";
        progressText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        progressText.fontSize = 20;
        progressText.fontStyle = FontStyle.Bold;
        progressText.alignment = TextAnchor.MiddleCenter;
        progressText.color = Color.white;

        // Hide initially
        canvasGroup.alpha = 0;
    }

    public void Show()
    {
        StartCoroutine(FadeIn());
    }

    public void Hide()
    {
        StartCoroutine(FadeOut());
    }

    public void SetProgress(float progress, string message)
    {
        progress = Mathf.Clamp01(progress);

        if (progressBarFill != null)
        {
            RectTransform fillRect = progressBarFill.rectTransform;
            fillRect.sizeDelta = new Vector2(500 * progress, 50);
        }

        if (loadingText != null)
        {
            loadingText.text = message;
        }
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1, 0, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }
}
