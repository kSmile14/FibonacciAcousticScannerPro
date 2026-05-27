#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Utility window that replaces BoxColliders with MeshColliders on scaled geometry.
/// Open via: Window > Acoustics > Fix Wall Colliders
/// </summary>
public class ColliderFixer : EditorWindow
{
    [MenuItem("Window/Acoustics/Fix Wall Colliders")]
    public static void ShowWindow() => GetWindow<ColliderFixer>("Fix Colliders");

    private void OnGUI()
    {
        GUILayout.Label("Fix Scaled Box Colliders", EditorStyles.boldLabel);
        EditorGUILayout.Space(8);

        EditorGUILayout.HelpBox(
            "Replaces BoxColliders with MeshColliders on objects tagged 'Wall'.\n" +
            "MeshColliders conform to the actual mesh shape, ensuring accurate ray hits.",
            MessageType.Info);

        EditorGUILayout.Space(6);
        if (GUILayout.Button("Fix All 'Wall' Tagged Objects")) FixWallColliders();

        EditorGUILayout.Space(6);
        if (GUILayout.Button("Fix Selected Objects")) FixSelected();
    }

    private void FixWallColliders()
    {
        int count = 0;
        foreach (var go in FindObjectsOfType<GameObject>())
            if (go.CompareTag("Wall") && ReplaceWithMeshCollider(go))
                count++;

        Debug.Log($"[ColliderFixer] Fixed {count} objects tagged 'Wall'.");
        EditorUtility.DisplayDialog("Done", $"Fixed {count} object(s).", "OK");
    }

    private void FixSelected()
    {
        int count = 0;
        foreach (var go in Selection.gameObjects)
            if (ReplaceWithMeshCollider(go))
                count++;

        Debug.Log($"[ColliderFixer] Fixed {count} selected object(s).");
    }

    private bool ReplaceWithMeshCollider(GameObject go)
    {
        var mf = go.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return false;

        var box = go.GetComponent<BoxCollider>();
        if (box != null) Undo.DestroyObjectImmediate(box);

        var mc = go.GetComponent<MeshCollider>();
        if (mc == null) mc = Undo.AddComponent<MeshCollider>(go);

        mc.sharedMesh = mf.sharedMesh;
        mc.convex     = false;
        return true;
    }
}
#endif
