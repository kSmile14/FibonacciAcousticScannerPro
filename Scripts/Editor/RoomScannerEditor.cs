#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RoomScanner))]
public class RoomScannerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var scanner = (RoomScanner)target;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("── Visualisation ──", EditorStyles.boldLabel);

        GUI.backgroundColor = scanner.showGizmoRays
            ? new Color(0.4f, 0.9f, 0.4f)
            : new Color(0.9f, 0.4f, 0.4f);

        string label = scanner.showGizmoRays
            ? "🟢  Rays ON  — click to disable"
            : "🔴  Rays OFF — click to enable";

        if (GUILayout.Button(label, GUILayout.Height(32)))
        {
            Undo.RecordObject(scanner, "Toggle RoomScanner Rays");
            scanner.showGizmoRays = !scanner.showGizmoRays;
            EditorUtility.SetDirty(scanner);
        }

        GUI.backgroundColor = Color.white;

        if (!Application.isPlaying) return;

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("── Live Values ──", EditorStyles.boldLabel);

        EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 18),
            scanner.CurrentRoomSize,
            $"Room Size: {scanner.CurrentRoomSize * 100f:F1}");

        EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 18),
            scanner.CurrentReverb,
            $"Reverb Amount: {scanner.CurrentReverb * 100f:F1}");

        Repaint();
    }
}
#endif
