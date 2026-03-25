using UnityEngine;
using UnityEngine.InputSystem;

public sealed class BonusLevelJoystick : MonoBehaviour
{
    [SerializeField] private RectTransform baseTransform;
    [SerializeField] private RectTransform handleTransform;
    [SerializeField] private float radiusScale = 0.4f;

    private Canvas canvas;
    private Camera uiCamera;
    private Vector2 movement;
    private float movementRadius;
    private bool isDragging;

    public Vector2 Movement => movement;

    private void Awake()
    {
        if (baseTransform == null)
        {
            baseTransform = transform.Find("Base") as RectTransform;
        }

        if (handleTransform == null)
        {
            handleTransform = transform.Find("Handle") as RectTransform;
        }

        canvas = GetComponentInParent<Canvas>();
        uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        RecalculateRadius();
        ResetHandle();
    }

    private void OnEnable()
    {
        ResetHandle();
    }

    private void Update()
    {
        RecalculateRadius();

        if (!TryGetPointerState(out var pointerPressed, out var pointerPressedThisFrame, out var screenPosition))
        {
            ResetHandle();
            return;
        }

        if (!pointerPressed)
        {
            ResetHandle();
            return;
        }

        if (!ScreenPointToLocalPoint(screenPosition, out var localPoint))
        {
            ResetHandle();
            return;
        }

        if (!isDragging)
        {
            if (!pointerPressedThisFrame || !IsInsideBase(localPoint))
            {
                return;
            }

            isDragging = true;
        }

        UpdateHandle(localPoint);
    }

    private void RecalculateRadius()
    {
        if (baseTransform == null)
        {
            movementRadius = 0f;
            return;
        }

        movementRadius = Mathf.Min(baseTransform.rect.width, baseTransform.rect.height) * 0.5f * radiusScale;
    }

    private bool IsInsideBase(Vector2 localPoint)
    {
        return localPoint.sqrMagnitude <= movementRadius * movementRadius;
    }

    private void UpdateHandle(Vector2 localPoint)
    {
        var clampedOffset = Vector2.ClampMagnitude(localPoint, movementRadius);
        movement = movementRadius > 0f ? clampedOffset / movementRadius : Vector2.zero;

        if (handleTransform != null)
        {
            handleTransform.anchoredPosition = clampedOffset;
        }
    }

    private void ResetHandle()
    {
        isDragging = false;
        movement = Vector2.zero;

        if (handleTransform != null)
        {
            handleTransform.anchoredPosition = Vector2.zero;
        }
    }

    private bool ScreenPointToLocalPoint(Vector2 screenPosition, out Vector2 localPoint)
    {
        if (baseTransform == null)
        {
            localPoint = Vector2.zero;
            return false;
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            baseTransform,
            screenPosition,
            uiCamera,
            out localPoint);
    }

    private static bool TryGetPointerState(out bool isPressed, out bool wasPressedThisFrame, out Vector2 screenPosition)
    {
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            isPressed = touch.press.isPressed;
            wasPressedThisFrame = touch.press.wasPressedThisFrame;
            screenPosition = touch.position.ReadValue();
            return true;
        }

        if (Mouse.current != null)
        {
            isPressed = Mouse.current.leftButton.isPressed;
            wasPressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        isPressed = false;
        wasPressedThisFrame = false;
        screenPosition = Vector2.zero;
        return false;
    }
}
