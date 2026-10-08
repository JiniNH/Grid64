using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 설정 패널의 [한국어] [English] 버튼
public class LanguageSelector : MonoBehaviour
{
    [Header("버튼")]
    public Button koreanButton;
    public Button englishButton;
    public TextMeshProUGUI koreanLabel;
    public TextMeshProUGUI englishLabel;

    [Header("색상")]
    public Color selectedColor   = new Color(0.25f, 0.65f, 0.88f);   // #3FA7E0
    public Color unselectedColor = new Color(0.83f, 0.91f, 0.97f);   // #D3E9F8
    public Color selectedText    = Color.white;
    public Color unselectedText  = new Color(0.10f, 0.35f, 0.54f);   // #1A5A8A

    void Awake()
    {
        Localization.Load();
        if (koreanButton != null) koreanButton.onClick.AddListener(() => Localization.Set(Language.Korean));
        if (englishButton != null) englishButton.onClick.AddListener(() => Localization.Set(Language.English));
    }

    void OnEnable()
    {
        Localization.Changed += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        Localization.Changed -= Refresh;
    }

    private void Refresh()
    {
        bool isKorean = Localization.Current == Language.Korean;
        Paint(koreanButton, koreanLabel, isKorean);
        Paint(englishButton, englishLabel, !isKorean);
    }

    private void Paint(Button button, TextMeshProUGUI label, bool selected)
    {
        if (button != null && button.image != null)
            button.image.color = selected ? selectedColor : unselectedColor;
        if (label != null)
            label.color = selected ? selectedText : unselectedText;
    }
}