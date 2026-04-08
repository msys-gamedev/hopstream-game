using System.Collections;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class LogoDropAnimation : MonoBehaviour
{
    [Tooltip("How far above the resting position the logo starts.")]
    public float startHeight = 1200f;

    [Tooltip("Time (seconds) for the initial fall.")]
    public float fallDuration = 0.5f;

    [Tooltip("Bounce heights as a fraction of startHeight, in order. Empty = no bounces.")]
    public float[] bounceHeights = { 0.35f, 0.15f, 0.05f };

    [Tooltip("Duration of each bounce (up + down).")]
    public float bounceDuration = 0.35f;

    [Tooltip("Squash amount on impact (0 = none, 0.4 = strong).")]
    public float squashAmount = 0.3f;

    [Tooltip("How long the squash/stretch lasts on each impact.")]
    public float squashDuration = 0.12f;

    [Tooltip("Play automatically on enable.")]
    public bool playOnEnable = true;

    private RectTransform rt;
    private Vector2 restPos;
    private Vector3 baseScale;
    private Coroutine routine;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        restPos = rt.anchoredPosition;
        baseScale = rt.localScale;
    }

    private void OnEnable()
    {
        if (playOnEnable)
            Play();
    }

    public void Play()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(AnimateRoutine());
    }

    private IEnumerator AnimateRoutine()
    {
        rt.anchoredPosition = restPos + Vector2.up * startHeight;
        rt.localScale = baseScale;

        // Initial fall (ease-in: gravity-like)
        yield return Move(startHeight, 0f, fallDuration, EaseInQuad);
        yield return Squash();

        // Bounces
        if (bounceHeights != null)
        {
            for (int i = 0; i < bounceHeights.Length; i++)
            {
                float h = startHeight * bounceHeights[i];
                float dur = bounceDuration * Mathf.Sqrt(bounceHeights[i] / Mathf.Max(0.0001f, bounceHeights[0]));
                dur = Mathf.Max(dur, 0.12f);

                // Up
                yield return Move(0f, h, dur * 0.5f, EaseOutQuad);
                // Down
                yield return Move(h, 0f, dur * 0.5f, EaseInQuad);
                yield return Squash();
            }
        }

        rt.anchoredPosition = restPos;
        rt.localScale = baseScale;
        routine = null;
    }

    private IEnumerator Move(float fromY, float toY, float duration, System.Func<float, float> ease)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = ease(Mathf.Clamp01(t / duration));
            float y = Mathf.Lerp(fromY, toY, k);
            rt.anchoredPosition = restPos + Vector2.up * y;
            yield return null;
        }
        rt.anchoredPosition = restPos + Vector2.up * toY;
    }

    private IEnumerator Squash()
    {
        if (squashAmount <= 0f || squashDuration <= 0f) yield break;

        float t = 0f;
        while (t < squashDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(t / squashDuration) * Mathf.PI);
            float sx = 1f + squashAmount * k;
            float sy = 1f - squashAmount * k;
            rt.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);
            yield return null;
        }
        rt.localScale = baseScale;
    }

    private static float EaseInQuad(float x) => x * x;
    private static float EaseOutQuad(float x) => 1f - (1f - x) * (1f - x);
}
