using UnityEngine;

public sealed class VertoBallCollectible : MonoBehaviour
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
            bonusLevelManager.AddVertoBall();
        }
        else
        {
            Debug.LogWarning("VertoBallCollectible could not find BonusLevelManager. VertoBall will be removed without scoring.", this);
        }

        Destroy(gameObject);
    }
}
