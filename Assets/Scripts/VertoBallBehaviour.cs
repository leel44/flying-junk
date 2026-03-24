using UnityEngine;

public sealed class VertoBallBehaviour : MonoBehaviour
{
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

    public Transform PropellerTransform => propellerTransform;
    public Transform VisualBodyTransform => visualBodyTransform;

    private void Awake()
    {
        CacheReferences();
        CacheVisualState();
        groundPosition = transform.position;
        targetPosition = groundPosition;
        BeginIdle();
    }

    private void OnValidate()
    {
        CacheReferences();
        minIdleTime = Mathf.Max(0f, minIdleTime);
        maxIdleTime = Mathf.Max(minIdleTime, maxIdleTime);
        takeoffDuration = Mathf.Max(0.01f, takeoffDuration);
        landingDuration = Mathf.Max(0.01f, landingDuration);
        flightHeight = Mathf.Max(0f, flightHeight);
        movementSpeed = Mathf.Max(0.01f, movementSpeed);
        flightRadius = Mathf.Max(0f, flightRadius);
        maxTargetSelectionRetries = Mathf.Max(1, maxTargetSelectionRetries);
        minTargetDistance = Mathf.Max(0f, minTargetDistance);
        hoverAmplitude = Mathf.Max(0f, hoverAmplitude);
        hoverFrequency = Mathf.Max(0f, hoverFrequency);
        propellerAccelerationSpeed = Mathf.Max(0f, propellerAccelerationSpeed);
        propellerMaxRotationSpeed = Mathf.Max(0f, propellerMaxRotationSpeed);
        propellerDecelerationSpeed = Mathf.Max(0f, propellerDecelerationSpeed);
        propellerLandingInertia = Mathf.Max(0f, propellerLandingInertia);
        maxTiltAngle = Mathf.Max(0f, maxTiltAngle);
        tiltSmoothingSpeed = Mathf.Max(0.01f, tiltSmoothingSpeed);
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
        transform.position = groundPosition;

        if (stateTimer >= currentIdleDuration)
        {
            BeginTakeoff();
        }
    }

    private void UpdateTakeoff()
    {
        var progress = Mathf.Clamp01(stateTimer / takeoffDuration);
        var liftedPosition = groundPosition + Vector3.up * (flightHeight * SmoothStep(progress));
        transform.position = liftedPosition;

        if (progress >= 1f)
        {
            BeginFlying();
        }
    }

    private void UpdateFlying()
    {
        var desiredPosition = new Vector3(targetPosition.x, groundPosition.y + flightHeight, targetPosition.z);
        transform.position = Vector3.MoveTowards(transform.position, desiredPosition, movementSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, desiredPosition) <= 0.01f)
        {
            BeginLanding();
        }
    }

    private void UpdateLanding()
    {
        var progress = Mathf.Clamp01(stateTimer / landingDuration);
        var landedPosition = Vector3.Lerp(stateStartPosition, targetPosition, SmoothStep(progress));
        transform.position = landedPosition;

        if (progress >= 1f)
        {
            groundPosition = targetPosition;
            BeginIdle();
        }
    }

    private void BeginIdle()
    {
        currentState = VertoBallState.Idle;
        stateTimer = 0f;
        currentIdleDuration = Random.Range(minIdleTime, maxIdleTime);
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
        transform.position = new Vector3(transform.position.x, groundPosition.y + flightHeight, transform.position.z);
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
        var planarDistance = Vector3.Distance(
            new Vector3(candidate.x, groundPosition.y, candidate.z),
            new Vector3(groundPosition.x, groundPosition.y, groundPosition.z));

        if (planarDistance < minTargetDistance)
        {
            return false;
        }

        // Future obstacle avoidance can be added here before accepting a candidate.
        return true;
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
        planarVelocity = (transform.position - previousPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
        planarVelocity.y = 0f;

        AnimateHover();
        AnimateTilt();
    }

    private void AnimateHover()
    {
        var hoverOffset = currentState == VertoBallState.Flying
            ? Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude
            : 0f;

        visualBodyTransform.localPosition = baseLocalPosition + Vector3.up * hoverOffset;
    }

    private void AnimateTilt()
    {
        var localVelocity = transform.InverseTransformDirection(planarVelocity);
        var normalizedVelocity = Vector3.ClampMagnitude(localVelocity / Mathf.Max(movementSpeed, 0.01f), 1f);
        var targetTiltX = normalizedVelocity.z * maxTiltAngle;
        var targetTiltZ = -normalizedVelocity.x * maxTiltAngle;

        currentTiltAngleX = Mathf.Lerp(currentTiltAngleX, targetTiltX, tiltSmoothingSpeed * Time.deltaTime);
        currentTiltAngleZ = Mathf.Lerp(currentTiltAngleZ, targetTiltZ, tiltSmoothingSpeed * Time.deltaTime);

        var pivotOffset = Vector3.up * baseLocalPosition.y;
        var targetTilt = Quaternion.Euler(currentTiltAngleX, 0f, currentTiltAngleZ);
        var tiltedBodyOffset = targetTilt * -pivotOffset;

        visualBodyTransform.localRotation = Quaternion.Slerp(
            visualBodyTransform.localRotation,
            bodyBaseLocalRotation * targetTilt,
            tiltSmoothingSpeed * Time.deltaTime);

        visualBodyTransform.localPosition += pivotOffset + tiltedBodyOffset;
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        var gizmoCenter = Application.isPlaying ? groundPosition : transform.position;
        Gizmos.DrawWireSphere(gizmoCenter, flightRadius);

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
