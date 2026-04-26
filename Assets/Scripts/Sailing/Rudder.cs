using UnityEngine;

namespace SailingVR.Sailing
{
    /// <summary>
    /// Rudder yaw-moment model.
    ///
    /// The rudder is a small underwater wing.  Its lift (≈ perpendicular to water flow)
    /// acts at the transom and creates a yaw torque around the boat's vertical axis.
    ///
    /// Simplified to one tunable parameter:
    ///   τ = tillerInput × RudderEffectiveness × V²
    ///
    /// RudderEffectiveness bundles: ½ρ · A_rudder · Cl · leverArm
    /// (all the constants that a non-CFD model doesn't need to separate).
    ///
    /// Key teaching property: torque is proportional to V², so at zero boat-speed
    /// the rudder has NO effect — the boat won't steer.
    ///
    /// Laser geometry: pushing tiller to starboard → bow goes to port.
    /// Convention here: tillerInput > 0 turns bow to starboard (positive yaw in Unity).
    /// Adjust sign if your mesh/coordinate system differs.
    /// </summary>
    public class Rudder : MonoBehaviour
    {
        // ── Inspector ───────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private Rigidbody _rigidbody;

        [Header("Rudder Parameters")]
        [Tooltip("Effectiveness coefficient, N·m·s²/m².\n" +
                 "= ½ρ_water · A_rudder · Cl_rudder · lever_arm\n" +
                 "Laser estimate: 0.5·1025·0.06·1.0·2.0 ≈ 61.5.  Start at 60, tune in Play Mode.")]
        [SerializeField] [Range(10f, 200f)] private float _rudderEffectiveness = 60f;

        [Tooltip("Maximum rudder angle degrees. Hard-over on a Laser ≈ 35°.")]
        [SerializeField] [Range(20f, 45f)] private float _maxRudderAngleDeg = 35f;

        [Tooltip("Angular drag added in water to prevent endless spinning. " +
                 "Unity's Rigidbody.angularDamping already helps; this adds physics-based damping.")]
        [SerializeField] [Range(0f, 5f)] private float _yawDampingCoeff = 1.0f;

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Tiller input in [-1, +1].
        /// +1 = tiller to port  → bow swings to starboard.
        /// -1 = tiller to starboard → bow swings to port.
        /// (On a Laser the tiller points aft; pushing port rotates bow starboard.)
        /// Set each frame by BoatInput.
        /// </summary>
        public float TillerInput
        {
            get => _tillerInput;
            set => _tillerInput = Mathf.Clamp(value, -1f, 1f);
        }

        /// <summary>Rudder angle in degrees for mesh animation (positive = to starboard).</summary>
        public float RudderAngleDeg { get; private set; }

        // ── Private ──────────────────────────────────────────────────────────

        [SerializeField] [Range(-1f, 1f)] private float _tillerInput; // serialised for Play Mode tweak

        private void FixedUpdate()
        {
            // Horizontal speed only — heave and pitch don't steer the boat
            Vector3 vel = _rigidbody.linearVelocity;
            float boatSpeedMs = new Vector3(vel.x, 0f, vel.z).magnitude;

            // Rudder angle tracks tiller (direct mechanical link)
            RudderAngleDeg = _tillerInput * _maxRudderAngleDeg;

            // Yaw torque: τ = tillerInput · effectiveness · V²
            // The V² term means: no speed → no steering.  Fundamental to teaching.
            float yawTorque = _tillerInput * _rudderEffectiveness * boatSpeedMs * boatSpeedMs;

            // Yaw damping proportional to angular velocity (resists spin)
            float angularVelY = _rigidbody.angularVelocity.y;
            float dampingTorque = -angularVelY * _yawDampingCoeff * boatSpeedMs;

            _rigidbody.AddTorque(Vector3.up * (yawTorque + dampingTorque));
        }

        // ── Gizmos ───────────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            // Visualise rudder angle as a small coloured ray at the stern
            Gizmos.color = Mathf.Abs(_tillerInput) < 0.05f ? Color.white : Color.yellow;
            Vector3 rudderPos = transform.position - transform.forward * 2.1f; // approximate transom
            Vector3 rudderDir = Quaternion.AngleAxis(-RudderAngleDeg, Vector3.up) * transform.forward;
            Gizmos.DrawRay(rudderPos, rudderDir * 1.5f);
        }
    }
}
