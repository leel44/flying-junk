using UnityEngine;

public sealed class CoinCollectible : MonoBehaviour
{
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

        if (audioManager == null)
        {
            audioManager = BonusLevelAudioManager.Instance != null
                ? BonusLevelAudioManager.Instance
                : FindAnyObjectByType<BonusLevelAudioManager>();
        }

        audioManager?.PlayCoinPickup();

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
                collectFeedback.PlayCoinFeedback(transform.position);
            }

            bonusLevelManager.AddCoin();
        }
        else
        {
            Debug.LogWarning("CoinCollectible could not find BonusLevelManager. Coin will be removed without scoring.", this);
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
            Debug.LogWarning("CoinCollectible could not find Hole/FxCoinEating for collect feedback.", this);
            return;
        }

        holeCoinEatingFxTransform.localScale = holeCoinEatingBaseLocalScale;
        holeCoinEatingFxTransform.localRotation = holeCoinEatingBaseLocalRotation;
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
}
