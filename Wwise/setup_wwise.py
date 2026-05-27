"""
Fibonacci Acoustic Scanner Pro — Wwise Auto-Setup
K.K.Sound · v1.3

Creates everything needed for the plugin in Wwise:
  - 4 Game Parameters
  - Aux Bus (Aux_Reverb) with RoomVerb effect
  - RTPC curves on Aux Bus and RoomVerb
  - Attenuation ShareSet with Volume and Low-pass filter curves
  - Configures a target sound object:
      · Game-Defined Auxiliary Sends (enabled)
      · Early Reflections → Aux_Reverb
      · RTPC curves (Voice Volume, LPF, Aux Sends)
      · Positioning (3D, Attenuation, Diffraction)

Requirements:
    pip install waapi-client
    Wwise must be open with WAAPI enabled:
    Project > User Preferences > Enable Wwise Authoring API

Usage:
    python setup_wwise.py
    python setup_wwise.py --sound "MySound"
    python setup_wwise.py --sound-path "\\Actor-Mixer Hierarchy\\Default Work Unit\\MyFolder" --sound "MySound"
    python setup_wwise.py --no-sound
"""

import sys
import argparse

try:
    from waapi import WaapiClient, CannotConnectToWaapiException
except ImportError:
    print("[ERROR] waapi-client is not installed. Run: pip install waapi-client")
    sys.exit(1)


# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------

WAAPI_URL             = "ws://127.0.0.1:8080/waapi"
AUX_BUS_NAME          = "Aux_Reverb"
EFFECT_NAME           = "New_RoomVerb"
ATTENUATION_NAME      = "FAS_Attenuation"
DEFAULT_SOUND         = "Radio"
ATTENUATION_MAX_DIST  = 21.0

GAME_PARAMETERS = [
    {"name": "Acoustics_Occlusion",    "min": 0.0, "max": 100.0, "default": 0.0},
    {"name": "Acoustics_Diffraction",  "min": 0.0, "max": 100.0, "default": 0.0},
    {"name": "Acoustics_RoomSize",     "min": 0.0, "max": 100.0, "default": 0.0},
    {"name": "Acoustics_ReverbAmount", "min": 0.0, "max": 100.0, "default": 0.0},
]

VOLUME_CURVE_POINTS = [
    {"x": 0.0,                   "y": 0.0,    "shape": "Log3"},
    {"x": 10.0,                  "y": -18.0,  "shape": "Log3"},
    {"x": ATTENUATION_MAX_DIST,  "y": -200.0, "shape": "Linear"},
]

LPF_CURVE_POINTS = [
    {"x": 0.0,                  "y": 0.0,   "shape": "Linear"},
    {"x": 12.0,                 "y": 40.0,  "shape": "Linear"},
    {"x": 16.0,                 "y": 70.0,  "shape": "Linear"},
    {"x": ATTENUATION_MAX_DIST, "y": 100.0, "shape": "Linear"},
]

TRANSMISSION_VOLUME_POINTS = [
    {"x": 0.0,   "y": 0.0,    "shape": "Linear"},
    {"x": 100.0, "y": -200.0, "shape": "Linear"},
]

# ReverbAmount curve: silent outdoors (0-35), fades in indoors (35-100)
REVERB_AMOUNT_CURVE_POINTS = [
    {"x": 0.0,   "y": -200.0, "shape": "Linear"},
    {"x": 35.0,  "y": -200.0, "shape": "Linear"},
    {"x": 100.0, "y": 0.0,    "shape": "SCurve"},
]


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def obj_exists(client, path):
    try:
        r = client.call("ak.wwise.core.object.get", {
            "from": {"path": [path]},
            "options": {"return": ["id"]},
        })
        return bool(r and r.get("return"))
    except Exception:
        return False


def get_or_create(client, parent, name, obj_type, class_id=None):
    path = f"{parent}\\{name}"
    if obj_exists(client, path):
        r = client.call("ak.wwise.core.object.get", {
            "from": {"path": [path]},
            "options": {"return": ["id"]},
        })
        guid = r["return"][0]["id"]
        print(f"  [exists]  {name}  ({guid})")
        return guid, path

    args = {
        "parent": parent,
        "type": obj_type,
        "name": name,
        "onNameConflict": "rename",
    }
    if class_id:
        args["classId"] = class_id

    r = client.call("ak.wwise.core.object.create", args)
    guid = r["id"]
    print(f"  [created] {name}  ({guid})")
    return guid, path


def set_prop(client, path, prop, value):
    try:
        client.call("ak.wwise.core.object.setProperty", {
            "object": path, "property": prop, "value": value,
        })
    except Exception as e:
        print(f"  [warn] setProperty {prop} on {path}: {e}")


def remove_existing_rtpc(client, obj_path, prop, gp_name):
    try:
        client.call("ak.wwise.core.object.removeRTPCCurve", {
            "object":   obj_path,
            "property": prop,
            "rtpc":     {"name": gp_name},
        })
    except Exception:
        pass


def add_rtpc(client, obj_path, prop, gp_name, points, mode="absolute"):
    remove_existing_rtpc(client, obj_path, prop, gp_name)
    try:
        client.call("ak.wwise.core.object.addRTPCCurve", {
            "object":   obj_path,
            "property": prop,
            "rtpc":     {"name": gp_name},
            "mode":     mode,
            "points":   points,
        })
        print(f"  [rtpc]    {prop} -> {gp_name}  ({obj_path.split(chr(92))[-1]})")
    except Exception as e:
        print(f"  [warn] addRTPCCurve {prop} -> {gp_name}: {e}")


def prompt_sound_path():
    print()
    print("  Enter the Wwise path to the folder containing your sound object.")
    print("  Example: \\Actor-Mixer Hierarchy\\Default Work Unit\\Ambient\\MySounds")
    print("  Press Enter to use the Actor-Mixer root.")
    value = input("  Sound parent path: ").strip()
    if not value:
        return "\\Actor-Mixer Hierarchy\\Default Work Unit"
    return value


# ---------------------------------------------------------------------------
# Setup steps
# ---------------------------------------------------------------------------

def step_game_parameters(client):
    print("\n--- Game Parameters ---")
    parent = "\\Game Parameters\\Default Work Unit"
    for gp in GAME_PARAMETERS:
        _, path = get_or_create(client, parent, gp["name"], "GameParameter")
        set_prop(client, path, "RangeMin",     gp["min"])
        set_prop(client, path, "RangeMax",     gp["max"])
        set_prop(client, path, "InitialValue", gp["default"])


def step_aux_bus(client):
    print("\n--- Aux Bus ---")
    parent = "\\Master-Mixer Hierarchy\\Default Work Unit\\Master Audio Bus"
    _, bus_path = get_or_create(client, parent, AUX_BUS_NAME, "AuxBus")

    add_rtpc(client, bus_path, "OutputBusVolume", "Acoustics_ReverbAmount",
             REVERB_AMOUNT_CURVE_POINTS)

    return bus_path


def step_room_verb(client, bus_path):
    print("\n--- RoomVerb Effect ---")
    _, fx_path = get_or_create(client, bus_path, EFFECT_NAME, "Effect")

    add_rtpc(client, fx_path, "DecayTime", "Acoustics_RoomSize", [
        {"x": 0.0,   "y": 0.2, "shape": "Linear"},
        {"x": 100.0, "y": 5.0, "shape": "Linear"},
    ])


def step_attenuation(client):
    print("\n--- Attenuation ShareSet ---")
    parent   = "\\ShareSets\\Attenuations\\Default Work Unit"
    att_path = f"{parent}\\{ATTENUATION_NAME}"

    if not obj_exists(client, att_path):
        r = client.call("ak.wwise.core.object.create", {
            "parent": parent,
            "type": "Attenuation",
            "name": ATTENUATION_NAME,
            "onNameConflict": "rename",
        })
        print(f"  [created] {ATTENUATION_NAME}  ({r['id']})")
    else:
        r = client.call("ak.wwise.core.object.get", {
            "from": {"path": [att_path]},
            "options": {"return": ["id"]},
        })
        print(f"  [exists]  {ATTENUATION_NAME}  ({r['return'][0]['id']})")

    set_prop(client, att_path, "MaxDistance", ATTENUATION_MAX_DIST)

    for curve_type, points, label in [
        ("DistanceOutputBusVolume",   VOLUME_CURVE_POINTS,          "Distance -> Volume"),
        ("DistanceLowPassFilter",     LPF_CURVE_POINTS,             "Distance -> Low-pass filter"),
        ("DistanceTransmissionVolume",TRANSMISSION_VOLUME_POINTS,   "Transmission -> Volume"),
    ]:
        try:
            client.call("ak.wwise.core.attenuation.setCurve", {
                "attenuation": att_path,
                "curveType":   curve_type,
                "use":         "Custom",
                "points":      points,
            })
            print(f"  [curve]   {label}")
        except Exception as e:
            print(f"  [warn] {label}: {e}")

    for curve_type in ["DistanceGameDefinedAuxiliarySendsVolume",
                       "DistanceUserDefinedAuxiliarySendsVolume"]:
        try:
            client.call("ak.wwise.core.attenuation.setCurve", {
                "attenuation": att_path,
                "curveType":   curve_type,
                "use":         "UseDistanceVolume",
                "points":      [],
            })
        except Exception as e:
            print(f"  [warn] {curve_type}: {e}")

    return att_path


def step_sound(client, sound_name, sound_parent_path, att_path):
    print(f"\n--- Sound: {sound_name} ---")
    sound_path = f"{sound_parent_path}\\{sound_name}"

    if not obj_exists(client, sound_path):
        print(f"  [skip] Sound not found: {sound_path}")
        print(f"         Use --sound-path to specify the correct parent path.")
        return

    aux_bus_path = f"\\Master-Mixer Hierarchy\\Default Work Unit\\Master Audio Bus\\{AUX_BUS_NAME}"

    set_prop(client, sound_path, "OverrideGameAuxSends",  True)
    set_prop(client, sound_path, "UseGameDefinedAuxSend", True)
    print(f"  [set]     Game-Defined Auxiliary Sends = ON")

    try:
        set_prop(client, sound_path, "OverrideEarlyReflections", True)
        set_prop(client, sound_path, "EarlyReflectionsAuxSend",  aux_bus_path)
        print(f"  [set]     Early Reflections -> {AUX_BUS_NAME}")
    except Exception as e:
        print(f"  [warn] Early Reflections: {e}")

    set_prop(client, sound_path, "OverridePositioning",        True)
    set_prop(client, sound_path, "3DSpatialization",           "SpatializationMode_PositionOnly")
    set_prop(client, sound_path, "SpeakerPanning",             "SpeakerPanningType_BalanceFade")
    set_prop(client, sound_path, "DiffractionAndTransmission", True)
    print(f"  [set]     Positioning: Position, Balance-Fade, Diffraction ON")

    try:
        set_prop(client, sound_path, "OverrideAttenuation", True)
        set_prop(client, sound_path, "Attenuation",         att_path)
        set_prop(client, sound_path, "AttenuationMode",     "AttenuationMode_Custom")
        print(f"  [set]     Attenuation -> {ATTENUATION_NAME}")
    except Exception as e:
        print(f"  [warn] Attenuation: {e}")

    add_rtpc(client, sound_path, "Volume", "Acoustics_Occlusion", [
        {"x": 0.0,   "y": 0.0,    "shape": "Linear"},
        {"x": 100.0, "y": -200.0, "shape": "Linear"},
    ])
    add_rtpc(client, sound_path, "LowPassFilter", "Acoustics_Occlusion", [
        {"x": 0.0,   "y": 0.0,   "shape": "Linear"},
        {"x": 100.0, "y": 100.0, "shape": "Linear"},
    ])
    add_rtpc(client, sound_path, "LowPassFilter", "Acoustics_Diffraction", [
        {"x": 0.0,   "y": 0.0,   "shape": "Linear"},
        {"x": 100.0, "y": 100.0, "shape": "Linear"},
    ])
    add_rtpc(client, sound_path, "GameAuxSendVolume", "Acoustics_RoomSize", [
        {"x": 0.0,   "y": -200.0, "shape": "Linear"},
        {"x": 100.0, "y": 0.0,    "shape": "Linear"},
    ])
    add_rtpc(client, sound_path, "OutputBusVolume", "Acoustics_ReverbAmount",
             REVERB_AMOUNT_CURVE_POINTS)


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(description="FAS Pro — Wwise Auto-Setup")
    parser.add_argument("--sound",      default=DEFAULT_SOUND,
                        help="Name of the sound object in Wwise")
    parser.add_argument("--sound-path", default=None,
                        help="Wwise path to the folder containing the sound object")
    parser.add_argument("--no-sound",   action="store_true",
                        help="Skip sound object configuration")
    args = parser.parse_args()

    print("=" * 60)
    print("  Fibonacci Acoustic Scanner Pro — Wwise Setup v1.3")
    print("  K.K.Sound")
    print("=" * 60)

    try:
        with WaapiClient(url=WAAPI_URL) as client:
            print(f"\nConnected to Wwise: {WAAPI_URL}")

            step_game_parameters(client)
            bus_path = step_aux_bus(client)
            step_room_verb(client, bus_path)
            att_path = step_attenuation(client)

            if not args.no_sound:
                sound_parent = args.sound_path or prompt_sound_path()
                step_sound(client, args.sound, sound_parent, att_path)

            print("\n" + "=" * 60)
            print("  Done!")
            print()
            print("  Next steps:")
            print("  1. Wwise -> Generate All (SoundBank Manager)")
            print("  2. Unity -> AcousticRTPCConfig -> assign 4 Game Parameters")
            print("  3. Press Play and open Acoustics > RTPC Debug Monitor")
            print("=" * 60)

    except CannotConnectToWaapiException:
        print("\n[ERROR] Cannot connect to Wwise.")
        print("  Make sure Wwise is open and WAAPI is enabled:")
        print("  Project > User Preferences > Enable Wwise Authoring API")
        sys.exit(1)
    except Exception as e:
        print(f"\n[ERROR] {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)


if __name__ == "__main__":
    main()
