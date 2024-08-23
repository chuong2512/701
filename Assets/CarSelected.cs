using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarSelected : MonoBehaviour {

	// Use this for initialization
	void Start () {
		Debug.Log (PlayerPrefs.GetString ("CurrentCar", "Stock Car"));
		transform.Find (PlayerPrefs.GetString ("CurrentCar", "Stock Car")).gameObject.SetActive (true);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
