using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SailingVR.Sailing
{
    /// <summary>
    /// Moves the XR camera rig with the boat so the player stays aboard.
    ///
    /// Every LateUpdate we compute the boat's position/yaw delta and apply it to the
    /// XR Origin. When _lockToHelm is on, the origin is also snapped to a fixed
    /// boat-local XZ position — the player can still look around freely via head
    /// tracking, but the floor origin never drifts away from the helm.
    ///
    /// Heel (roll) is NOT applied to the XR rig by default — causes motion sickness.
    ///
    /// Setup:
    ///   1. Add to Boat GameObject.
    ///   2. Assign _xrOrigin → OVRCameraRig Transform.
    ///   3. Right-click → "Position Rig at Helm".
    ///   4. Save and build.
    /// </summary>
    public class PlayerBoatAnchor : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Root Transform of your OVRCameraRig.")]
        [SerializeField] private Transform _xrOrigin;

        [Header("Helm Position Lock")]
        [Tooltip("Lock the XR origin to a fixed position relative to the boat. " +
                 "Player can still look freely; origin never drifts from the helm.")]
        [SerializeField] private bool  _lockToHelm  = true;
        [Tooltip("Boat-local X of the lock point. 0 = centred on boat.")]
        [SerializeField] private float _helmLocalX  = 0f;
        [Tooltip("Boat-local Z of the lock point. " +
                 "Negative = aft. Wheel is at -0.28; sit ~0.4 m behind it.")]
        [SerializeField] private float _helmLocalZ  = -0.68f;

        [Header("Heel following")]
        [Tooltip("Apply boat roll to XR rig. Realistic but causes motion sickness.")]
        [SerializeField] private bool _followHeel = false;

        [Header("Cockpit Bounds  (only used when _lockToHelm is OFF)")]
        [SerializeField] private float _boundsForward      = 0.5f;
        [SerializeField] private float _boundsBackward     = 1.2f;
        [SerializeField] private float _boundsHalfWidth    = 0.45f;
        [SerializeField] private float _boundsCorrectionSpeed = 4f;

        [Header("Seated Sailor Height")]
        [Tooltip("Standing user eye height above Quest floor origin, metres.")]
        [SerializeField] private float _userStandingEyeHeight = 1.65f;
        [Tooltip("Virtual eye height for a seated helmsman above cockpit floor, metres.")]
        [SerializeField] private float _seatedEyeHeight = 1.05f;
        [Tooltip("Cockpit floor Y above the Boat pivot, metres.")]
        [SerializeField] private float _cockpitFloorY = 0.15f;

        // ── Private ──────────────────────────────────────────────────────────

        private Vector3    _prevBoatPos;
        private Quaternion _prevBoatRot;

        // ── Editor helper ─────────────────────────────────────────────────────

#if UNITY_EDITOR
        [ContextMenu("Position Rig at Helm")]
        private void PositionRigAtHelm()
        {
            if (_xrOrigin == null) { Debug.LogWarning("[PlayerBoatAnchor] Assign _xrOrigin first."); return; }

            // rigY places the Quest floor so standing eye-height equals seatedEyeHeight
            // above the cockpit floor:
            //   rigY = boatY + cockpitFloorY + seatedEyeHeight - userStandingEyeHeight
            float rigY = transform.position.y + _cockpitFloorY + _seatedEyeHeight - _userStandingEyeHeight;

            // XZ: transform the helm boat-local position to world space
            Vector3 helmWorld = transform.TransformPoint(new Vector3(_helmLocalX, 0f, _helmLocalZ));

            _xrOrigin.position = new Vector3(helmWorld.x, rigY, helmWorld.z);
            _xrOrigin.rotation = transform.rotation;

            EditorUtility.SetDirty(_xrOrigin.gameObject);
            Debug.Log($"[PlayerBoatAnchor] Rig placed at helm. Y={rigY:F2}, " +
                      $"boat-local Z={_helmLocalZ:F2}. Save scene (Cmd+S).");
        }
#endif

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Start()
        {
            if (_xrOrigin == null)
            {
                Debug.LogWarning("[PlayerBoatAnchor] _xrOrigin not assigned.");
                enabled = false;
                return;
            }
            _prevBoatPos = transform.position;
            _prevBoatRot = transform.rotation;
        }

        private void LateUpdate()
        {
            ApplyBoatDelta();

            if (_lockToHelm)
                LockToHelm();
            else
                EnforceCockpitBounds();

            _prevBoatPos = transform.position;
            _prevBoatRot = transform.rotation;
        }

        // ── Movement ─────────────────────────────────────────────────────────

        private void ApplyBoatDelta()
        {
            _xrOrigin.position += transform.position - _prevBoatPos;

            Quaternion deltaRot = transform.rotation * Quaternion.Inverse(_prevBoatRot);
            deltaRot.ToAngleAxis(out float angle, out Vector3 axis);
            if (Mathf.Abs(angle) < 0.001f) return;

            if (_followHeel)
            {
                _xrOrigin.RotateAround(transform.position, axis, angle);
            }
            else
            {
                float deltaYaw = Mathf.DeltaAngle(_prevBoatRot.eulerAngles.y, transform.eulerAngles.y);
                _xrOrigin.RotateAround(transform.position, Vector3.up, deltaYaw);
            }
        }

        // Snap XR origin XZ to the helm point in boat local space.
        // Y stays unchanged — height is set once by PositionRigAtHelm and
        // then tracked via ApplyBoatDelta's position delta.
        private void LockToHelm()
        {
            Vector3 helmWorld = transform.TransformPoint(new Vector3(_helmLocalX, 0f, _helmLocalZ));
            _xrOrigin.position = new Vector3(helmWorld.x, _xrOrigin.position.y, helmWorld.z);
        }

        // ── Cockpit bounds (fallback when lock is off) ────────────────────────

        private void EnforceCockpitBounds()
        {
            Vector3 local    = transform.InverseTransformPoint(_xrOrigin.position);
            float   clampedX = Mathf.Clamp(local.x, -_boundsHalfWidth, _boundsHalfWidth);
            float   clampedZ = Mathf.Clamp(local.z, -_boundsBackward,  _boundsForward);
            Vector3 clamped  = transform.TransformPoint(new Vector3(clampedX, local.y, clampedZ));
            _xrOrigin.position = Vector3.Lerp(_xrOrigin.position, clamped, _boundsCorrectionSpeed * Time.deltaTime);
        }

        // ── Gizmos ───────────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            // Helm lock point
            Vector3 helmWorld = transform.TransformPoint(new Vector3(_helmLocalX, 1f, _helmLocalZ));
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(helmWorld, 0.15f);

            if (_lockToHelm) return;

            // Cockpit bounds when lock is off
            Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
            Matrix4x4 old = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            float midZ = (_boundsForward - _boundsBackward) * 0.5f;
            float lenZ = _boundsForward + _boundsBackward;
            Gizmos.DrawCube(new Vector3(0f, 1f, midZ), new Vector3(_boundsHalfWidth * 2f, 2f, lenZ));
            Gizmos.matrix = old;
        }
    }
}
