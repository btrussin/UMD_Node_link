using UnityEngine;
using System.Collections;

public class CameraOriented : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        gameObject.transform.forward = Camera.main.transform.forward;
    }
}
