using UnityEngine;
using TMPro;

// 인스펙터에 적힌 고정 문구를 현재 언어로 바꿔줌
[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [Tooltip("Localization 표의 키 (예: resume, pause_title)")]
    public string key;

    private TMP_Text label;

    void Awake()
    {
        label = GetComponent<TMP_Text>();
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

    public void Refresh()
    {
        if (label != null && !string.IsNullOrEmpty(key))
            label.text = Localization.Get(key);
    }
}