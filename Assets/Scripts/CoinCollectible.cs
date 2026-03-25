using UnityEngine;

public sealed class CoinCollectible : MonoBehaviour
{
    private BonusLevelManager bonusLevelManager;
    private bool isCollected;

    private void Awake()
    {
        bonusLevelManager = FindAnyObjectByType<BonusLevelManager>();
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

        Destroy(gameObject);
    }
}
