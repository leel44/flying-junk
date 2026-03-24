using UnityEngine;

public sealed class VertoBallBehaviour : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform propellerTransform;
    [SerializeField] private Transform visualBodyTransform;

    [Header("Hover")]
    [SerializeField] private float hoverAmplitude = 0.08f;
    [SerializeField] private float hoverFrequency = 1.5f;

    [Header("Propeller")]
    [SerializeField] private float propellerSpinSpeed = 720f;

    [Header("Tilt")]
    [SerializeField] private float maxTiltAngle = 12f;
    [SerializeField] private float tiltSmoothness = 10f;

    [Header("Debug Motion")]
    [SerializeField] private Vector3 debugPlanarVelocity = Vector3.zero;

    private Vector3 baseLocalPosition;
    private Quaternion bodyBaseLocalRotation;
    private Vector3 planarVelocity;

    public Transform PropellerTransform => propellerTransform;
    public Transform VisualBodyTransform => visualBodyTransform;

    private void Awake()
    {
        CacheVisualState();
        planarVelocity = debugPlanarVelocity;
    }

    private void OnValidate()
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

    private void Update()
    {
        if (visualBodyTransform == null || propellerTransform == null)
        {
            return;
        }

        AnimateHover();
        AnimatePropeller();
        AnimateTilt();
    }

    public void SetPlanarVelocity(Vector3 worldVelocity)
    {
        planarVelocity = new Vector3(worldVelocity.x, 0f, worldVelocity.z);
    }

    private void CacheVisualState()
    {
        if (visualBodyTransform != null)
        {
            baseLocalPosition = visualBodyTransform.localPosition;
            bodyBaseLocalRotation = visualBodyTransform.localRotation;
        }
    }

    private void AnimateHover()
    {
        var hoverOffset = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
        visualBodyTransform.localPosition = baseLocalPosition + Vector3.up * hoverOffset;
    }

    private void AnimatePropeller()
    {
        propellerTransform.Rotate(Vector3.up, propellerSpinSpeed * Time.deltaTime, Space.Self);
    }

    private void AnimateTilt()
    {
        var localVelocity = transform.InverseTransformDirection(planarVelocity);
        var normalizedVelocity = Vector3.ClampMagnitude(localVelocity, 1f);

        var targetTilt = Quaternion.Euler(
            normalizedVelocity.z * maxTiltAngle,
            0f,
            -normalizedVelocity.x * maxTiltAngle);

        visualBodyTransform.localRotation = Quaternion.Slerp(
            visualBodyTransform.localRotation,
            bodyBaseLocalRotation * targetTilt,
            tiltSmoothness * Time.deltaTime);
    }
}
