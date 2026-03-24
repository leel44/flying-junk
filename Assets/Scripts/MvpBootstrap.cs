using UnityEngine;

public sealed class MvpBootstrap : MonoBehaviour
{
    private const string HoleName = "Hole";
    private const string ObjectName = "SwallowableObject";

    private void Awake()
    {
        SetupCamera();

        if (FindAnyObjectByType<HoleController>() == null)
        {
            CreateHole();
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

        sceneCamera.orthographic = true;
        sceneCamera.orthographicSize = 5f;
        sceneCamera.backgroundColor = new Color(0.73f, 0.9f, 0.76f);
        transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void CreateHole()
    {
        var hole = new GameObject(HoleName);
        hole.transform.position = Vector3.zero;
        hole.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

        var spriteRenderer = hole.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = SpriteFactory.CreateCircleSprite(64, new Color(0.08f, 0.08f, 0.08f));
        spriteRenderer.sortingOrder = 1;

        var rigidbody2D = hole.AddComponent<Rigidbody2D>();
        rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
        rigidbody2D.gravityScale = 0f;

        var circleCollider = hole.AddComponent<CircleCollider2D>();
        circleCollider.isTrigger = true;
        circleCollider.radius = 0.45f;

        hole.AddComponent<HoleController>();
    }

    private static void CreateSwallowableObject()
    {
        var swallowableObject = new GameObject(ObjectName);
        swallowableObject.transform.position = new Vector3(2f, 1f, 0f);
        swallowableObject.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

        var spriteRenderer = swallowableObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = SpriteFactory.CreateSquareSprite(32, new Color(1f, 0.85f, 0.35f));

        swallowableObject.AddComponent<BoxCollider2D>();
        swallowableObject.AddComponent<SwallowableObject>();
    }
}

public sealed class HoleController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float clampPadding = 0.55f;

    private void Update()
    {
        var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        transform.position += (Vector3)(input * (moveSpeed * Time.deltaTime));
        ClampInsideCamera();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<SwallowableObject>() == null)
        {
            return;
        }

        Destroy(other.gameObject);
    }

    private void ClampInsideCamera()
    {
        var sceneCamera = Camera.main;
        if (sceneCamera == null || !sceneCamera.orthographic)
        {
            return;
        }

        var halfHeight = sceneCamera.orthographicSize;
        var halfWidth = halfHeight * sceneCamera.aspect;
        var position = transform.position;

        position.x = Mathf.Clamp(position.x, -halfWidth + clampPadding, halfWidth - clampPadding);
        position.y = Mathf.Clamp(position.y, -halfHeight + clampPadding, halfHeight - clampPadding);

        transform.position = position;
    }
}

public sealed class SwallowableObject : MonoBehaviour
{
}

internal static class SpriteFactory
{
    public static Sprite CreateCircleSprite(int size, Color color)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        var center = (size - 1) * 0.5f;
        var radius = size * 0.5f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                texture.SetPixel(x, y, distance <= radius ? color : Color.clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    public static Sprite CreateSquareSprite(int size, Color color)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
