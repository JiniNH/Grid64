using UnityEngine;
using UnityEngine.UI;

// 음량 슬라이더 한 줄 (아이콘 탭 = 음소거, 슬라이더 = 음량)
public class VolumeRow : MonoBehaviour
{
    [Header("대상 채널")]
    public SoundChannel channel = SoundChannel.Bgm;

    [Header("UI 연결")]
    public Button iconButton;
    public Image iconImage;
    public Slider slider;
    public Image sliderFill;             // 슬라이더의 Fill 이미지

    [Header("아이콘")]
    public Sprite iconOn;                // icon_music 또는 icon_sound
    public Sprite iconOff;               // icon_music_off 또는 icon_sound_off

    [Header("색상")]
    public Color iconColor  = new Color(0.10f, 0.35f, 0.54f);   // #1A5A8A
    public Color fillColor  = new Color(0.29f, 0.62f, 0.85f);   // #4A9FD8
    public Color mutedColor = new Color(0.63f, 0.71f, 0.77f);   // #A0B4C4

    [Header("효과음 미리듣기")]
    public float previewInterval = 0.15f;   // 슬라이더를 끄는 동안 너무 자주 울리지 않게

    private float lastPreviewTime = -1f;

    void Awake()
    {
        SoundSettings.Load();

        if (iconButton != null) iconButton.onClick.AddListener(OnIconClicked);

        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.onValueChanged.AddListener(OnSliderChanged);
        }
    }

    // 패널이 열릴 때마다 저장값으로 동기화
    void OnEnable()
    {
        Refresh();
    }

    private void OnIconClicked()
    {
        float vol = SoundSettings.GetVolume(channel);
        bool silent = SoundSettings.IsMuted(channel) || vol <= 0.001f;

        if (silent)
        {
            // 볼륨이 0인 채로 해제하면 여전히 안 들리므로 절반으로 복구
            if (vol <= 0.001f) SoundSettings.SetVolume(channel, 0.5f);
            SoundSettings.SetMuted(channel, false);
            if (channel == SoundChannel.Sfx) PlayPreview(true);
        }
        else
        {
            SoundSettings.SetMuted(channel, true);
        }

        Refresh();
    }

    private void OnSliderChanged(float value)
    {
        SoundSettings.SetVolume(channel, value);   // 0보다 크면 음소거 자동 해제
        Refresh();

        if (channel == SoundChannel.Sfx) PlayPreview(false);
    }

    // 효과음은 계속 나는 소리가 아니라서, 조절할 때 한 번씩 들려줌
    private void PlayPreview(bool force)
    {
        float now = Time.unscaledTime;   // 일시정지 중에도 동작하도록
        if (!force && now - lastPreviewTime < previewInterval) return;
        lastPreviewTime = now;

        if (RetroAudio.Instance != null) RetroAudio.Instance.PlayDrop();
    }

    public void Refresh()
    {
        float vol = SoundSettings.GetVolume(channel);
        bool muted = SoundSettings.IsMuted(channel);
        bool silent = muted || vol <= 0.001f;

        if (slider != null) slider.SetValueWithoutNotify(vol);

        if (iconImage != null)
        {
            iconImage.sprite = silent ? iconOff : iconOn;
            Color c = iconColor;
            c.a = silent ? 0.5f : 1f;
            iconImage.color = c;
        }

        if (sliderFill != null) sliderFill.color = muted ? mutedColor : fillColor;
    }
}