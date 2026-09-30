using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Defaults to a Camera on this object, then Camera.main.")]
    [SerializeField] private Camera cam;
    
    [Tooltip("The grid controls object for this player")]
    [SerializeField] private GridControls gridControls;

    [Tooltip("While a building is selected, placement takes over the mouse and drag selection pauses.")]
    [SerializeField] private BuildingPlacer buildingPlacer;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 10f;
    [Tooltip("Scale move speed with zoom so panning feels the same zoomed in or out. moveSpeed applies at the starting zoom.")]
    [SerializeField] private bool scaleSpeedWithZoom = true;

    [Header("Zoom")]
    [SerializeField, Min(0.01f)] private float zoomStep = 1f;
    [SerializeField, Min(0.01f)] private float minZoom = 2f;
    [SerializeField, Min(0.01f)] private float maxZoom = 30f;
    [Tooltip("How quickly zoom eases toward the target. 0 = instant.")]
    [SerializeField, Min(0f)] private float zoomSmoothing = 12f;
    
    private float _targetZoom;
    private float _startZoom;
    
    private Vector3? _dragStartLocation;
    private Vector3? _dragEndLocation; 
    private bool _wasPlacing;

    private void Awake()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        _targetZoom = Mathf.Clamp(GetZoom(), minZoom, maxZoom);
        _startZoom = _targetZoom;
    }

    private void Update()
    {
        HandleMovement();
        HandleZoom();
        HandleGridControls();
    }

    private void HandleGridControls()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || gridControls == null) return;

        if (buildingPlacer != null && buildingPlacer.IsPlacing)
        {
            _dragStartLocation = null;
            _dragEndLocation = null;
            _wasPlacing = true;
            return;
        }

        if (_wasPlacing)
        { _wasPlacing = false;
            gridControls.ResetCache();
        }

        Vector3? mouseWorldPosition = null;
        Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
        Plane ground = new Plane(Vector3.up, Vector3.zero);
        if (ground.Raycast(ray, out float distance))
        {
            mouseWorldPosition = ray.GetPoint(distance);
        }

        if (_dragStartLocation == null || _dragEndLocation == null)
        {
            if (mouse.leftButton.wasPressedThisFrame && !IsPointerOverUI())
            {
                _dragStartLocation = mouseWorldPosition;
            }
            if (mouse.leftButton.wasReleasedThisFrame)
            {
                _dragEndLocation = mouseWorldPosition;
            }
        }
        if (mouse.rightButton.wasPressedThisFrame)
        {
            _dragStartLocation = null;
            _dragEndLocation = null;
        }
        
        gridControls.UpdateHoveredCells(_dragStartLocation, _dragEndLocation, mouseWorldPosition);
    }

    private static bool IsPointerOverUI() =>
        EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    
    private void HandleMovement()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        Vector2 input = Vector2.zero;
        if (keyboard.wKey.isPressed) input.y += 1f;
        if (keyboard.sKey.isPressed) input.y -= 1f;
        if (keyboard.dKey.isPressed) input.x += 1f;
        if (keyboard.aKey.isPressed) input.x -= 1f;
        if (input == Vector2.zero) return;

        input.Normalize();
        Transform view = cam != null ? cam.transform : transform;
        Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f) forward = view.up; // looking straight down
        forward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);

        float speed = moveSpeed;
        if (scaleSpeedWithZoom) speed *= GetZoom() / _startZoom;

        transform.position += (forward * input.y + right * input.x) * (speed * Time.deltaTime);
    }

    private void HandleZoom()
    {
        if (cam == null) return;

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll != 0f)
                _targetZoom = Mathf.Clamp(_targetZoom - Mathf.Sign(scroll) * zoomStep, minZoom, maxZoom);
        }

        float current = GetZoom();
        float next = zoomSmoothing <= 0f
            ? _targetZoom
            : Mathf.Lerp(current, _targetZoom, 1f - Mathf.Exp(-zoomSmoothing * Time.deltaTime));

        SetZoom(next);
    }

    private float GetZoom()
    {
        if (cam == null) return minZoom;
        if (cam.orthographic) return cam.orthographicSize;
        
        Transform view = cam.transform;
        float down = -view.forward.y;
        return down > 0.0001f ? view.position.y / down : view.position.y;
    }

    private void SetZoom(float zoom)
    {
        if (cam.orthographic)
        {
            cam.orthographicSize = zoom;
            return;
        }
        
        float delta = GetZoom() - zoom;
        cam.transform.position += cam.transform.forward * delta;
    }
}
