using UnityEngine;
using UnityEngine.InputSystem;

public sealed class BonusLevelJoystick : MonoBehaviour
{
    [SerializeField] private RectTransform baseTransform;
    [SerializeField] private RectTransform handleTransform;
    [SerializeField] private float radiusScale = 0.4f;

    private Vector2 movement;
    private Vector2 dragStartScreenPosition;
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

        if (pointerPressedThisFrame || !isDragging)
        {
            dragStartScreenPosition = screenPosition;
            isDragging = true;
        }

        UpdateHandle(screenPosition - dragStartScreenPosition);
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

    private void UpdateHandle(Vector2 screenDelta)
    {
        var clampedOffset = Vector2.ClampMagnitude(screenDelta, movementRadius);
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
