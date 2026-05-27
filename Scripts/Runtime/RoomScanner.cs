using UnityEngine;

/// <summary>
/// Scans the space around the listener using a Fibonacci sphere of rays.
/// Derives room size and reverb amount from average ray distance and surface absorption.
/// Attach this component to the same GameObject as AcousticListener.
/// </summary>
[RequireComponent(typeof(AcousticListener))]
public class RoomScanner : MonoBehaviour
{
    public static RoomScanner Instance { get; private set; }

    [Header("Material Config")]
    [Tooltip("Assign an AcousticMaterialConfig to enable per-material absorption and debug ray colours.")]
    public AcousticMaterialConfig materialConfig;

    [Header("Debug")]
    [Tooltip("Draw ray visualisation in the Scene view during Play Mode.")]
    public bool showGizmoRays = true;

    [Header("Scan Settings")]
    [Range(16, 128)]
    [Tooltip("Number of rays in the Fibonacci sphere. Higher values are more accurate but more expensive.")]
    public int rayCount = 32;

    [Tooltip("Maximum ray travel distance. Acts as the upper bound for room size normalisation.")]
    public float maxRayDistance = 40f;

    [Tooltip("Layer mask defining which geometry is considered for room scanning.")]
    public LayerMask geometryMask = ~0;

    [Header("Normalisation")]
    [Tooltip("Average ray distance at which RoomSize reaches 1.0 (open space).")]
    public float maxRoomSize = 30f;

    [Header("Smoothing")]
    [Range(1f, 10f)]
    [Tooltip("Speed at which RoomSize and ReverbAmount interpolate toward their targets.")]
    public float smoothingSpeed = 3f;

    [Header("Update Rate")]
    [Range(0, 30)]
    [Tooltip("Scan every N frames. 0 scans every frame.")]
    public int updateInterval = 6;

    public float CurrentRoomSize { get; private set; }
    public float CurrentReverb   { get; private set; }

    private float     _targetRoomSize;
    private float     _targetReverb;
    private Vector3[] _directions;
    private int       _frameCounter;

    // Bell-curve reverb model: peaks around mid-sized rooms, falls off toward very small or very large spaces.
    private static float ReverbFromRoomSize(float t)
    {
        const float peak  = 0.4f;
        const float width = 0.35f;
        float       d     = t - peak;
        return Mathf.Clamp01(Mathf.Exp(-(d * d) / (2f * width * width)));
    }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(this);
            return;
        }

        _directions = FibonacciUtils.GenerateDirections(rayCount);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        _frameCounter++;

        if (updateInterval > 0 && _frameCounter % updateInterval != 0)
        {
            ApplySmoothing();
            return;
        }

        PerformScan();
        ApplySmoothing();
    }

    private void PerformScan()
    {
        Vector3 origin    = transform.position;
        float   totalDist = 0f;
        float   totalAbs  = 0f;
        int     hitCount  = 0;

        for (int i = 0; i < _directions.Length; i++)
        {
            if (Physics.Raycast(origin, _directions[i], out RaycastHit hit,
                maxRayDistance, geometryMask, QueryTriggerInteraction.Ignore))
            {
                totalDist += hit.distance;
                hitCount++;

                float abs = 0.5f;
                if (materialConfig != null)
                {
                    abs = materialConfig.GetAbsorption(hit.collider.tag, out _);

                    if (showGizmoRays)
                        Debug.DrawLine(origin, hit.point,
                            materialConfig.GetDebugColor(hit.collider.tag));
                }
                else if (showGizmoRays)
                {
                    Debug.DrawLine(origin, hit.point, Color.gray);
                }

                totalAbs += abs;
            }
            else
            {
                totalDist += maxRayDistance;
                totalAbs  += materialConfig != null ? materialConfig.skyAbsorption : 0f;

                if (showGizmoRays)
                    Debug.DrawRay(origin, _directions[i] * maxRayDistance,
                        materialConfig != null ? materialConfig.skyDebugColor : Color.black);
            }
        }

        float avgDist = totalDist / _directions.Length;
        float avgAbs  = totalAbs  / _directions.Length;

        _targetRoomSize = Mathf.Clamp01(avgDist / maxRoomSize);

        float reverbFromSize  = ReverbFromRoomSize(_targetRoomSize);
        float absorptionDamp  = Mathf.Lerp(1f, 0.1f, avgAbs);
        float hitRatio        = hitCount / (float)_directions.Length;

        _targetReverb = reverbFromSize * absorptionDamp * Mathf.Lerp(0.1f, 1f, hitRatio);
    }

    private void ApplySmoothing()
    {
        float t = Time.deltaTime * smoothingSpeed;
        CurrentRoomSize = Mathf.Lerp(CurrentRoomSize, _targetRoomSize, t);
        CurrentReverb   = Mathf.Lerp(CurrentReverb,   _targetReverb,   t);
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || _directions == null) return;

        float radius = CurrentRoomSize * maxRoomSize;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
        foreach (var dir in _directions)
            Gizmos.DrawRay(transform.position, dir * radius);

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
