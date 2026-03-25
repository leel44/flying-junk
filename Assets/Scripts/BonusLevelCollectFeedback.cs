using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class BonusLevelCollectFeedback : MonoBehaviour
{
    private sealed class ActiveFeedback
    {
        public RectTransform RectTransform;
        public Vector2 StartLocalPosition;
        public Vector2 TargetLocalPosition;
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

    private readonly List<ActiveFeedback> activeFeedbacks = new List<ActiveFeedback>();

    public void PlayCoinFeedback(Vector3 worldPosition)
    {
        PlayFeedback(worldPosition, coinTarget, coinIconSource, "coin");
    }

    public void PlayVertoBallFeedback(Vector3 worldPosition)
    {
        PlayFeedback(worldPosition, vertoBallTarget, vertoBallIconSource, "vertoball");
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

            Destroy(feedback.RectTransform.gameObject);
            activeFeedbacks.RemoveAt(i);
        }
    }

    private void PlayFeedback(Vector3 worldPosition, RectTransform target, Image iconSource, string feedbackName)
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
            StartLocalPosition = startLocalPoint,
            TargetLocalPosition = targetLocalPoint,
            ElapsedTime = 0f,
        });
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
