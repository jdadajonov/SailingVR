using UnityEngine;

namespace SailingVR.Sailing
{
    /// <summary>
    /// Keeps the boat riding on the water surface.
    ///
    /// Model: two-part vertical force
    ///   1. Gravity compensation: exactly cancels Rigidbody gravity when hull is in water.
    ///      This means the spring only needs to correct *position*, not fight gravity —
    ///      so stiffness can be modest and the equilibrium is exact.
    ///   2. Position spring + damper: drives the pivot toward (surfaceY + _freeboard).
    ///
    /// _freeboard = how far the pivot sits ABOVE the water surface when floating.
    ///   For the placeholder hull (0.25 m tall, pivot at centre):
    ///     freeboard = 0.04 m → pivot 4 cm above water → bottom of hull 8.5 cm below → looks right.
    ///   Adjust until the boat looks correctly waterlined in the Scene view.
    ///
    /// IMPORTANT: remove "Freeze Position Y" from the Rigidbody before using this component.
    /// </summary>
    public class Buoyancy : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody    _rigidbody;
        [SerializeField] private WaterSurface _water;

        [Header("Float Height")]
        [Tooltip("Distance from the GameObject pivot to the water surface when floating, metres.\n" +
                 "Positive = pivot above water (normal). Tune until hull looks right.\n" +
                 "Placeholder hull (0.25 m tall, pivot centred): try 0.04.")]
        [SerializeField] private float _freeboard = 0.04f;

        [Header("Spring")]
        [Tooltip("Restoring force per metre of position error, N/m.\n" +
                 "Does NOT need to fight gravity (that's handled separately).\n" +
                 "Higher → snappier response, lower → softer bobbing. Start at 3000.")]
        [SerializeField] private float _springStiffness = 3000f;

        [Tooltip("Vertical velocity damping, N·s/m. Prevents bouncing. Start at 250.")]
        [SerializeField] private float _springDamping = 250f;

        private void FixedUpdate()
        {
            if (_water == null) return;

            float surfaceY = _water.GetHeight(transform.position);
            float targetY  = surfaceY + _freeboard;
            float error    = targetY - transform.position.y; // positive = hull too low

            if (error <= 0f) return; // pivot above target — gravity brings it back, no buoyancy

            float vy = _rigidbody.linearVelocity.y;

            // 1. Cancel gravity so the spring only corrects position
            float gravityCancel = _rigidbody.mass * -Physics.gravity.y; // = mass × 9.81

            // 2. PD spring to drive pivot toward targetY
            float spring = error * _springStiffness - vy * _springDamping;

            _rigidbody.AddForce(Vector3.up * (gravityCancel + spring));
        }
    }
}
