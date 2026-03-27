using UnityEngine;

public sealed class VertoBallCollectible : MonoBehaviour
{
    private const string BubbleFxChildName = "FxBubbleBlast";

    private BonusLevelManager bonusLevelManager;
    private BonusLevelCollectFeedback collectFeedback;
    private BonusLevelAudioManager audioManager;
    private bool isCollected;

    private void Awake()
    {
        bonusLevelManager = FindAnyObjectByType<BonusLevelManager>();
        collectFeedback = FindAnyObjectByType<BonusLevelCollectFeedback>();
        audioManager = BonusLevelAudioManager.Instance != null
            ? BonusLevelAudioManager.Instance
            : FindAnyObjectByType<BonusLevelAudioManager>();
    }

    public void Collect()
    {
        if (isCollected)
        {
            return;
        }

        isCollected = true;

        PlayDetachedBubbleFx();
        HideCollectedVertoBall();

        if (audioManager == null)
        {
            audioManager = BonusLevelAudioManager.Instance != null
                ? BonusLevelAudioManager.Instance
                : FindAnyObjectByType<BonusLevelAudioManager>();
        }

        audioManager?.PlayVertoBallPickup();

        if (bonusLevelManager == null)
        {
            bonusLevelManager = FindAnyObjectByType<BonusLevelManager>();
        }

        if (bonusLevelManager != null)
        {
            if (collectFeedback == null)
            {
                collectFeedback = FindAnyObjectByType<BonusLevelCollectFeedback>();
            }

            if (collectFeedback != null)
            {
                collectFeedback.PlayVertoBallFeedback(transform.position);
            }

            bonusLevelManager.AddVertoBall();
        }
        else
        {
            Debug.LogWarning("VertoBallCollectible could not find BonusLevelManager. VertoBall will be removed without scoring.", this);
        }

        Destroy(gameObject);
    }

    private void PlayDetachedBubbleFx()
    {
        var bubbleFx = transform.Find(BubbleFxChildName);
        if (bubbleFx == null)
        {
            Debug.LogWarning("VertoBallCollectible could not find FxBubbleBlast child for burst feedback.", this);
            return;
        }

        bubbleFx.SetParent(null, true);
        bubbleFx.gameObject.SetActive(true);

        var particleSystems = bubbleFx.GetComponentsInChildren<ParticleSystem>(true);
        var destroyDelay = 0f;

        for (var i = 0; i < particleSystems.Length; i++)
        {
            var particleSystem = particleSystems[i];
            if (particleSystem == null)
            {
                continue;
            }

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Play(true);

            var main = particleSystem.main;
            var lifetime = main.startLifetime;
            var lifetimeSeconds = lifetime.mode == ParticleSystemCurveMode.TwoConstants
                ? lifetime.constantMax
                : lifetime.constant;
            destroyDelay = Mathf.Max(destroyDelay, main.duration + lifetimeSeconds);
        }

        Destroy(bubbleFx.gameObject, Mathf.Max(0.5f, destroyDelay));
    }

    private void HideCollectedVertoBall()
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        for (var i = 0; i < renderers.Length; i++)
        {
            var rendererComponent = renderers[i];
            if (rendererComponent == null)
            {
                continue;
            }

            rendererComponent.enabled = false;
        }

        var colliders = GetComponentsInChildren<Collider>(true);
        for (var i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }
    }
}
