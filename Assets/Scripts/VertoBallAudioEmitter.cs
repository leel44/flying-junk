using UnityEngine;

public sealed class VertoBallAudioEmitter : MonoBehaviour
{
    [SerializeField] private AudioSource buzzSource;
    [SerializeField] private AudioSource rustleSource;
    [SerializeField] private Renderer visibilityRenderer;

    private BonusLevelAudioManager audioManager;
    private HoleController holeController;

    private void Awake()
    {
        EnsureAudioSources();

        if (visibilityRenderer == null)
        {
            visibilityRenderer = GetComponentInChildren<Renderer>();
        }
    }

    private void Update()
    {
        audioManager ??= BonusLevelAudioManager.Instance != null
            ? BonusLevelAudioManager.Instance
            : FindAnyObjectByType<BonusLevelAudioManager>();
        holeController ??= FindAnyObjectByType<HoleController>();

        if (audioManager == null)
        {
            StopLoop(buzzSource);
            StopLoop(rustleSource);
            return;
        }

        UpdateLoop(
            buzzSource,
            audioManager.VertoBallBuzzLoop,
            audioManager.VertoBallBuzzVolume,
            EvaluateAudibility());
        UpdateLoop(
            rustleSource,
            audioManager.VertoBallRustleLoop,
            audioManager.VertoBallRustleVolume,
            EvaluateAudibility());
    }

    private float EvaluateAudibility()
    {
        var nearFactor = EvaluateDistanceFactor();
        var visibilityFactor = EvaluateVisibilityFactor();

        if (audioManager != null && audioManager.RequireCameraVisibilityForVertoBallLoops)
        {
            return Mathf.Max(nearFactor, visibilityFactor);
        }

        return nearFactor;
    }

    private float EvaluateDistanceFactor()
    {
        if (audioManager == null || holeController == null)
        {
            return 0f;
        }

        var holePosition = holeController.transform.position;
        var selfPosition = transform.position;
        var horizontalDistance = Vector2.Distance(
            new Vector2(holePosition.x, holePosition.z),
            new Vector2(selfPosition.x, selfPosition.z));

        var farDistance = audioManager.VertoBallFarDistance;
        if (farDistance <= 0f || horizontalDistance >= farDistance)
        {
            return 0f;
        }

        var nearDistance = Mathf.Min(audioManager.VertoBallNearDistance, farDistance);
        if (horizontalDistance <= nearDistance)
        {
            return 1f;
        }

        return 1f - Mathf.InverseLerp(nearDistance, farDistance, horizontalDistance);
    }

    private float EvaluateVisibilityFactor()
    {
        if (audioManager == null || !audioManager.RequireCameraVisibilityForVertoBallLoops)
        {
            return 0f;
        }

        var mainCamera = Camera.main;
        if (mainCamera == null || visibilityRenderer == null)
        {
            return 0f;
        }

        var viewportPosition = mainCamera.WorldToViewportPoint(visibilityRenderer.bounds.center);
        var isVisible =
            viewportPosition.z > 0f &&
            viewportPosition.x >= -0.15f &&
            viewportPosition.x <= 1.15f &&
            viewportPosition.y >= -0.15f &&
            viewportPosition.y <= 1.15f;

        return isVisible ? 1f : 0f;
    }

    private static void UpdateLoop(AudioSource source, AudioClip clip, float baseVolume, float audibility)
    {
        if (source == null)
        {
            return;
        }

        if (clip == null || audibility <= 0f)
        {
            StopLoop(source);
            return;
        }

        if (source.clip != clip)
        {
            source.clip = clip;
        }

        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.volume = baseVolume * audibility;

        if (!source.isPlaying)
        {
            source.Play();
        }
    }

    private void EnsureAudioSources()
    {
        if (buzzSource == null)
        {
            buzzSource = CreateLoopSource("BuzzLoopSource");
        }

        if (rustleSource == null)
        {
            rustleSource = CreateLoopSource("RustleLoopSource");
        }
    }

    private AudioSource CreateLoopSource(string sourceName)
    {
        var audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.name = sourceName;
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 1f;
        audioSource.dopplerLevel = 0f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 15f;
        return audioSource;
    }

    private static void StopLoop(AudioSource source)
    {
        if (source != null && source.isPlaying)
        {
            source.Stop();
        }
    }
}
