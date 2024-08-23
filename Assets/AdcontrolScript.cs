using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class AdcontrolScript : MonoBehaviour {

	public int ShowInterstitialInterval;
	public bool ShowAdAtEveryGameover;

	public static AdcontrolScript instance;


	void Awake()
	{
		if (instance == null)
			instance = this;
		else if(instance != this)
			Destroy (gameObject);
	}


	void Start () {


		
	}


	public void showAd(){
		if (ShowAdAtEveryGameover) {
		} else {
			int AdCount = PlayerPrefs.GetInt ("AdCount", 0);
			if (AdCount == ShowInterstitialInterval) {
				PlayerPrefs.SetInt ("AdCount", 0);
			} else {
				PlayerPrefs.SetInt ("AdCount", AdCount + 1);
			
			}
		
		}

	}
}
