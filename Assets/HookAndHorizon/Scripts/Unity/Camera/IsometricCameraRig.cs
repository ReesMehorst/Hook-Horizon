using UnityEngine;

namespace HookHorizon.Unity.CameraRig
{
    /// <summary>
    /// Sets up and drives a Project-Zomboid-style isometric camera: an orthographic
    /// projection tilted down and rotated 45°, smoothly trailing the player.
    /// The <see cref="Yaw"/> is read by movement so WASD maps to screen directions.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class IsometricCameraRig : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Isometric angle")]
        [SerializeField, Range(10f, 60f)] private float pitch = 40f;
        [SerializeField] private float yaw = 45f;

        [Header("Framing")]
        [SerializeField, Min(1f)] private float distance = 20f;
        [SerializeField, Min(1f)] private float orthographicSize = 6.5f;
        [SerializeField, Min(0f)] private float followSmoothing = 10f;

        private Camera _camera;

        /// <summary>The camera's yaw in degrees, used to align player input with the screen.</summary>
        public float Yaw => yaw;

        public void SetTarget(Transform newTarget) => target = newTarget;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = orthographicSize;
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desired = target.position - transform.forward * distance;
            transform.position = followSmoothing <= 0f
                ? desired
                : Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-followSmoothing * Time.deltaTime));
        }
    }
}
