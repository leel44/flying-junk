using UnityEngine;
using UnityEngine.InputSystem;

public sealed class HoleController : MonoBehaviour
{
    private const string FloorObjectName = "Floor";

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float clampPadding = 0.75f;
    [SerializeField] private float targetStopDistance = 0.05f;

    private readonly Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
    private Vector3 targetPosition;
    private bool hasPointerTarget;
    private float holeY;
    private bool hasWarnedAboutMissingFloor;
    private Transform floorTransform;
    private Collider floorCollider;
    private Renderer floorRenderer;

    private void Awake()
    {
        holeY = transform.position.y;
        targetPosition = transform.position;
        CacheFloorReferences();
    }

    private void Update()
    {
        if (TryUpdatePointerTarget())
        {
            MoveTowardsPointerTarget();
        }
        else
        {
            MoveWithKeyboardFallback();
        }

        ClampInsideFloor();
    }

    private bool TryUpdatePointerTarget()
    {
        if (!TryGetPointerScreenPosition(out var screenPosition))
        {
            hasPointerTarget = false;
            return false;
        }

        if (!TryGetWorldPointOnGround(screenPosition, out var worldPoint))
        {
            return false;
        }

        targetPosition = new Vector3(worldPoint.x, holeY, worldPoint.z);
        hasPointerTarget = true;
        return true;
    }

    private bool TryGetPointerScreenPosition(out Vector2 screenPosition)
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        screenPosition = Vector2.zero;
        return false;
    }

    private void MoveTowardsPointerTarget()
    {
        if (!hasPointerTarget)
        {
            return;
        }

        var currentPosition = transform.position;
        var currentFlatPosition = new Vector3(currentPosition.x, holeY, currentPosition.z);
        var targetFlatPosition = new Vector3(targetPosition.x, holeY, targetPosition.z);

        if (Vector3.Distance(currentFlatPosition, targetFlatPosition) <= targetStopDistance)
        {
            transform.position = targetFlatPosition;
            return;
        }

        var nextPosition = Vector3.MoveTowards(currentFlatPosition, targetFlatPosition, moveSpeed * Time.deltaTime);
        transform.position = nextPosition;
    }

    private void MoveWithKeyboardFallback()
    {
        var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        var delta = new Vector3(input.x, 0f, input.y) * (moveSpeed * Time.deltaTime);
        transform.position += delta;
    }

    private bool TryGetWorldPointOnGround(Vector2 screenPosition, out Vector3 worldPoint)
    {
        var sceneCamera = Camera.main;
        if (sceneCamera == null)
        {
            worldPoint = Vector3.zero;
            return false;
        }

        var ray = sceneCamera.ScreenPointToRay(screenPosition);
        if (!groundPlane.Raycast(ray, out var enter))
        {
            worldPoint = Vector3.zero;
            return false;
        }

        worldPoint = ray.GetPoint(enter);
        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        var swallowableObject = other.GetComponentInParent<SwallowableObject>();
        if (swallowableObject == null)
        {
            return;
        }

        Destroy(swallowableObject.gameObject);
    }

    private void ClampInsideFloor()
    {
        var position = transform.position;
        if (TryGetFloorBounds(out var floorBounds))
        {
            position.x = Mathf.Clamp(position.x, floorBounds.min.x + clampPadding, floorBounds.max.x - clampPadding);
            position.z = Mathf.Clamp(position.z, floorBounds.min.z + clampPadding, floorBounds.max.z - clampPadding);
        }

        position.y = holeY;
        transform.position = position;
    }

    private void CacheFloorReferences()
    {
        if (floorTransform != null)
        {
            return;
        }

        var floorObject = GameObject.Find(FloorObjectName);
        if (floorObject == null)
        {
            return;
        }

        floorTransform = floorObject.transform;
        floorCollider = floorObject.GetComponent<Collider>();
        floorRenderer = floorObject.GetComponent<Renderer>();
    }

    private bool TryGetFloorBounds(out Bounds floorBounds)
    {
        CacheFloorReferences();

        if (floorCollider != null)
        {
            floorBounds = floorCollider.bounds;
            return true;
        }

        if (floorRenderer != null)
        {
            floorBounds = floorRenderer.bounds;
            return true;
        }

        if (!hasWarnedAboutMissingFloor)
        {
            Debug.LogWarning(
                "HoleController could not find a usable 'Floor' object with a Collider or Renderer. Floor-based movement bounds are disabled.",
                this);
            hasWarnedAboutMissingFloor = true;
        }

        floorBounds = default;
        return false;
    }
}
