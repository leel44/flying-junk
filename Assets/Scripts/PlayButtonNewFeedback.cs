using UnityEngine;
using UnityEngine.EventSystems;

public sealed class PlayButtonNewFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private GameObject buttonBaseVisual;
    [SerializeField] private GameObject buttonVisual;
    [SerializeField] private GameObject buttonPushedVisual;
    [SerializeField] private float pressedScale = 0.93f;
    [SerializeField] private float overshootScale = 1.05f;
    [SerializeField] private float releaseDuration = 0.14f;

    private bool isPressed;
    private float releaseElapsed;
    private RectTransform buttonVisualTransform;
    private RectTransform buttonPushedVisualTransform;
    private Vector3 buttonVisualBaseScale = Vector3.one;
    private Vector3 buttonPushedBaseScale = Vector3.one;

    private void Awake()
    {
        if (buttonVisual != null)
        {
            buttonVisualTransform = buttonVisual.transform as RectTransform;
            if (buttonVisualTransform != null)
            {
                buttonVisualBaseScale = buttonVisualTransform.localScale;
            }
        }

        if (buttonPushedVisual != null)
        {
            buttonPushedVisualTransform = buttonPushedVisual.transform as RectTransform;
            if (buttonPushedVisualTransform != null)
            {
                buttonPushedBaseScale = buttonPushedVisualTransform.localScale;
            }
        }

        ApplyNormalVisual();
    }

    private void OnDisable()
    {
        isPressed = false;
        releaseElapsed = 0f;
        ResetVisualScales();
        ApplyNormalVisual();
    }

    private void Update()
    {
        if (isPressed || buttonVisualTransform == null || releaseElapsed >= releaseDuration)
        {
            return;
        }

        releaseElapsed += Time.unscaledDeltaTime;
        var normalizedTime = Mathf.Clamp01(releaseElapsed / Mathf.Max(0.01f, releaseDuration));
        buttonVisualTransform.localScale = buttonVisualBaseScale * EvaluateReleaseScale(normalizedTime);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        releaseElapsed = 0f;
        ApplyPressedVisual();

        if (buttonPushedVisualTransform != null)
        {
            buttonPushedVisualTransform.localScale = buttonPushedBaseScale * pressedScale;
        }
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
        if (buttonVisualTransform != null)
        {
            buttonVisualTransform.localScale = buttonVisualBaseScale * pressedScale;
        }
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
        ResetVisualScales();

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
        ResetVisualScales();

        if (buttonVisual != null)
        {
            buttonVisual.SetActive(false);
        }

        if (buttonPushedVisual != null)
        {
            buttonPushedVisual.SetActive(true);
        }
    }

    private void ResetVisualScales()
    {
        if (buttonVisualTransform != null)
        {
            buttonVisualTransform.localScale = buttonVisualBaseScale;
        }

        if (buttonPushedVisualTransform != null)
        {
            buttonPushedVisualTransform.localScale = buttonPushedBaseScale;
        }
    }
}
