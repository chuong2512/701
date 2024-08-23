using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using BeautifulTransitions.Scripts.Transitions;
namespace EndlessCarChase
{
public class LevelConrol : MonoBehaviour {


	public static LevelConrol instance;
	ECCCar carController;
	public ECCGameController gameController;
	public Transform[] LevelPrefabs;
	public Transform[] SnowPrefabs;
	int CurrentLevel;
	public Text CurrentLevelText,NextLevelText;
	public Slider LevelSlider;
	public GameObject Levelwin;
	float SnowCount;
	float activeSnow;
		public GameObject CamAnim;
	
	bool GameWin;
	public void Awake(){

	}


	void Start () {
		GameWin = false;

		//PlayerPrefs.SetInt ("Level", 10);
		CurrentLevel = PlayerPrefs.GetInt ("Level", 0);
		Debug.Log ("Current Level: " + CurrentLevel);

		Instantiate (LevelPrefabs [CurrentLevel]);
		Instantiate (SnowPrefabs [CurrentLevel]);
		CurrentLevelText.text = CurrentLevel + "";
		NextLevelText.text = CurrentLevel + 1 + "";
		SnowCount = GameObject.FindGameObjectsWithTag ("snow").Length;
			Debug.Log("Snow COunt.........."+ SnowCount);

		gameController.StartGame ();
	}





	void Update () {

		 activeSnow = GameObject.FindGameObjectsWithTag ("snow").Length;
			int container = GameObject.FindGameObjectsWithTag ("SnowContainer").Length;
			if (activeSnow < 1 && container!=0 && !GameWin) {
			Debug.Log("LevelCompleted..........");
			if (Levelwin)
				Levelwin.SetActive (true);
				GameObject.FindGameObjectWithTag ("Player").GetComponent<ECCCar> ().enabled = false;
				StartCoroutine (LoadNextLevel ());

			
		}


		//Level Bar 
		float Levelbar = (activeSnow / SnowCount) * 100;
		float timeSpeed = 25f* Time.deltaTime;
		LevelSlider.value = Mathf.Lerp(LevelSlider.value, 100 - Levelbar, timeSpeed / 5);

	}

		IEnumerator LoadNextLevel(){
			
			int level = PlayerPrefs.GetInt ("Level", 0);
			PlayerPrefs.SetInt ("Level", level + 1);
			GameWin = true;
			TransitionHelper.TransitionOut(CamAnim);
			yield return new WaitForSeconds (1.0f);
			SceneManager.LoadScene (SceneManager.GetActiveScene ().buildIndex);
			Destroy (gameObject);
		
		}
}


}