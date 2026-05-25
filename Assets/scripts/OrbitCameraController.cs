using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class OrbitCameraController : MonoBehaviour
{
    public Transform focusTarget;
    public Vector3 focusPoint = Vector3.zero;
    public float autoOrbitSpeed = 10f;
    public float dragSensitivity = 0.15f;
    public float minPitch = -20f;
    public float maxPitch = 75f;
    public bool ignorePointerOverUI = true;

    float distanceToFocus;
    float yaw;
    float pitch;
    bool pointerBlockedByUI;
    int activeTouchId = -1;
    Vector2 lastMousePosition;

    void Awake()
    {
        CacheOrbitState();
        ApplyCameraTransform();
    }

    void OnValidate()
    {
        if (maxPitch < minPitch)
        {
            maxPitch = minPitch;
        }
    }

    void LateUpdate()
    {
        bool pointerHeld = false;
        Vector2 dragDelta = ReadPointerDelta(ref pointerHeld);

        if (pointerHeld)
        {
            yaw += dragDelta.x * dragSensitivity;
            pitch = Mathf.Clamp(pitch + (dragDelta.y * dragSensitivity), minPitch, maxPitch);
        }
        else
        {
            yaw += autoOrbitSpeed * Time.deltaTime;
        }

        ApplyCameraTransform();
    }

    void CacheOrbitState()
    {
        Vector3 offset = transform.position - GetFocusPosition();
        distanceToFocus = offset.magnitude;

        if (distanceToFocus <= 0.001f)
        {
            distanceToFocus = 10f;
            offset = new Vector3(0f, 0f, -distanceToFocus);
        }

        yaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
        pitch = Mathf.Asin(offset.y / distanceToFocus) * Mathf.Rad2Deg;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    Vector3 GetFocusPosition()
    {
        return focusTarget != null ? focusTarget.position : focusPoint;
    }

    Vector2 ReadPointerDelta(ref bool pointerHeld)
    {
        if (Input.touchCount > 0)
        {
            return ReadTouchDelta(ref pointerHeld);
        }

        return ReadMouseDelta(ref pointerHeld);
    }

    Vector2 ReadTouchDelta(ref bool pointerHeld)
    {
        Touch touch = GetActiveTouch();

        if (touch.phase == TouchPhase.Began)
        {
            activeTouchId = touch.fingerId;
            pointerBlockedByUI = ignorePointerOverUI && IsScreenPositionOverUI(touch.position, touch.fingerId);
        }

        if (touch.fingerId != activeTouchId)
        {
            pointerHeld = false;
            return Vector2.zero;
        }

        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            pointerHeld = false;
            activeTouchId = -1;
            pointerBlockedByUI = false;
            return Vector2.zero;
        }

        pointerHeld = !pointerBlockedByUI;

        if (!pointerHeld)
        {
            return Vector2.zero;
        }

        return touch.phase == TouchPhase.Moved ? touch.deltaPosition : Vector2.zero;
    }

    Touch GetActiveTouch()
    {
        if (activeTouchId >= 0)
        {
            for (int index = 0; index < Input.touchCount; index++)
            {
                Touch existingTouch = Input.GetTouch(index);

                if (existingTouch.fingerId == activeTouchId)
                {
                    return existingTouch;
                }
            }
        }

        return Input.GetTouch(0);
    }

    Vector2 ReadMouseDelta(ref bool pointerHeld)
    {
        if (Input.GetMouseButtonDown(0))
        {
            pointerBlockedByUI = ignorePointerOverUI && IsScreenPositionOverUI(Input.mousePosition);
            lastMousePosition = Input.mousePosition;
        }

        if (!Input.GetMouseButton(0))
        {
            pointerHeld = false;
            pointerBlockedByUI = false;
            return Vector2.zero;
        }

        if (ignorePointerOverUI && IsScreenPositionOverUI(Input.mousePosition))
        {
            pointerBlockedByUI = true;
        }

        pointerHeld = !pointerBlockedByUI;
        Vector2 currentMousePosition = Input.mousePosition;
        Vector2 delta = currentMousePosition - lastMousePosition;
        lastMousePosition = currentMousePosition;

        if (!pointerHeld)
        {
            return Vector2.zero;
        }

        return delta;
    }

    bool IsScreenPositionOverUI(Vector2 screenPosition, int pointerId = -1)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition,
            pointerId = pointerId
        };

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);
        return raycastResults.Count > 0;
    }

    void ApplyCameraTransform()
    {
        Vector3 focusPosition = GetFocusPosition();
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 orbitOffset = orbitRotation * new Vector3(0f, 0f, -distanceToFocus);

        transform.position = focusPosition + orbitOffset;
        transform.rotation = Quaternion.LookRotation(focusPosition - transform.position, Vector3.up);
    }
}