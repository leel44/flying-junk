using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class BonusLevelCollectFeedback : MonoBehaviour
{
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private static void LogDebug(string message)
    {
        Debug.Log(message);
    }

    private sealed class ActiveFlight
    {
        public RectTransform RectTransform;
        public RectTransform BounceTarget;
        public Vector2 StartPosition;
        public Vector2 EndPosition;
        public float ElapsedTime;
    }

    private sealed class ActiveBounce
    {
        public RectTransform Target;
        public Vector3 BaseScale;
        public float ElapsedTime;
    }

    [Header("Optional Overrides")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private RectTransform coinCounterRoot;
    [SerializeField] private RectTransform coinTargetAnchor;
    [SerializeField] private Image coinIconImage;
    [SerializeField] private RectTransform vertoBallCounterRoot;
    [SerializeField] private RectTransform vertoBallTargetAnchor;
    [SerializeField] private Image vertoBallIconImage;

    [Header("Animation")]
    [SerializeField] private float flightDuration = 0.45f;
    [SerializeField] private float flightArcHeight = 90f;
    [SerializeField] private Vector2 iconSize = new Vector2(64f, 64f);
    [SerializeField] private float bounceDuration = 0.2f;
    [SerializeField] private float bounceScaleAmount = 0.12f;
    [SerializeField] private bool enableDebugLogs;

    private readonly List<ActiveFlight> activeFlights = new List<ActiveFlight>();
    private readonly List<ActiveBounce> activeBounces = new List<ActiveBounce>();

    private RectTransform canvasRectTransform;
    private Camera uiCamera;
    private bool hasWarnedAboutMissingReferences;

    public static BonusLevelCollectFeedback Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        ResolveReferences();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        UpdateFlights();
        UpdateBounces();
    }

    public void PlayCoinFeedback(Vector3 worldPosition)
    {
        PlayFeedback(worldPosition, coinIconImage, coinTargetAnchor, coinCounterRoot);
    }

    public void PlayVertoBallFeedback(Vector3 worldPosition)
    {
        PlayFeedback(worldPosition, vertoBallIconImage, vertoBallTargetAnchor, vertoBallCounterRoot);
    }

    private void ResolveReferences()
    {
        if (targetCanvas == null)
        {
            targetCanvas = GetComponent<Canvas>();
        }

        if (targetCanvas != null)
        {
            canvasRectTransform = targetCanvas.GetComponent<RectTransform>();
            uiCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera;
        }

        coinCounterRoot ??= transform.Find("InGameHUD/CoinCounter") as RectTransform;
        coinTargetAnchor ??= transform.Find("InGameHUD/CoinCounter/FutureBindingAnchor") as RectTransform;
        coinIconImage ??= transform.Find("InGameHUD/CoinCounter/Icon")?.GetComponent<Image>();

        vertoBallCounterRoot ??= transform.Find("InGameHUD/VertoBallCounter") as RectTransform;
        vertoBallTargetAnchor ??= transform.Find("InGameHUD/VertoBallCounter/FutureBindingAnchor") as RectTransform;
        vertoBallIconImage ??= transform.Find("InGameHUD/VertoBallCounter/Icon")?.GetComponent<Image>();
    }

    private void PlayFeedback(
        Vector3 worldPosition,
        Image sourceIcon,
        RectTransform targetAnchor,
        RectTransform bounceTarget)
    {
        ResolveReferences();

        if (canvasRectTransform == null || sourceIcon == null || sourceIcon.sprite == null || targetAnchor == null || bounceTarget == null)
        {
            WarnAboutMissingReferences();
            return;
        }

        if (!TryConvertWorldToCanvasPosition(worldPosition, out var startPosition))
        {
            return;
        }

        if (!TryConvertRectToCanvasPosition(targetAnchor, out var endPosition))
        {
            return;
        }

        if (enableDebugLogs)
        {
            LogDebug($"BonusLevelCollectFeedback.PlayFeedback called. World: {worldPosition}, start: {startPosition}, target: {endPosition}");
        }

        var iconObject = new GameObject("FlyingCollectIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var iconTransform = iconObject.GetComponent<RectTransform>();
        iconTransform.SetParent(canvasRectTransform, false);
        iconTransform.anchorMin = new Vector2(0.5f, 0.5f);
        iconTransform.anchorMax = new Vector2(0.5f, 0.5f);
        iconTransform.pivot = new Vector2(0.5f, 0.5f);
        iconTransform.sizeDelta = iconSize;
        iconTransform.localPosition = new Vector3(startPosition.x, startPosition.y, 0f);
        iconTransform.localScale = Vector3.one;
        iconTransform.SetAsLastSibling();

        var iconImage = iconObject.GetComponent<Image>();
        iconImage.sprite = sourceIcon.sprite;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        if (enableDebugLogs)
        {
            LogDebug($"BonusLevelCollectFeedback created icon '{iconObject.name}' with size {iconTransform.sizeDelta} and sprite '{iconImage.sprite.name}'.");
        }

        activeFlights.Add(new ActiveFlight
        {
            RectTransform = iconTransform,
            BounceTarget = bounceTarget,
            StartPosition = startPosition,
            EndPosition = endPosition,
            ElapsedTime = 0f,
        });
    }

    private void UpdateFlights()
    {
        var deltaTime = Time.unscaledDeltaTime;

        for (var i = activeFlights.Count - 1; i >= 0; i--)
        {
            var flight = activeFlights[i];
            if (flight.RectTransform == null)
            {
                activeFlights.RemoveAt(i);
                continue;
            }

            flight.ElapsedTime += deltaTime;
            var normalizedTime = Mathf.Clamp01(flight.ElapsedTime / Mathf.Max(0.01f, flightDuration));
            var easedTime = 1f - Mathf.Pow(1f - normalizedTime, 3f);
            var position = Vector2.LerpUnclamped(flight.StartPosition, flight.EndPosition, easedTime);
            position.y += Mathf.Sin(normalizedTime * Mathf.PI) * flightArcHeight;

            flight.RectTransform.localPosition = new Vector3(position.x, position.y, 0f);

            if (normalizedTime < 1f)
            {
                continue;
            }

            StartBounce(flight.BounceTarget);
            Destroy(flight.RectTransform.gameObject);
            activeFlights.RemoveAt(i);
        }
    }

    private void UpdateBounces()
    {
        var deltaTime = Time.unscaledDeltaTime;

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

    private bool TryConvertWorldToCanvasPosition(Vector3 worldPosition, out Vector2 canvasPosition)
    {
        var worldCamera = Camera.main;
        if (worldCamera == null)
        {
            canvasPosition = default;
            return false;
        }

        var screenPosition = worldCamera.WorldToScreenPoint(worldPosition);
        if (enableDebugLogs)
        {
            LogDebug($"BonusLevelCollectFeedback world->screen: {worldPosition} -> {screenPosition}");
        }

        if (screenPosition.z < 0f)
        {
            canvasPosition = default;
            return false;
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform,
            screenPosition,
            uiCamera,
            out canvasPosition);
    }

    private bool TryConvertRectToCanvasPosition(RectTransform target, out Vector2 canvasPosition)
    {
        var screenPosition = RectTransformUtility.WorldToScreenPoint(uiCamera, target.position);
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform,
            screenPosition,
            uiCamera,
            out canvasPosition);
    }

    private void WarnAboutMissingReferences()
    {
        if (hasWarnedAboutMissingReferences)
        {
            return;
        }

        Debug.LogWarning(
            "BonusLevelCollectFeedback is missing one or more required UI references under BonusLevelCanvas/InGameHUD.",
            this);
        hasWarnedAboutMissingReferences = true;
    }
}
