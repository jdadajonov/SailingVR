# SailingVR — Project Context for Claude Code

## Project Summary

VR simulator for teaching basic sailing skills. Target platform: Meta Quest 3 standalone.
Goal: help beginners learn sailing terminology,motoring, docking,  points of sail, sail trim, tacking and
gybing, wind reading, and basic navigation before they step onto a real boat.

First boat modeled: Laser (single-handed dinghy). Future: keelboat, crewed sailing.

## Tech Stack

- Unity 6000.3.14f1 LTS (check ProjectSettings/ProjectVersion.txt for exact version)
- Render Pipeline: URP (Universal Render Pipeline)
- Target Platform: Android, Meta Quest 3
- XR Stack: OpenXR + Meta XR Feature Group (via Meta XR All-in-One SDK)
- Scripting Backend: IL2CPP
- Target Architecture: ARM64
- Minimum Android API: 29 (Android 10)
- Input: hand tracking primary, controllers fallback

## Development Machine

Working across two machines:
- MacBook Pro (macOS) — primary dev, editor work, logic
- Windows PC — builds, VR testing with Quest Link, performance profiling

Synchronized via GitHub with Git LFS for binary assets.

## Project Structure

Assets/
Scripts/
Sailing/     — Wind, Sail, Hull, Rudder physics
Input/       — VR input handling (hand tracking, controllers)
UI/          — In-world UI, instructor prompts
Utils/       — Helpers, math utilities
Prefabs/       — Boat, sail, cockpit assemblies
Scenes/        — Test scenes, tutorial scenes
Materials/     — Water, sail, hull materials
Models/        — 3D models (LFS)
Audio/         — Wind, water, rope sounds (LFS)
Settings/      — URP assets, input actions

## Code Conventions

- Namespaces: `SailingVR.Sailing`, `SailingVR.Input`, `SailingVR.UI`, `SailingVR.Utils`
- Naming: PascalCase for public members and types, _camelCase for private fields
- Use `[SerializeField] private` instead of public fields for Inspector-exposed values
- Prefer composition over inheritance for sailing components
- All public APIs documented with XML comments
- Physics components are MonoBehaviours with clear single responsibility

## Performance Constraints (Quest 3)

- Target 72 FPS minimum, aim for 90 FPS
- Mobile GPU (Snapdragon XR2 Gen 2) — no expensive shaders
- Draw calls budget: keep under 150 per frame
- Use URP Mobile-friendly features only
- MSAA 4x in URP asset for VR clarity
- HDR disabled, Render Scale 1.0
- No dynamic additional lights beyond 1 main directional

## Current Status

- [x] Unity project created with URP
- [x] Meta XR SDK installed and configured
- [x] First test build running on Quest 3 (empty scene with cube)
- [x] Git + GitHub + LFS set up
- [ ] Sailing physics (next task)
- [ ] VR hand interactions for sheet and tiller
- [ ] Water surface with waves
- [ ] Wind system
- [ ] Tutorial scenarios

## Next Task

Implement simplified sailing physics as MonoBehaviour components:
- `WindSystem` — global wind direction and speed, true vs apparent wind
- `Sail` — force from sail based on angle of attack to apparent wind
- `Hull` — hydrodynamic drag, keel lateral resistance
- `Rudder` — yaw control via tiller input

Start with keyboard input for testing (WASD + trim up/down).
VR input will be wired up afterwards.

## Things to Keep in Mind

- I am a sailor myself — I will notice if the physics is unrealistic.
  Add comments explaining lift/drag math so I can verify correctness.
- I am NOT an experienced Unity/VR developer — explain non-obvious Unity-isms.
- Prefer small, testable MonoBehaviours over large monolithic scripts.
- Always expose physics parameters via `[SerializeField]` so I can tune in Play Mode.
- Don't create new files unless necessary — ask before creating new systems.
- When writing physics: use SI units (meters, seconds, radians internally — degrees in Inspector is fine).
