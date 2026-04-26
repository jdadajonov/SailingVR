using UnityEngine;

namespace SailingVR.Sailing
{
    /// <summary>
    /// Global wind: maintains true wind and computes apparent wind for any moving vessel.
    /// Attach once to a scene-level GameObject; Sail references it via Inspector.
    /// </summary>
    public class WindSystem : MonoBehaviour
    {
        // --- Inspector ---

        [Header("True Wind")]
        [Tooltip("Wind speed in knots (1 kn ≈ 0.514 m/s). Laser sails in 5-25 kn.")]
        [SerializeField] private float _trueWindSpeedKnots = 10f;

        [Tooltip("Direction the wind comes FROM, degrees clockwise from world +Z (north). " +
                 "0 = northerly (blows south), 90 = easterly (blows west).")]
        [SerializeField] [Range(0f, 360f)] private float _trueWindDirectionDeg = 0f;

        [Header("Gusts (optional)")]
        [Tooltip("Peak gust amplitude as fraction of base speed (0 = steady, 0.3 = ±30%).")]
        [SerializeField] [Range(0f, 0.5f)] private float _gustAmplitude = 0f;

        [Tooltip("Gust cycle frequency in Hz. 0.1 = one gust every 10 s.")]
        [SerializeField] [Range(0f, 1f)] private float _gustFrequency = 0.1f;

        // --- Public API ---

        /// <summary>
        /// True wind velocity vector in world space (m/s), pointing in the direction
        /// the air is travelling (opposite of "where it comes from").
        /// </summary>
        public Vector3 TrueWindVector { get; private set; }

        /// <summary>
        /// True wind speed in m/s (after gust modulation).
        /// </summary>
        public float TrueWindSpeedMs { get; private set; }

        /// <summary>
        /// Apparent wind a vessel at <paramref name="boatVelocity"/> would feel.
        /// V_AW = V_TW - V_boat  (classic vector triangle).
        /// </summary>
        public Vector3 GetApparentWind(Vector3 boatVelocity) => TrueWindVector - boatVelocity;

        // --- Private ---

        private float _gustPhase;

        private void FixedUpdate()
        {
            // Gust: simple sinusoidal modulation of speed
            _gustPhase += _gustFrequency * Time.fixedDeltaTime * Mathf.PI * 2f;
            float gustFactor = 1f + _gustAmplitude * Mathf.Sin(_gustPhase);

            float speedMs = _trueWindSpeedKnots * 0.51444f * gustFactor; // 1 kn = 0.51444 m/s
            TrueWindSpeedMs = speedMs;

            // Convert meteorological convention (where wind comes FROM, degrees CW from north)
            // to a Unity world-space direction vector (where the wind is going).
            //
            // Unity world: +X = east, +Z = north, +Y = up.
            // A northerly (0°) comes FROM north → travels TOWARD south → Vector3(0,0,-1).
            // We rotate that base "southward" vector by directionDeg around Y.
            float dirRad = _trueWindDirectionDeg * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Sin(dirRad), 0f, -Mathf.Cos(dirRad));
            TrueWindVector = direction * speedMs;
        }

        // Gizmo: draw an arrow showing true wind direction in the Scene view
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Vector3 origin = transform.position + Vector3.up * 2f;

            float dirRad = _trueWindDirectionDeg * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(dirRad), 0f, -Mathf.Cos(dirRad));
            Gizmos.DrawRay(origin, dir * 3f);
            Gizmos.DrawSphere(origin + dir * 3f, 0.15f);
        }
    }
}
