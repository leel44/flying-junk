using UnityEngine;

public sealed class VertoBallCollectible : MonoBehaviour
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
            bonusLevelManager.AddVertoBall();
        }
        else
        {
            Debug.LogWarning("VertoBallCollectible could not find BonusLevelManager. VertoBall will be removed without scoring.", this);
        }

        if (collectFeedback == null)
        {
            collectFeedback = FindAnyObjectByType<BonusLevelCollectFeedback>();
        }

        if (collectFeedback != null)
        {
            collectFeedback.PlayVertoBallFeedback(transform.position);
        }

        Destroy(gameObject);
    }
}
