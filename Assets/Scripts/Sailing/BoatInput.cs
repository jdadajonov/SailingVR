using UnityEngine;

namespace SailingVR.Sailing
{
    /// <summary>
    /// Keyboard input for motor-boat testing.
    ///
    ///   W / S  — газ вперёд / назад (задний ход)
    ///   A / D  — тиллер влево / вправо
    ///
    /// Не перезаписывает тиллер когда клавиши не нажаты,
    /// чтобы не конфликтовать с VRSailingController.
    /// </summary>
    [RequireComponent(typeof(Rudder))]
    public class BoatInput : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MotorController _motor;
        [SerializeField] private Rudder          _rudder;
        [SerializeField] private Hull            _hull;
        [SerializeField] private Stability       _stability;

        [Header("Tiller")]
        [SerializeField] [Range(0.5f, 3f)]  private float _tillerSpeed       = 1.5f;
        [SerializeField] [Range(0.5f, 5f)]  private float _tillerReturnSpeed  = 2f;

        [Header("Throttle")]
        [SerializeField] [Range(0.2f, 2f)]  private float _throttleSpeed = 0.8f;

        private float _tillerValue;

        private void Reset()
        {
            _motor     = GetComponent<MotorController>();
            _rudder    = GetComponent<Rudder>();
            _hull      = GetComponent<Hull>();
            _stability = GetComponent<Stability>();
        }

        private void Update()
        {
            // ── Tiller ────────────────────────────────────────────────────────
            float tillerAxis = 0f;
            if (Input.GetKey(KeyCode.A)) tillerAxis -= 1f;
            if (Input.GetKey(KeyCode.D)) tillerAxis += 1f;

            bool keyboardTillerActive = Mathf.Abs(tillerAxis) > 0.01f || Mathf.Abs(_tillerValue) > 0.01f;
            if (keyboardTillerActive)
            {
                _tillerValue = Mathf.Abs(tillerAxis) > 0.01f
                    ? Mathf.MoveTowards(_tillerValue, tillerAxis, _tillerSpeed * Time.deltaTime)
                    : Mathf.MoveTowards(_tillerValue, 0f, _tillerReturnSpeed * Time.deltaTime);
                _rudder.TillerInput = _tillerValue;
            }
            else
            {
                _tillerValue = _rudder.TillerInput; // sync to VR value
            }

            // ── Throttle ──────────────────────────────────────────────────────
            if (_motor == null) return;

            float throttleDelta = 0f;
            if (Input.GetKey(KeyCode.W)) throttleDelta += _throttleSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.S)) throttleDelta -= _throttleSpeed * Time.deltaTime;
            if (Mathf.Abs(throttleDelta) > 0.0001f)
                _motor.ThrottleTarget += throttleDelta;
        }

        private void OnGUI()
        {
            if (_motor == null || _rudder == null) return;

            float x = 20f, y = 20f, h = 22f;
            int   row = 0;
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y + h * row++, 340f, h), $"Газ:    {_motor.ThrottleCurrent:+0.00;-0.00;0.00}  (W/S)");
            GUI.Label(new Rect(x, y + h * row++, 340f, h), $"Руль:   {_rudder.TillerInput:+0.00;-0.00;0.00}  (A/D)");
            if (_hull != null)
                GUI.Label(new Rect(x, y + h * row++, 340f, h), $"Скорость: {_hull.SpeedKnots:F1} уз");
            if (_stability != null)
            {
                GUI.color = _stability.IsCapsized ? Color.red : Color.white;
                GUI.Label(new Rect(x, y + h * row++, 340f, h),
                    $"Крен:   {_stability.HeelAngleDeg:+0.0;-0.0;0.0}°  {(_stability.IsCapsized ? "ПЕРЕВЁРНУТА" : "")}");
                GUI.color = Color.white;
            }
        }
    }
}
