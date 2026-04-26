using UnityEngine;

namespace SailingVR.Sailing
{
    /// <summary>
    /// Rotates the boom pivot and rudder pivot to match the physics state each frame.
    /// Purely cosmetic — does not affect forces.
    ///
    /// Attach to the Boat GameObject alongside Sail and Rudder.
    /// Wire _boomPivot and _rudderPivot in the Inspector (BoatPlaceholder does this automatically).
    /// </summary>
    public class BoomVisual : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Sail      _sail;
        [SerializeField] private Rudder    _rudder;
        [SerializeField] private Transform _boomPivot;    // [PH]BoomPivot Transform
        [SerializeField] private Transform _rudderPivot;  // [PH]RudderPivot Transform

        private void Reset()
        {
            _sail   = GetComponent<Sail>();
            _rudder = GetComponent<Rudder>();
        }

        private void Start()
        {
            if (_sail   == null) _sail   = GetComponent<Sail>();
            if (_rudder == null) _rudder = GetComponent<Rudder>();

            // Auto-find placeholder pivots by name if not wired in Inspector
            if (_boomPivot == null)
                _boomPivot = FindDeep(transform, "[PH]BoomPivot");
            if (_rudderPivot == null)
                _rudderPivot = FindDeep(transform, "[PH]RudderPivot");

            if (_boomPivot   == null) Debug.LogWarning("[BoomVisual] _boomPivot не найден. Назначь в Inspector или запусти Build Placeholder Boat.");
            if (_rudderPivot == null) Debug.LogWarning("[BoomVisual] _rudderPivot не найден.");
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform child in root)
            {
                if (child.name == name) return child;
                var found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private void LateUpdate()
        {
            UpdateBoom();
            UpdateRudder();
        }

        private void UpdateBoom()
        {
            if (_sail == null || _boomPivot == null) return;

            // Determine which side the boom is on.
            // Cross(boatForward, apparentWindHorizontal).y:
            //   > 0 → wind on starboard → boom goes to port (negative Y rotation)
            //   < 0 → wind on port      → boom goes to starboard (positive Y rotation)
            Vector3 aw          = _sail.ApparentWind;
            Vector3 awHoriz     = new Vector3(aw.x, 0f, aw.z);
            float   crossY      = Vector3.Cross(transform.forward, awHoriz).y;
            float   tackSign    = crossY >= 0f ? -1f : 1f;

            float targetAngle = _sail.IsLuffing
                ? 0f                                      // centred when luffing
                : tackSign * _sail.BoomAngleDeg;

            // Snap directly — smooth interpolation can be added later if desired
            _boomPivot.localRotation = Quaternion.Euler(0f, targetAngle, 0f);
        }

        private void UpdateRudder()
        {
            if (_rudder == null || _rudderPivot == null) return;

            // Rudder angle is positive to starboard when tiller goes to port.
            // On a Laser the tiller and rudder move opposite: tiller to port → rudder to starboard.
            _rudderPivot.localRotation = Quaternion.Euler(0f, _rudder.RudderAngleDeg, 0f);
        }
    }
}
