# Wwise Setup Guide
### Fibonacci Acoustic Scanner Pro · K.K.Sound · v1.0

---

## Automatic Setup (Recommended)

The fastest way to configure Wwise is to run the included Python script.

### Prerequisites
- Python 3.7 or later
- Wwise open with WAAPI enabled: **Project → User Preferences → Enable Wwise Authoring API**

### Install the WAAPI client
```bash
pip install waapi-client
```

### Run the script
```bash
cd Wwise/
python setup_wwise.py
```

The script creates:
- 4 Game Parameters (`Acoustics_Occlusion`, `Acoustics_Diffraction`, `Acoustics_RoomSize`, `Acoustics_ReverbAmount`)
- `Aux_Reverb` bus under Master Audio Bus
- `New_RoomVerb` effect on that bus
- RTPC curves: `Decay Time → Acoustics_RoomSize` and `Output Bus Volume → Acoustics_ReverbAmount`

---

## Manual Setup

If you prefer to configure Wwise by hand, follow these steps exactly.

### Step 1 — Game Parameters

Go to **Game Syncs → Game Parameters → Default Work Unit**.  
Create four Game Parameters:

| Name | Min | Max | Default |
|------|-----|-----|---------|
| `Acoustics_Occlusion` | 0 | 100 | 0 |
| `Acoustics_Diffraction` | 0 | 100 | 0 |
| `Acoustics_RoomSize` | 0 | 100 | 0 |
| `Acoustics_ReverbAmount` | 0 | 100 | 0 |

---

### Step 2 — Aux Bus

Go to **Master-Mixer Hierarchy → Master Audio Bus**.  
Right-click → **New Child → Auxiliary Bus** → name it `Aux_Reverb`.

---

### Step 3 — RoomVerb Effect

Select `Aux_Reverb` → **Effects** tab → click **+** → choose **Wwise RoomVerb**.

---

### Step 4 — RTPC on Aux Bus

Select `Aux_Reverb` → **RTPC** tab → click **>>**:
- **Y Axis:** `Output Bus Volume`
- **X Axis:** `Acoustics_ReverbAmount`
- Curve points: `(0, -200)` and `(100, 0)`

---

### Step 5 — RTPC on RoomVerb

Click the **ROOMVERB** tab at the bottom → select the effect → **RTPC** tab → click **>>**:
- **Y Axis:** `Decay Time`
- **X Axis:** `Acoustics_RoomSize`
- Curve points: `(0, 0.2)` and `(100, 5.0)`

---

### Step 6 — Sound Objects

For each sound object that should respond to the acoustic system:

1. Open the sound in Wwise.
2. **General Settings** → **Game-Defined Auxiliary Sends** → enable both checkboxes.
3. **RTPC** tab → add the following mappings:

| Y Axis | X Axis |
|--------|--------|
| Voice Low-pass Filter | Acoustics_Occlusion |
| Voice Low-pass Filter | Acoustics_Diffraction |
| Voice Volume | Acoustics_Occlusion |
| Game-Defined Auxiliary Sends Volume | Acoustics_RoomSize |

4. **Positioning** tab → enable **Attenuation** → assign or create an attenuation asset with a Volume curve and a Low-pass filter curve.

---

### Step 7 — SoundBanks

Make sure your SoundBank output path points to:
```
<UnityProject>/Assets/StreamingAssets/Audio/GeneratedSoundBanks/
```
Set this in **Project → Project Settings → SoundBanks → Root Output Path**.

Click **Generate All**.

---

## After Setup — Unity Side

1. Open the `AcousticRTPCConfig` asset in Unity.
2. Click `>>` next to each RTPC field and assign the matching Game Parameter from your Wwise project.
3. Assign the config to `AcousticManager → Rtpc Config` in the Inspector.
4. Press Play and open **Acoustics → RTPC Debug Monitor** to verify values are flowing.
