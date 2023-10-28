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
    public float xOffset = 0.0f;
    public float zOffset;
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
    public float heightOffset = 0.4f;

    Dictionary<string, GameObject> objsDict = new Dictionary<string, GameObject>();
    Dictionary<string, GameObject> linesDict = new Dictionary<string, GameObject>();
    float LerpTime = 2.0f;

    Vector3 lerpStart;
    Vector3 lerpEnd;
    bool isLerping;

    float timeStarted;

    [SerializeField]
    float distanceThreshold = 1.0f;
    [SerializeField]
    float distanceTimeThreshold = 5.0f;
    [SerializeField]
    int numDivisions = 20;
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
    //This is done when the recipe goes from one step to another. By default, the overview
    //stays on the center of all required objects
    public void SnapToCentroid()
    {
        Vector3 centroid = Camera.main.transform.position + Camera.main.transform.forward * zOffset + Camera.main.transform.right * xOffset;
        Vector3 finalPos = new Vector3(centroid.x, Camera.main.transform.position.y + heightOffset, centroid.z);
        float DistanceToCam = finalPos.GetDistanceToSpatialMap();
        if(DistanceToCam > 0 && DistanceToCam < zOffset)
        {
            float currOffset = Mathf.Max(minDistance, DistanceToCam);
            centroid = Camera.main.transform.position + Camera.main.transform.forward * currOffset + Camera.main.transform.right * xOffset;
            finalPos = new Vector3(centroid.x, Camera.main.transform.position.y + heightOffset, centroid.z);
        };
        BeginLerp(this.transform.position, finalPos);
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
        if (currBounds == setBounds && dist < distanceThreshold)
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
        //MANAGING POSITION
        if (dist >= distanceThreshold)
        {
            positionTimer += Time.deltaTime;
        }
        else
        {
            positionTimer = 0.0f;
        }
        if (positionTimer >= distanceTimeThreshold)
        {
            setPosition = Camera.main.transform.position;
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