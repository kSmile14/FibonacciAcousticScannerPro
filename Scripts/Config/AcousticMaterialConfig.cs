using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AcousticMaterialConfig", menuName = "Acoustics/Material Config")]
public class AcousticMaterialConfig : ScriptableObject
{
    [System.Serializable]
    public class MaterialEntry
    {
        [Tooltip("Unity object tag to match (e.g. Wall, Door, Glass).")]
        public string tag;

        [Range(0f, 1f)]
        [Tooltip("Absorption coefficient. 0 = fully reflective, 1 = fully absorptive.")]
        public float absorption = 0.5f;

        [Tooltip("Debug ray colour displayed in the Scene view for this material.")]
        public Color debugColor = Color.white;
    }

    [Header("Material Absorption Table")]
    public List<MaterialEntry> materials = new List<MaterialEntry>
    {
        new MaterialEntry { tag = "Wall",   absorption = 0.85f, debugColor = new Color(0.8f, 0.2f, 0.2f) },
        new MaterialEntry { tag = "Door",   absorption = 0.55f, debugColor = new Color(0.8f, 0.5f, 0.1f) },
        new MaterialEntry { tag = "Glass",  absorption = 0.25f, debugColor = new Color(0.2f, 0.8f, 0.9f) },
        new MaterialEntry { tag = "Wood",   absorption = 0.45f, debugColor = new Color(0.6f, 0.4f, 0.1f) },
        new MaterialEntry { tag = "Metal",  absorption = 0.30f, debugColor = new Color(0.5f, 0.5f, 0.7f) },
    };

    [Header("Fallback")]
    [Range(0f, 1f)]
    [Tooltip("Absorption value used when no matching tag is found.")]
    public float defaultAbsorption = 0.5f;

    [Range(0f, 1f)]
    [Tooltip("Absorption value applied when a ray misses all geometry (open sky). Set to 1.0 to suppress reverb outdoors.")]
    public float skyAbsorption = 0.9f;

    [Tooltip("Debug ray colour used for rays that reach the sky.")]
    public Color skyDebugColor = Color.black;

    private Dictionary<string, MaterialEntry> _lookup;

    private void OnEnable() => BuildLookup();

    private void BuildLookup()
    {
        _lookup = new Dictionary<string, MaterialEntry>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var entry in materials)
            if (!string.IsNullOrEmpty(entry.tag))
                _lookup[entry.tag] = entry;
    }

    /// <summary>Returns the absorption coefficient for the given Unity tag.</summary>
    public float GetAbsorption(string tag, out string resolvedTag)
    {
        if (_lookup == null) BuildLookup();

        if (!string.IsNullOrEmpty(tag) && _lookup.TryGetValue(tag, out MaterialEntry entry))
        {
            resolvedTag = entry.tag;
            return entry.absorption;
        }

        resolvedTag = "default";
        return defaultAbsorption;
    }

    /// <summary>Returns the debug ray colour for the given Unity tag.</summary>
    public Color GetDebugColor(string tag)
    {
        if (_lookup == null) BuildLookup();

        if (!string.IsNullOrEmpty(tag) && _lookup.TryGetValue(tag, out MaterialEntry entry))
            return entry.debugColor;

        return Color.gray;
    }
}
