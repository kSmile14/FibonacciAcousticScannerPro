using UnityEngine;
using System.Collections.Generic;

public class AcousticManager : MonoBehaviour
{
    public static AcousticManager Instance { get; private set; }

    [Header("Wwise RTPC Config")]
    [Tooltip("Assign the AcousticRTPCConfig asset here to configure RTPC names and operation mode.")]
    public AcousticRTPCConfig rtpcConfig;

    [Header("Performance")]
    [Range(5, 40)]
    [Tooltip("Maximum number of emitters updated per frame.")]
    public int maxEmittersPerFrame = 15;

    [Header("LOD Settings")]
    public LODSettings lod = new LODSettings();

    private readonly List<AcousticEmitter> _emitters = new List<AcousticEmitter>();
    private int _frameCounter;

    [System.Serializable]
    public class LODSettings
    {
        [Tooltip("Emitters closer than this distance update every frame.")]
        public float closeDistance  = 22f;

        [Tooltip("Emitters beyond this distance use the far update rate.")]
        public float mediumDistance = 100f;

        public int updateRateClose  = 1;
        public int updateRateMedium = 4;
        public int updateRateFar    = 12;
    }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        if (rtpcConfig != null)
            WwiseAcousticBridge.Config = rtpcConfig;
        else
            Debug.LogWarning("[AcousticManager] No RTPCConfig assigned. Please assign an AcousticRTPCConfig asset in the Inspector.");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void RegisterEmitter(AcousticEmitter emitter)
    {
        if (emitter != null && !_emitters.Contains(emitter))
            _emitters.Add(emitter);
    }

    public void UnregisterEmitter(AcousticEmitter emitter)
    {
        if (emitter != null)
            _emitters.Remove(emitter);
    }

    private void Update()
    {
        _frameCounter++;

        if (_emitters.Count == 0) return;

        if (_frameCounter % 120 == 0)
            _emitters.RemoveAll(e => e == null);

        Vector3 listenerPos = Vector3.zero;
        if (AcousticListener.Instance != null)
            listenerPos = AcousticListener.Instance.transform.position;
        else if (Camera.main != null)
            listenerPos = Camera.main.transform.position;

        int updatedThisFrame = 0;

        for (int i = 0; i < _emitters.Count; i++)
        {
            AcousticEmitter emitter = _emitters[i];
            if (emitter == null) continue;

            if (ShouldUpdateThisFrame(emitter, listenerPos))
            {
                emitter.PerformScan(listenerPos);
                updatedThisFrame++;
                if (updatedThisFrame >= maxEmittersPerFrame) break;
            }
        }
    }

    private bool ShouldUpdateThisFrame(AcousticEmitter emitter, Vector3 listenerPos)
    {
        if (emitter == null) return false;

        float dist = Vector3.Distance(listenerPos, emitter.transform.position);

        int interval;
        if (dist < lod.closeDistance)
            interval = lod.updateRateClose;
        else if (dist < lod.mediumDistance)
            interval = lod.updateRateMedium;
        else
            interval = lod.updateRateFar;

        if (interval <= 0) interval = 1;

        return (_frameCounter + emitter.GetInstanceID()) % interval == 0;
    }
}
