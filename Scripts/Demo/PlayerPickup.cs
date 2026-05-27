using UnityEngine;

/// <summary>
/// Attach to the player. Scans nearby pickable items and lets the player
/// pick them up or put them down with a configurable key.
/// </summary>
public class PlayerPickup : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Key used to pick up or drop an item.")]
    public KeyCode pickupKey = KeyCode.E;

    [Tooltip("Transform the held item will be parented to (e.g. right hand bone).")]
    public Transform holdPoint;

    [Tooltip("Radius within which the player can detect pickable items.")]
    public float pickupRadius = 2f;

    [Tooltip("Layer mask used when searching for nearby PickupItem components.")]
    public LayerMask pickupMask;

    [Header("UI")]
    [Tooltip("Show the pickup prompt on screen when an item is in range.")]
    public bool showPrompt = true;

    private PickupItem _heldItem;
    private PickupItem _nearestItem;

    private void Update()
    {
        FindNearestItem();

        if (Input.GetKeyDown(pickupKey))
        {
            if (_heldItem != null)
                PutDown();
            else if (_nearestItem != null)
                Pickup(_nearestItem);
        }
    }

    private void FindNearestItem()
    {
        if (_heldItem != null)
        {
            _nearestItem = null;
            return;
        }

        Collider[]  hits    = Physics.OverlapSphere(transform.position, pickupRadius, pickupMask);
        float       minDist = float.MaxValue;
        PickupItem  nearest = null;

        foreach (var hit in hits)
        {
            var item = hit.GetComponent<PickupItem>();
            if (item == null || item.IsHeld) continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < minDist) { minDist = dist; nearest = item; }
        }

        _nearestItem = nearest;
    }

    private void Pickup(PickupItem item)
    {
        if (holdPoint == null)
        {
            Debug.LogWarning("[PlayerPickup] Hold point not assigned.");
            return;
        }

        _heldItem = item;
        item.OnPickup(holdPoint);
    }

    private void PutDown()
    {
        if (_heldItem == null) return;
        _heldItem.OnPutdown();
        _heldItem = null;
    }

    private void OnGUI()
    {
        if (!showPrompt || _nearestItem == null || _heldItem != null) return;

        var style = new GUIStyle
        {
            fontSize  = 24,
            alignment = TextAnchor.MiddleCenter
        };
        style.normal.textColor = Color.white;

        GUI.Label(new Rect(Screen.width / 2f - 200f, Screen.height / 2f + 50f, 400f, 50f),
            _nearestItem.promptText, style);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
