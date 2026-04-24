# SailingVR — Dev Notes & Decisions

## 2026-04-24 — Project kickoff

### Setup complete
- Unity project created with URP template
- Switched build target to Android, configured for Quest 3
- Installed Meta XR All-in-One SDK, ran Project Setup Tool
- Fixed URP Upscaling = Automatic (XR requirement)
- Assigned URP asset in Graphics and Quality settings
- First test build on Quest 3: successful, I can see the scene in VR
- Git + LFS configured, pushed to GitHub

### Decisions made
- **Platform**: Meta Quest 3 only for now. Not targeting Quest 2 to avoid
  performance compromises early. PCVR not a goal (want standalone).
- **XR stack**: OpenXR with Meta feature group (modern path, future-proof).
- **Render pipeline**: URP Mobile-tuned. Not Built-in, not HDRP.
- **First boat**: Laser dinghy. Simple to simulate (one sail, one sheet, tiller),
  teaches the core loop.
- **Physics approach**: simplified lift/drag model on the sail, NOT full CFD.
  Parameters exposed for tuning. Goal is "feels right to a sailor", not simulator-grade.
- **Input**: hand tracking as primary, controllers as fallback. Quest 3 has
  good hand tracking, and pulling a rope with your hand is the point.

### Open questions
- How to simulate apparent wind vs true wind clearly enough for learning?
- What's the right level of physics abstraction for teaching beginners
  without drowning them in realism?
- Water surface: Gerstner waves shader, or simpler wave model?
- Motion sickness: how much boat heel is tolerable before I need to stabilize
  the horizon? Need to test with real people.

## Next session
- Implement WindSystem, Sail, Hull, Rudder components
- Simple scene: flat water, one boat, keyboard input
- Verify that pulling the sheet actually makes the boat go
