using UnityEngine;

[RequireComponent(typeof(AudioListener))]
public class AcousticListener : MonoBehaviour
{
    public static AcousticListener Instance { get; private set; }

    [Header("Global Settings")]
    [Range(32, 256)]
    [Tooltip("Maximum number of rays cast per emitter per scan.")]
    public int maxRaysPerEmitter = 64;

    [Tooltip("Layer mask defining which geometry blocks sound propagation.")]
    public LayerMask geometryMask = ~0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[AcousticListener] Duplicate detected and destroyed.", gameObject);
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
