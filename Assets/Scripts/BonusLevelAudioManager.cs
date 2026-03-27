using UnityEngine;

public sealed class BonusLevelAudioManager : MonoBehaviour
{
    public static BonusLevelAudioManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioClip gameplayMusic;
    [SerializeField] [Range(0f, 1f)] private float gameplayMusicVolume = 1f;

    [Header("UI / Screen Sfx")]
    [SerializeField] private AudioClip finalFanfareSfx;
    [SerializeField] [Range(0f, 1f)] private float finalFanfareVolume = 1f;
    [SerializeField] private AudioClip playButtonClickSfx;
    [SerializeField] [Range(0f, 1f)] private float playButtonClickVolume = 1f;

    [Header("Collect Pickup Sfx")]
    [SerializeField] private AudioClip coinPickupSfx;
    [SerializeField] [Range(0f, 1f)] private float coinPickupVolume = 1f;
    [SerializeField] private AudioClip vertoBallPickupSfx;
    [SerializeField] [Range(0f, 1f)] private float vertoBallPickupVolume = 1f;

    [Header("Collect Ui Confirm Sfx")]
    [SerializeField] private AudioClip coinUiConfirmSfx;
    [SerializeField] [Range(0f, 1f)] private float coinUiConfirmVolume = 0.45f;
    [SerializeField] private AudioClip vertoBallUiConfirmSfx;
    [SerializeField] [Range(0f, 1f)] private float vertoBallUiConfirmVolume = 0.45f;

    [Header("Pitch Randomization")]
    [SerializeField] private bool enablePitchRandomization = true;
    [SerializeField] private float pitchMin = 0.97f;
    [SerializeField] private float pitchMax = 1.03f;

    [Header("Hole Movement Sfx")]
    [SerializeField] private AudioClip holeMoveLoop;
    [SerializeField] [Range(0f, 1f)] private float holeMoveVolume = 0.25f;
    [SerializeField] private float holeMoveFadeInSpeed = 8f;
    [SerializeField] private float holeMoveFadeOutSpeed = 10f;
    [SerializeField] private float holeMoveMinSpeedThreshold = 0.02f;

    [Header("VertoBall Loop Sfx")]
    [SerializeField] private AudioClip vertoBallBuzzLoop;
    [SerializeField] [Range(0f, 1f)] private float vertoBallBuzzVolume = 1f;
    [SerializeField] private AudioClip vertoBallRustleLoop;
    [SerializeField] [Range(0f, 1f)] private float vertoBallRustleVolume = 1f;

    [Header("Distance / Activation")]
    [SerializeField] private float vertoBallNearDistance = 4f;
    [SerializeField] private float vertoBallFarDistance = 10f;
    [SerializeField] private bool requireCameraVisibilityForVertoBallLoops = true;

    [Header("Internal Sources")]
    [SerializeField] private AudioSource gameplayMusicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource holeMoveSource;

    public AudioClip VertoBallBuzzLoop => vertoBallBuzzLoop;
    public float VertoBallBuzzVolume => vertoBallBuzzVolume;
    public AudioClip VertoBallRustleLoop => vertoBallRustleLoop;
    public float VertoBallRustleVolume => vertoBallRustleVolume;
    public float VertoBallNearDistance => Mathf.Max(0f, vertoBallNearDistance);
    public float VertoBallFarDistance => Mathf.Max(VertoBallNearDistance, vertoBallFarDistance);
    public bool RequireCameraVisibilityForVertoBallLoops => requireCameraVisibilityForVertoBallLoops;

    private HoleController holeController;
    private Vector3 previousHolePosition;
    private bool hasPreviousHolePosition;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple BonusLevelAudioManager instances found. Keeping the first one.", this);
            enabled = false;
            return;
        }

        Instance = this;
        EnsureAudioSources();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        UpdateHoleMovementLoop(Time.unscaledDeltaTime);
    }

    private void OnValidate()
    {
        vertoBallNearDistance = Mathf.Max(0f, vertoBallNearDistance);
        vertoBallFarDistance = Mathf.Max(vertoBallNearDistance, vertoBallFarDistance);
        pitchMax = Mathf.Max(pitchMin, pitchMax);
        holeMoveFadeInSpeed = Mathf.Max(0f, holeMoveFadeInSpeed);
        holeMoveFadeOutSpeed = Mathf.Max(0f, holeMoveFadeOutSpeed);
        holeMoveMinSpeedThreshold = Mathf.Max(0f, holeMoveMinSpeedThreshold);

        if (!Application.isPlaying)
        {
            return;
        }

        EnsureAudioSources();
        ApplyMusicSourceSettings();
        ApplySfxSourceSettings();
        ApplyHoleMoveSourceSettings();
    }

    public void PlayGameplayMusic()
    {
        EnsureAudioSources();
        if (gameplayMusicSource == null || gameplayMusic == null)
        {
            return;
        }

        gameplayMusicSource.clip = gameplayMusic;
        gameplayMusicSource.volume = gameplayMusicVolume;
        gameplayMusicSource.loop = true;

        if (!gameplayMusicSource.isPlaying)
        {
            gameplayMusicSource.Play();
        }
    }

    public void StopGameplayMusic()
    {
        if (gameplayMusicSource == null)
        {
            return;
        }

        gameplayMusicSource.Stop();
    }

    public void PlayFinalFanfare()
    {
        PlayOneShot(finalFanfareSfx, finalFanfareVolume, false);
    }

    public void PlayPlayButtonClick()
    {
        PlayOneShot(playButtonClickSfx, playButtonClickVolume, true);
    }

    public void PlayCoinCollect()
    {
        PlayOneShot(coinUiConfirmSfx, coinUiConfirmVolume, true);
    }

    public void PlayVertoBallCollect()
    {
        PlayOneShot(vertoBallUiConfirmSfx, vertoBallUiConfirmVolume, true);
    }

    public void PlayCoinPickup()
    {
        PlayOneShot(coinPickupSfx, coinPickupVolume, true);
    }

    public void PlayVertoBallPickup()
    {
        PlayOneShot(vertoBallPickupSfx, vertoBallPickupVolume, true);
    }

    private void EnsureAudioSources()
    {
        if (gameplayMusicSource == null)
        {
            gameplayMusicSource = GetOrAddAudioSource("GameplayMusicSource");
        }

        if (sfxSource == null)
        {
            sfxSource = GetOrAddAudioSource("SfxSource");
        }

        if (holeMoveSource == null)
        {
            holeMoveSource = GetOrAddAudioSource("HoleMoveSource");
        }

        ApplyMusicSourceSettings();
        ApplySfxSourceSettings();
        ApplyHoleMoveSourceSettings();
    }

    private void ApplyMusicSourceSettings()
    {
        if (gameplayMusicSource == null)
        {
            return;
        }

        gameplayMusicSource.playOnAwake = false;
        gameplayMusicSource.loop = true;
        gameplayMusicSource.spatialBlend = 0f;
        gameplayMusicSource.clip = gameplayMusic;
        gameplayMusicSource.volume = gameplayMusicVolume;
    }

    private void ApplySfxSourceSettings()
    {
        if (sfxSource == null)
        {
            return;
        }

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
    }

    private void ApplyHoleMoveSourceSettings()
    {
        if (holeMoveSource == null)
        {
            return;
        }

        holeMoveSource.playOnAwake = false;
        holeMoveSource.loop = true;
        holeMoveSource.spatialBlend = 0f;
        holeMoveSource.clip = holeMoveLoop;
        if (!holeMoveSource.isPlaying)
        {
            holeMoveSource.volume = 0f;
        }
    }

    private void PlayOneShot(AudioClip clip, float volume, bool usePitchRandomization)
    {
        EnsureAudioSources();
        if (sfxSource == null || clip == null)
        {
            return;
        }

        var originalPitch = sfxSource.pitch;
        if (usePitchRandomization && enablePitchRandomization)
        {
            sfxSource.pitch = Random.Range(pitchMin, pitchMax);
        }

        sfxSource.PlayOneShot(clip, volume);
        sfxSource.pitch = originalPitch;
    }

    private void UpdateHoleMovementLoop(float deltaTime)
    {
        EnsureAudioSources();
        if (holeMoveSource == null)
        {
            return;
        }

        if (holeController == null)
        {
            holeController = FindAnyObjectByType<HoleController>();
            if (holeController != null)
            {
                previousHolePosition = holeController.transform.position;
                hasPreviousHolePosition = true;
            }
        }

        if (holeController == null || holeMoveLoop == null)
        {
            FadeOutAndStopHoleMoveLoop(deltaTime);
            return;
        }

        var currentHolePosition = holeController.transform.position;
        if (!hasPreviousHolePosition)
        {
            previousHolePosition = currentHolePosition;
            hasPreviousHolePosition = true;
        }

        var movementDelta = (currentHolePosition - previousHolePosition).magnitude;
        previousHolePosition = currentHolePosition;

        if (movementDelta > holeMoveMinSpeedThreshold)
        {
            if (holeMoveSource.clip != holeMoveLoop)
            {
                holeMoveSource.clip = holeMoveLoop;
            }

            if (!holeMoveSource.isPlaying)
            {
                holeMoveSource.Play();
            }

            holeMoveSource.volume = Mathf.MoveTowards(
                holeMoveSource.volume,
                holeMoveVolume,
                holeMoveFadeInSpeed * deltaTime);
            return;
        }

        FadeOutAndStopHoleMoveLoop(deltaTime);
    }

    private void FadeOutAndStopHoleMoveLoop(float deltaTime)
    {
        if (holeMoveSource == null)
        {
            return;
        }

        holeMoveSource.volume = Mathf.MoveTowards(
            holeMoveSource.volume,
            0f,
            holeMoveFadeOutSpeed * deltaTime);

        if (holeMoveSource.isPlaying && holeMoveSource.volume <= 0.001f)
        {
            holeMoveSource.Stop();
        }
    }

    private AudioSource GetOrAddAudioSource(string sourceName)
    {
        var audioSources = GetComponents<AudioSource>();
        for (var i = 0; i < audioSources.Length; i++)
        {
            if (audioSources[i] != null && audioSources[i].name == sourceName)
            {
                return audioSources[i];
            }
        }

        var audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.name = sourceName;
        return audioSource;
    }
}
