using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class SimulationController : MonoBehaviour
{
    public enum SimulationState
    {
        Idle,
        Intro,
        InStage,
        StageCompleted
    }

    [System.Serializable]
    public class SequentialStageDefinition
    {
        [Header("Stage Camera")]
        [SerializeField] Transform cameraTarget;

        [Header("Stage Objects")]
        [SerializeField] DraggableObject[] objects;

        [Header("Stage Instructions")]
        [TextArea] [SerializeField] string[] instructions;
        public Transform CameraTarget => cameraTarget;

        public int StepCount
        {
            get
            {
                if (objects == null || instructions == null)
                {
                    return 0;
                }

                return Mathf.Min(objects.Length, instructions.Length);
            }
        }

        public bool HasStep(int index)
        {
            return index >= 0 && index < StepCount;
        }

        public DraggableObject GetObject(int index)
        {
            return HasStep(index) ? objects[index] : null;
        }

        public string GetInstruction(int index)
        {
            return HasStep(index) ? instructions[index] : string.Empty;
        }

        public bool IsConfigured()
        {
            return cameraTarget != null && StepCount > 0;
        }
    }

    public static SimulationController Instance { get; private set; }

    [Header("Core References")]
    [SerializeField] Camera mainCamera;
    [SerializeField] OrbitCameraController orbitController;

    [Header("UI References")]
    [SerializeField] RectTransform judulGemasRect;
    [SerializeField] GameObject startButtonGO;
    [SerializeField] GameObject materisGO;
    [SerializeField] GameObject lanjutButtonGO;
    [SerializeField] Button runtimeBackButton;
    [SerializeField] Text signText;
    [SerializeField] Text lanjutButtonText;

    [Header("Video References")]
    [SerializeField] VideoManager videoManager;
    [SerializeField] Button stageVideoTriggerButton;

    [Header("Stage Video Paths (StreamingAssets)")]
    [SerializeField] string defaultStageVideoPath;
    [SerializeField] string stage1VideoPath;
    [SerializeField] string stage2VideoPath;
    [SerializeField] string stage3VideoPath;
    [SerializeField] string stage4VideoPath;
    [SerializeField] string stage5VideoPath;
    [SerializeField] string stage6VideoPath;

    [Header("Stage Sequences")]
    [SerializeField] SequentialStageDefinition stage1Sequence;
    [SerializeField] SequentialStageDefinition stage2Sequence;
    [SerializeField] SequentialStageDefinition stage3Sequence;
    [SerializeField] SequentialStageDefinition stage4Sequence;
    [SerializeField] SequentialStageDefinition stage5Sequence;
    [SerializeField] SequentialStageDefinition stage6Sequence;

    [Header("Motion")]
    [SerializeField] float cameraMoveDuration = 1.5f;
    [SerializeField] float titleAnimationDuration = 0.8f;
    [SerializeField] float scatterRange = 100f;

    SimulationState currentState = SimulationState.Idle;
    SequentialStageDefinition activeSequence;
    int activeSequenceStageIndex = -1;
    int completedSequenceStageIndex = -1;
    int activeSequenceStepIndex = -1;
    Coroutine titleRoutine;
    Vector2 initialTitleAnchorMin;
    Vector2 initialTitleAnchorMax;
    readonly Vector2 targetTitleAnchorMin = new Vector2(0.4f, 0.8f);
    readonly Vector2 targetTitleAnchorMax = new Vector2(0.6f, 1f);

    public SimulationState CurrentState => currentState;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (judulGemasRect != null)
        {
            initialTitleAnchorMin = judulGemasRect.anchorMin;
            initialTitleAnchorMax = judulGemasRect.anchorMax;
        }

        SetInitialUIState();
        DisableAllDraggables();
    }

    void Start()
    {
        HookStageVideoTriggerButton();
        HookRuntimeBackButton();
    }

    void OnDestroy()
    {
        if (stageVideoTriggerButton != null)
        {
            stageVideoTriggerButton.onClick.RemoveListener(PlayCurrentStageVideo);
        }

        if (runtimeBackButton != null)
        {
            runtimeBackButton.onClick.RemoveListener(OnBackRuntimePressed);
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void StartSimulation()
    {
        if (currentState != SimulationState.Idle)
        {
            return;
        }

        StartCoroutine(StartSimulationRoutine());
    }

    public void OnBackRuntimePressed()
    {
        if (videoManager != null)
        {
            videoManager.BackToThumbnails();
        }

        ResetAll();
    }

    public void PlayCurrentStageVideo()
    {
        if (videoManager == null)
        {
            return;
        }

        PlayVideoForStage(GetCurrentStageNumber());
    }

    public void PlayVideoForStage(int stageNumber)
    {
        if (videoManager == null)
        {
            return;
        }

        string videoPath = GetVideoPathForStage(stageNumber);

        if (string.IsNullOrWhiteSpace(videoPath))
        {
            Debug.LogError("Path video belum diisi untuk stage " + stageNumber + ".");
            return;
        }

        videoManager.PlayVideoUrl(videoPath);
    }

    public void OnLanjutPressed()
    {
        if (activeSequence != null)
        {
            return;
        }

        int nextStageIndex = GetNextConfiguredStageIndex(completedSequenceStageIndex);

        if (nextStageIndex >= 0)
        {
            StartCoroutine(BeginSequentialStageByIndex(nextStageIndex));
            return;
        }

        if (IsAtFinalConfiguredStage(completedSequenceStageIndex))
        {
            ResetAll();
        }
    }

    public void OnObjectSnapped(DraggableObject snappedObject)
    {
        if (snappedObject == null)
        {
            return;
        }

        HandleSequentialStageSnap(snappedObject);
    }

    public void ResetAll()
    {
        activeSequence = null;
        activeSequenceStageIndex = -1;
        completedSequenceStageIndex = -1;
        activeSequenceStepIndex = -1;

        if (titleRoutine != null)
        {
            StopCoroutine(titleRoutine);
            titleRoutine = null;
        }

        DisableAllDraggables();

        DraggableObject[] allObjects = GetAllObjects();

        for (int index = 0; index < allObjects.Length; index++)
        {
            if (allObjects[index] != null)
            {
                allObjects[index].ResetToDefault();
            }
        }

        if (judulGemasRect != null)
        {
            titleRoutine = StartCoroutine(AnimateAnchors(
                judulGemasRect,
                initialTitleAnchorMin,
                initialTitleAnchorMax,
                titleAnimationDuration));
        }

        if (materisGO != null)
        {
            materisGO.SetActive(false);
        }

        if (startButtonGO != null)
        {
            startButtonGO.SetActive(true);
        }

        if (lanjutButtonGO != null)
        {
            lanjutButtonGO.SetActive(false);
        }

        if (lanjutButtonText != null)
        {
            lanjutButtonText.text = "Lanjut";
        }

        if (signText != null)
        {
            signText.text = string.Empty;
        }

        if (orbitController != null)
        {
            orbitController.enabled = true;
        }

        currentState = SimulationState.Idle;
    }

    IEnumerator StartSimulationRoutine()
    {
        currentState = SimulationState.Intro;

        if (lanjutButtonGO != null)
        {
            lanjutButtonGO.SetActive(false);
        }

        if (lanjutButtonText != null)
        {
            lanjutButtonText.text = "Lanjut";
        }

        if (startButtonGO != null)
        {
            startButtonGO.SetActive(false);
        }

        if (materisGO != null)
        {
            materisGO.SetActive(true);
        }

        if (orbitController != null)
        {
            orbitController.enabled = false;
        }

        if (judulGemasRect != null)
        {
            if (titleRoutine != null)
            {
                StopCoroutine(titleRoutine);
            }

            titleRoutine = StartCoroutine(AnimateAnchors(
                judulGemasRect,
                targetTitleAnchorMin,
                targetTitleAnchorMax,
                titleAnimationDuration));
        }

        int firstStageIndex = GetNextConfiguredStageIndex(-1);
        if (firstStageIndex >= 0)
        {
            yield return BeginSequentialStageByIndex(firstStageIndex);
        }
    }

    IEnumerator BeginSequentialStageByIndex(int stageIndex)
    {
        SequentialStageDefinition[] stages = GetStageSequenceList();

        if (stageIndex < 0 || stageIndex >= stages.Length)
        {
            yield break;
        }

        SequentialStageDefinition sequence = stages[stageIndex];

        if (sequence == null || !sequence.IsConfigured())
        {
            yield break;
        }

        yield return BeginSequentialStage(sequence, stageIndex);
    }

    void PrepareObjectForStep(DraggableObject draggableObject, string instruction)
    {
        if (draggableObject == null)
        {
            return;
        }

        DisableAllDraggables();
        draggableObject.EnableForDrag();
        UpdateInstruction(instruction);
    }

    void ScatterSequenceObjects(SequentialStageDefinition sequence)
    {
        if (sequence == null)
        {
            return;
        }

        HashSet<DraggableObject> processedObjects = new HashSet<DraggableObject>();

        for (int stepIndex = 0; stepIndex < sequence.StepCount; stepIndex++)
        {
            DraggableObject stageObject = sequence.GetObject(stepIndex);

            if (stageObject == null || !processedObjects.Add(stageObject))
            {
                continue;
            }

            stageObject.ResetToDefault();

            if (stageObject.HasScatterOverride)
            {
                stageObject.ApplyScatterOverride();
            }
            else
            {
                stageObject.SetScatterPosition(GetRandomScatterPosition(stageObject));
            }

            stageObject.DisableAndLock();
        }
    }

    Vector3 GetRandomScatterPosition(DraggableObject draggableObject)
    {
        Vector3 basePosition = draggableObject.DefaultLocalPosition;
        float randomYOffset = Random.Range(-scatterRange, scatterRange);
        float randomZOffset = Random.Range(-scatterRange, scatterRange);
        return new Vector3(basePosition.x, basePosition.y + randomYOffset, basePosition.z + randomZOffset);
    }

    void DisableAllDraggables()
    {
        DraggableObject[] allObjects = GetAllObjects();

        for (int index = 0; index < allObjects.Length; index++)
        {
            if (allObjects[index] != null)
            {
                allObjects[index].DisableAndLock();
            }
        }
    }

    DraggableObject[] GetAllObjects()
    {
        List<DraggableObject> allObjects = new List<DraggableObject>();
        HashSet<DraggableObject> uniqueObjects = new HashSet<DraggableObject>();

        SequentialStageDefinition[] stages = GetStageSequenceList();
        for (int stageIndex = 0; stageIndex < stages.Length; stageIndex++)
        {
            SequentialStageDefinition sequence = stages[stageIndex];

            if (sequence == null)
            {
                continue;
            }

            for (int stepIndex = 0; stepIndex < sequence.StepCount; stepIndex++)
            {
                DraggableObject stepObject = sequence.GetObject(stepIndex);
                if (stepObject != null && uniqueObjects.Add(stepObject))
                {
                    allObjects.Add(stepObject);
                }
            }
        }

        return allObjects.ToArray();
    }

    void SetInitialUIState()
    {
        if (materisGO != null)
        {
            materisGO.SetActive(false);
        }

        if (lanjutButtonGO != null)
        {
            lanjutButtonGO.SetActive(false);
        }

        if (lanjutButtonText != null)
        {
            lanjutButtonText.text = "Lanjut";
        }

        if (signText != null)
        {
            signText.text = string.Empty;
        }
    }

    IEnumerator BeginSequentialStage(SequentialStageDefinition sequence, int stageIndex)
    {
        if (sequence == null || !sequence.IsConfigured())
        {
            yield break;
        }

        if (lanjutButtonGO != null)
        {
            lanjutButtonGO.SetActive(false);
        }

        yield return MoveCameraToTarget(sequence.CameraTarget);
        ScatterSequenceObjects(sequence);

        activeSequence = sequence;
        activeSequenceStageIndex = stageIndex;
        activeSequenceStepIndex = 0;
        PrepareActiveSequenceStep();
    }

    void PrepareActiveSequenceStep()
    {
        if (activeSequence == null || !activeSequence.HasStep(activeSequenceStepIndex))
        {
            return;
        }

        DraggableObject stepObject = activeSequence.GetObject(activeSequenceStepIndex);
        string instruction = activeSequence.GetInstruction(activeSequenceStepIndex);
        PrepareObjectForStep(stepObject, instruction);
        currentState = SimulationState.InStage;
    }

    void HandleSequentialStageSnap(DraggableObject snappedObject)
    {
        if (activeSequence == null || currentState != SimulationState.InStage)
        {
            return;
        }

        if (!activeSequence.HasStep(activeSequenceStepIndex))
        {
            return;
        }

        if (snappedObject != activeSequence.GetObject(activeSequenceStepIndex))
        {
            return;
        }

        activeSequenceStepIndex++;

        if (activeSequence.HasStep(activeSequenceStepIndex))
        {
            PrepareActiveSequenceStep();
            return;
        }

        completedSequenceStageIndex = activeSequenceStageIndex;
        ShowLanjut(GetDoneButtonTextForStageIndex(completedSequenceStageIndex));
        currentState = SimulationState.StageCompleted;
        activeSequence = null;
        activeSequenceStageIndex = -1;
        activeSequenceStepIndex = -1;
    }

    SequentialStageDefinition[] GetStageSequenceList()
    {
        return new[]
        {
            stage1Sequence,
            stage2Sequence,
            stage3Sequence,
            stage4Sequence,
            stage5Sequence,
            stage6Sequence
        };
    }

    int GetNextConfiguredStageIndex(int afterStageIndex)
    {
        SequentialStageDefinition[] stages = GetStageSequenceList();

        for (int index = afterStageIndex + 1; index < stages.Length; index++)
        {
            if (stages[index] != null && stages[index].IsConfigured())
            {
                return index;
            }
        }

        return -1;
    }

    int GetFinalConfiguredStageIndex()
    {
        SequentialStageDefinition[] stages = GetStageSequenceList();

        for (int index = stages.Length - 1; index >= 0; index--)
        {
            if (stages[index] != null && stages[index].IsConfigured())
            {
                return index;
            }
        }

        return -1;
    }

    bool IsAtFinalConfiguredStage(int stageIndex)
    {
        return stageIndex >= 0 && stageIndex == GetFinalConfiguredStageIndex();
    }

    string GetDoneButtonTextForStageIndex(int stageIndex)
    {
        return IsAtFinalConfiguredStage(stageIndex) ? "Selesai" : "Lanjut";
    }

    void HookStageVideoTriggerButton()
    {
        if (stageVideoTriggerButton == null)
        {
            GameObject triggerObject = GameObject.Find("tomateri");

            if (triggerObject != null)
            {
                stageVideoTriggerButton = triggerObject.GetComponent<Button>();
            }
        }

        if (stageVideoTriggerButton == null)
        {
            return;
        }

        stageVideoTriggerButton.onClick.RemoveListener(PlayCurrentStageVideo);
        stageVideoTriggerButton.onClick.AddListener(PlayCurrentStageVideo);
    }

    void HookRuntimeBackButton()
    {
        if (runtimeBackButton == null)
        {
            GameObject backObject = GameObject.Find("Buttonback");

            if (backObject != null)
            {
                runtimeBackButton = backObject.GetComponent<Button>();
            }
        }

        if (runtimeBackButton == null)
        {
            return;
        }

        runtimeBackButton.onClick.RemoveListener(OnBackRuntimePressed);
        runtimeBackButton.onClick.AddListener(OnBackRuntimePressed);
    }

    void UpdateInstruction(string instruction)
    {
        if (signText != null)
        {
            signText.text = instruction;
        }
    }

    void ShowLanjut(string buttonText)
    {
        if (lanjutButtonText != null)
        {
            lanjutButtonText.text = buttonText;
        }

        if (lanjutButtonGO != null)
        {
            lanjutButtonGO.SetActive(true);
        }
    }

    int GetCurrentStageNumber()
    {
        if (activeSequenceStageIndex >= 0)
        {
            return activeSequenceStageIndex + 1;
        }

        if (completedSequenceStageIndex >= 0)
        {
            return completedSequenceStageIndex + 1;
        }

        return 0;
    }

    string GetVideoPathForStage(int stageNumber)
    {
        switch (stageNumber)
        {
            case 1:
                return string.IsNullOrWhiteSpace(stage1VideoPath) ? defaultStageVideoPath : stage1VideoPath;
            case 2:
                return string.IsNullOrWhiteSpace(stage2VideoPath) ? defaultStageVideoPath : stage2VideoPath;
            case 3:
                return string.IsNullOrWhiteSpace(stage3VideoPath) ? defaultStageVideoPath : stage3VideoPath;
            case 4:
                return string.IsNullOrWhiteSpace(stage4VideoPath) ? defaultStageVideoPath : stage4VideoPath;
            case 5:
                return string.IsNullOrWhiteSpace(stage5VideoPath) ? defaultStageVideoPath : stage5VideoPath;
            case 6:
                return string.IsNullOrWhiteSpace(stage6VideoPath) ? defaultStageVideoPath : stage6VideoPath;
            default:
                return defaultStageVideoPath;
        }
    }

    IEnumerator MoveCameraToTarget(Transform target)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null || target == null)
        {
            yield break;
        }

        Transform cameraTransform = mainCamera.transform;
        Vector3 startPosition = cameraTransform.position;
        Quaternion startRotation = cameraTransform.rotation;
        float elapsed = 0f;

        while (elapsed < cameraMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / cameraMoveDuration);
            cameraTransform.position = Vector3.Lerp(startPosition, target.position, t);
            cameraTransform.rotation = Quaternion.Slerp(startRotation, target.rotation, t);
            yield return null;
        }

        cameraTransform.position = target.position;
        cameraTransform.rotation = target.rotation;
    }

    IEnumerator AnimateAnchors(
        RectTransform targetRect,
        Vector2 targetAnchorMin,
        Vector2 targetAnchorMax,
        float duration)
    {
        Vector2 startAnchorMin = targetRect.anchorMin;
        Vector2 startAnchorMax = targetRect.anchorMax;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            targetRect.anchorMin = Vector2.Lerp(startAnchorMin, targetAnchorMin, t);
            targetRect.anchorMax = Vector2.Lerp(startAnchorMax, targetAnchorMax, t);
            yield return null;
        }

        targetRect.anchorMin = targetAnchorMin;
        targetRect.anchorMax = targetAnchorMax;
        titleRoutine = null;
    }
}
