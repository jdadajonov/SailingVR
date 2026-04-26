using UnityEngine;

namespace SailingVR.Sailing
{
    /// <summary>
    /// VR throttle lever (РУД). Pinch the handle, then push forward (bow) for ahead,
    /// pull back (stern) for reverse.
    ///
    /// Attach to the LeverPivot GameObject (child of Boat).
    /// The pivot is at the BASE of the lever; children rotate with it so the
    /// handle swings forward/back visually.
    ///
    /// Push direction is measured in Boat-local Z so it stays correct
    /// regardless of boat heading.
    /// </summary>
    public class VRThrottleLever : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private OVRCameraRig    _cameraRig;
        [SerializeField] private MotorController _motor;
        [SerializeField] private Transform       _handleTransform; // sphere at top of lever

        [Header("Interaction")]
        [SerializeField] [Range(0.05f, 0.25f)] private float _grabRadius     = 0.12f;
        [SerializeField] [Range(0.1f,  0.9f)]  private float _pinchThreshold = 0.20f;

        [Header("Lever")]
        [Tooltip("Max lever tilt angle from vertical (degrees).")]
        [SerializeField] [Range(20f, 80f)]   private float _maxLeverAngleDeg = 45f;
        [Tooltip("Hand travel (metres, boat-local Z) for full deflection from grab point.")]
        [SerializeField] [Range(0.1f, 0.5f)] private float _handRange        = 0.25f;

        // ── State ─────────────────────────────────────────────────────────────

        private bool  _grabbed;
        private bool  _isRightHand;
        private float _grabRefZ;
        private float _grabRefThrottle;

        private OVRHand _leftHand, _rightHand;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Start()
        {
            if (_cameraRig == null) _cameraRig = FindObjectOfType<OVRCameraRig>();
            if (_motor     == null) _motor     = GetComponentInParent<MotorController>();

            if (_cameraRig != null)
            {
                _leftHand  = _cameraRig.leftHandAnchor .GetComponentInChildren<OVRHand>();
                _rightHand = _cameraRig.rightHandAnchor.GetComponentInChildren<OVRHand>();
            }

            // Auto-find handle sphere if not wired in Inspector
            if (_handleTransform == null)
            {
                foreach (Transform child in transform)
                    if (child.name.Contains("Handle")) { _handleTransform = child; break; }
            }
        }

        private void Update()
        {
            if (_cameraRig == null || _motor == null) return;

            Vector3 grabCenter = _handleTransform != null ? _handleTransform.position : transform.position;
            Vector3 lPos   = _cameraRig.leftHandAnchor .position;
            Vector3 rPos   = _cameraRig.rightHandAnchor.position;
            float   lPinch = PinchStrength(false);
            float   rPinch = PinchStrength(true);

            if (!_grabbed)
            {
                if (Vector3.Distance(rPos, grabCenter) < _grabRadius && rPinch >= _pinchThreshold)
                    BeginGrab(true, rPos);
                else if (Vector3.Distance(lPos, grabCenter) < _grabRadius && lPinch >= _pinchThreshold)
                    BeginGrab(false, lPos);
            }
            else
            {
                float pinch = _isRightHand ? rPinch : lPinch;
                if (pinch < _pinchThreshold * 0.5f)
                {
                    _grabbed = false;
                }
                else
                {
                    Vector3 hand  = _isRightHand ? rPos : lPos;
                    float   localZ = BoatLocalZ(hand);
                    float   delta  = (localZ - _grabRefZ) / _handRange;
                    _motor.ThrottleTarget = Mathf.Clamp(_grabRefThrottle + delta, -1f, 1f);
                }
            }

            // Rotate lever visual: positive throttle tilts top toward bow (+Z)
            // Euler(-angle,0,0): positive X rotation tilts +Y toward -Z, so negate for bow
            float leverAngle = _motor.ThrottleTarget * _maxLeverAngleDeg;
            transform.localRotation = Quaternion.Euler(-leverAngle, 0f, 0f);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void BeginGrab(bool isRight, Vector3 handPos)
        {
            _grabbed         = true;
            _isRightHand     = isRight;
            _grabRefZ        = BoatLocalZ(handPos);
            _grabRefThrottle = _motor.ThrottleTarget;
        }

        // Hand Z in Boat local space — pushing toward bow increases throttle.
        private float BoatLocalZ(Vector3 worldPos)
        {
            var parent = transform.parent;
            return parent != null
                ? parent.InverseTransformPoint(worldPos).z
                : worldPos.z;
        }

        private float PinchStrength(bool isRight)
        {
            OVRHand hand = isRight ? _rightHand : _leftHand;
            if (hand != null && hand.IsTracked)
            {
                float s = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
                if (s > 0.01f) return s;
            }
            return isRight
                ? Mathf.Max(OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger),
                            OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger))
                : Mathf.Max(OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger),
                            OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger));
        }
    }
}
