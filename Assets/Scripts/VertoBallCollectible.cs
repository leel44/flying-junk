using UnityEngine;

public sealed class VertoBallCollectible : MonoBehaviour
{
    private const string BubbleFxChildName = "FxBubbleBlast";
    private const string HoleCoinEatingFxChildName = "FxCoinEating";

    private BonusLevelManager bonusLevelManager;
    private BonusLevelCollectFeedback collectFeedback;
    private BonusLevelAudioManager audioManager;
    private HoleController holeController;
    private GameObject holeCoinEatingFxObject;
    private Transform holeCoinEatingFxTransform;
    private ParticleSystem[] holeCoinEatingParticleSystems;
    private Vector3 holeCoinEatingBaseLocalScale;
    private Quaternion holeCoinEatingBaseLocalRotation;
    private bool isCollected;

    private void Awake()
    {
        bonusLevelManager = FindAnyObjectByType<BonusLevelManager>();
        collectFeedback = FindAnyObjectByType<BonusLevelCollectFeedback>();
        audioManager = BonusLevelAudioManager.Instance != null
            ? BonusLevelAudioManager.Instance
            : FindAnyObjectByType<BonusLevelAudioManager>();
        CacheHoleCoinEatingFx();
    }

    public void Collect()
    {
        if (isCollected)
        {
            return;
        }

        isCollected = true;

        PlayHoleCoinEatingFx();
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

    private void CacheHoleCoinEatingFx()
    {
        if (holeCoinEatingFxObject != null && holeCoinEatingParticleSystems != null && holeCoinEatingParticleSystems.Length > 0)
        {
            return;
        }

        if (holeController == null)
        {
            holeController = FindAnyObjectByType<HoleController>();
        }

        if (holeController == null)
        {
            return;
        }

        var fxTransform = holeController.transform.Find(HoleCoinEatingFxChildName);
        if (fxTransform == null)
        {
            return;
        }

        holeCoinEatingFxObject = fxTransform.gameObject;
        holeCoinEatingFxTransform = fxTransform;
        holeCoinEatingParticleSystems = fxTransform.GetComponentsInChildren<ParticleSystem>(true);
        holeCoinEatingBaseLocalScale = fxTransform.localScale;
        holeCoinEatingBaseLocalRotation = fxTransform.localRotation;
    }

    private void PlayHoleCoinEatingFx()
    {
        CacheHoleCoinEatingFx();
        if (holeCoinEatingFxObject == null || holeCoinEatingFxTransform == null)
        {
            Debug.LogWarning("VertoBallCollectible could not find Hole/FxCoinEating for collect feedback.", this);
            return;
        }

        holeCoinEatingFxTransform.localScale = holeCoinEatingBaseLocalScale * Random.Range(1.2f, 1.6f);
        holeCoinEatingFxTransform.localRotation =
            holeCoinEatingBaseLocalRotation * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        holeCoinEatingFxObject.SetActive(true);
        for (var i = 0; i < holeCoinEatingParticleSystems.Length; i++)
        {
            var particleSystem = holeCoinEatingParticleSystems[i];
            if (particleSystem == null)
            {
                continue;
            }

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Play(true);
        }
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
