using UnityEngine;

namespace SailingVR.Sailing
{
    /// <summary>
    /// Aerodynamic sail model for a single-sail dinghy (Laser).
    ///
    /// Lift/drag coefficients use a sinusoidal approximation over angle of attack (α):
    ///   Cl(α) = ClMax · sin(2α)          — peaks at α=45°, zero at luff and run
    ///   Cd(α) = CdMin + (CdMax-CdMin) · sin²(α)  — min at luff, max running dead
    ///
    /// The sheet controls boom angle.  Luffing is detected when the apparent wind
    /// is on the wrong side of the sail or α < LuffThreshold.
    /// Force is applied at the sail's centre of effort (≈ 1/3 up the mast).
    /// </summary>
    public class Sail : MonoBehaviour
    {
        // ── Inspector ───────────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private WindSystem _windSystem;
        [SerializeField] private Rigidbody  _rigidbody;

        [Tooltip("Transform at the mast base.  Used to resolve mast-up direction.")]
        [SerializeField] private Transform _mastBase;

        [Header("Sail Geometry")]
        [Tooltip("Sail area in m². Laser mainsail = 7.06 m².")]
        [SerializeField] private float _sailAreaM2 = 7.06f;

        [Tooltip("Mast height in metres. Laser = 6.27 m.")]
        [SerializeField] private float _mastHeightM = 6.27f;

        [Tooltip("Centre of effort as fraction of mast height. 1/3 is standard.")]
        [SerializeField] [Range(0.2f, 0.5f)] private float _centreOfEffortFraction = 0.333f;

        [Tooltip("Maximum boom angle from centreline, degrees. " +
                 "Laser traveller lets the boom go to ~80°.")]
        [SerializeField] [Range(60f, 90f)] private float _maxBoomAngleDeg = 80f;

        [Header("Aerodynamics")]
        [Tooltip("Air density kg/m³. Sea-level standard = 1.225.")]
        [SerializeField] private float _airDensityKgM3 = 1.225f;

        [Tooltip("Peak lift coefficient. Flat sail ≈ 1.0–1.5.")]
        [SerializeField] [Range(0.5f, 2.0f)] private float _clMax = 1.2f;

        [Tooltip("Minimum drag coefficient (friction when luffing).")]
        [SerializeField] [Range(0.01f, 0.2f)] private float _cdMin = 0.05f;

        [Tooltip("Maximum drag coefficient (running dead downwind — parachute mode).")]
        [SerializeField] [Range(0.5f, 2.0f)] private float _cdMax = 1.1f;

        [Tooltip("Angle of attack below this threshold counts as luffing (degrees).")]
        [SerializeField] [Range(1f, 10f)] private float _luffThresholdDeg = 3f;

        // ── Public state (read-only) ─────────────────────────────────────────

        /// <summary>Sheet position: 0 = hard in (boom at centreline), 1 = fully eased.</summary>
        public float SheetAmount
        {
            get => _sheetAmount;
            set => _sheetAmount = Mathf.Clamp01(value);
        }

        /// <summary>Current angle of attack in degrees (0 at luff, 90 at dead run).</summary>
        public float AngleOfAttackDeg { get; private set; }

        /// <summary>True when the sail is luffing (no useful drive force).</summary>
        public bool IsLuffing { get; private set; }

        /// <summary>Current boom angle from centreline in degrees (for mesh rotation).</summary>
        public float BoomAngleDeg { get; private set; }

        /// <summary>Apparent wind this frame (cached for HUD/debug).</summary>
        public Vector3 ApparentWind { get; private set; }

        /// <summary>Total aero force vector this frame (world space, for debug).</summary>
        public Vector3 LastSailForce { get; private set; }

        // ── Private ─────────────────────────────────────────────────────────

        [SerializeField] [Range(0f, 1f)] private float _sheetAmount = 0.3f; // serialised so it shows in Inspector

        private void FixedUpdate()
        {
            ApparentWind = _windSystem.GetApparentWind(_rigidbody.linearVelocity);

            Vector3 force = ComputeSailForce(ApparentWind);
            LastSailForce = force;

            if (force == Vector3.zero) return;

            // Centre of effort in world space: mast base + (mastUp × fraction × height)
            Vector3 mastUp = _mastBase != null ? _mastBase.up : transform.up;
            Vector3 coe = (_mastBase != null ? _mastBase.position : transform.position)
                          + mastUp * (_mastHeightM * _centreOfEffortFraction);

            _rigidbody.AddForceAtPosition(force, coe);
        }

        private Vector3 ComputeSailForce(Vector3 apparentWind)
        {
            float awSpeed = apparentWind.magnitude;
            if (awSpeed < 0.01f)
            {
                IsLuffing = true;
                AngleOfAttackDeg = 0f;
                return Vector3.zero;
            }

            // ── Boom direction ───────────────────────────────────────────────
            // The sheet lets the boom swing out to the leeward side.
            // We need to know which side: use the cross product of boat-forward and
            // apparent-wind to find the leeward side automatically.
            //
            // Cross(forward, AW_horizontal) points UP when wind is on starboard,
            // DOWN when wind is on port.  The boom always goes to the leeward side.
            Vector3 boatForward = transform.forward;
            Vector3 awHorizontal = Vector3.ProjectOnPlane(apparentWind, Vector3.up);

            // tackSign: +1 = wind on starboard (boom to port), -1 = wind on port (boom to starboard)
            float cross = Vector3.Cross(boatForward, awHorizontal).y;
            float tackSign = cross >= 0f ? -1f : 1f; // boom goes opposite side to wind

            BoomAngleDeg = _sheetAmount * _maxBoomAngleDeg;
            float boomRad = BoomAngleDeg * Mathf.Deg2Rad;

            // Boom direction in world space (horizontal, from mast toward clew)
            Vector3 boomDir = Quaternion.AngleAxis(tackSign * BoomAngleDeg, Vector3.up) * boatForward;

            // ── Angle of attack ──────────────────────────────────────────────
            // α = angle between apparent wind and boom chord.
            // We use the horizontal projections for a 2-D sail model.
            Vector3 awDir = awHorizontal.normalized;
            // AoA is the angle between where wind blows and the sail chord.
            // Both vectors in horizontal plane; take the unsigned angle.
            float cosAlpha = Vector3.Dot(awDir, boomDir);
            cosAlpha = Mathf.Clamp(cosAlpha, -1f, 1f);
            float alpha = Mathf.Acos(Mathf.Abs(cosAlpha)); // radians, [0, π/2]

            AngleOfAttackDeg = alpha * Mathf.Rad2Deg;

            // Luffing: AW comes from behind the sail chord (wrong side) or AoA too small
            bool wrongSide = cosAlpha < 0f; // wind pushes against back of sail
            IsLuffing = wrongSide || AngleOfAttackDeg < _luffThresholdDeg;
            if (IsLuffing)
                return Vector3.zero;

            // ── Lift and drag coefficients ───────────────────────────────────
            // Cl = ClMax · sin(2α)  → peaks at 45°, correct behaviour at 0° and 90°
            // Cd = CdMin + (CdMax-CdMin) · sin²(α)
            float sin2a = Mathf.Sin(2f * alpha);
            float sinA  = Mathf.Sin(alpha);
            float cl = _clMax * sin2a;
            float cd = _cdMin + (_cdMax - _cdMin) * (sinA * sinA);

            // ── Dynamic pressure ─────────────────────────────────────────────
            // q = ½ ρ V²   (Pascals)
            float q = 0.5f * _airDensityKgM3 * awSpeed * awSpeed;

            float fLift = q * _sailAreaM2 * cl; // Newtons, perpendicular to AW
            float fDrag = q * _sailAreaM2 * cd; // Newtons, opposing AW

            // ── Force vectors ────────────────────────────────────────────────
            // Drag: opposes apparent wind direction
            Vector3 dragVec = -awDir * fDrag;

            // Lift: perpendicular to AW in horizontal plane, toward leeward side.
            // Rotate AW direction by +90° (toward boom side) to get lift direction.
            // Lift always pushes the sail "away" from the wind (leeward).
            Vector3 liftDir = Vector3.Cross(Vector3.up, awDir) * tackSign; // horizontal perp
            // tackSign ensures lift goes the right way on each tack
            Vector3 liftVec = liftDir.normalized * fLift;

            return liftVec + dragVec;
        }

        // ── Gizmos ───────────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            Vector3 origin = transform.position + Vector3.up * (_mastHeightM * _centreOfEffortFraction);

            // Apparent wind (blue)
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(origin, ApparentWind * 0.3f);

            // Sail force (green = driving, yellow = small)
            Gizmos.color = IsLuffing ? Color.red : Color.green;
            Gizmos.DrawRay(origin, LastSailForce * 0.01f);
        }
    }
}
