# Body Atlas Unity — Build Status

**Date:** 2026-09-26 ~12:50 IST (Asia/Calcutta)

## Delivered

| Item | Status |
|------|--------|
| Project `/workspace/body-atlas-unity` | ✅ Complete |
| Package `com.sumit.bodyatlasunity` / Product **Body Atlas** / Company **Sumit** | ✅ |
| Z-Anatomy FBX layers (Muscular, Skeletal, Visceral, Cardio, Regions, Joints) | ✅ Imported (~169MB) |
| Premium OrbitCamera + layer peel + picker + heart focus + UI | ✅ Scripts |
| URP settings assets | ✅ |
| Android Build Support module on Editor 6000.3.23f1 | ✅ Installed into `PlaybackEngines/AndroidPlayer` (~6.5GB) |
| GitHub `sumitkeshari93-cmd/body-atlas-unity` | ✅ Pushed `main` |
| APK `/workspace/artifacts/BodyAtlasUnity-debug.apk` | ❌ Blocked (see below) |

## APK blocker

Batchmode build log: `/workspace/artifacts/body-atlas-unity-build.log`

```
No valid Unity Editor license found. Please activate your license.
[Licensing::Module] Error: 'com.unity.editor.headless' was not found.
```

Unity Personal/Pro must be activated on this machine (Unity Hub sign-in or manual `.ulf` from the ALF at `/tmp/Unity_v6.alf`).  
Android SDK at `/workspace/android-sdk` is ready; AndroidPlayer module is installed.

## Build after license activation

```bash
export ANDROID_HOME=/workspace/android-sdk
export ANDROID_SDK_ROOT=/workspace/android-sdk
/home/box/Unity/Hub/Editor/6000.3.23f1/Editor/Unity \
  -batchmode -nographics -quit \
  -projectPath /workspace/body-atlas-unity \
  -executeMethod BodyAtlas.Editor.BodyAtlasBuild.SetupAndBuildAndroid \
  -logFile /workspace/artifacts/body-atlas-unity-build.log
```

Or in Editor: **Body Atlas → 1. Setup Scene** then **Body Atlas → 3. Build Android APK (debug)**.

## Control scheme

| Input | Action |
|-------|--------|
| 1-finger / LMB drag | Orbit (damped inertia, pitch 10–80°) |
| Pinch / scroll | Zoom (clamped) |
| Two-finger drag | Pan |
| Tap (below move/time threshold) | Select mesh → highlight + note |
| Reset / Heart / Inside | UI buttons |
| Layer chips | Skin / Muscles / Organs / Cardio / Skeleton |

## Quality note

Open **Z-Anatomy** atlas (CC BY-SA 4.0, Lluís Vinent) — not commercial Visible Body photogrammetry — but real layered FBX + premium camera, far above the rejected Godot capsule demo.
