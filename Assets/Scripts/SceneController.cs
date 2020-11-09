using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneController : MonoBehaviour
{

    public GameObject teleportFeature;
    public GameObject teleportPoint;

    private bool sceneEnded;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(endScene());
    }

    // Update is called once per frame
    void Update()
    {
        
        if (sceneEnded)
        {
            // teleportFeature.SetActive(true);
            // teleportPoint.SetActive(true);
        }
    }

    private IEnumerator endScene()
    {
        yield return new WaitForSeconds(5f);
        sceneEnded = true;
    }
}
