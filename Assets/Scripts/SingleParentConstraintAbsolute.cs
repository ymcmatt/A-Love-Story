using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleParentConstraintAbsolute : MonoBehaviour
{
    public Transform affected;
    public Transform target;

    public Vector3 positionOffset;
    public Vector3 rotationOffset;
    public bool swapXAndZ = false;

    // Update is called once per frame
    void Update()
    {
        UpdatePositionAndRotation();
    }

    void UpdatePositionAndRotation() {
        affected.position = target.position + positionOffset;

        if (swapXAndZ)
        {
            affected.eulerAngles = new Vector3(target.eulerAngles.z, target.eulerAngles.y, target.eulerAngles.x) + rotationOffset;
        }
        else
        {
            affected.eulerAngles = target.eulerAngles + rotationOffset;
        }
    }
}
