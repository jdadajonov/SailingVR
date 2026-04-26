using UnityEngine;

namespace SailingVR.Utils
{
    /// <summary>
    /// Simple third-person camera for desktop testing.
    /// Remove (or disable) when switching to VR — the XR camera rig replaces this.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;

        [Tooltip("Offset from target in target's local space.")]
        [SerializeField] private Vector3 _offset = new Vector3(0f, 4f, -9f);

        [Tooltip("How quickly the camera catches up. Lower = more lag.")]
        [SerializeField] [Range(1f, 20f)] private float _smoothSpeed = 6f;

        [Tooltip("If true, camera yaw follows the boat heading. " +
                 "If false, camera stays world-aligned (easier to see wind direction).")]
        [SerializeField] private bool _followHeading = true;

        private void LateUpdate()
        {
            if (_target == null) return;

            Quaternion targetRot = _followHeading
                ? Quaternion.Euler(0f, _target.eulerAngles.y, 0f)
                : Quaternion.identity;

            Vector3 desiredPos = _target.position + targetRot * _offset;
            transform.position = Vector3.Lerp(transform.position, desiredPos, _smoothSpeed * Time.deltaTime);
            transform.LookAt(_target.position + Vector3.up * 1.5f);
        }
    }
}
