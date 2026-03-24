using UnityEngine;

public sealed class HoleScareTrigger : MonoBehaviour
{
    [SerializeField] private SphereCollider scareCollider;
    [SerializeField] private float scareRadius = 2.5f;

    public void EnsureTriggerSetup()
    {
        if (scareCollider == null)
        {
            scareCollider = GetComponent<SphereCollider>();
        }

        if (scareCollider == null)
        {
            scareCollider = gameObject.AddComponent<SphereCollider>();
        }

        scareCollider.isTrigger = true;
        scareCollider.center = Vector3.zero;
        scareCollider.radius = Mathf.Max(0.01f, scareRadius);
    }

    private void Awake()
    {
        EnsureTriggerSetup();
    }

    private void OnValidate()
    {
        scareRadius = Mathf.Max(0.01f, scareRadius);
        if (scareCollider != null)
        {
            scareCollider.isTrigger = true;
            scareCollider.center = Vector3.zero;
            scareCollider.radius = scareRadius;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!TryGetVertoBall(other, out var vertoBall))
        {
            return;
        }

        vertoBall.OnHoleNearby();
    }

    private static bool TryGetVertoBall(Collider other, out VertoBallBehaviour vertoBall)
    {
        if (other.TryGetComponent(out vertoBall))
        {
            return true;
        }

        vertoBall = other.GetComponentInParent<VertoBallBehaviour>();
        return vertoBall != null;
    }
}
