using UnityEngine;

namespace RPG.Core
{
    /// <summary>
    /// Isometric-style camera controller.
    /// Orbits around a target point with mouse scroll zoom.
    /// Matches Final Fantasy Tactics' classic fixed isometric angle.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Target")]
        public Transform Target;
        public Vector3 TargetOffset = new Vector3(0f, 0.5f, 0f);

        [Header("Isometric Angle")]
        [Range(20f, 75f)] public float Elevation = 50f;
        [Range(0f, 360f)] public float Azimuth   = 45f;

        [Header("Zoom")]
        public float Distance    = 14f;
        public float MinDistance = 7f;
        public float MaxDistance = 22f;
        public float ZoomSpeed   = 3f;
        public float ZoomSmooth  = 8f;

        [Header("Pan")]
        public float PanSpeed    = 0.06f;

        private float _targetDistance;
        private Vector3 _targetPosition;

        private void Start()
        {
            _targetDistance = Distance;

            if (Target == null)
            {
                // Default to grid center
                var grid = RPG.Grid.BattleGrid.Instance;
                if (grid != null)
                {
                    var go = new GameObject("CameraTarget");
                    go.transform.position = grid.GetGridCenter();
                    Target = go.transform;
                }
            }

            if (Target != null)
                _targetPosition = Target.position;
        }

        private void LateUpdate()
        {
            // Scroll zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            _targetDistance = Mathf.Clamp(_targetDistance - scroll * ZoomSpeed * _targetDistance,
                MinDistance, MaxDistance);
            Distance = Mathf.Lerp(Distance, _targetDistance, Time.deltaTime * ZoomSmooth);

            if (Target != null)
                _targetPosition = Target.position + TargetOffset;

            // Compute position from spherical coordinates
            Quaternion rot = Quaternion.Euler(Elevation, Azimuth, 0f);
            Vector3 pos    = _targetPosition - rot * Vector3.forward * Distance;

            transform.position = pos;
            transform.LookAt(_targetPosition);
        }

        public void SetTarget(Transform t)     => Target = t;
        public void SetAzimuth(float degrees)  => Azimuth = degrees;
        public void SetElevation(float degrees) => Elevation = degrees;
    }
}
