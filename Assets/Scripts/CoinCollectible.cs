using UnityEngine;

public sealed class CoinCollectible : MonoBehaviour
{
    private BonusLevelManager bonusLevelManager;
    private BonusLevelCollectFeedback collectFeedback;
    private bool isCollected;

    private void Awake()
    {
        bonusLevelManager = FindAnyObjectByType<BonusLevelManager>();
        collectFeedback = FindAnyObjectByType<BonusLevelCollectFeedback>();
    }

    public void Collect()
    {
        if (isCollected)
        {
            return;
        }

        isCollected = true;

        if (bonusLevelManager == null)
        {
            bonusLevelManager = FindAnyObjectByType<BonusLevelManager>();
        }

        if (bonusLevelManager != null)
        {
            bonusLevelManager.AddCoin();
        }
        else
        {
            Debug.LogWarning("CoinCollectible could not find BonusLevelManager. Coin will be removed without scoring.", this);
        }

        if (collectFeedback == null)
        {
            collectFeedback = FindAnyObjectByType<BonusLevelCollectFeedback>();
        }

        if (collectFeedback != null)
        {
            collectFeedback.PlayCoinFeedback(transform.position);
        }

        Destroy(gameObject);
    }
}
