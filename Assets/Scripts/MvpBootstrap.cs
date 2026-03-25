using UnityEngine;
using UnityEngine.InputSystem;

public sealed class MvpBootstrap : MonoBehaviour
{
    private const string HoleName = "Hole";
    private const string ObjectName = "SwallowableObject";

    private void Awake()
    {
        var holeController = FindAnyObjectByType<HoleController>();
        if (holeController == null)
        {
            holeController = CreateHole();
        }

        EnsureHoleScareTrigger(holeController.transform);
        SetupCamera(holeController != null ? holeController.transform : null);

        if (FindAnyObjectByType<SwallowableObject>() == null)
        {
            CreateSwallowableObject();
        }
    }

    private void SetupCamera(Transform holeTransform)
    {
        var sceneCamera = GetComponent<Camera>();
        if (sceneCamera == null)
        {
            return;
        }

        sceneCamera.orthographic = false;
        sceneCamera.fieldOfView = 60f;
        sceneCamera.backgroundColor = new Color(0.73f, 0.9f, 0.76f);
        transform.position = new Vector3(0f, 5f, -3f);
        transform.rotation = Quaternion.Euler(60f, 0f, 0f);

        var cameraFollow = GetComponent<HoleCameraFollow>();
        if (cameraFollow == null)
        {
            cameraFollow = gameObject.AddComponent<HoleCameraFollow>();
        }

        if (holeTransform != null)
        {
            cameraFollow.Configure(holeTransform, transform.position - holeTransform.position);
        }
    }

    private static HoleController CreateHole()
    {
        var hole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hole.name = HoleName;
        hole.transform.position = new Vector3(0f, 0.05f, 0f);
        hole.transform.localScale = new Vector3(1.1f, 0.05f, 1.1f);

        var renderer = hole.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        renderer.material.color = new Color(0.08f, 0.08f, 0.08f);

        var capsuleCollider = hole.GetComponent<CapsuleCollider>();
        capsuleCollider.isTrigger = true;

        var rigidbody = hole.AddComponent<Rigidbody>();
        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;

        return hole.AddComponent<HoleController>();
    }

    private static void CreateSwallowableObject()
    {
        var swallowableObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        swallowableObject.name = ObjectName;
        swallowableObject.transform.position = new Vector3(2f, 0.5f, 1.5f);
        swallowableObject.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);

        var renderer = swallowableObject.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        renderer.material.color = new Color(1f, 0.85f, 0.35f);

        swallowableObject.AddComponent<SwallowableObject>();
    }

    private static void EnsureHoleScareTrigger(Transform holeTransform)
    {
        if (holeTransform == null)
        {
            return;
        }

        var scareTriggerTransform = holeTransform.Find("HoleScareTrigger");
        if (scareTriggerTransform == null)
        {
            var scareTriggerObject = new GameObject("HoleScareTrigger");
            scareTriggerTransform = scareTriggerObject.transform;
            scareTriggerTransform.SetParent(holeTransform, false);
            scareTriggerTransform.localPosition = Vector3.zero;
            scareTriggerTransform.localRotation = Quaternion.identity;
            scareTriggerTransform.localScale = Vector3.one;
        }

        var rigidbody = scareTriggerTransform.GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = scareTriggerTransform.gameObject.AddComponent<Rigidbody>();
        }

        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;

        var scareTrigger = scareTriggerTransform.GetComponent<HoleScareTrigger>();
        if (scareTrigger == null)
        {
            scareTrigger = scareTriggerTransform.gameObject.AddComponent<HoleScareTrigger>();
        }

        scareTrigger.EnsureTriggerSetup();
    }
}

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

public sealed class HoleCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 followOffset;
    [SerializeField] private float followSmoothness = 8f;

    public void Configure(Transform targetTransform, Vector3 offset)
    {
        target = targetTransform;
        followOffset = offset;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        var targetPosition = target.position + followOffset;
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followSmoothness * Time.deltaTime);
    }
}

