using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIController : MonoBehaviour
{
    private GameObject obj;
    public GameObject pos2;
    public GameObject pos3;
    public GameObject pos4;

    // 0 for nothing, 1 for first movement position, 2 for second movement position, 3 for first book position, 4 for second book position
    public int statusCode;
    // Start is called before the first frame update
    void Start()
    {
        obj = this.gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.gameObject.name);
        Debug.Log("!!");
        if (other.gameObject.tag == "Player")
        {
            Debug.Log(this.gameObject.name);
            if (this.gameObject.name == "FirstPos")
            {
                statusCode += 1;
                this.gameObject.transform.parent.gameObject.SetActive(false);
                // this.gameObject.SetActive(false);
                pos2.SetActive(true);
                Debug.Log(statusCode);
            }
            //if (this.gameObject.name == "SecondPos")
            //{
            //    statusCode += 1;
            //    this.gameObject.transform.parent.gameObject.SetActive(false);
            //    pos3.SetActive(true);
            //    Debug.Log(statusCode);
            //}
            if (this.gameObject.name == "ThirdPos")
            {
                statusCode += 1;
                this.gameObject.transform.parent.gameObject.SetActive(false);
                pos4.SetActive(true);
                Debug.Log(statusCode);
                // pos3.SetActive(true);
            }
            if (this.gameObject.name == "LastPos")
            {
                SceneManager.LoadScene(2);
                Debug.Log(statusCode);
                // pos3.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("??");
        if (other.gameObject.name == "movable")
        {
            if (this.gameObject.name == "SecondPos")
            {
                Debug.Log("22");
                statusCode += 1;
                this.gameObject.transform.parent.gameObject.SetActive(false);
                pos3.SetActive(true);

            }
        }
    }
}
