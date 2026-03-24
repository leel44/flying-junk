using UnityEngine;

public sealed class MvpBootstrap : MonoBehaviour
{
    private const string HoleName = "Hole";
    private const string FloorName = "Floor";
    private const string ObjectName = "SwallowableObject";

    private void Awake()
    {
        SetupCamera();

        if (FindAnyObjectByType<HoleController>() == null)
        {
            CreateHole();
        }

        if (GameObject.Find(FloorName) == null)
        {
            CreateFloor();
        }

        if (FindAnyObjectByType<SwallowableObject>() == null)
        {
            CreateSwallowableObject();
        }
    }

    private void SetupCamera()
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
    }

    private static void CreateFloor()
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = FloorName;
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(1.2f, 1f, 1.2f);

        var renderer = floor.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        renderer.material.color = new Color(0.47f, 0.73f, 0.42f);
    }

    private static void CreateHole()
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

        hole.AddComponent<HoleController>();
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
}

public sealed class HoleController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float clampPadding = 0.75f;

    private void Update()
    {
        var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        var delta = new Vector3(input.x, 0f, input.y) * (moveSpeed * Time.deltaTime);
        transform.position += delta;
        ClampInsideFloor();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<SwallowableObject>() == null)
        {
            return;
        }

        Destroy(other.gameObject);
    }

    private void ClampInsideFloor()
    {
        var position = transform.position;
        position.x = Mathf.Clamp(position.x, -5f + clampPadding, 5f - clampPadding);
        position.z = Mathf.Clamp(position.z, -5f + clampPadding, 5f - clampPadding);
        position.y = 0.05f;

        transform.position = position;
    }
}

public sealed class SwallowableObject : MonoBehaviour
{
}
