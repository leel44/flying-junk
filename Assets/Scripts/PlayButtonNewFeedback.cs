using UnityEngine;
using UnityEngine.EventSystems;

public sealed class PlayButtonNewFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private RectTransform scaleRoot;
    [SerializeField] private GameObject buttonBaseVisual;
    [SerializeField] private GameObject buttonVisual;
    [SerializeField] private GameObject buttonPushedVisual;
    [SerializeField] private float pressedScale = 0.93f;
    [SerializeField] private float overshootScale = 1.05f;
    [SerializeField] private float releaseDuration = 0.14f;

    private bool isPressed;
    private float releaseElapsed;
    private Vector3 baseScale = Vector3.one;

    private void Awake()
    {
        if (scaleRoot == null)
        {
            scaleRoot = transform as RectTransform;
        }

        if (scaleRoot != null)
        {
            baseScale = scaleRoot.localScale;
        }

        ApplyNormalVisual();
    }

    private void OnDisable()
    {
        isPressed = false;
        releaseElapsed = 0f;

        if (scaleRoot != null)
        {
            scaleRoot.localScale = baseScale;
        }

        ApplyNormalVisual();
    }

    private void Update()
    {
        if (isPressed || scaleRoot == null || releaseElapsed >= releaseDuration)
        {
            return;
        }

        releaseElapsed += Time.unscaledDeltaTime;
        var normalizedTime = Mathf.Clamp01(releaseElapsed / Mathf.Max(0.01f, releaseDuration));
        scaleRoot.localScale = baseScale * EvaluateReleaseScale(normalizedTime);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        releaseElapsed = 0f;

        if (scaleRoot != null)
        {
            scaleRoot.localScale = baseScale * pressedScale;
        }

        ApplyPressedVisual();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Release();
    }

    private void Release()
    {
        if (!isPressed)
        {
            return;
        }

        isPressed = false;
        releaseElapsed = 0f;
        ApplyNormalVisual();
    }

    private float EvaluateReleaseScale(float normalizedTime)
    {
        if (normalizedTime <= 0.5f)
        {
            return Mathf.Lerp(pressedScale, overshootScale, normalizedTime / 0.5f);
        }

        return Mathf.Lerp(overshootScale, 1f, (normalizedTime - 0.5f) / 0.5f);
    }

    private void ApplyNormalVisual()
    {
        if (buttonBaseVisual != null)
        {
            buttonBaseVisual.SetActive(true);
        }

        if (buttonVisual != null)
        {
            buttonVisual.SetActive(true);
        }

        if (buttonPushedVisual != null)
        {
            buttonPushedVisual.SetActive(false);
        }
    }

    private void ApplyPressedVisual()
    {
        if (buttonBaseVisual != null)
        {
            buttonBaseVisual.SetActive(true);
        }

        if (buttonVisual != null)
        {
            buttonVisual.SetActive(false);
        }

        if (buttonPushedVisual != null)
        {
            buttonPushedVisual.SetActive(true);
        }
    }
}
