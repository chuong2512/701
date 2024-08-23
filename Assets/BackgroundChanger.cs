using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PostProcessing;

public class BackgroundChanger : MonoBehaviour {


	public Color[] BackgroundColor;
	public PostProcessingProfile[] profiles;
	int RandomColor,RandomProfile;
	Camera cameraColor;
	PostProcessingBehaviour ppb;


	void Start () {
		RandomColor = Random.Range (0, BackgroundColor.Length);
		RandomProfile = Random.Range (0, profiles.Length);
		cameraColor = GetComponent<Camera> ();
		cameraColor.backgroundColor = BackgroundColor [RandomColor];
		ppb = GetComponent<PostProcessingBehaviour> ();
	//	ppb.profile = profiles[RandomProfile];
	}

}
