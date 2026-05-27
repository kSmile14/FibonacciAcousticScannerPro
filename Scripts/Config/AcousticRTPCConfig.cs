using UnityEngine;

[CreateAssetMenu(fileName = "AcousticRTPCConfig", menuName = "Acoustics/RTPC Config")]
public class AcousticRTPCConfig : ScriptableObject
{
    [Header("Wwise Game Parameters")]
    [Tooltip("Wwise Game Parameter that receives the occlusion value.")]
    public AK.Wwise.RTPC occlusionRTPC;

    [Tooltip("Wwise Game Parameter that receives the diffraction value.")]
    public AK.Wwise.RTPC diffractionRTPC;

    [Tooltip("Wwise Game Parameter that receives the room size value.")]
    public AK.Wwise.RTPC roomSizeRTPC;

    [Tooltip("Wwise Game Parameter that receives the reverb amount value.")]
    public AK.Wwise.RTPC reverbAmountRTPC;

    [Header("Obstruction / Occlusion")]
    [Tooltip("Use Wwise's built-in per-object obstruction and occlusion system (affects LPF and volume). Recommended.")]
    public bool useBuiltInObstructionOcclusion = true;

    [Header("Scaling")]
    [Range(1f, 100f)]
    [Tooltip("Multiplier applied before sending normalised 0–1 values to Wwise Game Parameters.")]
    public float rtpcScale = 100f;
}
