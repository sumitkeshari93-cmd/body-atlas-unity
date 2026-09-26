# Body Atlas (Unity)

High-quality **Unity** Android anatomy learning app for medical students.

> Honest scope: open **Z-Anatomy** atlas meshes (CC BY-SA 4.0), not commercial Visible Body photogrammetry — but a real layered 3D atlas with premium camera controls, far above capsule demos.

## Features

- **Premium orbit camera** — damped inertia, pitch clamp (~10–80°), zoom limits, separate rotate / pinch-zoom / two-finger pan, tap-vs-drag threshold, Reset View
- **Layer peel** — Skin → Muscles → Organs (Visceral) → Cardiovascular → Skeleton (fade / isolate chips)
- **Select** — tap mesh → emission highlight + student note panel + search
- **Heart focus** — fly to cardio region; **Go Inside** isolates cardiovascular + chamber labels (RA/LA/RV/LV)
- **Visuals** — URP-ready lighting (soft key + fill + rim), dark clinical studio, PBR-ish materials

## Models (Z-Anatomy)

Imported FBX from [LluisV/Z-Anatomy](https://github.com/LluisV/Z-Anatomy) (PC-Version):

| File | System |
|------|--------|
| `Regions of human body100.fbx` | Skin / regions |
| `MuscularSystem100.fbx` | Muscles |
| `VisceralSystem100.fbx` | Organs |
| `CardioVascular41.fbx` | Cardiovascular |
| `SkeletalSystem100.fbx` | Skeleton |
| `Joints100.fbx` | Joints (merged with skeleton) |

**License:** [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/) — credit **Z-Anatomy / Lluís Vinent Juanico**.

## Package

- Product: **Body Atlas**
- Company: **Sumit**
- Application id: `com.sumit.bodyatlasunity`
- Unity: **6000.3.23f1**
- Path: `/workspace/body-atlas-unity`

## Controls (mobile)

| Gesture | Action |
|---------|--------|
| 1-finger drag | Orbit (yaw/pitch, damped) |
| Pinch | Zoom |
| Two-finger drag | Pan target |
| Tap | Select structure |
| **Reset** | Restore default framing |
| **Heart** | Fly to heart / cardio |
| **Inside** | Isolate cardio + chamber labels |
| Layer chips | Toggle peel layers |
| Search | Jump to named mesh |

## Open & build

```bash
# Editor
/home/box/Unity/Hub/Editor/6000.3.23f1/Editor/Unity \
  -projectPath /workspace/body-atlas-unity

# Menu: Body Atlas → 1. Setup Scene
# Menu: Body Atlas → 3. Build Android APK (debug)

# Or batchmode (requires Unity Personal/Pro license + Android Build Support):
export ANDROID_HOME=/workspace/android-sdk
/home/box/Unity/Hub/Editor/6000.3.23f1/Editor/Unity \
  -batchmode -nographics -quit \
  -projectPath /workspace/body-atlas-unity \
  -executeMethod BodyAtlas.Editor.BodyAtlasBuild.SetupAndBuildAndroid \
  -logFile /workspace/artifacts/body-atlas-unity-build.log
```

APK output: `/workspace/artifacts/BodyAtlasUnity-debug.apk`

### Requirements for APK

1. Valid Unity Editor license (Personal OK)
2. **Android Build Support** + OpenJDK + Android SDK modules installed on the Editor
3. `ANDROID_HOME=/workspace/android-sdk` (already present on this machine)

## Scripts

`Assets/BodyAtlas/Scripts/` — OrbitCamera, LayerController, AnatomyPicker, HeartFocus, AnatomyCatalog, MaterialFactory, BodyAtlasUI, BodyAtlasApp  
`Assets/BodyAtlas/Editor/` — Setup Scene, Android Build

## Quality note

This is an **open anatomy atlas** for learning layers and structure names. It is intentionally above low-quality placeholder demos, and intentionally not a commercial photogrammetry product.


## Current build blocker (this machine)

APK batchmode requires an activated Unity Editor license. Android Build Support **is installed**. See `BUILD_STATUS.md`.

Activate Unity Personal via Hub, then re-run the batchmode command above → APK at `/workspace/artifacts/BodyAtlasUnity-debug.apk`.
