using UnityEngine;

public class AcousticEmitter : MonoBehaviour
{
    [Header("General")]
    public AcousticMaterialConfig config;

    [Header("Quality")]
    [Range(1, 64)]
    public int extraRayCount = 16;

    [Range(0f, 45f)]
    public float spreadAngle = 15f;

    public float maxDistance = 60f;

    public enum ThicknessMethod
    {
        None,
        Bounds,
        DoubleRay,
    }

    [Header("Wall Thickness")]
    public ThicknessMethod thicknessMethod = ThicknessMethod.Bounds;
    public float maxWallThickness = 5f;

    [Range(0f, 1f)]
    public float thicknessInfluence = 0.4f;

    [Header("Smoothing")]
    [Range(0.5f, 10f)]
    public float smoothingSpeed = 3f;

    [Header("Debug")]
    public bool showDebugRays = true;

    public float CurrentOcclusion { get; private set; }
    public float TargetOcclusion  { get; private set; }

    private DiffractionCalculator _diffraction;
    private AkGameObj             _akGameObj;
    private float                 _lastSentOcclusion = -1f;
    private int                   _lastHitCount;
    private int                   _lastTotalCount;

    private void Awake()
    {
        _diffraction = GetComponent<DiffractionCalculator>();
        _akGameObj   = GetComponent<AkGameObj>();
    }

    private void Start()
    {
        if (AcousticManager.Instance != null)
            AcousticManager.Instance.RegisterEmitter(this);
        else
            Debug.LogError($"[AcousticEmitter] AcousticManager not found in scene. ({gameObject.name})");
    }

    private void OnDestroy()
    {
        if (AcousticManager.Instance != null)
            AcousticManager.Instance.UnregisterEmitter(this);
    }

    public void PerformScan(Vector3 listenerPos)
    {
        if (config == null) return;

        Vector3 toEmitter = transform.position - listenerPos;
        float   distance  = toEmitter.magnitude;

        if (distance > maxDistance || distance < 0.1f)
        {
            TargetOcclusion = 0f;
            if (_diffraction != null)
                _diffraction.CalculateDiffraction(listenerPos);
            return;
        }

        Vector3   dirToEmitter = toEmitter / distance;
        LayerMask mask         = AcousticListener.Instance != null
            ? AcousticListener.Instance.geometryMask : ~0;

        float totalAbs  = 0f;
        int   totalRays = 0;
        int   hitCount  = 0;

        CastRay(listenerPos, dirToEmitter, distance, mask,
            ref totalAbs, ref hitCount, ref totalRays);

        Quaternion baseRot = Quaternion.LookRotation(dirToEmitter);
        for (int i = 0; i < extraRayCount; i++)
        {
            float   a      = (float)i / extraRayCount * Mathf.PI * 2f;
            float   spread = spreadAngle * Mathf.Deg2Rad;
            Vector3 dir    = new Vector3(
                Mathf.Sin(spread) * Mathf.Cos(a),
                Mathf.Sin(spread) * Mathf.Sin(a),
                Mathf.Cos(spread)).normalized;
            CastRay(listenerPos, baseRot * dir, distance, mask,
                ref totalAbs, ref hitCount, ref totalRays);
        }

        _lastHitCount   = hitCount;
        _lastTotalCount = totalRays;
        TargetOcclusion = totalRays > 0 ? Mathf.Clamp01(totalAbs / totalRays) : 0f;

        if (_diffraction != null)
            _diffraction.CalculateDiffraction(listenerPos);

        if (Time.frameCount % 60 == 0)
            Debug.Log($"[AcousticEmitter] {gameObject.name} | " +
                      $"hits={hitCount}/{totalRays} | " +
                      $"occ={TargetOcclusion:F2} | " +
                      $"dist={distance:F1}m");
    }

    private void CastRay(Vector3 origin, Vector3 dir, float dist, LayerMask mask,
        ref float totalAbs, ref int hitCount, ref int totalRays)
    {
        totalRays++;

        if (!Physics.Raycast(origin, dir, out RaycastHit frontHit, dist,
            mask, QueryTriggerInteraction.Ignore))
        {
            totalAbs += config.skyAbsorption;
            if (showDebugRays)
                Debug.DrawRay(origin, dir * dist, new Color(0f, 1f, 0f, 0.2f), 0.1f);
            return;
        }

        hitCount++;

        float matAbs = config.GetAbsorption(frontHit.collider.tag, out _);

        if (thicknessMethod != ThicknessMethod.None)
        {
            float thicknessPct    = GetThickness(frontHit, dir, mask);
            float thicknessFactor = Mathf.Lerp(0.2f, 1f, thicknessPct / 100f);
            matAbs = Mathf.Lerp(matAbs, matAbs * thicknessFactor, thicknessInfluence);
        }

        totalAbs += matAbs;

        if (showDebugRays)
            Debug.DrawLine(origin, frontHit.point, Color.red, 0.1f);
    }

    private float GetThickness(RaycastHit frontHit, Vector3 dir, LayerMask mask)
    {
        switch (thicknessMethod)
        {
            case ThicknessMethod.Bounds:    return CalculateThicknessBounds(frontHit, dir);
            case ThicknessMethod.DoubleRay: return CalculateThicknessDoubleRay(frontHit, dir, mask);
            default:                        return 0f;
        }
    }

    private float CalculateThicknessBounds(RaycastHit frontHit, Vector3 dir)
    {
        Bounds bounds = frontHit.collider.bounds;
        Ray    ray    = new Ray(frontHit.point + dir * 0.01f, dir);

        if (bounds.IntersectRay(ray, out float distToExit))
        {
            float angleCorrection = Mathf.Abs(Vector3.Dot(dir, frontHit.normal));
            float correctedDist   = distToExit * Mathf.Lerp(1f, angleCorrection, 0.5f);

            if (showDebugRays)
                Debug.DrawLine(frontHit.point, frontHit.point + dir * correctedDist,
                    Color.magenta, 0.1f);

            return Mathf.Clamp((correctedDist / maxWallThickness) * 100f, 0f, 100f);
        }

        return 100f;
    }

    private float CalculateThicknessDoubleRay(RaycastHit frontHit, Vector3 dir, LayerMask mask)
    {
        Vector3 backOrigin = frontHit.point + dir * maxWallThickness;

        if (Physics.Raycast(backOrigin, -dir, out RaycastHit backHit,
            maxWallThickness, mask, QueryTriggerInteraction.Ignore))
        {
            float thickness = Vector3.Distance(frontHit.point, backHit.point);

            if (showDebugRays)
                Debug.DrawLine(frontHit.point, backHit.point, Color.magenta, 0.1f);

            return Mathf.Clamp((thickness / maxWallThickness) * 100f, 0f, 100f);
        }

        return 100f;
    }

    private void Update()
    {
        float t = Time.deltaTime * smoothingSpeed;
        CurrentOcclusion = Mathf.Clamp01(Mathf.Lerp(CurrentOcclusion, TargetOcclusion, t));

        if (_diffraction != null)
            _diffraction.UpdateSmoothing();

        SendToWwise();
        _lastSentOcclusion = CurrentOcclusion;
    }

    private void SendToWwise()
    {
        var data = new WwiseAcousticBridge.AcousticData
        {
            occlusion    = CurrentOcclusion,
            obstruction  = 0f,
            diffraction  = _diffraction != null ? _diffraction.CurrentDiffraction : 0f,
            roomSize     = RoomScanner.Instance != null ? RoomScanner.Instance.CurrentRoomSize : 0f,
            reverbAmount = RoomScanner.Instance != null ? RoomScanner.Instance.CurrentReverb   : 0f,
        };
        WwiseAcousticBridge.UpdateAcoustics(gameObject, data);
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || AcousticListener.Instance == null) return;
        Gizmos.color = Color.Lerp(Color.green, Color.red, CurrentOcclusion);
        Gizmos.DrawLine(transform.position, AcousticListener.Instance.transform.position);
        Gizmos.DrawSphere(transform.position, 0.3f);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.8f,
            $"occ={CurrentOcclusion:F2} diff={(_diffraction != null ? _diffraction.CurrentDiffraction : 0f):F2}\nhits={_lastHitCount}/{_lastTotalCount}");
#endif
    }
}
