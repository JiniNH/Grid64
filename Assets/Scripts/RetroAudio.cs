using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class RetroAudio : MonoBehaviour
{
    public static RetroAudio Instance;

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

    void Awake()
    {
        if (Instance == null) Instance = this;
        sampleRate = AudioSettings.outputSampleRate;
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

    // 라인 클리어: 툭툭툭 연타로 경쾌하게
    public void PlayClear(int combo = 0)
    {
        StartCoroutine(ClearRoutine());
    }

    private IEnumerator ClearRoutine()
    {
        PlayThock(200f, 0.06f, 0.4f, 0.5f);
        yield return new WaitForSeconds(0.06f);
        PlayThock(260f, 0.06f, 0.4f, 0.5f);
        yield return new WaitForSeconds(0.06f);
        PlayThock(320f, 0.1f, 0.4f, 0.55f);
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

            for (int j = 0; j < channels; j++)
            {
                data[i + j] += value;
            }
        }
    }
}