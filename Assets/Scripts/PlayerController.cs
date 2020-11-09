using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    public SteamVR_Action_Boolean input;
    public SteamVR_Action_Boolean endScene;
    public SteamVR_Action_Boolean trigger;
    public float speed = 1;
    private Vector3 direction;
    private CharacterController characterController;
    public ServerHosting serverHosting;
    public bool isServer = false;
    private Animator animator;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        // animator = bride.GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (input.state)
        {
            direction = Player.instance.hmdTransform.forward;
            characterController.Move(speed * Time.deltaTime * Vector3.ProjectOnPlane(direction, Vector3.up));
        }
        if (endScene.state)
        {
            serverHosting.NextScene();
        }

        if (isServer && trigger.state && SceneManager.GetActiveScene().name == "WeddingScene")
        {
            serverHosting.QuitGame();
        }

        if (isServer && trigger.state)
        {
            serverHosting.SpawnNote();
        }

    }
}