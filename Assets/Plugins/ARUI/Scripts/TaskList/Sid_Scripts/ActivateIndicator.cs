using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateIndicator : MonoBehaviour
{
    GameObject _indicatorObj;
    // Start is called before the first frame update
    void Start()
    {
        _indicatorObj = transform.GetChild(0).gameObject;
    }

    public void ToggleIndicator(bool val)
    {
        _indicatorObj.SetActive(val);
    }
}
