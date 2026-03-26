using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class BonusLevelManager : MonoBehaviour
{
    private enum BonusLevelState
    {
        WaitingToStart,
        Playing,
        Finished
    }

    [Header("Screens")]
    [SerializeField] private GameObject startScreen;
    [SerializeField] private GameObject inGameHud;
    [SerializeField] private GameObject endScreen;

    [Header("UI")]
    [SerializeField] private Button playButton;
    [SerializeField] private Text timerText;
    [SerializeField] private Text coinCountText;
    [SerializeField] private Text vertoBallCountText;
    [SerializeField] private Text coinResultText;
    [SerializeField] private Text vertoBallResultText;
    [SerializeField] private RectTransform coinFx;
    [SerializeField] private RectTransform vertoBallFx;

    [Header("Timing")]
    [SerializeField] private float levelDurationSeconds = 120f;
    [SerializeField] private float warningThresholdSeconds = 12f;
    [SerializeField] private Color normalTimerColor = Color.white;
    [SerializeField] private Color warningTimerColor = new Color(0.92f, 0.22f, 0.22f, 1f);
    [SerializeField] private float warningPulseAmplitude = 0.08f;
    [SerializeField] private float warningPulseFrequency = 5f;

    private BonusLevelState currentState = BonusLevelState.WaitingToStart;
    private float remainingTime;
    private int coinCount;
    private int vertoBallCount;
    private Vector3 timerTextBaseScale = Vector3.one;
    private ParticleSystem[] coinFxSystems;
    private ParticleSystem[] vertoBallFxSystems;

    public void AddCoin(int amount = 1)
    {
        coinCount += Mathf.Max(0, amount);
        RefreshCountTexts();
        PlayFx(coinFx, coinFxSystems);
    }

    public void AddVertoBall(int amount = 1)
    {
        vertoBallCount += Mathf.Max(0, amount);
        RefreshCountTexts();
        PlayFx(vertoBallFx, vertoBallFxSystems);
    }

    private void Awake()
    {
        if (timerText != null)
        {
            timerTextBaseScale = timerText.rectTransform.localScale;
        }

        coinFxSystems = GetFxSystems(coinFx);
        vertoBallFxSystems = GetFxSystems(vertoBallFx);

        Time.timeScale = 1f;
        ApplyWaitingToStartState();
    }

    private void OnEnable()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(StartLevel);
        }
    }

    private void OnDisable()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(StartLevel);
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (currentState == BonusLevelState.Playing)
        {
            UpdateTimer();
            return;
        }

        if (currentState == BonusLevelState.Finished && TryGetRestartInputDown())
        {
            RestartScene();
        }
    }

    private void ApplyWaitingToStartState()
    {
        currentState = BonusLevelState.WaitingToStart;
        remainingTime = Mathf.Max(1f, levelDurationSeconds);
        coinCount = 0;
        vertoBallCount = 0;
        Time.timeScale = 0f;

        SetScreenState(showStartScreen: true, showHud: false, showEndScreen: false);
        RefreshCountTexts();
        RefreshTimerText();
    }

    private void StartLevel()
    {
        currentState = BonusLevelState.Playing;
        remainingTime = Mathf.Max(1f, levelDurationSeconds);
        coinCount = 0;
        vertoBallCount = 0;
        Time.timeScale = 1f;

        SetScreenState(showStartScreen: false, showHud: true, showEndScreen: false);
        RefreshCountTexts();
        RefreshTimerText();
    }

    private void FinishLevel()
    {
        currentState = BonusLevelState.Finished;
        remainingTime = 0f;
        Time.timeScale = 0f;

        SetScreenState(showStartScreen: false, showHud: false, showEndScreen: true);
        RefreshCountTexts();
        RefreshTimerText();
        ResetTimerWarningVisuals();
    }

    private void UpdateTimer()
    {
        remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
        RefreshTimerText();

        if (remainingTime <= 0f)
        {
            FinishLevel();
        }
    }

    private void RefreshTimerText()
    {
        if (timerText == null)
        {
            return;
        }

        var totalSeconds = Mathf.CeilToInt(remainingTime);
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;
        timerText.text = $"{minutes:00}:{seconds:00}";

        if (currentState == BonusLevelState.Playing && remainingTime <= warningThresholdSeconds)
        {
            timerText.color = warningTimerColor;

            var pulse = 1f + Mathf.Sin(Time.unscaledTime * warningPulseFrequency) * warningPulseAmplitude;
            timerText.rectTransform.localScale = timerTextBaseScale * pulse;
            return;
        }

        ResetTimerWarningVisuals();
    }

    private void RefreshCountTexts()
    {
        SetText(coinCountText, $"x {coinCount:00}");
        SetText(vertoBallCountText, $"x {vertoBallCount:00}");
        SetText(coinResultText, $"x {coinCount:00}");
        SetText(vertoBallResultText, $"x {vertoBallCount:00}");
    }

    private void ResetTimerWarningVisuals()
    {
        if (timerText == null)
        {
            return;
        }

        timerText.color = normalTimerColor;
        timerText.rectTransform.localScale = timerTextBaseScale;
    }

    private static ParticleSystem[] GetFxSystems(RectTransform fxRoot)
    {
        return fxRoot != null ? fxRoot.GetComponentsInChildren<ParticleSystem>(true) : null;
    }

    private static void PlayFx(RectTransform fxRoot, ParticleSystem[] systems)
    {
        if (fxRoot == null || systems == null || systems.Length == 0)
        {
            return;
        }

        fxRoot.gameObject.SetActive(true);

        for (var i = 0; i < systems.Length; i++)
        {
            var particleSystem = systems[i];
            if (particleSystem == null)
            {
                continue;
            }

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Play(true);
        }
    }

    private void SetScreenState(bool showStartScreen, bool showHud, bool showEndScreen)
    {
        if (startScreen != null)
        {
            startScreen.SetActive(showStartScreen);
        }

        if (inGameHud != null)
        {
            inGameHud.SetActive(showHud);
        }

        if (endScreen != null)
        {
            endScreen.SetActive(showEndScreen);
        }
    }

    private static void SetText(Text label, string value)
    {
        if (label != null)
        {
            label.text = value;
        }
    }

    private static bool TryGetRestartInputDown()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            return true;
        }

        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }

    private static void RestartScene()
    {
        Time.timeScale = 1f;
        var activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }
}
