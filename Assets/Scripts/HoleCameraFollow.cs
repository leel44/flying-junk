using UnityEngine;

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
