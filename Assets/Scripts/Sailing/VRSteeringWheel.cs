using UnityEngine;

namespace SailingVR.Sailing
{
    /// <summary>
    /// VR steering wheel. Pinch anywhere near the rim, then rotate your hand to steer.
    ///
    /// Attach to the WheelPivot GameObject (child of Boat).
    /// Rotates that pivot around its local Z axis (fore-aft axis) and maps
    /// wheel angle to Rudder.TillerInput.
    ///
    /// Dependencies resolved at Start() via GetComponentInParent / FindObjectOfType —
    /// no manual wiring required.
    /// </summary>
    public class VRSteeringWheel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private OVRCameraRig _cameraRig;
        [SerializeField] private Rudder       _rudder;

        [Header("Interaction")]
        [SerializeField] [Range(0.05f, 0.35f)] private float _grabRadius     = 0.22f;
        [SerializeField] [Range(0.1f,  0.9f)]  private float _pinchThreshold = 0.20f;

        [Header("Steering")]
        [Tooltip("Wheel lock-to-lock rotation in each direction.")]
        [SerializeField] [Range(90f, 540f)] private float _maxWheelAngleDeg = 270f;
        [Tooltip("Degrees per second the wheel self-centres when not held.")]
        [SerializeField] [Range(30f, 300f)] private float _returnSpeed       = 120f;

        // ── State ─────────────────────────────────────────────────────────────

        private bool  _grabbed;
        private bool  _isRightHand;
        private float _prevHandAngle;
        private float _wheelAngle;

        private OVRHand _leftHand, _rightHand;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Start()
        {
            if (_cameraRig == null) _cameraRig = FindObjectOfType<OVRCameraRig>();
            if (_rudder    == null) _rudder    = GetComponentInParent<Rudder>();

            if (_cameraRig != null)
            {
                _leftHand  = _cameraRig.leftHandAnchor .GetComponentInChildren<OVRHand>();
                _rightHand = _cameraRig.rightHandAnchor.GetComponentInChildren<OVRHand>();
            }

            // Prevent conflict with VRSailingController tiller input
            var legacy = GetComponentInParent<VRSailingController>();
            if (legacy != null && legacy.enabled)
            {
                legacy.enabled = false;
                Debug.Log("[VRSteeringWheel] Disabled VRSailingController to avoid input conflict.");
            }
        }

        private void Update()
        {
            if (_cameraRig == null || _rudder == null) return;

            Vector3 lPos   = _cameraRig.leftHandAnchor .position;
            Vector3 rPos   = _cameraRig.rightHandAnchor.position;
            float   lPinch = PinchStrength(false);
            float   rPinch = PinchStrength(true);

            if (!_grabbed)
            {
                if (Vector3.Distance(rPos, transform.position) < _grabRadius && rPinch >= _pinchThreshold)
                    BeginGrab(true, rPos);
                else if (Vector3.Distance(lPos, transform.position) < _grabRadius && lPinch >= _pinchThreshold)
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
                    float   angle = HandAngle(hand);
                    _wheelAngle    = Mathf.Clamp(_wheelAngle + Mathf.DeltaAngle(_prevHandAngle, angle),
                                                 -_maxWheelAngleDeg, _maxWheelAngleDeg);
                    _prevHandAngle = angle;
                }
            }

            if (!_grabbed)
                _wheelAngle = Mathf.MoveTowards(_wheelAngle, 0f, _returnSpeed * Time.deltaTime);

            transform.localRotation = Quaternion.Euler(0f, 0f, _wheelAngle);
            _rudder.TillerInput = _wheelAngle / _maxWheelAngleDeg;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void BeginGrab(bool isRight, Vector3 handPos)
        {
            _grabbed       = true;
            _isRightHand   = isRight;
            _prevHandAngle = HandAngle(handPos);
        }

        // Angle of the hand projected onto the wheel face (wheel's local XY plane).
        // Uses the wheel's own InverseTransformPoint so the reference frame rotates
        // with the wheel — delta correctly captures only the hand's angular motion.
        private float HandAngle(Vector3 handWorld)
        {
            Vector3 local = transform.InverseTransformPoint(handWorld);
            return Mathf.Atan2(local.x, local.y) * Mathf.Rad2Deg;
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
