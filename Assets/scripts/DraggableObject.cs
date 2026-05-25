using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableObject : MonoBehaviour
{
    static DraggableObject activeObject;

    [SerializeField] Transform snapTarget;
    [SerializeField] Transform scatterOverride;
    [SerializeField] float snapThreshold = 2f;
    [SerializeField] float snapDuration = 0.2f;
    [SerializeField] bool startEnabled;
    [SerializeField] AudioSource interactionAudioSource;
    [SerializeField] AudioClip dragStartClip;
    [SerializeField] AudioClip snapSuccessClip;
    [SerializeField] AudioClip snapFailClip;

    Camera mainCamera;
    Collider objectCollider;
    Coroutine snapRoutine;
    Vector3 defaultWorldPosition;
    Vector3 defaultLocalPosition;
    Quaternion defaultWorldRotation;
    Quaternion defaultLocalRotation;
    Vector3 scatterLocalPosition;
    Vector3 snapTargetLocalPosition;
    Quaternion snapTargetLocalRotation;
    bool hasSnapTargetLocalPose;
    Transform originalParent;
    int activeTouchId = -1;
    bool isDragging;
    bool isDragEnabled;
    bool isSnapped;
    Vector2 dragLocalOffset;

    public Vector3 DefaultWorldPosition => defaultWorldPosition;
    public Vector3 DefaultLocalPosition => defaultLocalPosition;
    public Quaternion DefaultWorldRotation => defaultWorldRotation;
    public bool IsSnapped => isSnapped;
    public bool HasScatterOverride => scatterOverride != null;
    public Transform ScatterOverride => scatterOverride;
    public Transform SnapTarget => snapTarget;

    public void SetSnapTarget(Transform newSnapTarget)
    {
        snapTarget = newSnapTarget;
        CacheSnapPoseOnTarget();
    }

    public void SetSnapThreshold(float newThreshold)
    {
        snapThreshold = Mathf.Max(0.01f, newThreshold);
    }

    void Awake()
    {
        mainCamera = Camera.main;
        objectCollider = GetComponent<Collider>();
        originalParent = transform.parent;
        defaultWorldPosition = transform.position;
        defaultLocalPosition = transform.localPosition;
        defaultWorldRotation = transform.rotation;
        defaultLocalRotation = transform.localRotation;
        scatterLocalPosition = defaultLocalPosition;
        CacheSnapPoseOnTarget();
        SetDragEnabled(startEnabled);
    }

    void OnEnable()
    {
        if (objectCollider == null)
        {
            objectCollider = GetComponent<Collider>();
        }
    }

    void Update()
    {
        if (!isDragEnabled || isSnapped)
        {
            return;
        }

        if (Input.touchCount > 0)
        {
            HandleTouchInput();
            return;
        }

        HandleMouseInput();
    }

    public void EnableForDrag()
    {
        isSnapped = false;
        SetDragEnabled(true);
    }

    public void DisableAndLock()
    {
        EndDrag();
        SetDragEnabled(false);
    }

    public void SetScatterPosition(Vector3 newScatterLocalPosition)
    {
        scatterLocalPosition = new Vector3(defaultLocalPosition.x, newScatterLocalPosition.y, newScatterLocalPosition.z);
        transform.localPosition = scatterLocalPosition;
        transform.localRotation = defaultLocalRotation;
    }

    public void ApplyScatterOverride()
    {
        if (scatterOverride == null)
        {
            return;
        }

        SetScatterPosition(scatterOverride.localPosition);
    }

    public void ResetToDefault()
    {
        if (snapRoutine != null)
        {
            StopCoroutine(snapRoutine);
            snapRoutine = null;
        }

        if (activeObject == this)
        {
            activeObject = null;
        }

        isDragging = false;
        isSnapped = false;
        activeTouchId = -1;
        transform.SetParent(originalParent, true);
        transform.localPosition = defaultLocalPosition;
        transform.localRotation = defaultLocalRotation;
        scatterLocalPosition = defaultLocalPosition;
        SetDragEnabled(startEnabled);
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0) && CanStartMouseDrag())
        {
            BeginDrag(-1, Input.mousePosition);
        }

        if (activeObject != this || !isDragging)
        {
            return;
        }

        if (Input.GetMouseButton(0))
        {
            UpdateDrag(Input.mousePosition);
            return;
        }

        ReleaseDrag();
    }

    void HandleTouchInput()
    {
        if (activeObject == this && isDragging)
        {
            for (int index = 0; index < Input.touchCount; index++)
            {
                Touch trackedTouch = Input.GetTouch(index);

                if (trackedTouch.fingerId != activeTouchId)
                {
                    continue;
                }

                if (trackedTouch.phase == TouchPhase.Ended || trackedTouch.phase == TouchPhase.Canceled)
                {
                    ReleaseDrag();
                    return;
                }

                UpdateDrag(trackedTouch.position);
                return;
            }

            ReleaseDrag();
            return;
        }

        if (activeObject != null)
        {
            return;
        }

        for (int index = 0; index < Input.touchCount; index++)
        {
            Touch touch = Input.GetTouch(index);

            if (touch.phase != TouchPhase.Began)
            {
                continue;
            }

            if (IsScreenPositionOverUI(touch.position, touch.fingerId))
            {
                continue;
            }

            if (!IsPointerOverThisObject(touch.position))
            {
                continue;
            }

            BeginDrag(touch.fingerId, touch.position);
            return;
        }
    }

    bool CanStartMouseDrag()
    {
        if (activeObject != null)
        {
            return false;
        }

        if (IsScreenPositionOverUI(Input.mousePosition))
        {
            return false;
        }

        return IsPointerOverThisObject(Input.mousePosition);
    }

    void BeginDrag(int pointerId, Vector2 screenPosition)
    {
        activeObject = this;
        isDragging = true;
        activeTouchId = pointerId;
        ComputeDragOffset(screenPosition);
        PlayInteractionClip(dragStartClip);
    }

    void ComputeDragOffset(Vector2 screenPosition)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            dragLocalOffset = Vector2.zero;
            return;
        }

        Plane dragPlane = BuildDragPlane();
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        float distance;

        if (!dragPlane.Raycast(ray, out distance))
        {
            dragLocalOffset = Vector2.zero;
            return;
        }

        Vector3 hitPoint = ray.GetPoint(distance);

        if (originalParent != null)
        {
            Vector3 hitLocalPoint = originalParent.InverseTransformPoint(hitPoint);
            dragLocalOffset = new Vector2(
                transform.localPosition.y - hitLocalPoint.y,
                transform.localPosition.z - hitLocalPoint.z);
        }
        else
        {
            dragLocalOffset = new Vector2(
                transform.position.y - hitPoint.y,
                transform.position.z - hitPoint.z);
        }
    }

    void ReleaseDrag()
    {
        if (snapTarget == null)
        {
            PlayInteractionClip(snapFailClip);
            ReturnToScatter();
            return;
        }

        Vector3 targetPosition;
        Quaternion targetRotation;
        GetSnapWorldPose(out targetPosition, out targetRotation);

        if (Vector3.Distance(transform.position, targetPosition) <= snapThreshold)
        {
            PlayInteractionClip(snapSuccessClip);
            Snap(targetPosition, targetRotation);
            return;
        }

        PlayInteractionClip(snapFailClip);
        ReturnToScatter();
    }

    void ReturnToScatter()
    {
        transform.localPosition = scatterLocalPosition;
        transform.localRotation = defaultLocalRotation;
        EndDrag();
    }

    void Snap(Vector3 targetPosition, Quaternion targetRotation)
    {
        if (snapRoutine != null)
        {
            StopCoroutine(snapRoutine);
        }

        snapRoutine = StartCoroutine(SnapRoutine(targetPosition, targetRotation));
    }

    IEnumerator SnapRoutine(Vector3 targetPosition, Quaternion targetRotation)
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        float elapsed = 0f;

        EndDrag();
        SetDragEnabled(false);

        while (elapsed < snapDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / snapDuration);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        transform.position = targetPosition;
        transform.rotation = targetRotation;
        transform.SetParent(snapTarget, true);
        isSnapped = true;
        snapRoutine = null;

        if (SimulationController.Instance != null)
        {
            SimulationController.Instance.OnObjectSnapped(this);
        }
    }

    void GetSnapWorldPose(out Vector3 targetPosition, out Quaternion targetRotation)
    {
        if (snapTarget != null && hasSnapTargetLocalPose)
        {
            targetPosition = snapTarget.TransformPoint(snapTargetLocalPosition);
            targetRotation = snapTarget.rotation * snapTargetLocalRotation;
            return;
        }

        targetPosition = defaultWorldPosition;
        targetRotation = defaultWorldRotation;
    }

    void CacheSnapPoseOnTarget()
    {
        if (snapTarget == null)
        {
            hasSnapTargetLocalPose = false;
            return;
        }

        snapTargetLocalPosition = snapTarget.InverseTransformPoint(defaultWorldPosition);
        snapTargetLocalRotation = Quaternion.Inverse(snapTarget.rotation) * defaultWorldRotation;
        hasSnapTargetLocalPose = true;
    }

    void EndDrag()
    {
        isDragging = false;
        activeTouchId = -1;

        if (activeObject == this)
        {
            activeObject = null;
        }
    }

    void PlayInteractionClip(AudioClip clip)
    {
        if (interactionAudioSource == null || clip == null)
        {
            return;
        }

        interactionAudioSource.PlayOneShot(clip);
    }

    void UpdateDrag(Vector2 screenPosition)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return;
        }

        Plane localYZPlane = BuildDragPlane();
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        float distance;

        if (!localYZPlane.Raycast(ray, out distance))
        {
            return;
        }

        Vector3 hitPoint = ray.GetPoint(distance);
        if (originalParent != null)
        {
            Vector3 hitLocalPoint = originalParent.InverseTransformPoint(hitPoint);
            transform.localPosition = new Vector3(
                defaultLocalPosition.x,
                hitLocalPoint.y + dragLocalOffset.x,
                hitLocalPoint.z + dragLocalOffset.y);
            transform.localRotation = defaultLocalRotation;
            return;
        }

        transform.position = new Vector3(
            defaultWorldPosition.x,
            hitPoint.y + dragLocalOffset.x,
            hitPoint.z + dragLocalOffset.y);
        transform.rotation = defaultWorldRotation;
    }

    Plane BuildDragPlane()
    {
        if (originalParent != null)
        {
            Vector3 planeNormal = originalParent.TransformDirection(Vector3.right);
            Vector3 planePoint = originalParent.TransformPoint(new Vector3(defaultLocalPosition.x, 0f, 0f));
            return new Plane(planeNormal, planePoint);
        }

        return new Plane(Vector3.right, new Vector3(defaultWorldPosition.x, 0f, 0f));
    }

    bool IsPointerOverThisObject(Vector2 screenPosition)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null || objectCollider == null)
        {
            return false;
        }

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 10000f);

        for (int index = 0; index < hits.Length; index++)
        {
            Transform hitTransform = hits[index].transform;

            if (hitTransform == transform || hitTransform.IsChildOf(transform))
            {
                return true;
            }
        }

        return false;
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

    void SetDragEnabled(bool value)
    {
        isDragEnabled = value;

        if (objectCollider != null)
        {
            objectCollider.enabled = value;
        }
    }
}