using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class RetroAudio : MonoBehaviour
{
    public static RetroAudio Instance;

    [Header("콤보 음높이")]
    [Tooltip("콤보 1단계마다 올라가는 반음 수 (0이면 음높이 고정)")]
    [Range(0f, 2f)] public float comboSemitones = 1f;
    [Tooltip("최대 상승 반음 수 (12 = 한 옥타브)")]
    public int maxComboSemitones = 12;

    private float sampleRate = 44100f;
    private float phase = 0f;
    private float duration = 0f;
    private float time = 0f;
    private bool isPlaying = false;

    private float frequency = 0f;
    private float volume = 0.5f;
    private float noiseAmount = 0f;   // 노이즈 섞는 비율 (툭 소리용)
    private float decayPower = 2f;    // 감쇠 급격함 (클수록 더 빨리 사라짐)

    private System.Random rng = new System.Random();
    private Coroutine clearRoutine;   // 연타 중복 방지

        void Awake()
    {
        // 씬마다 하나씩 있으므로 항상 가장 최근 것을 사용
        Instance = this;
        sampleRate = AudioSettings.outputSampleRate;

        // 오디오 스레드가 읽기 전에 메인 스레드에서 설정을 불러옴
        SoundSettings.Load();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // 블록 집기: 가볍고 짧은 "톡"
    public void PlayPickup()
    {
        PlayThock(220f, 0.06f, 0.4f, 0.5f);
    }

    // 블록 놓기: 묵직하게 "툭!" 떨어지는 소리
    public void PlayDrop()
    {
        PlayThock(130f, 0.09f, 0.6f, 0.6f);
    }

    // 라인 클리어: 툭툭툭 연타 (콤보가 높을수록 음이 올라감)
    public void PlayClear(int combo = 0)
    {
        // 1콤보는 기본 음높이, 이후 콤보마다 반음씩 상승 (상한 있음)
        float semitones = Mathf.Min(Mathf.Max(0, combo - 1) * comboSemitones, maxComboSemitones);
        float pitch = Mathf.Pow(2f, semitones / 12f);

        // 이전 연타가 진행 중이면 끊고 새로 시작 (소리가 뒤섞이지 않게)
        if (clearRoutine != null) StopCoroutine(clearRoutine);
        clearRoutine = StartCoroutine(ClearRoutine(pitch));
    }

    private IEnumerator ClearRoutine(float pitch)
    {
        PlayThock(200f * pitch, 0.06f, 0.4f, 0.5f);
        yield return new WaitForSecondsRealtime(0.06f);   // 일시정지·게임오버 중에도 끊기지 않게
        PlayThock(260f * pitch, 0.06f, 0.4f, 0.5f);
        yield return new WaitForSecondsRealtime(0.06f);
        PlayThock(320f * pitch, 0.1f, 0.4f, 0.55f);
        clearRoutine = null;
    }

    // 툭 소리 세팅: 기본 주파수, 길이, 노이즈량, 음량
    private void PlayThock(float freq, float dur, float noise, float vol)
    {
        frequency = freq;
        duration = dur;
        noiseAmount = noise;
        volume = vol;
        time = 0f;
        phase = 0f;
        isPlaying = true;
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!isPlaying) return;

        // [설정] 효과음 음량 (음소거면 0). 버퍼마다 한 번만 읽음
        float gain = SoundSettings.SfxGain;

        for (int i = 0; i < data.Length; i += channels)
        {
            time += 1f / sampleRate;
            if (time > duration)
            {
                isPlaying = false;
                break;
            }

            float t = time / duration;

            // 낮은 사인파 (툭 소리의 몸통)
            phase += 2f * Mathf.PI * frequency / sampleRate;
            if (phase > 2f * Mathf.PI) phase -= 2f * Mathf.PI;
            float tone = Mathf.Sin(phase);

            // 노이즈 (툭 소리의 "타격" 질감) — 앞부분에만 강하게
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            float noiseEnv = Mathf.Pow(1f - t, 8f);  // 노이즈는 맨 처음 순간에만
            float value = (tone * (1f - noiseAmount) + noise * noiseAmount * noiseEnv) * volume;

            // 급격한 감쇠 → "툭!" 하고 순식간에 사라짐
            value *= Mathf.Pow(1f - t, decayPower);

            // 설정 음량 적용
            value *= gain;

            for (int j = 0; j < channels; j++)
            {
                data[i + j] += value;
            }
        }
    }
}