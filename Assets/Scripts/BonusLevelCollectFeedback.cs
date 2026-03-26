using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class BonusLevelCollectFeedback : MonoBehaviour
{
    private enum FeedbackType
    {
        Coin,
        VertoBall
    }

    private sealed class ActiveFeedback
    {
        public RectTransform RectTransform;
        public RectTransform BounceTarget;
        public Vector2 StartLocalPosition;
        public Vector2 TargetLocalPosition;
        public float ElapsedTime;
        public FeedbackType FeedbackType;
    }

    private sealed class ActiveBounce
    {
        public RectTransform Target;
        public Vector3 BaseScale;
        public float ElapsedTime;
    }

    [Header("Inspector References")]
    public RectTransform coinTarget;
    public RectTransform vertoBallTarget;
    public Image coinIconSource;
    public Image vertoBallIconSource;
    public RectTransform canvasRoot;

    [Header("Animation")]
    [SerializeField] private float durationSeconds = 0.5f;
    [SerializeField] private Vector2 iconSize = new Vector2(64f, 64f);
    [SerializeField] private float startScale = 1f;
    [SerializeField] private float endScale = 0.82f;
    [SerializeField] private float bounceDuration = 0.2f;
    [SerializeField] private float bounceScaleAmount = 0.12f;

    private readonly List<ActiveFeedback> activeFeedbacks = new List<ActiveFeedback>();
    private readonly List<ActiveBounce> activeBounces = new List<ActiveBounce>();

    private BonusLevelManager bonusLevelManager;

    private void Awake()
    {
        bonusLevelManager = FindAnyObjectByType<BonusLevelManager>();
    }

    public void PlayCoinFeedback(Vector3 worldPosition)
    {
        PlayFeedback(worldPosition, coinTarget, coinIconSource, coinIconSource.rectTransform, "coin", FeedbackType.Coin);
    }

    public void PlayVertoBallFeedback(Vector3 worldPosition)
    {
        PlayFeedback(worldPosition, vertoBallTarget, vertoBallIconSource, vertoBallIconSource.rectTransform, "vertoball", FeedbackType.VertoBall);
    }

    private void Update()
    {
        var deltaTime = Time.unscaledDeltaTime;

        for (var i = activeFeedbacks.Count - 1; i >= 0; i--)
        {
            var feedback = activeFeedbacks[i];
            if (feedback.RectTransform == null)
            {
                activeFeedbacks.RemoveAt(i);
                continue;
            }

            feedback.ElapsedTime += deltaTime;
            var normalizedTime = Mathf.Clamp01(feedback.ElapsedTime / Mathf.Max(0.01f, durationSeconds));
            var easedTime = 1f - Mathf.Pow(1f - normalizedTime, 3f);

            feedback.RectTransform.localPosition = Vector2.LerpUnclamped(
                feedback.StartLocalPosition,
                feedback.TargetLocalPosition,
                easedTime);

            var currentScale = Mathf.Lerp(startScale, endScale, easedTime);
            feedback.RectTransform.localScale = Vector3.one * currentScale;

            if (normalizedTime < 1f)
            {
                continue;
            }

            TriggerArrivalFeedback(feedback);
            Destroy(feedback.RectTransform.gameObject);
            activeFeedbacks.RemoveAt(i);
        }

        UpdateBounces(deltaTime);
    }

    private void PlayFeedback(
        Vector3 worldPosition,
        RectTransform target,
        Image iconSource,
        RectTransform bounceTarget,
        string feedbackName,
        FeedbackType feedbackType)
    {
        if (!HasRequiredReferences(target, iconSource, feedbackName))
        {
            return;
        }

        if (!TryGetCanvasLocalPointFromWorldPosition(worldPosition, out var startLocalPoint))
        {
            Debug.LogWarning($"BonusLevelCollectFeedback could not convert {feedbackName} world position to canvas space.", this);
            return;
        }

        if (!TryGetCanvasLocalPointFromTarget(target, out var targetLocalPoint))
        {
            Debug.LogWarning($"BonusLevelCollectFeedback could not convert {feedbackName} target to canvas space.", this);
            return;
        }

        var iconObject = new GameObject($"Flying{feedbackName}Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var iconTransform = iconObject.GetComponent<RectTransform>();
        iconTransform.SetParent(canvasRoot, false);
        iconTransform.anchorMin = new Vector2(0.5f, 0.5f);
        iconTransform.anchorMax = new Vector2(0.5f, 0.5f);
        iconTransform.pivot = new Vector2(0.5f, 0.5f);
        iconTransform.sizeDelta = iconSize;
        iconTransform.localPosition = new Vector3(startLocalPoint.x, startLocalPoint.y, 0f);
        iconTransform.localScale = Vector3.one * startScale;

        var iconImage = iconObject.GetComponent<Image>();
        iconImage.sprite = iconSource.sprite;
        iconImage.color = Color.white;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        activeFeedbacks.Add(new ActiveFeedback
        {
            RectTransform = iconTransform,
            BounceTarget = bounceTarget,
            StartLocalPosition = startLocalPoint,
            TargetLocalPosition = targetLocalPoint,
            ElapsedTime = 0f,
            FeedbackType = feedbackType,
        });
    }

    private void TriggerArrivalFeedback(ActiveFeedback feedback)
    {
        if (bonusLevelManager == null)
        {
            bonusLevelManager = FindAnyObjectByType<BonusLevelManager>();
        }

        if (bonusLevelManager != null)
        {
            if (feedback.FeedbackType == FeedbackType.Coin)
            {
                bonusLevelManager.PlayCoinCounterFx();
            }
            else
            {
                bonusLevelManager.PlayVertoBallCounterFx();
            }
        }

        StartBounce(feedback.BounceTarget);
    }

    private void StartBounce(RectTransform target)
    {
        if (target == null)
        {
            return;
        }

        for (var i = 0; i < activeBounces.Count; i++)
        {
            var existingBounce = activeBounces[i];
            if (existingBounce.Target != target)
            {
                continue;
            }

            existingBounce.ElapsedTime = 0f;
            target.localScale = existingBounce.BaseScale;
            return;
        }

        activeBounces.Add(new ActiveBounce
        {
            Target = target,
            BaseScale = target.localScale,
            ElapsedTime = 0f,
        });
    }

    private void UpdateBounces(float deltaTime)
    {
        for (var i = activeBounces.Count - 1; i >= 0; i--)
        {
            var bounce = activeBounces[i];
            if (bounce.Target == null)
            {
                activeBounces.RemoveAt(i);
                continue;
            }

            bounce.ElapsedTime += deltaTime;
            var normalizedTime = Mathf.Clamp01(bounce.ElapsedTime / Mathf.Max(0.01f, bounceDuration));
            var scaleMultiplier = 1f + Mathf.Sin(normalizedTime * Mathf.PI) * bounceScaleAmount;
            bounce.Target.localScale = bounce.BaseScale * scaleMultiplier;

            if (normalizedTime < 1f)
            {
                continue;
            }

            bounce.Target.localScale = bounce.BaseScale;
            activeBounces.RemoveAt(i);
        }
    }

    private bool HasRequiredReferences(RectTransform target, Image iconSource, string feedbackName)
    {
        if (canvasRoot == null)
        {
            Debug.LogWarning($"BonusLevelCollectFeedback is missing canvasRoot for {feedbackName} feedback.", this);
            return false;
        }

        if (target == null)
        {
            Debug.LogWarning($"BonusLevelCollectFeedback is missing target for {feedbackName} feedback.", this);
            return false;
        }

        if (iconSource == null || iconSource.sprite == null)
        {
            Debug.LogWarning($"BonusLevelCollectFeedback is missing icon source for {feedbackName} feedback.", this);
            return false;
        }

        return true;
    }

    private bool TryGetCanvasLocalPointFromWorldPosition(Vector3 worldPosition, out Vector2 localPoint)
    {
        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            localPoint = default;
            return false;
        }

        var screenPoint = mainCamera.WorldToScreenPoint(worldPosition);
        if (screenPoint.z < 0f)
        {
            localPoint = default;
            return false;
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRoot,
            screenPoint,
            null,
            out localPoint);
    }

    private bool TryGetCanvasLocalPointFromTarget(RectTransform target, out Vector2 localPoint)
    {
        var screenPoint = RectTransformUtility.WorldToScreenPoint(null, target.position);
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRoot,
            screenPoint,
            null,
            out localPoint);
    }
}
