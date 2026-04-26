using UnityEngine;

namespace SailingVR.Sailing
{
    /// <summary>
    /// Hull form stability and crew hiking moment for a sailing dinghy.
    ///
    /// Two contributions to righting moment:
    ///
    /// 1. Hull GZ:
    ///    When the boat heels, the underwater volume shifts to leeward, moving the
    ///    centre of buoyancy (B) away from the centreline.  This creates an upright
    ///    restoring moment: τ_hull = W × GM_hull × sin(θ)
    ///    where W = boat weight, GM = metacentric height, θ = heel angle.
    ///    Laser hull-only GM ≈ 0.25 m (soft — without crew the boat is tender).
    ///
    /// 2. Crew hiking:
    ///    The sailor moves their centre of mass to windward.  On a Laser the sailor
    ///    can hike out ~0.5 m beyond the rail, adding significant righting moment.
    ///    We ramp the hiking arm linearly from 0° to _fullHikeAtDeg, then hold.
    ///    τ_crew = crewWeight × hikingArm × sin(θ)  (sin carries the direction).
    ///
    /// Implementation trick:
    ///    Vector3.Cross(transform.up, Vector3.up) is a vector whose direction is
    ///    "rotate boat back to upright" and whose magnitude is sin(heelAngle).
    ///    Multiplying by the combined righting coefficient gives the correct torque
    ///    in one line, with no explicit sin() call needed.
    ///
    /// Roll damping:
    ///    Water viscosity damps rolling motion.  We add a torque proportional to
    ///    angular velocity around the boat's fore-aft axis.
    /// </summary>
    public class Stability : MonoBehaviour
    {
        // ── Inspector ───────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private Rigidbody _rigidbody;

        [Header("Hull Form Stability")]
        [Tooltip("Metacentric height GM for hull form only, metres.\n" +
                 "Laser hull (no crew, no rig): GM ≈ 0.25 m.\n" +
                 "Higher → stiffer hull, harder to heel.")]
        [SerializeField] private float _hullGM = 0.25f;

        [Header("Crew Hiking")]
        [Tooltip("Crew mass, kg. Solo Laser sailor: 60–90 kg is typical.")]
        [SerializeField] private float _crewMassKg = 75f;

        [Tooltip("Maximum hiking arm: lateral distance of crew CoM from centreline, metres.\n" +
                 "Laser: sailor hikes to roughly 0.5 m outboard of the rail.")]
        [SerializeField] private float _maxHikingArmM = 0.5f;

        [Tooltip("Heel angle at which crew reaches full hike, degrees.\n" +
                 "A good Laser sailor is fully hiked by ~20–25°.")]
        [SerializeField] [Range(10f, 40f)] private float _fullHikeAtDeg = 22f;

        [Header("Roll Damping")]
        [Tooltip("Water resistance to rolling motion.\n" +
                 "Too low → boat oscillates; too high → feels sluggish.")]
        [SerializeField] [Range(0f, 5000f)] private float _rollDampingNmS = 800f;

        [Header("Capsize")]
        [Tooltip("Heel angle at which the boat is declared capsized and physics stops.")]
        [SerializeField] [Range(80f, 150f)] private float _capsizeAngleDeg = 110f;

        // ── Public state ─────────────────────────────────────────────────────

        /// <summary>Current heel angle, degrees. Positive = heeled to starboard.</summary>
        public float HeelAngleDeg { get; private set; }

        /// <summary>0 = sitting in, 1 = fully hiked. Drive hike animation from this.</summary>
        public float HikingFraction { get; private set; }

        /// <summary>True when heel exceeds the capsize threshold.</summary>
        public bool IsCapsized { get; private set; }

        // ── Private ──────────────────────────────────────────────────────────

        private const float Gravity = 9.81f;

        private void FixedUpdate()
        {
            // ── Heel angle ───────────────────────────────────────────────────
            // Signed angle from world-up to boat-up, around the boat's forward axis.
            // Positive = starboard side down (heeled to starboard).
            HeelAngleDeg = Vector3.SignedAngle(Vector3.up, transform.up, transform.forward);

            if (Mathf.Abs(HeelAngleDeg) >= _capsizeAngleDeg)
            {
                IsCapsized = true;
                return;
            }
            IsCapsized = false;

            // ── Righting axis ────────────────────────────────────────────────
            // Cross(transform.up, Vector3.up) points in exactly the direction that
            // rotates the boat back to upright, and its magnitude = sin(heelAngle).
            // This is the torque axis + sin(θ) factor in one vector.
            Vector3 rightingAxis = Vector3.Cross(transform.up, Vector3.up);

            // ── Hull righting moment coefficient ─────────────────────────────
            // τ_hull = W × GM × sin(θ)
            // sin(θ) is already in rightingAxis, so coefficient = W × GM
            float boatWeight = _rigidbody.mass * Gravity;
            float hullCoeff  = boatWeight * _hullGM;

            // ── Crew hiking moment coefficient ───────────────────────────────
            // Sailor hikes progressively as heel increases.
            HikingFraction = Mathf.Clamp01(Mathf.Abs(HeelAngleDeg) / _fullHikeAtDeg);
            float hikingArm  = HikingFraction * _maxHikingArmM;
            float crewWeight = _crewMassKg * Gravity;
            float crewCoeff  = crewWeight * hikingArm;

            // ── Apply combined righting torque ────────────────────────────────
            // τ = (hullCoeff + crewCoeff) × sin(θ), direction = back toward upright
            _rigidbody.AddTorque(rightingAxis * (hullCoeff + crewCoeff));

            // ── Roll damping ──────────────────────────────────────────────────
            // Damp angular velocity around the boat's longitudinal axis.
            // Without this the boat oscillates like a pendulum.
            float rollRate = Vector3.Dot(_rigidbody.angularVelocity, transform.forward);
            _rigidbody.AddTorque(-transform.forward * rollRate * _rollDampingNmS);
        }

        // ── Gizmos ───────────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            // Draw boat-up vector (green = upright, yellow = heeled, red = near capsize)
            float absHeel = Mathf.Abs(HeelAngleDeg);
            Gizmos.color = absHeel < 30f ? Color.green
                         : absHeel < 60f ? Color.yellow
                         : Color.red;
            Gizmos.DrawRay(transform.position, transform.up * 2f);
        }
    }
}
