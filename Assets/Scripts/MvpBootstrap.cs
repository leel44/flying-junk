using UnityEngine;
public sealed class MvpBootstrap : MonoBehaviour
{
    private const string HoleName = "Hole";
    private const string ObjectName = "SwallowableObject";
    private const string HoleTrailName = "FxTrail";

    [SerializeField] private GameObject holeTrailPrefab;

    private void Awake()
    {
        var holeController = FindAnyObjectByType<HoleController>();
        if (holeController == null)
        {
            holeController = CreateHole();
        }

        EnsureHoleTrail(holeController.transform);
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

    private void EnsureHoleTrail(Transform holeTransform)
    {
        if (holeTransform == null)
        {
            return;
        }

        if (holeTransform.Find(HoleTrailName) != null)
        {
            return;
        }

        if (holeTrailPrefab == null)
        {
            Debug.LogWarning(
                "MvpBootstrap does not have an FxTrail prefab assigned, so the hole trail effect was not attached.",
                this);
            return;
        }

        var trailInstance = Instantiate(holeTrailPrefab, holeTransform, false);
        trailInstance.name = HoleTrailName;
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

