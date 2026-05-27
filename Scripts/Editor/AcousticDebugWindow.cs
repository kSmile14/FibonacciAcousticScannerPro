using UnityEngine;
using UnityEditor;

public class AcousticDebugWindow : EditorWindow
{
    [MenuItem("Acoustics/RTPC Debug Monitor")]
    public static void ShowWindow()
    {
        var window = GetWindow<AcousticDebugWindow>("RTPC Monitor");
        window.minSize = new Vector2(350, 300);
    }

    private AcousticEmitter _emitter;
    private float  _updateInterval = 0.1f;
    private double _lastUpdate;

    private float _occlusion;
    private float _diffraction;
    private float _roomSize;
    private float _reverbAmount;
    private float _targetOcc;
    private float _currentOcc;
    private float _dist;

    private void OnEnable()  => EditorApplication.update += OnEditorUpdate;
    private void OnDisable() => EditorApplication.update -= OnEditorUpdate;

    private void OnEditorUpdate()
    {
        if (!Application.isPlaying) return;
        if (EditorApplication.timeSinceStartup - _lastUpdate < _updateInterval) return;
        _lastUpdate = EditorApplication.timeSinceStartup;

        if (_emitter == null)
            _emitter = FindObjectOfType<AcousticEmitter>();

        if (_emitter != null)
        {
            _currentOcc = _emitter.CurrentOcclusion;
            _targetOcc  = _emitter.TargetOcclusion;

            float scale  = WwiseAcousticBridge.Config != null ? WwiseAcousticBridge.Config.rtpcScale : 100f;

            _occlusion    = _currentOcc * scale;
            _diffraction  = _emitter.TryGetComponent<DiffractionCalculator>(out var dc)
                ? dc.CurrentDiffraction * scale : 0f;
            _roomSize     = RoomScanner.Instance != null ? RoomScanner.Instance.CurrentRoomSize * scale : 0f;
            _reverbAmount = RoomScanner.Instance != null ? RoomScanner.Instance.CurrentReverb   * scale : 0f;

            if (AcousticListener.Instance != null)
                _dist = Vector3.Distance(_emitter.transform.position,
                    AcousticListener.Instance.transform.position);
        }

        Repaint();
    }

    private void OnGUI()
    {
        GUILayout.Space(8);
        EditorGUILayout.LabelField("Acoustic RTPC Monitor", EditorStyles.boldLabel);
        GUILayout.Space(4);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to see live values.", MessageType.Info);
            return;
        }

        if (_emitter == null)
        {
            EditorGUILayout.HelpBox("No AcousticEmitter found in the scene.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField($"Emitter: {_emitter.gameObject.name}   dist: {_dist:F1} m");
        GUILayout.Space(8);

        EditorGUILayout.LabelField("── RTPC Values ──", EditorStyles.boldLabel);
        DrawBar("Acoustics_Occlusion",    _occlusion,    100f, new Color(0.9f, 0.3f, 0.3f));
        DrawBar("Acoustics_Diffraction",  _diffraction,  100f, new Color(0.9f, 0.6f, 0.2f));
        DrawBar("Acoustics_RoomSize",     _roomSize,     100f, new Color(0.3f, 0.7f, 0.9f));
        DrawBar("Acoustics_ReverbAmount", _reverbAmount, 100f, new Color(0.5f, 0.9f, 0.5f));

        GUILayout.Space(8);
        EditorGUILayout.LabelField("── Occlusion Detail ──", EditorStyles.boldLabel);
        DrawBar("Current (smoothed)", _currentOcc, 1f, new Color(0.9f, 0.3f, 0.3f));
        DrawBar("Target  (raw scan)", _targetOcc,  1f, new Color(0.7f, 0.2f, 0.2f));

        GUILayout.Space(8);
        EditorGUILayout.LabelField("── Settings ──", EditorStyles.boldLabel);
        _updateInterval = EditorGUILayout.Slider("Refresh Rate (sec)", _updateInterval, 0.05f, 1f);

        GUILayout.Space(4);
        if (GUILayout.Button("Select Emitter from Hierarchy"))
        {
            if (Selection.activeGameObject != null)
                _emitter = Selection.activeGameObject.GetComponent<AcousticEmitter>();
        }
    }

    private void DrawBar(string label, float value, float max, Color color)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(180));
        EditorGUILayout.LabelField($"{value:F1}", GUILayout.Width(45));

        Rect barRect = GUILayoutUtility.GetRect(0, 16, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(barRect, new Color(0.2f, 0.2f, 0.2f));
        float fill = max > 0 ? Mathf.Clamp01(value / max) : 0f;
        EditorGUI.DrawRect(new Rect(barRect.x, barRect.y, barRect.width * fill, barRect.height), color);

        EditorGUILayout.EndHorizontal();
    }
}
