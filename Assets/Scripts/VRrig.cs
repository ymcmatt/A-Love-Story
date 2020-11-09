using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class VRMap
{
    public Transform vrTarget;
    public Transform rigTarget;
    public Vector3 trackingPositionOffset;
    public Vector3 trackingRotationOffset;

    public void Map()
    {
        rigTarget.position = vrTarget.TransformPoint(trackingPositionOffset);
        rigTarget.rotation = vrTarget.rotation * Quaternion.Euler(trackingRotationOffset);
    }
}


public class VRrig : MonoBehaviour
{
    public VRMap head;
    public VRMap leftHand;
    public VRMap rightHand;

    public Transform headConstraint;
    public Vector3 headBodyOffset;

    public Transform steamVRPlayArea;

    // Start is called before the first frame update
    void Start()
    {
        headBodyOffset = headConstraint.position - transform.position;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.position = new Vector3(head.vrTarget.position.x, head.vrTarget.position.y - headBodyOffset.y, head.vrTarget.position.z);
        transform.right = Vector3.ProjectOnPlane(head.vrTarget.forward, Vector3.up).normalized;
        // transform.right = headConstraint.up;

        leftHand.Map();
        rightHand.Map();

        head.Map();
    }
}
