using UnityEngine;

public sealed class VertoBallBehaviour : MonoBehaviour
{
    private const string FloorObjectName = "Floor";

    private enum VertoBallState
    {
        Idle,
        Takeoff,
        Flying,
        Landing
    }

    [Header("References")]
    [SerializeField] private Transform propellerTransform;
    [SerializeField] private Transform visualBodyTransform;

    [Header("Timing")]
    [SerializeField] private float minIdleTime = 1.5f;
    [SerializeField] private float maxIdleTime = 3f;
    [SerializeField] private float takeoffDuration = 0.6f;
    [SerializeField] private float landingDuration = 0.6f;

    [Header("Flight")]
    [SerializeField] private float flightHeight = 1.25f;
    [SerializeField] private float movementSpeed = 2.5f;
    [SerializeField] private float flightRadius = 4f;
    [SerializeField] private int maxTargetSelectionRetries = 8;
    [SerializeField] private float minTargetDistance = 1f;

    [Header("Avoidance")]
    [SerializeField] private LayerMask obstacleLayers = ~0;
    [SerializeField] private float pathCheckRadius = 0.35f;
    [SerializeField] private float landingCheckRadius = 0.45f;

    [Header("Visuals")]
    [SerializeField] private float hoverAmplitude = 0.08f;
    [SerializeField] private float hoverFrequency = 3f;
    [SerializeField] private float propellerAccelerationSpeed = 1080f;
    [SerializeField] private float propellerMaxRotationSpeed = 720f;
    [SerializeField] private float propellerDecelerationSpeed = 540f;
    [SerializeField] private float propellerLandingInertia = 360f;
    [SerializeField] private float maxTiltAngle = 12f;
    [SerializeField] private float tiltSmoothingSpeed = 10f;

    [Header("Debug")]
    [SerializeField] private bool drawCurrentTargetGizmo = true;
    [SerializeField] private bool drawFloorBoundsGizmo = true;

    private VertoBallState currentState = VertoBallState.Idle;
    private Vector3 groundPosition;
    private Vector3 stateStartPosition;
    private Vector3 targetPosition;
    private Vector3 baseLocalPosition;
    private Quaternion bodyBaseLocalRotation;
    private Vector3 planarVelocity;
    private float stateTimer;
    private float currentIdleDuration;
    private float currentPropellerSpeed;
    private float propellerInertiaSpeed;
    private float currentTiltAngleX;
    private float currentTiltAngleZ;
    private readonly RaycastHit[] pathHitBuffer = new RaycastHit[8];
    private readonly Collider[] landingCheckBuffer = new Collider[8];
    private bool initializedBySpawnManager;
    private bool hasCompletedInitialization;
    private bool hasWarnedAboutMissingFloor;
    private Transform floorTransform;
    private Collider floorCollider;
    private Renderer floorRenderer;

    public Transform PropellerTransform => propellerTransform;
    public Transform VisualBodyTransform => visualBodyTransform;

    public void Initialize(float configuredMinIdleTime, float configuredMaxIdleTime, float configuredFlightRadius, bool fromSpawnManager)
    {
        minIdleTime = Mathf.Max(0f, configuredMinIdleTime);
        maxIdleTime = Mathf.Max(minIdleTime, configuredMaxIdleTime);
        flightRadius = Mathf.Max(0f, configuredFlightRadius);
        initializedBySpawnManager = fromSpawnManager;

        if (!Application.isPlaying)
        {
            return;
        }

        CompleteInitialization();
    }

    public void ApplyRuntimeSettings(float configuredMinIdleTime, float configuredMaxIdleTime, float configuredFlightRadius)
    {
        Initialize(configuredMinIdleTime, configuredMaxIdleTime, configuredFlightRadius, true);
    }

    private void Awake()
    {
        CacheReferences();
        CacheVisualState();
        CacheFloorReferences();
    }

    private void Start()
    {
        if (!initializedBySpawnManager)
        {
            Debug.LogWarning(
                $"VertoBall '{name}' was not initialized by VertoBallSpawnManager. Applying self-initialization using its serialized settings.",
                this);
        }

        CompleteInitialization();
    }

    private void OnValidate()
    {
        CacheReferences();
        CacheFloorReferences();
        minIdleTime = Mathf.Max(0f, minIdleTime);
        maxIdleTime = Mathf.Max(minIdleTime, maxIdleTime);
        takeoffDuration = Mathf.Max(0.01f, takeoffDuration);
        landingDuration = Mathf.Max(0.01f, landingDuration);
        flightHeight = Mathf.Max(0f, flightHeight);
        movementSpeed = Mathf.Max(0.01f, movementSpeed);
        flightRadius = Mathf.Max(0f, flightRadius);
        maxTargetSelectionRetries = Mathf.Max(1, maxTargetSelectionRetries);
        minTargetDistance = Mathf.Max(0f, minTargetDistance);
        pathCheckRadius = Mathf.Max(0.01f, pathCheckRadius);
        landingCheckRadius = Mathf.Max(0.01f, landingCheckRadius);
        hoverAmplitude = Mathf.Max(0f, hoverAmplitude);
        hoverFrequency = Mathf.Max(0f, hoverFrequency);
        propellerAccelerationSpeed = Mathf.Max(0f, propellerAccelerationSpeed);
        propellerMaxRotationSpeed = Mathf.Max(0f, propellerMaxRotationSpeed);
        propellerDecelerationSpeed = Mathf.Max(0f, propellerDecelerationSpeed);
        propellerLandingInertia = Mathf.Max(0f, propellerLandingInertia);
        maxTiltAngle = Mathf.Max(0f, maxTiltAngle);
        tiltSmoothingSpeed = Mathf.Max(0.01f, tiltSmoothingSpeed);
    }

    private void CompleteInitialization()
    {
        if (hasCompletedInitialization)
        {
            return;
        }

        groundPosition = transform.position;
        targetPosition = groundPosition;
        BeginIdle();
        hasCompletedInitialization = true;
    }

    private void Update()
    {
        if (visualBodyTransform == null || propellerTransform == null)
        {
            return;
        }

        var previousPosition = transform.position;

        UpdateState();
        AnimatePropeller();
        AnimateBody(previousPosition);
    }

    private void UpdateState()
    {
        stateTimer += Time.deltaTime;

        switch (currentState)
        {
            case VertoBallState.Idle:
                UpdateIdle();
                break;
            case VertoBallState.Takeoff:
                UpdateTakeoff();
                break;
            case VertoBallState.Flying:
                UpdateFlying();
                break;
            case VertoBallState.Landing:
                UpdateLanding();
                break;
        }
    }

    private void UpdateIdle()
    {
        transform.position = ConstrainPositionToFloor(groundPosition);

        if (stateTimer >= currentIdleDuration)
        {
            BeginTakeoff();
        }
    }

    private void UpdateTakeoff()
    {
        var progress = Mathf.Clamp01(stateTimer / takeoffDuration);
        var liftedPosition = groundPosition + Vector3.up * (flightHeight * SmoothStep(progress));
        transform.position = ConstrainPositionToFloor(liftedPosition);

        if (progress >= 1f)
        {
            BeginFlying();
        }
    }

    private void UpdateFlying()
    {
        var desiredPosition = new Vector3(targetPosition.x, groundPosition.y + flightHeight, targetPosition.z);

        if (IsFlightPathBlocked(transform.position, desiredPosition))
        {
            if (!TrySelectFlightTarget(out var alternateTarget))
            {
                BeginIdle();
                return;
            }

            targetPosition = alternateTarget;
            return;
        }

        transform.position = ConstrainPositionToFloor(
            Vector3.MoveTowards(transform.position, desiredPosition, movementSpeed * Time.deltaTime));

        if (Vector3.Distance(transform.position, desiredPosition) <= 0.01f)
        {
            BeginLanding();
        }
    }

    private void UpdateLanding()
    {
        var progress = Mathf.Clamp01(stateTimer / landingDuration);
        var landedPosition = Vector3.Lerp(stateStartPosition, targetPosition, SmoothStep(progress));
        transform.position = ConstrainPositionToFloor(landedPosition);

        if (progress >= 1f)
        {
            groundPosition = ConstrainPositionToFloor(targetPosition);
            BeginIdle();
        }
    }

    private void BeginIdle()
    {
        currentState = VertoBallState.Idle;
        stateTimer = 0f;
        currentIdleDuration = Random.Range(minIdleTime, maxIdleTime);
        groundPosition = ConstrainPositionToFloor(groundPosition);
        transform.position = groundPosition;
        targetPosition = groundPosition;
    }

    private void BeginTakeoff()
    {
        currentState = VertoBallState.Takeoff;
        stateTimer = 0f;
        stateStartPosition = groundPosition;
    }

    private void BeginFlying()
    {
        if (!TrySelectFlightTarget(out var nextTarget))
        {
            BeginIdle();
            return;
        }

        currentState = VertoBallState.Flying;
        stateTimer = 0f;
        targetPosition = nextTarget;
        transform.position = ConstrainPositionToFloor(
            new Vector3(transform.position.x, groundPosition.y + flightHeight, transform.position.z));
    }

    private void BeginLanding()
    {
        currentState = VertoBallState.Landing;
        stateTimer = 0f;
        stateStartPosition = transform.position;
        targetPosition = new Vector3(targetPosition.x, groundPosition.y, targetPosition.z);
    }

    private bool TrySelectFlightTarget(out Vector3 nextTarget)
    {
        for (var attempt = 0; attempt < maxTargetSelectionRetries; attempt++)
        {
            var randomOffset2D = Random.insideUnitCircle * flightRadius;
            var candidate = groundPosition + new Vector3(randomOffset2D.x, 0f, randomOffset2D.y);

            if (!IsValidTargetPosition(candidate))
            {
                continue;
            }

            nextTarget = candidate;
            return true;
        }

        nextTarget = groundPosition;
        return false;
    }

    private bool IsValidTargetPosition(Vector3 candidate)
    {
        if (!IsInsideFloorXZ(candidate))
        {
            return false;
        }

        var planarDistance = Vector2.Distance(
            new Vector2(candidate.x, candidate.z),
            new Vector2(groundPosition.x, groundPosition.z));

        if (planarDistance < minTargetDistance)
        {
            return false;
        }

        var startFlightPosition = GetFlightPosition(transform.position);
        var targetFlightPosition = GetFlightPosition(candidate);

        if (IsFlightPathBlocked(startFlightPosition, targetFlightPosition))
        {
            return false;
        }

        return IsLandingAreaClear(candidate);
    }

    private void AnimatePropeller()
    {
        UpdatePropellerSpeed();
        propellerTransform.Rotate(Vector3.up, currentPropellerSpeed * Time.deltaTime, Space.Self);
    }

    private void UpdatePropellerSpeed()
    {
        var desiredSpeed = GetDesiredPropellerSpeed();

        if (currentState == VertoBallState.Landing)
        {
            propellerInertiaSpeed = Mathf.Max(propellerInertiaSpeed, propellerLandingInertia);
        }

        if (desiredSpeed > currentPropellerSpeed)
        {
            currentPropellerSpeed = Mathf.MoveTowards(
                currentPropellerSpeed,
                desiredSpeed,
                propellerAccelerationSpeed * Time.deltaTime);

            return;
        }

        var inertiaContribution = 0f;
        if (propellerInertiaSpeed > 0f)
        {
            propellerInertiaSpeed = Mathf.MoveTowards(
                propellerInertiaSpeed,
                0f,
                propellerDecelerationSpeed * Time.deltaTime);

            inertiaContribution = propellerInertiaSpeed;
        }

        var targetSpeed = Mathf.Max(desiredSpeed, inertiaContribution);
        currentPropellerSpeed = Mathf.MoveTowards(
            currentPropellerSpeed,
            targetSpeed,
            propellerDecelerationSpeed * Time.deltaTime);
    }

    private float GetDesiredPropellerSpeed()
    {
        switch (currentState)
        {
            case VertoBallState.Idle:
                return 0f;
            case VertoBallState.Takeoff:
                return propellerMaxRotationSpeed * Mathf.Clamp01(stateTimer / takeoffDuration);
            case VertoBallState.Flying:
                return propellerMaxRotationSpeed;
            case VertoBallState.Landing:
                return propellerMaxRotationSpeed * (1f - Mathf.Clamp01(stateTimer / landingDuration));
            default:
                return 0f;
        }
    }

    private void AnimateBody(Vector3 previousPosition)
    {
        AnimateHover();
        visualBodyTransform.localRotation = bodyBaseLocalRotation;
    }

    private void AnimateHover()
    {
        var hoverOffset = currentState == VertoBallState.Flying
            ? Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude
            : 0f;

        visualBodyTransform.localPosition = baseLocalPosition + Vector3.up * hoverOffset;
    }

    private void CacheReferences()
    {
        if (visualBodyTransform == null)
        {
            var body = transform.Find("vertoball");
            if (body != null)
            {
                visualBodyTransform = body;
            }
        }

        if (propellerTransform == null)
        {
            var propeller = transform.Find("vertoball_propeller");
            if (propeller != null)
            {
                propellerTransform = propeller;
            }
        }
    }

    private void CacheVisualState()
    {
        if (visualBodyTransform != null)
        {
            baseLocalPosition = visualBodyTransform.localPosition;
            bodyBaseLocalRotation = visualBodyTransform.localRotation;
        }
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
                "VertoBall could not find a usable 'Floor' object with a Collider or Renderer. Floor-based movement bounds are disabled.",
                this);
            hasWarnedAboutMissingFloor = true;
        }

        floorBounds = default;
        return false;
    }

    private bool IsInsideFloorXZ(Vector3 worldPosition)
    {
        if (!TryGetFloorBounds(out var floorBounds))
        {
            return true;
        }

        return worldPosition.x >= floorBounds.min.x &&
               worldPosition.x <= floorBounds.max.x &&
               worldPosition.z >= floorBounds.min.z &&
               worldPosition.z <= floorBounds.max.z;
    }

    private Vector3 ConstrainPositionToFloor(Vector3 worldPosition)
    {
        if (!TryGetFloorBounds(out var floorBounds))
        {
            return worldPosition;
        }

        worldPosition.x = Mathf.Clamp(worldPosition.x, floorBounds.min.x, floorBounds.max.x);
        worldPosition.z = Mathf.Clamp(worldPosition.z, floorBounds.min.z, floorBounds.max.z);
        return worldPosition;
    }

    private Vector3 GetFlightPosition(Vector3 worldPosition)
    {
        return new Vector3(worldPosition.x, groundPosition.y + flightHeight, worldPosition.z);
    }

    private bool IsFlightPathBlocked(Vector3 startPosition, Vector3 endPosition)
    {
        var direction = endPosition - startPosition;
        var distance = direction.magnitude;
        if (distance <= 0.001f)
        {
            return false;
        }

        var hitCount = Physics.SphereCastNonAlloc(
            startPosition,
            pathCheckRadius,
            direction.normalized,
            pathHitBuffer,
            distance,
            obstacleLayers,
            QueryTriggerInteraction.Ignore);

        for (var i = 0; i < hitCount; i++)
        {
            var hitCollider = pathHitBuffer[i].collider;
            if (hitCollider == null || IsIgnoredObstacleCollider(hitCollider))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private bool IsLandingAreaClear(Vector3 candidate)
    {
        var checkPosition = candidate + Vector3.up * landingCheckRadius;
        var overlapCount = Physics.OverlapSphereNonAlloc(
            checkPosition,
            landingCheckRadius,
            landingCheckBuffer,
            obstacleLayers,
            QueryTriggerInteraction.Ignore);

        for (var i = 0; i < overlapCount; i++)
        {
            var colliderHit = landingCheckBuffer[i];
            if (colliderHit == null || IsIgnoredObstacleCollider(colliderHit))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private bool IsIgnoredObstacleCollider(Collider hitCollider)
    {
        if (hitCollider.transform.IsChildOf(transform))
        {
            return true;
        }

        CacheFloorReferences();

        if (floorCollider != null && hitCollider == floorCollider)
        {
            return true;
        }

        return floorTransform != null && hitCollider.transform.IsChildOf(floorTransform);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        var gizmoCenter = Application.isPlaying ? groundPosition : transform.position;
        Gizmos.DrawWireSphere(gizmoCenter, flightRadius);

        if (drawFloorBoundsGizmo && TryGetFloorBounds(out var floorBounds))
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(
                floorBounds.center,
                new Vector3(floorBounds.size.x, 0.05f, floorBounds.size.z));
        }

        if (!drawCurrentTargetGizmo)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(targetPosition, 0.12f);
    }

    private static float SmoothStep(float value)
    {
        return value * value * (3f - 2f * value);
    }
}
