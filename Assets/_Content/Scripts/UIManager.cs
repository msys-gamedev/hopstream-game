using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Panels")]
    public GameObject menuPanel;
    public GameObject[] gamePanels;
    public GameObject gameOverPanel;

    [Header("Menu")]
    public Button playButton;

    [Header("Fade")]
    [Tooltip("Full-screen Image used as the fade overlay.")]
    public Image fadeOverlay;
    public float fadeDuration = 0.5f;

    [Header("HUD Texts")]
    public TMP_Text scoreText;
    public TMP_Text timerText;

    [Header("Game Over")]
    public TMP_Text finalScoreText;

    // Score pop state
    private int lastScore = -1;
    private Vector3 originalScoreScale;
    private Coroutine scorePopRoutine;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        ShowMenu();

        if (playButton != null)
            playButton.onClick.AddListener(() => TransitionToGame());

        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(false);
            SetFadeAlpha(0f);
        }
    }

    // ───────── Public API ─────────

    public void ShowMenu()
    {
        SetPanel(menu: true, game: false, gameOver: false);
    }

    public void TransitionToGame()
    {
        StartCoroutine(TransitionToGameRoutine());
    }

    public void ShowGameOver(int finalScore)
    {
        StartCoroutine(GameOverRoutine(finalScore));
    }

    public void ReloadGame()
    {
        StartCoroutine(ReloadGameRoutine());
    }

    public void UpdateScore(int score)
    {
        if (scoreText == null) return;

        scoreText.text = score.ToString("00");

        if (score != lastScore)
        {
            if (lastScore == -1)
                originalScoreScale = scoreText.transform.localScale;

            lastScore = score;

            if (scorePopRoutine != null)
                StopCoroutine(scorePopRoutine);

            scorePopRoutine = StartCoroutine(ScorePop());
        }
    }

    private IEnumerator ScorePop()
    {
        float duration = 0.2f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            float scale = 1f + 0.25f * Mathf.Sin(t * Mathf.PI);
            scoreText.transform.localScale = originalScoreScale * scale;
            yield return null;
        }

        scoreText.transform.localScale = originalScoreScale;
        scorePopRoutine = null;
    }

    public void UpdateTimer(string formatted)
    {
        if (timerText != null)
            timerText.text = formatted;
    }

    // ───────── Transitions ─────────

    private IEnumerator TransitionToGameRoutine()
    {
        yield return StartCoroutine(FadeIn());

        SetPanel(menu: false, game: true, gameOver: false);

        if (GameManager.Instance != null)
            GameManager.Instance.BeginGame();

        yield return StartCoroutine(FadeOut());
    }

    private IEnumerator GameOverRoutine(int finalScore)
    {
        yield return StartCoroutine(FadeIn());

        SetPanel(menu: false, game: false, gameOver: true);

        if (finalScoreText != null)
            finalScoreText.text = finalScore.ToString("00");

        yield return StartCoroutine(FadeOut());
    }

    private IEnumerator ReloadGameRoutine()
    {
        yield return StartCoroutine(FadeIn());

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ───────── Helpers ─────────

    private void SetPanel(bool menu, bool game, bool gameOver)
    {
        if (menuPanel != null)
            menuPanel.SetActive(menu);

        for (int i = 0; i < gamePanels.Length; i++)
        {
            if (gamePanels[i] != null)
                gamePanels[i].SetActive(game);
        }

        if (gameOverPanel != null)
            gameOverPanel.SetActive(gameOver);
    }

    private IEnumerator FadeIn()
    {
        if (fadeOverlay == null) yield break;

        fadeOverlay.gameObject.SetActive(true);
        yield return StartCoroutine(Fade(0f, 1f));
    }

    private IEnumerator FadeOut()
    {
        if (fadeOverlay == null) yield break;

        yield return StartCoroutine(Fade(1f, 0f));
        fadeOverlay.gameObject.SetActive(false);
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        SetFadeAlpha(from);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            SetFadeAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetFadeAlpha(to);
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeOverlay == null) return;
        Color c = fadeOverlay.color;
        c.a = alpha;
        fadeOverlay.color = c;
    }
}
