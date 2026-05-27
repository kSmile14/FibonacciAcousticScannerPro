#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// One-click setup window for the Fibonacci Acoustic System.
/// Open via: Window > Acoustics > Scene Setup
/// </summary>
public class AcousticSystemSetup : EditorWindow
{
    [MenuItem("Window/Acoustics/Scene Setup")]
    public static void ShowWindow() => GetWindow<AcousticSystemSetup>("Acoustic Setup");

    private void OnGUI()
    {
        GUILayout.Label("Fibonacci Acoustic System", EditorStyles.boldLabel);
        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField("1. Create System Root", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Creates the AcousticManager in the scene (skipped if one already exists).", MessageType.Info);
        if (GUILayout.Button("Create AcousticManager")) CreateManager();

        EditorGUILayout.Space(6);

        EditorGUILayout.LabelField("2. Setup Listener", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Adds AcousticListener and RoomScanner to the Main Camera.", MessageType.Info);
        if (GUILayout.Button("Setup Main Camera as Listener")) SetupListener();

        EditorGUILayout.Space(6);

        EditorGUILayout.LabelField("3. Add Emitter to Selected", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Adds AcousticEmitter and DiffractionCalculator to all selected objects.", MessageType.Info);
        if (GUILayout.Button("Add Emitter to Selection")) AddEmitterToSelection();

        EditorGUILayout.Space(6);

        EditorGUILayout.LabelField("4. Create Material Config", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Creates an AcousticMaterialConfig asset in Assets/Acoustics/.", MessageType.Info);
        if (GUILayout.Button("Create Material Config Asset")) CreateMaterialConfig();

        EditorGUILayout.Space(12);

        EditorGUILayout.LabelField("Scene Status", EditorStyles.boldLabel);
        DrawStatus("AcousticManager",  FindObjectOfType<AcousticManager>()  != null);
        DrawStatus("AcousticListener", FindObjectOfType<AcousticListener>() != null);
        DrawStatus("RoomScanner",      FindObjectOfType<RoomScanner>()      != null);
        EditorGUILayout.LabelField($"AcousticEmitters in scene: {FindObjectsOfType<AcousticEmitter>().Length}");
    }

    private void DrawStatus(string label, bool exists)
    {
        GUI.color = exists ? Color.green : Color.red;
        EditorGUILayout.LabelField($"  {(exists ? "✓" : "✗")}  {label}");
        GUI.color = Color.white;
    }

    private void CreateManager()
    {
        if (FindObjectOfType<AcousticManager>() != null)
        {
            EditorUtility.DisplayDialog("Acoustic Setup", "AcousticManager already exists in the scene.", "OK");
            return;
        }

        var go = new GameObject("_AcousticSystem");
        go.AddComponent<AcousticManager>();
        Selection.activeGameObject = go;
        Undo.RegisterCreatedObjectUndo(go, "Create AcousticManager");
        Debug.Log("[AcousticSetup] AcousticManager created.");
    }

    private void SetupListener()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            EditorUtility.DisplayDialog("Acoustic Setup",
                "Main Camera not found. Make sure the camera has the MainCamera tag.", "OK");
            return;
        }

        if (cam.GetComponent<AcousticListener>() == null)
        {
            Undo.AddComponent<AcousticListener>(cam.gameObject);
            Debug.Log("[AcousticSetup] AcousticListener added to Main Camera.");
        }

        if (cam.GetComponent<RoomScanner>() == null)
        {
            Undo.AddComponent<RoomScanner>(cam.gameObject);
            Debug.Log("[AcousticSetup] RoomScanner added to Main Camera.");
        }

        Selection.activeGameObject = cam.gameObject;
    }

    private void AddEmitterToSelection()
    {
        if (Selection.gameObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("Acoustic Setup",
                "Select one or more objects in the Hierarchy first.", "OK");
            return;
        }

        foreach (var go in Selection.gameObjects)
        {
            if (go.GetComponent<AcousticEmitter>() == null)
            {
                Undo.AddComponent<AcousticEmitter>(go);
                Undo.AddComponent<DiffractionCalculator>(go);
                Debug.Log($"[AcousticSetup] Emitter and DiffractionCalculator added to {go.name}.");
            }
        }
    }

    private void CreateMaterialConfig()
    {
        const string dir = "Assets/Acoustics";
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets", "Acoustics");

        string path = $"{dir}/AcousticMaterialConfig.asset";

        if (AssetDatabase.LoadAssetAtPath<AcousticMaterialConfig>(path) != null)
        {
            EditorUtility.DisplayDialog("Acoustic Setup", $"File already exists:\n{path}", "OK");
            return;
        }

        var asset = CreateInstance<AcousticMaterialConfig>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
        Debug.Log($"[AcousticSetup] Created {path}");
    }
}
#endif
