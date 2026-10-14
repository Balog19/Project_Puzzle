using System;
using UnityEngine;

namespace Puzzle.Player
{
    /// <summary>Tuning for <see cref="CameraController"/>, edited on the PlayerController.</summary>
    [Serializable]
    public class CameraSettings
    {
        [Tooltip("Pan speed in world units per second at the starting zoom.")]
        [Min(0f)] public float moveSpeed = 10f;
        [Tooltip("Pan slower when zoomed in and faster when zoomed out, so it feels the same at any zoom.")]
        public bool scaleSpeedWithZoom = true;
        [Tooltip("Zoom change per scroll notch.")]
        [Min(0.01f)] public float zoomStep = 1f;
        [Tooltip("Closest zoom (orthographic size, or distance to the ground for perspective).")]
        [Min(0.01f)] public float minZoom = 2f;
        [Tooltip("Farthest zoom.")]
        [Min(0.01f)] public float maxZoom = 30f;
        [Tooltip("How quickly zoom eases toward its target. 0 = instant.")]
        [Min(0f)] public float zoomSmoothing = 12f;
    }

    /// <summary>
    /// Pans the camera across the ground and zooms it (like a camera pawn's movement component).
    /// Owned by PlayerController, which feeds it input every frame.
    /// Orthographic cameras zoom by orthographicSize; perspective cameras dolly along their view.
    /// </summary>
    public class CameraController
    {
        private readonly Camera _camera;
        private readonly CameraSettings _settings;
        private readonly float _startZoom;
        private float _targetZoom;

        public CameraController(Camera camera, CameraSettings settings)
        {
            _camera = camera;
            _settings = settings;
            _targetZoom = Mathf.Clamp(GetZoom(), settings.minZoom, settings.maxZoom);
            _startZoom = _targetZoom;
        }

        /// <param name="move">Pan input, -1..1 on each axis (x = right, y = forward).</param>
        /// <param name="scroll">Scroll input; only its sign is used, since raw values differ by platform.</param>
        /// <param name="deltaTime">Frame time (unscaled, so the camera still moves while the game is paused).</param>
        public void Update(Vector2 move, float scroll, float deltaTime)
        {
            Pan(move, deltaTime);
            Zoom(scroll, deltaTime);
        }

        // ---------- Pan ----------

        private void Pan(Vector2 move, float deltaTime)
        {
            if (move == Vector2.zero) return;
            move = Vector2.ClampMagnitude(move, 1f);

            // Move relative to where the camera faces, flattened onto the ground.
            Transform view = _camera.transform;
            Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f) forward = view.up; // looking straight down
            forward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            float speed = _settings.moveSpeed;
            if (_settings.scaleSpeedWithZoom) speed *= GetZoom() / _startZoom;

            view.position += (forward * move.y + right * move.x) * (speed * deltaTime);
        }

        // ---------- Zoom ----------

        private void Zoom(float scroll, float deltaTime)
        {
            if (scroll != 0f)
            {
                _targetZoom = Mathf.Clamp(_targetZoom - Mathf.Sign(scroll) * _settings.zoomStep,
                    _settings.minZoom, _settings.maxZoom);
            }

            float next = _settings.zoomSmoothing <= 0f
                ? _targetZoom
                : Mathf.Lerp(GetZoom(), _targetZoom, 1f - Mathf.Exp(-_settings.zoomSmoothing * deltaTime));

            SetZoom(next);
        }

        private float GetZoom()
        {
            if (_camera.orthographic) return _camera.orthographicSize;

            // Distance from the camera to the ground plane along its view direction.
            Transform view = _camera.transform;
            float down = -view.forward.y;
            return down > 0.0001f ? view.position.y / down : view.position.y;
        }

        private void SetZoom(float zoom)
        {
            if (_camera.orthographic)
            {
                _camera.orthographicSize = zoom;
                return;
            }

            // Dolly along the view direction so the point being looked at stays put.
            _camera.transform.position += _camera.transform.forward * (GetZoom() - zoom);
        }
    }
}
