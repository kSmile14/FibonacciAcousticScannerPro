using UnityEngine;

public static class WwiseAcousticBridge
{
    public static AcousticRTPCConfig Config { get; set; }

    public struct AcousticData
    {
        public float occlusion;
        public float obstruction;
        public float diffraction;
        public float roomSize;
        public float reverbAmount;
    }

    public static void UpdateAcoustics(GameObject target, AcousticData data)
    {
        if (target == null) return;

        data.occlusion    = Mathf.Clamp01(data.occlusion);
        data.obstruction  = Mathf.Clamp01(data.obstruction);
        data.diffraction  = Mathf.Clamp01(data.diffraction);
        data.roomSize     = Mathf.Clamp01(data.roomSize);
        data.reverbAmount = Mathf.Clamp01(data.reverbAmount);

        if (Config == null)
        {
            Debug.LogWarning("[WwiseAcousticBridge] No AcousticRTPCConfig assigned.");
            return;
        }

        float scale = Config.rtpcScale;

#if AK_WWISE_UNITY_API
        // Occlusion: sent both per-object (voice volume) and globally (LPF).
        if (Config.occlusionRTPC.IsValid())
        {
            Config.occlusionRTPC.SetValue(target, data.occlusion * scale);
            Config.occlusionRTPC.SetValue(null,   data.occlusion * scale);
        }

        // Diffraction: sent both per-object and globally.
        if (Config.diffractionRTPC.IsValid())
        {
            Config.diffractionRTPC.SetValue(target, data.diffraction * scale);
            Config.diffractionRTPC.SetValue(null,   data.diffraction * scale);
        }

        // RoomSize and ReverbAmount: global only — these describe the listener's environment,
        // not any individual sound source.
        if (Config.roomSizeRTPC.IsValid())
            Config.roomSizeRTPC.SetValue(null, data.roomSize * scale);

        if (Config.reverbAmountRTPC.IsValid())
            Config.reverbAmountRTPC.SetValue(null, data.reverbAmount * scale);
#else
        Debug.Log($"[WwiseAcousticBridge] {target.name} | " +
                  $"occ={data.occlusion * scale:F1} | " +
                  $"diff={data.diffraction * scale:F1} | " +
                  $"room={data.roomSize * scale:F1} | " +
                  $"reverb={data.reverbAmount * scale:F1}");
#endif
    }
}
