using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// ARUI Control
/// </summary>
public class AngelARUI : Singleton<AngelARUI>
{
    private Camera _arCamera;
    public Camera ARCamera => _arCamera;              /// <Reference to camera rendering the AR scene

    ///****** Debug Settings
    private bool _showARUIDebugMessages = true;       /// <If true, ARUI debug messages are shown in the unity console and scene Logger (if available)
    private bool _showEyeGazeTarget = false;          /// <If true, the eye gaze target is shown if the eye ray hits UI elements (white small cube), can be toggled on/off at runtime

    ///****** Guidance Settings
    private bool _useViewManagement = true;           /// <If true, the ARUI view mangement will run
    public bool IsVMActiv => ViewManagement.Instance != null && _useViewManagement;

    [Tooltip("Set a custom Skip Notification Message. Can not be empty.")]
    public string SkipNotificationMessage = "You are skipping the current task:";

    ///****** Confirmation Dialogue
    private UnityAction _onUserIntentConfirmedAction = null;     /// <Action invoked if the user accepts the confirmation dialogue
    private ConfirmationDialogue _confirmationWindow = null;     /// <Reference to confirmation dialogue
    private GameObject _confirmationWindowPrefab = null;

    private Dictionary<string, CVDetectedObj> DetectedObjects = new Dictionary<string, CVDetectedObj>();

    private void Awake() => StartCoroutine(InitProjectSettingsAndScene());

    private IEnumerator InitProjectSettingsAndScene()
    {
        List<string> layers = new List<string>()
       {
           StringResources.zBuffer_layer, StringResources.Hand_layer, StringResources.VM_layer,
           StringResources.UI_layer, StringResources.spatialAwareness_layer
       };

        StringResources.LayerToLayerInt = new Dictionary<string, int>
        {
            {  layers[0], 24 },
            {  layers[1], 25 },
            {  layers[2], 26 },
            {  layers[3], 5 },
            {  layers[4], 31 }
        };

#if UNITY_EDITOR
        foreach (string layer in layers)
            Utils.CreateLayer(layer, StringResources.LayerToLayerInt[layer]);
#endif

        yield return new WaitForEndOfFrame();

        //Get persistant reference to ar cam
        _arCamera = Camera.main;
        int oldMask = _arCamera.cullingMask;
        _arCamera.cullingMask = oldMask & ~(1 << (StringResources.LayerToLayerInt[StringResources.zBuffer_layer]));

        //Instantiate audio manager, for audio feedback
        AudioManager am = new GameObject("AudioManager").AddComponent<AudioManager>();
        am.gameObject.name = "***ARUI-" + StringResources.audioManager_name;

        //Instantiate eye gaze managing script
        GameObject eyeTarget = Instantiate(Resources.Load(StringResources.EyeTarget_path)) as GameObject;
        eyeTarget.gameObject.name = "***ARUI-" + StringResources.eyeGazeManager_name;
        eyeTarget.AddComponent<EyeGazeManager>();
        EyeGazeManager.Instance.ShowDebugTarget(_showEyeGazeTarget);

        //Instantiate the AI assistant - orb
        GameObject orb = Instantiate(Resources.Load(StringResources.Orb_path)) as GameObject;
        orb.gameObject.name = "***ARUI-" + StringResources.orb_name;
        orb.transform.parent = transform;
        orb.AddComponent<Orb>();

        //Start View Management, if enabled
        if (_useViewManagement)
            StartCoroutine(TryStartVM());

        //Instantiate empty tasklist
        GameObject taskList = Instantiate(Resources.Load(StringResources.TaskList_path)) as GameObject;
        taskList.gameObject.name = "***ARUI-" + StringResources.tasklist_name;
        taskList.AddComponent<TaskListManager>();

        //Load resources for UI elements
        _confirmationWindowPrefab = Resources.Load(StringResources.ConfNotification_path) as GameObject;
        _confirmationWindowPrefab.gameObject.name = "***ARUI-" + StringResources.confirmationWindow_name;

        InitVisibilityManagement();
    }


    #region Task Guidance

    /// <summary>
    /// Set the task list and set the current task id to 0 (first in the given list)
    /// </summary>
    /// <param name="tasks">2D array tasks</param>
    public void SetTasks(string[,] tasks)
    {
        TaskListManager.Instance.SetTasklist(tasks);
        TaskListManager.Instance.SetCurrentTask(0);
    }

    /// <summary>
    /// Set the current task the user has to do.
    /// If taskID is >= 0 and < the number of tasks, the orb won't react.
    /// If taskID is the same as the current one, the ARUI won't react.
    /// If taskID has subtasks, the orb shows the first subtask as the current task
    /// </summary>
    /// <param name="taskID">index of the current task that should be highlighted in the UI</param>
    public void SetCurrentTaskID(int taskID) => TaskListManager.Instance.SetCurrentTask(taskID);

    /// <summary>
    /// Enable/Disable Tasklist
    /// </summary>
    public void SetTaskListActive(bool isActive) => TaskListManager.Instance.SetTaskListActive(isActive);

    /// <summary>
    /// Set all tasks in the tasklist as done. The orb will show a "All Done" message
    /// </summary>
    public void SetAllTasksDone() => TaskListManager.Instance.SetAllTasksDone();

    /// <summary>
    /// Toggles the task list. If on, the task list is positioned in front of the user's current gaze.
    /// </summary>
    public void ToggleTasklist() => TaskListManager.Instance.ToggleTasklist();

    /// <summary>
    /// Mute voice feedback for task guidance. ONLY influences task guidance.
    /// </summary>
    /// <param name="mute">if true, the user will hear the tasks, in addition to text.</param>
    public void MuteAudio(bool mute) => AudioManager.Instance.MuteAudio(mute);

    #endregion

    #region Notifications
    /// <summary>
    /// Set the callback function that is invoked if the user confirms the confirmation dialogue
    /// </summary>
    public void SetUserIntentCallback(UnityAction userIntentCallBack) => _onUserIntentConfirmedAction = userIntentCallBack;


    ///// <summary>
    ///// If confirmation action is set - SetUserIntentCallback(...) - and no confirmation window is active at the moment, the user is shown a 
    ///// timed confirmation window. Recommended text: "Did you mean ...". If the user confirms the dialogue, the onUserIntentConfirmedAction action is invoked. 
    ///// </summary>
    ///// <param name="msg">message that is shown in the confirmation dialogue</param>
    public void TryGetUserFeedbackOnUserIntent(string msg)
    {
        if (_onUserIntentConfirmedAction == null || _confirmationWindow != null || msg == null || msg.Length == 0) return;

        GameObject window = Instantiate(_confirmationWindowPrefab, transform);
        window.gameObject.name = "***ARUI-Confirmation-" + msg;
        _confirmationWindow = window.AddComponent<ConfirmationDialogue>();
        _confirmationWindow.InitializeConfirmationNotification(msg, _onUserIntentConfirmedAction);
    }

    /// <summary>
    /// If given paramter is true, the orb will show message to the user that the system detected an attempt to skip the current task.
    /// The message will disappear if "SetCurrentTaskID(..)" is called, or ShowSkipNotification(false)
    /// </summary>
    /// <param name="show">if true, the orb will show a skip notification, if false, the notification will disappear</param>
    public void ShowSkipNotification(bool show)
    {
        if (TaskListManager.Instance.GetTaskCount() <= 0 || TaskListManager.Instance.IsDone) return;

        if (show)
        {
            if (SkipNotificationMessage == null || SkipNotificationMessage.Length == 0)
                SkipNotificationMessage = "You are skipping the current task:";

            Orb.Instance.SetNotificationMessage(SkipNotificationMessage);
        }
        else
            Orb.Instance.SetNotificationMessage("");
    }

    #endregion

    #region Detected Physical Object Registration

    public void RegisterDetectedObject(GameObject bbox, string ID)
    {
        if (DetectedObjects.ContainsKey(ID)) return;

        GameObject copy = Instantiate(bbox);
        copy.gameObject.name = "***ARUI-CVDetected-" + ID;
        CVDetectedObj ndetection = copy.AddComponent<CVDetectedObj>();
        DetectedObjects.Add(ID, ndetection);

    }

    public void DeRegisterDetectedObject(string ID)
    {
        if (!DetectedObjects.ContainsKey(ID)) return;

        GameObject temp = DetectedObjects[ID].gameObject;
        DetectedObjects.Remove(ID);
        Destroy(temp);
    }

    #endregion

    #region View management

    /// <summary>
    /// Enable or disable view management. enabled by default 
    /// </summary>
    /// <param name="enabled"></param>
    public void SetViewManagement(bool enabled)
    {
        if (_useViewManagement != enabled)
        {
            if (enabled)
            {
                StartCoroutine(TryStartVM());
            }
            else if (ViewManagement.Instance != null)
            {
                Destroy(ARCamera.gameObject.GetComponent<ViewManagement>());
                Destroy(ARCamera.gameObject.GetComponent<SpaceManagement>());
                _useViewManagement = false;

                AngelARUI.Instance.LogDebugMessage("View Management is OFF",true);
            }
        }
    }

    /// <summary>
    /// Start view management if dll is available. If dll could not be loaded, view management is turned off.
    /// </summary>
    /// <returns></returns>
    private IEnumerator TryStartVM()
    {
        SpaceManagement sm = ARCamera.gameObject.gameObject.AddComponent<SpaceManagement>();
        yield return new WaitForEndOfFrame();

        bool loaded = sm.CheckIfDllLoaded();

        if (loaded)
        {
            ARCamera.gameObject.AddComponent<ViewManagement>();
            AngelARUI.Instance.LogDebugMessage("View Management is ON", true);
        }
        else
        {
            Destroy(sm);
            LogDebugMessage("VM could not be loaded. Setting vm disabled.", true);
        }

        _useViewManagement = loaded;
    }
    #endregion

    /// <summary>
    /// Initialize components for the visibility computation of physical objects
    /// </summary>
    private void InitVisibilityManagement()
    {
        Camera zBufferCam = new GameObject("zBuffer").AddComponent<Camera>();
        zBufferCam.transform.parent = _arCamera.transform;
        zBufferCam.transform.position = Vector3.zero;
        zBufferCam.fieldOfView = _arCamera.fieldOfView;

        zBufferCam.clearFlags = CameraClearFlags.Color;
        zBufferCam.backgroundColor = Color.black;
        zBufferCam.cullingMask = 1 << Utils.GetLayerInt(StringResources.zBuffer_layer);

        zBufferCam.nearClipPlane = 0.1f;
        zBufferCam.targetDisplay = 1;
        zBufferCam.targetTexture = new RenderTexture(Resources.Load(StringResources.zBufferTexture_path) as RenderTexture);
        zBufferCam.allowHDR = false;
        zBufferCam.allowMSAA = false;

        zBufferCam.nearClipPlane = _arCamera.nearClipPlane;

        zBufferCam.gameObject.AddComponent<ProcessObjectVisibility>();
    }


    #region Logging

    public bool PrintVMDebug = false;
    public void Update()
    {
        if (Input.GetKeyUp(KeyCode.M))
            PrintVMDebug = !PrintVMDebug;
    }

    /// <summary>
    /// Set if debug information is shown in the logger window
    /// </summary>
    /// <param name="show">if true, ARUI debug messages are shown in the unity console and scene Logger (if available)</param>
    public void ShowDebugMessagesInLogger(bool show) => _showARUIDebugMessages = show;

    /// <summary>
    /// Set if debug information is shown about the users eye gaze
    /// </summary>
    /// <param name="show">if true and the user is looking at a virtual UI element, a debug cube that represents the eye target is shown</param>
    public void ShowDebugEyeGazeTarget(bool show)
    {
        _showEyeGazeTarget = show;
        EyeGazeManager.Instance.ShowDebugTarget(_showEyeGazeTarget);
    }

    /// <summary>
    /// ********FOR DEBUGGING ONLY, prints ARUI logging messages
    /// </summary>
    /// <param name="message"></param>
    /// <param name="showInLogger"></param>
    public void LogDebugMessage(string message, bool showInLogger)
    {
        if (_showARUIDebugMessages)
        {
            if (showInLogger && FindObjectOfType<Logger>() != null)
                Logger.Instance.LogInfo("***ARUI: " + message);
            Debug.Log("***ARUI: " + message);
        }
    }

    #endregion
}
