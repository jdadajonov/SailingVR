using UnityEngine;
using UnityEngine.UI;

namespace SailingVR.Sailing
{
    /// <summary>
    /// VR motor-boat controls via Meta hand tracking.
    ///
    /// ПРАВАЯ РУКА → Тиллер
    ///   Щипок (большой + указательный) → двигай руку влево/вправо.
    ///   Вправо = поворот вправо, влево = поворот влево.
    ///
    /// ЛЕВАЯ РУКА → Газ
    ///   Щипок → двигай руку вперёд (к носу) = газ.
    ///   Назад (к корме) = нейтраль / задний ход.
    ///
    /// Жёлтые руки = низкая уверенность трекинга.
    /// Поверни ладони немного вниз — станут белыми.
    /// </summary>
    public class VRSailingController : MonoBehaviour
    {
        [Header("Meta XR")]
        [SerializeField] private OVRCameraRig _cameraRig;

        [Header("Boat References")]
        [SerializeField] private MotorController _motor;
        [SerializeField] private Rudder          _rudder;

        [Header("Тиллер (правая рука)")]
        [Tooltip("Расстояние движения руки (м, локальный X) для полного отклонения руля.")]
        [SerializeField] private float _tillerHandRange = 0.25f;

        [Header("Газ (левая рука)")]
        [Tooltip("Расстояние движения руки (м, локальный Z) от нейтрали до полного газа.")]
        [SerializeField] private float _throttleHandRange = 0.30f;

        [Header("Pinch")]
        [SerializeField] [Range(0.1f, 0.9f)] private float _pinchThreshold = 0.2f;

        [Header("Debug")]
        [SerializeField] private bool _showDebug = true;

        // ── Private ──────────────────────────────────────────────────────────

        private OVRHand _leftHand;
        private OVRHand _rightHand;

        private bool  _tillerActive;
        private float _tillerRefX;

        private bool  _throttleActive;
        private float _throttleRefZ;
        private float _throttleRefValue;

        private Text _hudText;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Reset()
        {
            _motor  = GetComponent<MotorController>();
            _rudder = GetComponent<Rudder>();
        }

        private void Start()
        {
            if (_cameraRig == null) return;
            _leftHand  = _cameraRig.leftHandAnchor .GetComponentInChildren<OVRHand>();
            _rightHand = _cameraRig.rightHandAnchor.GetComponentInChildren<OVRHand>();

            if (_leftHand == null || _rightHand == null)
                Debug.LogWarning("[VRSailingController] OVRHand не найден. " +
                    "Добавь OVRHandPrefab под LeftHandAnchor и RightHandAnchor.");

            if (_showDebug) BuildHud();
        }

        private void Update()
        {
            if (_cameraRig == null) return;

            Vector3 rPos   = _cameraRig.rightHandAnchor.position;
            Vector3 lPos   = _cameraRig.leftHandAnchor .position;
            float   rPinch = PinchStrength(isRight: true);
            float   lPinch = PinchStrength(isRight: false);

            UpdateTiller  (rPos, rPinch >= _pinchThreshold);
            UpdateThrottle(lPos, lPinch >= _pinchThreshold);

            if (_showDebug) UpdateHud(rPinch, lPinch);
        }

        // ── Tiller ────────────────────────────────────────────────────────────

        private void UpdateTiller(Vector3 handWorld, bool pinching)
        {
            if (_rudder == null) return;

            if (pinching)
            {
                float localX = transform.InverseTransformPoint(handWorld).x;
                if (!_tillerActive) { _tillerRefX = localX; _tillerActive = true; }
                _rudder.TillerInput = Mathf.Clamp((localX - _tillerRefX) / _tillerHandRange, -1f, 1f);
            }
            else
            {
                _tillerActive = false;
                _rudder.TillerInput = Mathf.MoveTowards(_rudder.TillerInput, 0f, Time.deltaTime * 2f);
            }
        }

        // ── Throttle ──────────────────────────────────────────────────────────

        private void UpdateThrottle(Vector3 handWorld, bool pinching)
        {
            if (_motor == null) return;

            if (pinching)
            {
                float localZ = transform.InverseTransformPoint(handWorld).z;
                if (!_throttleActive)
                {
                    _throttleRefZ     = localZ;
                    _throttleRefValue = _motor.ThrottleTarget;
                    _throttleActive   = true;
                }
                // Вперёд (+Z) = газ вперёд (+1); назад = задний ход (-1)
                float delta = (localZ - _throttleRefZ) / _throttleHandRange;
                _motor.ThrottleTarget = Mathf.Clamp(_throttleRefValue + delta, -1f, 1f);
            }
            else
            {
                _throttleActive = false;
            }
        }

        // ── Pinch ─────────────────────────────────────────────────────────────

        private float PinchStrength(bool isRight)
        {
            OVRHand hand = isRight ? _rightHand : _leftHand;
            if (hand != null && hand.IsTracked)
            {
                float s = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
                if (s > 0.01f) return s;
            }
            if (isRight)
                return Mathf.Max(OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger),
                                 OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger));
            else
                return Mathf.Max(OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger),
                                 OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger));
        }

        // ── HUD ──────────────────────────────────────────────────────────────

        private void BuildHud()
        {
            var anchor   = _cameraRig.centerEyeAnchor;
            var canvasGO = new GameObject("VR_DebugHUD");
            canvasGO.transform.SetParent(anchor, false);
            canvasGO.transform.localPosition = new Vector3(0f, 0.1f, 1.8f);
            canvasGO.transform.localRotation = Quaternion.identity;

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(900f, 280f);
            canvasGO.transform.localScale = Vector3.one * 0.001f;

            var bg = new GameObject("BG").AddComponent<Image>();
            bg.transform.SetParent(canvasGO.transform, false);
            bg.color = new Color(0f, 0f, 0f, 0.72f);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(canvasGO.transform, false);
            _hudText = textGO.AddComponent<Text>();
            _hudText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                         ?? Resources.GetBuiltinResource<Font>("Arial.ttf")
                         ?? Font.CreateDynamicFontFromOSFont("Arial", 26);
            _hudText.fontSize = 26;
            _hudText.color    = Color.white;
            _hudText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _hudText.verticalOverflow   = VerticalWrapMode.Overflow;
            var tRt = textGO.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
            tRt.offsetMin = new Vector2(14f, 10f); tRt.offsetMax = new Vector2(-14f, -10f);
        }

        private void UpdateHud(float rPinch, float lPinch)
        {
            if (_hudText == null) return;

            bool rTracked = _rightHand != null && _rightHand.IsTracked;
            bool lTracked = _leftHand  != null && _leftHand .IsTracked;
            string rConf  = _rightHand != null ? (_rightHand.IsDataHighConfidence ? "белая" : "желтая") : "нет OVRHand!";
            string lConf  = _leftHand  != null ? (_leftHand .IsDataHighConfidence ? "белая" : "желтая") : "нет OVRHand!";

            float throttle = _motor  != null ? _motor.ThrottleCurrent  : 0f;
            float rudder   = _rudder != null ? _rudder.TillerInput      : 0f;

            string throttleBar = ThrottleBar(throttle);

            _hudText.text =
                $"ПРАВАЯ (тиллер): {(rTracked ? "видна" : "НЕ ВИДНА")}  {rConf}  " +
                $"щипок={rPinch:F2}  {(_tillerActive   ? "<< РУЛЬ АКТИВЕН >>" : "")}\n" +
                $"ЛЕВАЯ  (газ):    {(lTracked ? "видна" : "НЕ ВИДНА")}  {lConf}  " +
                $"щипок={lPinch:F2}  {(_throttleActive ? "<< ГАЗ АКТИВЕН >>"  : "")}\n" +
                $"\n" +
                $"Газ: {throttleBar} {throttle:+0.00;-0.00;0.00}   Руль: {rudder:+0.00;-0.00;0.00}\n" +
                $"\n" +
                $"Щипок правой -> двигай влево/вправо = руль\n" +
                $"Щипок левой  -> двигай вперед/назад = газ (+/-)";
        }

        private static string ThrottleBar(float t)
        {
            int filled = Mathf.RoundToInt(Mathf.Abs(t) * 10f);
            string bar = new string('|', filled) + new string('.', 10 - filled);
            return t >= 0 ? $"[{bar}]" : $"[REV {bar}]";
        }
    }
}
