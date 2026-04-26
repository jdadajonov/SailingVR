using UnityEngine;

namespace SailingVR.Sailing
{
    /// <summary>
    /// Hydrodynamic drag for a sailing dinghy hull and daggerboard/centreboard.
    ///
    /// Two separate drag axes in the boat's local frame:
    ///   Longitudinal (fore-aft): low coefficient — allows the boat to build speed.
    ///   Lateral (beam):          high coefficient — the daggerboard resists leeway,
    ///                            which is what makes upwind sailing possible.
    ///
    /// Both forces are quadratic in velocity: F = ½ρV²·A·Cd
    /// Applied at the Rigidbody's centre of mass (no heeling moment from drag).
    /// </summary>
    public class Hull : MonoBehaviour
    {
        // ── Inspector ───────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private Rigidbody _rigidbody;

        [Header("Water Properties")]
        [Tooltip("Water density kg/m³. Salt water = 1025, fresh = 1000.")]
        [SerializeField] private float _waterDensityKgM3 = 1025f;

        [Header("Longitudinal Drag  (fore-aft)")]
        [Tooltip("Hull frontal cross-section area m². Laser ≈ 0.08 m².")]
        [SerializeField] private float _hullFrontalAreaM2 = 0.08f;

        [Tooltip("Longitudinal drag coefficient. Low: hull moves through water easily. " +
                 "Tune to match hull speed ~4.5 kn (≈2.3 m/s) at full sail.")]
        [SerializeField] [Range(0.005f, 0.2f)] private float _cdLongitudinal = 0.03f;

        [Header("Lateral Drag  (daggerboard / centreboard)")]
        [Tooltip("Daggerboard lateral projected area m². Laser blade ≈ 0.18 m².")]
        [SerializeField] private float _daggerboardAreaM2 = 0.18f;

        [Tooltip("Lateral drag coefficient. Must be >> longitudinal to prevent leeway. " +
                 "A high value means the keel does its job.")]
        [SerializeField] [Range(0.5f, 10f)] private float _cdLateral = 3.5f;

        // ── Public state (read-only) ─────────────────────────────────────────

        /// <summary>Leeway angle in degrees (positive = drifting to leeward).</summary>
        public float LeewayAngleDeg { get; private set; }

        /// <summary>Boat speed through the water in m/s.</summary>
        public float SpeedMs { get; private set; }

        /// <summary>Boat speed in knots.</summary>
        public float SpeedKnots => SpeedMs * 1.94384f;

        /// <summary>Total drag force this frame (world space, for debug).</summary>
        public Vector3 LastDragForce { get; private set; }

        // ── FixedUpdate ──────────────────────────────────────────────────────

        private void FixedUpdate()
        {
            Vector3 velocity = _rigidbody.linearVelocity;
            velocity.y = 0f; // ignore vertical — boat stays on water surface via constraints

            SpeedMs = velocity.magnitude;

            // Decompose velocity into boat-local axes
            // transform.forward = bow direction,  transform.right = starboard
            float vForward = Vector3.Dot(velocity, transform.forward);
            float vLateral = Vector3.Dot(velocity, transform.right);

            // Quadratic drag: F = ½ρV²·A·Cd, direction opposes motion
            // Longitudinal
            float fLong = 0.5f * _waterDensityKgM3
                          * vForward * Mathf.Abs(vForward)  // signed V² keeps direction
                          * _hullFrontalAreaM2 * _cdLongitudinal;

            // Lateral (daggerboard)
            float fLat  = 0.5f * _waterDensityKgM3
                          * vLateral * Mathf.Abs(vLateral)
                          * _daggerboardAreaM2 * _cdLateral;

            // Note: using V·|V| (not V²) preserves sign so force always opposes motion.

            Vector3 dragForce = -transform.forward * fLong - transform.right * fLat;
            LastDragForce = dragForce;
            _rigidbody.AddForce(dragForce);

            // Leeway: angle between actual velocity and bow direction
            if (SpeedMs > 0.1f)
            {
                float sinLeeway = vLateral / SpeedMs;
                LeewayAngleDeg = Mathf.Asin(Mathf.Clamp(sinLeeway, -1f, 1f)) * Mathf.Rad2Deg;
            }
            else
            {
                LeewayAngleDeg = 0f;
            }
        }

        // ── Gizmos ───────────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, LastDragForce * 0.01f);
        }
    }
}
