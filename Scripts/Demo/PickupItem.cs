using UnityEngine;

/// <summary>
/// Marks an object as pickable. Attach to any prop the player can carry.
/// Automatically disables acoustic occlusion and diffraction while held.
/// </summary>
public class PickupItem : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Prompt text shown on screen when the player is within pickup range.")]
    public string promptText = "Press E to pick up";

    [Tooltip("Position offset relative to the hold point.")]
    public Vector3 holdOffset = Vector3.zero;

    [Tooltip("Euler rotation applied while held.")]
    public Vector3 holdRotation = Vector3.zero;

    [Header("Wwise")]
    [Tooltip("Wwise Event posted when the item is picked up.")]
    public AK.Wwise.Event pickupSound;

    [Tooltip("Wwise Event posted when the item is put down.")]
    public AK.Wwise.Event putdownSound;

    public bool IsHeld       { get; private set; }
    public bool IsNearPlayer { get; private set; }

    private Rigidbody             _rb;
    private Collider              _col;
    private AcousticEmitter       _acousticEmitter;
    private DiffractionCalculator _diffractionCalc;

    private void Awake()
    {
        _rb              = GetComponent<Rigidbody>();
        _col             = GetComponent<Collider>();
        _acousticEmitter = GetComponent<AcousticEmitter>();
        _diffractionCalc = GetComponent<DiffractionCalculator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) IsNearPlayer = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) IsNearPlayer = false;
    }

    /// <summary>Called by PlayerPickup when the player picks up this item.</summary>
    public void OnPickup(Transform holdPoint)
    {
        IsHeld = true;

        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.useGravity  = false;
        }

        transform.SetParent(holdPoint);
        transform.localPosition    = holdOffset;
        transform.localEulerAngles = holdRotation;

        // Suppress occlusion and diffraction while the item is carried next to the listener.
        if (_acousticEmitter != null) _acousticEmitter.enabled = false;
        if (_diffractionCalc != null) _diffractionCalc.enabled = false;

        pickupSound?.Post(gameObject);
    }

    /// <summary>Called by PlayerPickup when the player drops this item.</summary>
    public void OnPutdown()
    {
        IsHeld = false;

        transform.SetParent(null);

        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.useGravity  = true;
        }

        if (_acousticEmitter != null) _acousticEmitter.enabled = true;
        if (_diffractionCalc != null) _diffractionCalc.enabled = true;

        putdownSound?.Post(gameObject);
    }
}
