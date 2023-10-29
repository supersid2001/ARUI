using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Shapes;
using System.Diagnostics;

public class TasklistPositionManager : Singleton<TasklistPositionManager>
{
    // Debug only
    public List<GameObject> objs;

    public GameObject LinePrefab;
    private GameObject _listContainer;

    public float movementSpeed = 1.0f;
    //Offset from camera if no objects exist
    [SerializeField]
    Vector3 positonOffsets = new Vector3(0.0f, 0.4f, 0.5f);
    // Min distance task overview can be from camera
    public float minDistance = 0.1f;

    #region Delay Values
    //public float xOffset;
    //public float yOffset;
    //public float zOffset;
    //public float SnapDelay = 2.0f;
    //public float minDistance = 0.5f;
    //Vector3 LastPosition;
    //float CurrDelay;
    #endregion

    Dictionary<string, GameObject> objsDict = new Dictionary<string, GameObject>();
    Dictionary<string, GameObject> linesDict = new Dictionary<string, GameObject>();
    float LerpTime = 2.0f;

    Vector3 lerpStart;
    Vector3 lerpEnd;
    bool isLerping;

    float timeStarted;

    // Specifies number of divisions on 360 degrees to specify for 
    // rotation parameters. For example, if numDivisions = 20
    // then we would have sections of 360/20 = 16 degrees
    [SerializeField]
    int numDivisions = 20;
    // Threshold for how long a user can be in a different rotation
    // section before overview snaps to new position
    [SerializeField]
    float rotationTimeThreshold = 5.0f;
    float divisionDifference;
    Vector3 setPosition;
    Vector2 currBounds;
    Vector2 setBounds;
    float rotationTimer = 0.0f;
    float positionTimer = 0.0f;

    bool isLooking = false;
    #region Delay Code
    // Start is called before the first frame update
/*    void Start()
    {
        _listContainer = transform.GetChild(1).gameObject;
        //LastPosition = Camera.main.transform.position;
    }*/
    #endregion
    //Function to have the task overview snap to the center of all required objects
    //This is done when the user either rotates onto a new section or moves a distance
    // beyond the distance threshold (and after their respectiev time thresholds have been
    // exceeded)
    public void SnapToCentroid()
    {
        Vector3 centroid = Camera.main.transform.position + Camera.main.transform.forward * positonOffsets.z + Camera.main.transform.right * positonOffsets.x;
        Vector3 finalPos = new Vector3(centroid.x, Camera.main.transform.position.y + positonOffsets.y, centroid.z);
        float DistanceToCam = finalPos.GetDistanceToSpatialMap();
        if(DistanceToCam > 0 && DistanceToCam < positonOffsets.z)
        {
            float currOffset = Mathf.Max(minDistance, DistanceToCam);
            centroid = Camera.main.transform.position + Camera.main.transform.forward * currOffset + Camera.main.transform.right * positonOffsets.x;
            finalPos = new Vector3(centroid.x, Camera.main.transform.position.y + positonOffsets.y, centroid.z);
        };
        this.transform.position = finalPos;
    }

    void Start()
    {
        divisionDifference = 360.0f / numDivisions;
        setBounds = new Vector2(0.0f, divisionDifference);
        setPosition = Camera.main.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        UnityEngine.Debug.Log("Current Rotation: " + setBounds);
        UnityEngine.Debug.Log("Current Position: " + setPosition);
        float dist = Vector3.Distance(Camera.main.transform.position, setPosition);
        //MANAGING ROTATION
        float currAngle = Clamp0360(Camera.main.transform.eulerAngles.y);
        float lowerBound = currAngle - (currAngle % divisionDifference);
        float upperBound = lowerBound + divisionDifference;
        Vector2 newBounds = new Vector2(lowerBound, upperBound);
        if (currBounds != newBounds)
        {
            currBounds = newBounds;
            rotationTimer = 0.0f;
        }
        if (currBounds == setBounds)
        {
            //ACTIVATE INDICATOR??
        }
        if (currBounds != setBounds)
        {
            rotationTimer += Time.deltaTime;
        }
        if (rotationTimer >= rotationTimeThreshold)
        {
            rotationTimer = 0.0f;
            setBounds = currBounds;
            SnapToCentroid();
        }
    }

    //Source -> https://www.blueraja.com/blog/404/how-to-use-unity-3ds-linear-interpolation-vector3-lerp-correctly
    //Code to lerp from one position to another
    void BeginLerp(Vector3 startPos, Vector3 endPos)
    {
        timeStarted = Time.time;
        isLerping = true;
        lerpStart = startPos;
        lerpEnd = endPos;
    }

    void FixedUpdate()
    {
        if (isLerping)
        {
            float timeSinceStarted = Time.time - timeStarted;
            float percentComplete = timeSinceStarted / LerpTime;
            transform.position = Vector3.Lerp(lerpStart, lerpEnd, percentComplete);
            if (percentComplete >= 1.0f)
            {
                isLerping = false;
            }
        }
    }

    /// <summary>
    /// If the user is looking at a task overview object then set isLooking to true
    /// </summary>
    /// <param name="val"></param>
    public void SetIsLooking(bool val)
    {
        isLooking = val;
    }

    public float Clamp0360(float eulerAngles)
    {
        float result = eulerAngles - Mathf.CeilToInt(eulerAngles / 360f) * 360f;
        if (result < 0)
        {
            result += 360f;
        }
        return result;
    }



}