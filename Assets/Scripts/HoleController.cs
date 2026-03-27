using UnityEngine;

public sealed class HoleController : MonoBehaviour
{
    private const string FloorObjectName = "Floor";

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float clampPadding = 0.75f;

    private BonusLevelJoystick joystick;
    private float holeY;
    private bool hasWarnedAboutMissingFloor;
    private Transform floorTransform;
    private Collider floorCollider;
    private Renderer floorRenderer;

    private void Awake()
    {
        holeY = transform.position.y;
        joystick = FindAnyObjectByType<BonusLevelJoystick>();
        CacheFloorReferences();
    }

    private void Update()
    {
        if (joystick == null || !joystick.isActiveAndEnabled)
        {
            joystick = FindAnyObjectByType<BonusLevelJoystick>();
        }

        if (joystick != null && joystick.Movement.sqrMagnitude > 0f)
        {
            MoveWithInput(joystick.Movement);
        }
        else
        {
            MoveWithKeyboardFallback();
        }

        ClampInsideFloor();
    }

    private void MoveWithKeyboardFallback()
    {
        var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        MoveWithInput(input);
    }

    private void MoveWithInput(Vector2 input)
    {
        var delta = new Vector3(input.x, 0f, input.y) * (moveSpeed * Time.deltaTime);
        transform.position += delta;
    }

    private void OnTriggerEnter(Collider other)
    {
        var swallowableObject = other.GetComponentInParent<SwallowableObject>();
        if (swallowableObject == null)
        {
            return;
        }

        var coinCollectible = swallowableObject.GetComponent<CoinCollectible>();
        if (coinCollectible != null)
        {
            coinCollectible.Collect();
            return;
        }

        var vertoBallCollectible = swallowableObject.GetComponent<VertoBallCollectible>();
        if (vertoBallCollectible != null)
        {
            vertoBallCollectible.Collect();
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
