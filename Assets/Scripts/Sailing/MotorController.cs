using UnityEngine;

namespace SailingVR.Sailing
{
    /// <summary>
    /// Outboard motor: applies forward thrust proportional to throttle.
    ///
    /// Throttle range: -1 (full reverse) to +1 (full ahead).
    /// Force is applied at the transom so it creates a realistic yaw moment
    /// when combined with rudder deflection.
    ///
    /// Hull drag (Hull.cs) still applies and determines top speed.
    /// At full throttle, speed stabilises when thrust == drag.
    /// </summary>
    public class MotorController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody _rigidbody;

        [Header("Motor")]
        [Tooltip("Maximum thrust in Newtons. " +
                 "Small outboard (2-5 hp) ≈ 200-400 N. Tune for desired top speed.")]
        [SerializeField] private float _maxThrustN = 350f;

        [Tooltip("Throttle response speed — how fast throttle reaches the target value.")]
        [SerializeField] [Range(0.5f, 5f)] private float _throttleResponseSpeed = 2f;

        [Tooltip("Thrust application point in local space. " +
                 "Defaults to transom stern if left at (0,0,0).")]
        [SerializeField] private Vector3 _thrustPointLocal = new Vector3(0f, 0f, -2.1f);

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Target throttle set by input. -1 = full reverse, 0 = neutral, +1 = full ahead.</summary>
        public float ThrottleTarget
        {
            get => _throttleTarget;
            set => _throttleTarget = Mathf.Clamp(value, -1f, 1f);
        }

        /// <summary>Current (smoothed) throttle value.</summary>
        public float ThrottleCurrent { get; private set; }

        // ── Private ──────────────────────────────────────────────────────────

        private float _throttleTarget;

        private void FixedUpdate()
        {
            // Smooth throttle so engine doesn't snap to full power instantly
            ThrottleCurrent = Mathf.MoveTowards(
                ThrottleCurrent, _throttleTarget, _throttleResponseSpeed * Time.fixedDeltaTime);

            float thrust = ThrottleCurrent * _maxThrustN;
            if (Mathf.Abs(thrust) < 0.1f) return;

            Vector3 thrustPointWorld = transform.TransformPoint(_thrustPointLocal);
            _rigidbody.AddForceAtPosition(transform.forward * thrust, thrustPointWorld);
        }

        private void OnDrawGizmos()
        {
            Vector3 pt = transform.TransformPoint(_thrustPointLocal);
            Gizmos.color = ThrottleCurrent > 0.05f ? Color.green
                         : ThrottleCurrent < -0.05f ? Color.red
                         : Color.grey;
            Gizmos.DrawSphere(pt, 0.1f);
            Gizmos.DrawRay(pt, transform.forward * ThrottleCurrent * 1.5f);
        }
    }
}
