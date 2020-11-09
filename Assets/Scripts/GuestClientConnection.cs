using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Linq;
using UnityEngine.SceneManagement;
using Valve.VR.InteractionSystem;

public class GuestClientConnection : MonoBehaviourPunCallbacks
{
    class TransformCompare : IComparer
    {
        int IComparer.Compare(object x, object y)
        {
            return string.Compare(((Transform)x).name, ((Transform)y).name);
        }
    }

    class FlyingObject 
    {
        public int objectIndex;
        public Vector3 previousPosition;

        public FlyingObject(int newObjectIndex, Vector3 position) 
        {
            objectIndex = newObjectIndex;
            previousPosition = position;
        }
    }

    bool joinedRoom = false;

    PhotonView photonView;

    Transform leftHandRoot;
    Transform[] leftHandTransforms;
    Transform rightHandRoot;
    Transform[] rightHandTransforms;
    public Transform headRoot;

    public Transform otherLeftHandRoot;
    Transform[] otherLeftHandTransforms;
    public Transform otherRightHandRoot;
    Transform[] otherRightHandTransforms;
    public Transform otherHeadJoint;

    public Transform[] classroomSceneObjects;
    Coroutine[] updateCoroutines;

    int updateObject1 = -1;
    int updateObject2 = -1;

    int frameWaited = 0;
    List<FlyingObject> flyingObjects;

    public GameObject steamVRPlayArea;

    // Start is called before the first frame update
    void Start()
    {
        photonView = GetComponent<PhotonView>();

        PhotonNetwork.LocalPlayer.NickName = "guest";
        PhotonNetwork.ConnectUsingSettings();


        otherLeftHandTransforms = otherLeftHandRoot.GetComponentsInChildren<Transform>();
        Array.Sort(otherLeftHandTransforms, new TransformCompare());

        otherRightHandTransforms = otherRightHandRoot.GetComponentsInChildren<Transform>();
        Array.Sort(otherRightHandTransforms, new TransformCompare());

        updateCoroutines = new Coroutine[classroomSceneObjects.Length];
        flyingObjects = new List<FlyingObject>();
    }

    int sendCounter = 0;
    int framesInBetweenMessages = 6;

    // Update is called once per frame
    void Update()
    {
        if (leftHandRoot == null)
        {
            leftHandRoot = GameObject.Find("vr_glove_left 1(Clone)").transform.Find("vr_glove_model").Find("Root");

            leftHandTransforms = leftHandRoot.GetComponentsInChildren<Transform>();
            Array.Sort(leftHandTransforms, new TransformCompare());
        }
        if (rightHandRoot == null)
        {
            rightHandRoot = GameObject.Find("vr_glove_right 1(Clone)").transform.Find("vr_glove_model").Find("Root");

            rightHandTransforms = rightHandRoot.GetComponentsInChildren<Transform>();
            Array.Sort(rightHandTransforms, new TransformCompare());
        }


        if (PhotonNetwork.CountOfPlayers > 0)
        {
            Debug.Log("Sending RPC");

            if (sendCounter < framesInBetweenMessages)
            {
                sendCounter++;
            }
            else
            {
                sendCounter = 0;

                Vector3 leftHandRootPosition = leftHandRoot.parent.parent.transform.position;
                Quaternion leftHandRootRotation = leftHandRoot.parent.parent.transform.rotation;
                Vector3 rightHandRootPosition = rightHandRoot.parent.parent.transform.position;
                Quaternion rightHandRootRotation = rightHandRoot.parent.parent.transform.rotation;

                Vector3[] leftHandPositions = leftHandTransforms.Select(transform => transform.localPosition).ToArray();
                Quaternion[] leftHandRotations = leftHandTransforms.Select(transform => transform.localRotation).ToArray();
                Vector3[] rightHandPositions = rightHandTransforms.Select(transform => transform.localPosition).ToArray();
                Quaternion[] rightHandRotations = rightHandTransforms.Select(transform => transform.localRotation).ToArray();

                photonView.RPC("SetHands", RpcTarget.Others, leftHandRootPosition, leftHandRootRotation, rightHandRootPosition, rightHandRootRotation, leftHandPositions, leftHandRotations, rightHandPositions, rightHandRotations, headRoot.position, headRoot.rotation);

                if (updateObject1 != -1) 
                {
                    photonView.RPC("UpdateObjectWithIndex", RpcTarget.Others, updateObject1, classroomSceneObjects[updateObject1].position, classroomSceneObjects[updateObject1].rotation);
                }

                if (updateObject2 != -1)
                {
                    photonView.RPC("UpdateObjectWithIndex", RpcTarget.Others, updateObject2, classroomSceneObjects[updateObject2].position, classroomSceneObjects[updateObject2].rotation);
                }

                List<int> deleteIndices = new List<int>();
                for (int flyingObjectIndex = 0; flyingObjectIndex < flyingObjects.Count; flyingObjectIndex++) 
                {
                    FlyingObject flyingObject = flyingObjects[flyingObjectIndex];

                    Transform updateObject = classroomSceneObjects[flyingObject.objectIndex];

                    float positionDifferenceThreshold = 0.1f;
                    if ((updateObject.position - flyingObject.previousPosition).magnitude < positionDifferenceThreshold) 
                    {
                        deleteIndices.Add(flyingObjectIndex);
                    }
                    else
                    {
                        photonView.RPC("UpdateObjectWithIndex", RpcTarget.Others, classroomSceneObjects[flyingObjectIndex].name, classroomSceneObjects[flyingObjectIndex].position, classroomSceneObjects[flyingObjectIndex].rotation);
                    }

                    flyingObject.previousPosition = updateObject.position;
                }

                deleteIndices.Reverse();
                foreach (int deleteObjectIndex in deleteIndices)
                {
                    photonView.RPC("StopUpdatingObjectWithIndex", RpcTarget.Others, deleteObjectIndex);
                    flyingObjects.RemoveAt(deleteObjectIndex);
                }


                //PhotonNetwork.SendAllOutgoingCommands();
            }
        }
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();

        PhotonNetwork.JoinRandomRoom();
    }

    public void StartUpdatingObject(int objectIndex)
    {
        if (updateObject1 == -1)
        {
            updateObject1 = objectIndex;
            otherCanGrab1 = false;
            StartCoroutine(WaitUntilOtherCanGrab(1));
        }
        else
        {
            updateObject2 = objectIndex;
            otherCanGrab2 = false;
            StartCoroutine(WaitUntilOtherCanGrab(2));
        }
    }

    public void StopUpdatingObject(int objectIndex)
    {

        if (updateObject1 == objectIndex)
        {
            updateObject1 = -1;
            otherCanGrab1 = true;
        }
        else if (updateObject2 == objectIndex)
        {
            updateObject2 = -1;
            otherCanGrab2 = true;
        }

        flyingObjects.Add(new FlyingObject(objectIndex, classroomSceneObjects[objectIndex].position));
    }


    bool otherCanGrab1 = true;
    bool otherCanGrab2 = true;

    // This function is a quick fix to the problem of latency between picking something up and the other person still sending updates.
    // If we just picked up the object, ignore updates for the next second.
    // By then, the lingering updates should have stopped coming in, meaning if we get any more, the other person has grabbed it again.
    IEnumerator WaitUntilOtherCanGrab(int updateObject)
    {
        yield return new WaitForSeconds(1);

        if (updateObject == 1)
        {
            otherCanGrab1 = true;
        }
        if (updateObject == 2)
        {
            otherCanGrab2 = true;
        }
    }

    [PunRPC]
    public void LoadNextScene(int sceneNum, Vector3 playAreaPosition) 
    {
        steamVRPlayArea.transform.position = playAreaPosition;

        if (sceneNum == 3)
        {
            otherHeadJoint.GetChild(0).gameObject.SetActive(false);
            otherHeadJoint.GetChild(1).gameObject.SetActive(true);
        }

        SceneManager.LoadScene(sceneNum);

        StartCoroutine(UpdateInteractables());

        updateObject1 = -1;
        updateObject2 = -1;

    }

    [PunRPC]
    public void QuitGame() 
    {
        Application.Quit();
    }

    IEnumerator UpdateInteractables()
    {
        yield return new WaitForSeconds(1);

        GameObject interactables = GameObject.Find("Interactables");
        classroomSceneObjects = new Transform[interactables.transform.childCount];

        for (int i = 0; i < interactables.transform.childCount; i++)
        {
            classroomSceneObjects[i] = interactables.transform.GetChild(i);
        }

        Array.Sort(classroomSceneObjects, new TransformCompare());

        for (int i = 0; i < classroomSceneObjects.Length; i++)
        {
            Transform sceneObject = classroomSceneObjects[i];

            InteractableHoverEvents interactableHoverEvents = sceneObject.GetComponent<InteractableHoverEvents>();

            int newVal = i;
            interactableHoverEvents.onAttachedToHand.AddListener(delegate { int val = newVal; StartUpdatingObject(val); });
            interactableHoverEvents.onDetachedFromHand.AddListener(delegate { int val = newVal; StopUpdatingObject(val); });

        }
    }

    [PunRPC]
    public void EnableObjectWithIndex(int objectIndex, Vector3 newPosition, Quaternion newRotation) 
    {
        classroomSceneObjects[objectIndex].position = newPosition;
        classroomSceneObjects[objectIndex].rotation = newRotation;
        classroomSceneObjects[objectIndex].gameObject.SetActive(true);
    }

    [PunRPC]
    public void UpdateObjectWithIndex(int objectIndex, Vector3 newPosition, Quaternion newRotation)
    {
        if (updateObject1 == objectIndex)
        {
            if (!otherCanGrab1)
            {
                return;
            }
            updateObject1 = -1;
        }
        else if (updateObject2 == objectIndex)
        {
            if (!otherCanGrab2)
            {
                return;
            }
            updateObject2 = -1;
        }

        Transform objectTransform = classroomSceneObjects[objectIndex];

        Rigidbody rb = objectTransform.GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        StartCoroutine(LerpToObject(objectTransform, newPosition, newRotation));
    }

    IEnumerator LerpToObject(Transform updateObject, Vector3 newPosition, Quaternion newRotation)
    {

        Vector3 startingPosition = updateObject.position;
        Quaternion startingRotation = updateObject.rotation;

        int framesInBetweenMessages = 6;
        for (int frameNum = 1; frameNum <= framesInBetweenMessages; frameNum++)
        {
            float frameLerpPhase = (float)frameNum / (float)framesInBetweenMessages;

            updateObject.position = Vector3.Lerp(startingPosition, newPosition, frameLerpPhase);
            updateObject.rotation = Quaternion.Lerp(startingRotation, newRotation, frameLerpPhase);

            // Wait one frame.
            yield return new WaitForSeconds(0.0166f);
        }
    }

    [PunRPC]
    public void StopUpdatingObjectWithIndex(int objectIndex)
    {

        Transform objectTransform = classroomSceneObjects[objectIndex];

        Rigidbody rb = objectTransform.GetComponent<Rigidbody>();

        rb.useGravity = true;
        rb.isKinematic = false;

    }

    [PunRPC]
    public void SetHands(Vector3 leftHandRootPosition, Quaternion leftHandRootRotation, Vector3 rightHandRootPosition, Quaternion rightHandRootRotation, Vector3[] leftHandPositions, Quaternion[] leftHandRotations, Vector3[] rightHandPositions, Quaternion[] rightHandRotations, Vector3 headPosition, Quaternion headRotation)
    {
        StartCoroutine(LerpToHands(leftHandRootPosition, leftHandRootRotation, rightHandRootPosition, rightHandRootRotation, leftHandPositions, leftHandRotations, rightHandPositions, rightHandRotations, headPosition, headRotation));
    }

    IEnumerator LerpToHands(Vector3 leftHandRootPosition, Quaternion leftHandRootRotation, Vector3 rightHandRootPosition, Quaternion rightHandRootRotation, Vector3[] leftHandPositions, Quaternion[] leftHandRotations, Vector3[] rightHandPositions, Quaternion[] rightHandRotations, Vector3 headPosition, Quaternion headRotation)
    {
        Vector3 startingLeftHandRootPosition = otherLeftHandRoot.parent.parent.position;
        Quaternion startingLeftHandRootRotation = otherLeftHandRoot.parent.parent.rotation;
        Vector3 startingRightHandRootPosition = otherRightHandRoot.parent.parent.position;
        Quaternion startingRightHandRootRotation = otherRightHandRoot.parent.parent.rotation;

        Vector3 startingHeadRootPosition = otherHeadJoint.transform.position;
        Quaternion startingHeadRootRotation = otherHeadJoint.transform.rotation;

        Vector3[] startingLeftHandPositions = new Vector3[otherLeftHandTransforms.Length];
        Quaternion[] startingLeftHandRotations = new Quaternion[otherLeftHandTransforms.Length];
        for (int boneIndex = 0; boneIndex < otherLeftHandTransforms.Length; boneIndex++)
        {
            startingLeftHandPositions[boneIndex] = otherLeftHandTransforms[boneIndex].localPosition;
            startingLeftHandRotations[boneIndex] = otherLeftHandTransforms[boneIndex].localRotation;
        }

        Vector3[] startingRightHandPositions = new Vector3[otherRightHandTransforms.Length];
        Quaternion[] startingRightHandRotations = new Quaternion[otherRightHandTransforms.Length];
        for (int boneIndex = 0; boneIndex < otherRightHandTransforms.Length; boneIndex++)
        {
            startingRightHandPositions[boneIndex] = otherRightHandTransforms[boneIndex].localPosition;
            startingRightHandRotations[boneIndex] = otherRightHandTransforms[boneIndex].localRotation;
        }




        int framesInBetweenMessages = 6;
        for (int frameNum = 1; frameNum <= framesInBetweenMessages; frameNum++)
        {
            float frameLerpPhase = (float)frameNum / (float)framesInBetweenMessages;

            otherLeftHandRoot.parent.parent.position = Vector3.Lerp(startingLeftHandRootPosition, leftHandRootPosition, frameLerpPhase);
            otherLeftHandRoot.parent.parent.rotation = Quaternion.Lerp(startingLeftHandRootRotation, leftHandRootRotation, frameLerpPhase);
            otherRightHandRoot.parent.parent.position = Vector3.Lerp(startingRightHandRootPosition, rightHandRootPosition, frameLerpPhase);
            otherRightHandRoot.parent.parent.rotation = Quaternion.Lerp(startingRightHandRootRotation, rightHandRootRotation, frameLerpPhase);

            otherHeadJoint.transform.position = Vector3.Lerp(startingHeadRootPosition, headPosition, frameLerpPhase);
            otherHeadJoint.transform.rotation = Quaternion.Lerp(startingHeadRootRotation, Quaternion.Euler(new Vector3(0, headRotation.eulerAngles.y, 0)), frameLerpPhase);

            for (int boneIndex = 0; boneIndex < otherLeftHandTransforms.Length; boneIndex++)
            {
                otherLeftHandTransforms[boneIndex].localPosition = Vector3.Lerp(startingLeftHandPositions[boneIndex], leftHandPositions[boneIndex], frameLerpPhase);
                otherLeftHandTransforms[boneIndex].localRotation = Quaternion.Lerp(startingLeftHandRotations[boneIndex], leftHandRotations[boneIndex], frameLerpPhase);
            }

            for (int boneIndex = 0; boneIndex < otherRightHandTransforms.Length; boneIndex++)
            {
                otherRightHandTransforms[boneIndex].localPosition = Vector3.Lerp(startingRightHandPositions[boneIndex], rightHandPositions[boneIndex], frameLerpPhase);
                otherRightHandTransforms[boneIndex].localRotation = Quaternion.Lerp(startingRightHandRotations[boneIndex], rightHandRotations[boneIndex], frameLerpPhase);
            }

            // Wait one frame.
            yield return new WaitForSeconds(0.0166f);
        }
    }

}
