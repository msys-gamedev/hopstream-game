using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public int totalScore;
    public float gameTime;

    [Header("Timer Pulse Settings")]
    [Tooltip("Time in seconds when the heartbeat pulse starts.")]
    public float pulseThreshold = 5f;
    public Color pulseColor = Color.red;

    [Header("Buzz Settings")]
    [Tooltip("How long the buzz shakes after time hits zero.")]
    public float buzzDuration = 1f;
    [Tooltip("Intensity of the buzz shake in pixels.")]
    public float buzzIntensity = 5f;
    [Tooltip("Speed of the buzz vibration.")]
    public float buzzSpeed = 50f;

    private bool startGame = false;
    private float timer;

    // Pulse state
    private Color originalTimerColor;
    private Vector3 originalTimerScale;
    private Vector3 originalTimerPos;
    private int lastPulseSecond = -1;
    private float pulseAnimTimer;
    private bool isPulsing;
    private bool statesCaptured;

    // Buzz state
    private float buzzTimer;
    private bool isBuzzing;

    public bool StartGame => startGame;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        timer = gameTime;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateScore(0);
            UIManager.Instance.UpdateTimer("00:00");
        }
    }

    private void Update()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateScore(totalScore);

        CaptureTimerState();
        UpdateBuzz();

        if (!startGame) return;

        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            timer = 0;
            startGame = false;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateTimer("00:00");
                StartBuzz();
                StartCoroutine(DelayedGameOver());
            }
            return;
        }

        int minutes = Mathf.FloorToInt((timer % 3600) / 60);
        int seconds = Mathf.FloorToInt(timer % 60);

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateTimer($"{minutes:00}:{seconds:00}");

        UpdatePulse();
    }

    private void CaptureTimerState()
    {
        if (statesCaptured) return;
        if (UIManager.Instance == null || UIManager.Instance.timerText == null) return;

        var txt = UIManager.Instance.timerText;
        originalTimerColor = txt.color;
        originalTimerScale = txt.transform.localScale;
        originalTimerPos = txt.transform.localPosition;
        statesCaptured = true;
    }

    private void UpdatePulse()
    {
        if (!statesCaptured || UIManager.Instance == null) return;
        var txt = UIManager.Instance.timerText;

        if (timer <= pulseThreshold)
        {
            int currentSecond = Mathf.CeilToInt(timer);

            // Trigger a new pulse each second
            if (currentSecond != lastPulseSecond && currentSecond > 0)
            {
                lastPulseSecond = currentSecond;
                pulseAnimTimer = 0f;
                isPulsing = true;
            }

            if (isPulsing)
            {
                pulseAnimTimer += Time.deltaTime;
                float pulseDuration = 0.4f;
                float t = pulseAnimTimer / pulseDuration;

                if (t >= 1f)
                {
                    txt.transform.localScale = originalTimerScale;
                    txt.color = pulseColor;
                    isPulsing = false;
                }
                else
                {
                    // Beat: scale up fast then back down
                    float scale = 1f + 0.3f * Mathf.Sin(t * Mathf.PI);
                    txt.transform.localScale = originalTimerScale * scale;

                    // Flash pulse color then ease back
                    txt.color = Color.Lerp(pulseColor, originalTimerColor, t * t);
                }
            }
        }
        else
        {
            ResetTimerVisuals();
        }
    }

    private void StartBuzz()
    {
        isBuzzing = true;
        buzzTimer = 0f;
    }

    private void UpdateBuzz()
    {
        if (!isBuzzing || !statesCaptured || UIManager.Instance == null) return;
        var txt = UIManager.Instance.timerText;

        buzzTimer += Time.deltaTime;

        if (buzzTimer >= buzzDuration)
        {
            // Done buzzing — reset position
            isBuzzing = false;
            txt.transform.localPosition = originalTimerPos;
            txt.transform.localScale = originalTimerScale;
            return;
        }

        // Fade out intensity over the buzz duration
        float fade = 1f - (buzzTimer / buzzDuration);
        float offsetX = Mathf.Sin(buzzTimer * buzzSpeed) * buzzIntensity * fade;
        float offsetY = Mathf.Cos(buzzTimer * buzzSpeed * 1.3f) * buzzIntensity * 0.5f * fade;

        txt.transform.localPosition = originalTimerPos + new Vector3(offsetX, offsetY, 0f);

        // Rapid scale jitter
        float scaleJitter = 1f + 0.1f * Mathf.Sin(buzzTimer * buzzSpeed * 2f) * fade;
        txt.transform.localScale = originalTimerScale * scaleJitter;
    }

    private void ResetTimerVisuals()
    {
        if (!statesCaptured || UIManager.Instance == null) return;
        var txt = UIManager.Instance.timerText;

        txt.transform.localScale = originalTimerScale;
        txt.transform.localPosition = originalTimerPos;
        txt.color = originalTimerColor;
        lastPulseSecond = -1;
        isPulsing = false;
    }

    private IEnumerator DelayedGameOver()
    {
        // Wait for buzz to finish, then a small extra pause
        yield return new WaitForSeconds(buzzDuration + 0.5f);

        if (UIManager.Instance != null)
            UIManager.Instance.ShowGameOver(totalScore);
    }

    public void BeginGame()
    {
        timer = gameTime;
        totalScore = 0;
        startGame = true;
        isBuzzing = false;
        ResetTimerVisuals();
    }

    public void AddScore(int score)
    {
        totalScore += score;
    }

    public void DeductScore(int score)
    {
        totalScore = Mathf.Max(0, totalScore - score);
    }
}
