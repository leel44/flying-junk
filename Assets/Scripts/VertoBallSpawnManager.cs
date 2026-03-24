using System.Collections.Generic;
using UnityEngine;

public sealed class VertoBallSpawnManager : MonoBehaviour
{
    private const string FloorObjectName = "Floor";

    [Header("Prefab")]
    [SerializeField] private VertoBallBehaviour vertoBallPrefab;
    [SerializeField] private bool keepManualTestInstance = true;

    [Header("Spawn")]
    [SerializeField] private int spawnCount = 3;
    [SerializeField] private float spawnRadius = 6f;
    [SerializeField] private float minSpawnSeparation = 1.75f;
    [SerializeField] private int maxSpawnPositionRetries = 12;
    [SerializeField] private LayerMask spawnObstacleLayers = ~0;
    [SerializeField] private float spawnCheckRadius = 0.6f;

    [Header("Flight Settings")]
    [SerializeField] private float minIdleTime = 1.5f;
    [SerializeField] private float maxIdleTime = 3f;
    [SerializeField] private float flightRadius = 4f;

    private readonly List<Vector3> occupiedSpawnPositions = new List<Vector3>();
    private readonly Collider[] spawnCheckBuffer = new Collider[8];
    private Transform floorTransform;
    private Collider floorCollider;

    private void Start()
    {
        CacheFloorReferences();
        occupiedSpawnPositions.Clear();

        if (keepManualTestInstance)
        {
            var existingInstances = FindObjectsByType<VertoBallBehaviour>(FindObjectsSortMode.None);
            for (var i = 0; i < existingInstances.Length; i++)
            {
                ConfigureInstance(existingInstances[i]);
                occupiedSpawnPositions.Add(existingInstances[i].transform.position);
            }
        }

        for (var spawnIndex = 0; spawnIndex < spawnCount; spawnIndex++)
        {
            if (!TryGetSpawnPosition(out var spawnPosition))
            {
                continue;
            }

            var instance = Instantiate(vertoBallPrefab, spawnPosition, Quaternion.identity);
            ConfigureInstance(instance);
            occupiedSpawnPositions.Add(spawnPosition);
        }
    }

    private void OnValidate()
    {
        spawnCount = Mathf.Max(0, spawnCount);
        spawnRadius = Mathf.Max(0f, spawnRadius);
        minSpawnSeparation = Mathf.Max(0f, minSpawnSeparation);
        maxSpawnPositionRetries = Mathf.Max(1, maxSpawnPositionRetries);
        spawnCheckRadius = Mathf.Max(0.01f, spawnCheckRadius);
        minIdleTime = Mathf.Max(0f, minIdleTime);
        maxIdleTime = Mathf.Max(minIdleTime, maxIdleTime);
        flightRadius = Mathf.Max(0f, flightRadius);
    }

    private void ConfigureInstance(VertoBallBehaviour instance)
    {
        if (instance == null)
        {
            return;
        }

        instance.Initialize(minIdleTime, maxIdleTime, flightRadius, true);
    }

    private bool TryGetSpawnPosition(out Vector3 spawnPosition)
    {
        for (var attempt = 0; attempt < maxSpawnPositionRetries; attempt++)
        {
            var randomOffset2D = Random.insideUnitCircle * spawnRadius;
            var candidate = transform.position + new Vector3(randomOffset2D.x, 0f, randomOffset2D.y);

            if (!IsSpawnPositionValid(candidate))
            {
                continue;
            }

            spawnPosition = candidate;
            return true;
        }

        spawnPosition = transform.position;
        return false;
    }

    private bool IsSpawnPositionValid(Vector3 candidate)
    {
        for (var i = 0; i < occupiedSpawnPositions.Count; i++)
        {
            if (Vector3.Distance(candidate, occupiedSpawnPositions[i]) < minSpawnSeparation)
            {
                return false;
            }
        }

        var overlapPosition = candidate + Vector3.up * spawnCheckRadius;
        var overlapCount = Physics.OverlapSphereNonAlloc(
            overlapPosition,
            spawnCheckRadius,
            spawnCheckBuffer,
            spawnObstacleLayers,
            QueryTriggerInteraction.Ignore);

        for (var i = 0; i < overlapCount; i++)
        {
            var hitCollider = spawnCheckBuffer[i];
            if (hitCollider == null || IsIgnoredSpawnCollider(hitCollider))
            {
                continue;
            }

            return false;
        }

        return true;
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
    }

    private bool IsIgnoredSpawnCollider(Collider hitCollider)
    {
        CacheFloorReferences();

        if (floorCollider != null && hitCollider == floorCollider)
        {
            return true;
        }

        return floorTransform != null && hitCollider.transform.IsChildOf(floorTransform);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
