using UnityEngine;

[RequireComponent(typeof(AcousticEmitter))]
public class DiffractionCalculator : MonoBehaviour
{
    [Header("Edge Search")]
    [Tooltip("Step size used when searching along obstacle edges.")]
    public float edgeStep = 0.3f;

    [Tooltip("Maximum search radius around the hit point when looking for a diffraction edge.")]
    public float maxEdgeSearch = 5f;

    [Tooltip("Layer mask defining which geometry is tested for diffraction.")]
    public LayerMask geometryMask = ~0;

    [Header("Stability")]
    [Range(1, 5)]
    [Tooltip("Number of rays used to confirm line-of-sight. More rays reduce jitter on thin geometry.")]
    public int visibilityRays = 3;

    [Range(0f, 10f)]
    [Tooltip("Angular spread of visibility rays in degrees.")]
    public float visibilitySpread = 3f;

    [Header("Smoothing")]
    [Range(0.5f, 5f)]
    [Tooltip("Interpolation speed for the diffraction value.")]
    public float smoothingSpeed = 1.5f;

    public float CurrentDiffraction { get; private set; }
    private float _targetDiffraction;

    public void CalculateDiffraction(Vector3 listenerPos)
    {
        Vector3 emitterPos  = transform.position;
        Vector3 toEmitter   = emitterPos - listenerPos;
        float   directDist  = toEmitter.magnitude;

        if (directDist < 0.1f)
        {
            _targetDiffraction = 0f;
            return;
        }

        Vector3 dirToEmitter = toEmitter.normalized;

        Vector3 perpX = Vector3.Cross(dirToEmitter, Vector3.up).normalized;
        if (perpX.sqrMagnitude < 0.01f)
            perpX = Vector3.Cross(dirToEmitter, Vector3.right).normalized;

        int        blockedCount = 0;
        RaycastHit bestHit      = default;
        bool       gotHit       = false;

        for (int i = 0; i < visibilityRays; i++)
        {
            Vector3 rayDir = dirToEmitter;

            if (visibilityRays > 1 && visibilitySpread > 0f)
            {
                float t = (float)i / (visibilityRays - 1) - 0.5f;
                rayDir  = Quaternion.AngleAxis(t * visibilitySpread, perpX) * dirToEmitter;
                rayDir.Normalize();
            }

            if (Physics.Raycast(listenerPos, rayDir, out RaycastHit hit,
                directDist, geometryMask, QueryTriggerInteraction.Ignore))
            {
                blockedCount++;
                if (!gotHit) { bestHit = hit; gotHit = true; }
            }
        }

        if (blockedCount < visibilityRays / 2 + 1)
        {
            _targetDiffraction = 0f;
            return;
        }

        if (!gotHit)
        {
            _targetDiffraction = 0f;
            return;
        }

        Vector3 hitPoint = bestHit.point;
        Vector3 perpY    = Vector3.Cross(dirToEmitter, perpX).normalized;

        float bestDiff  = 1f;
        bool  foundEdge = false;

        for (int d = 0; d < 8; d++)
        {
            float   angle   = (float)d / 8 * Mathf.PI * 2f;
            Vector3 perpDir = (perpX * Mathf.Cos(angle) + perpY * Mathf.Sin(angle)).normalized;

            for (float dist = edgeStep; dist <= maxEdgeSearch; dist += edgeStep)
            {
                Vector3 candidate = hitPoint + perpDir * dist;
                Vector3 seg1      = candidate - listenerPos;
                Vector3 seg2      = emitterPos - candidate;

                if (seg1.sqrMagnitude < 0.01f || seg2.sqrMagnitude < 0.01f) continue;

                bool seg1Clear = !Physics.Raycast(listenerPos, seg1.normalized,
                    seg1.magnitude, geometryMask, QueryTriggerInteraction.Ignore);
                bool seg2Clear = !Physics.Raycast(candidate, seg2.normalized,
                    seg2.magnitude, geometryMask, QueryTriggerInteraction.Ignore);

                if (seg1Clear && seg2Clear)
                {
                    float angle2     = Vector3.Angle(dirToEmitter, seg1.normalized);
                    float normalised = Mathf.Clamp01(angle2 / 45f);

                    if (normalised < bestDiff)
                    {
                        bestDiff  = normalised;
                        foundEdge = true;
                    }
                    break;
                }
            }
        }

        _targetDiffraction = foundEdge ? bestDiff : 0.9f;
    }

    public void UpdateSmoothing()
    {
        CurrentDiffraction = Mathf.Lerp(CurrentDiffraction, _targetDiffraction,
            Time.deltaTime * smoothingSpeed);
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.Lerp(Color.cyan, Color.magenta, CurrentDiffraction);
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        if (AcousticListener.Instance != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
            Gizmos.DrawLine(transform.position, AcousticListener.Instance.transform.position);
        }
    }
}
