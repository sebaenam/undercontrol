﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using InControl;

public class GameManager : GenericSingletonClass<GameManager> {
	public GameObject playerPrefab;
	[Range(1,8)]
	public int possiblePlayers;
	public float secondsToWaitAfterDeath;
	public float suddenDeathTime;
	public float onlyTwoPlayersGameSuddenDeathTime;
	public float countdownBeforePlay;
	public List<RuntimeAnimatorController> animators = new List<RuntimeAnimatorController> ();

	int playersReady=0;
	List<GameObject> players = new List<GameObject>();
	List<Transform> spawnPoints = new List<Transform>();
	List<GameObject> deathReportPlayers = new List<GameObject>();
	List<int> deathReportKilledBy = new List<int>();
	LasersManager lasersManager;
	bool charSelection = false;
	bool pressStart = false;
	List<int> scores = new List<int> ();
	//To store auto-kills
	List<int> negativeScores = new List<int> ();
	MenuManager menuManager;
	UIManager uiManager;
	PowerUpManager powerUpManager;
	bool isTwoPlayersGame = false;
	int deadPlayers=0;
	int numberOfRounds;
	int actualSceneIndex;
	bool isGamePaused=false;
	Coroutine startedDeathsReport = null;
	new void Awake(){
		base.Awake ();
	}

	void Start(){
		powerUpManager = PowerUpManager.Instance;
		uiManager = UIManager.Instance;
		menuManager = MenuManager.Instance;
		menuManager.SetPossiblePlayers (possiblePlayers);
	}

	void Update () {
		if (pressStart) {
			OnPressStart ();
		}
		if (charSelection) {
			OnCharSelection ();
		}
	}

	public bool PauseGame(){
		if (!isGamePaused && Time.timeScale != 0) {
			isGamePaused = true;
			Time.timeScale = 0;
			return true;
		}
		return false;
	}
	public void BackToMain(){
		//Prepare the envoirement to re-play
		lasersManager.DisableLasers ();
		menuManager.StopLasersAdvice ();
		for (int i = 0; i < players.Count; i++) {
			players [i].SetActive (false);
			players [i].GetComponent<PlayerLife> ().ResetPlayer();
		}
		if (startedDeathsReport != null) {
			StopCoroutine (startedDeathsReport);
		}
		deadPlayers = 0;
		Time.timeScale = 1;
		isGamePaused = false;
		StartCoroutine (FinishGame ());
		deathReportPlayers.Clear ();
		deathReportKilledBy.Clear ();
	}

	public void UnPauseGame(){
		if (isGamePaused && Time.timeScale != 1) {
			StartCoroutine (UnPause ());
		}
	}
	private IEnumerator UnPause(){
		yield return new WaitForEndOfFrame ();
		Time.timeScale = 1;
		isGamePaused = false;
	}

	public void ReportDeath(GameObject playerObject, int killedByPlayerNumber){
		playerObject.SetActive (false);
		deathReportPlayers.Add (playerObject);
		deathReportKilledBy.Add (killedByPlayerNumber);
		AkSoundEngine.PostEvent("DeathSound",gameObject);
		if (startedDeathsReport != null) {
			StopCoroutine (startedDeathsReport);
		}
		startedDeathsReport = StartCoroutine (ReportDeath());
	}

	private IEnumerator ReportDeath(){
		yield return new WaitForSeconds (secondsToWaitAfterDeath);
		ProcessDeaths();
		startedDeathsReport = null;
	}

	private void ProcessDeaths(){
		for (int k = 0; k < deathReportPlayers.Count; k++) {
			deadPlayers++;
			if (players.Count - deadPlayers == 2) {
				lasersManager.StartLasers (suddenDeathTime);
			}
			deathReportPlayers[k].transform.rotation = Quaternion.identity;
			//Score assingment logic
			if (deathReportKilledBy [k] == deathReportPlayers [k].GetComponent<PlayerPreview> ().playerNumber
				|| deathReportKilledBy [k] == 0) {
				if (isTwoPlayersGame) {
					for (int i = 0; i < players.Count; i++) {
						if (!players [i].Equals (deathReportPlayers [k])) {
							scores [i]++;
						}
					}
				} else if(deathReportKilledBy [k] != 0) {
					negativeScores [deathReportKilledBy [k] - 1]++;
				}
			} else if (deathReportKilledBy[k] != 0 && deathReportKilledBy [k] != deathReportPlayers [k].GetComponent<PlayerPreview> ().playerNumber) {
				scores [deathReportKilledBy [k] - 1]++;
			}
			//

			//Music parameters
			switch (players.Count)
			{
				case 3:
				if(deadPlayers==1)
					AkSoundEngine.SetSwitch("InGameEvents","FirstDeath",menuManager.gameObject);
				break;
				case 4:
				if(deadPlayers==1)
					AkSoundEngine.SetSwitch("InGameEvents","FirstDeath",menuManager.gameObject);
				else if(deadPlayers==2)
					AkSoundEngine.SetSwitch("InGameEvents","SecondDeath",menuManager.gameObject);
				break;
			}
			//Music parameters

			if (players.Count -1 <= deadPlayers && 
				deathReportPlayers.Count-1 == k) {
				lasersManager.DisableLasers ();
				menuManager.StopLasersAdvice ();
				//Prepare the envoirement to re-play
				for (int i = 0; i < players.Count; i++) {
					players [i].SetActive (false);
					players [i].GetComponent<PlayerLife> ().ResetPlayer();
				}
				deadPlayers = 0;
				bool someoneWin = false;
				for (int i = 0; i < scores.Count; i++) {
					if ((scores [i]-negativeScores[i]) >= numberOfRounds) {
						someoneWin = true;
					}
					if ((scores [i] - negativeScores [i]) < 0) {
						negativeScores [i] = scores [i];
					}
				}

				if (someoneWin) {
					StartCoroutine (FinishGame ());
				} else {
					//GetNextMap
					StartCoroutine(NextRound());
				}
			}
		}
		deathReportPlayers.Clear ();
		deathReportKilledBy.Clear ();
	}
	private IEnumerator FinishGame(){
		powerUpManager.NotifyLevelFinished ();
		yield return StartCoroutine(uiManager.ShowActualScores(scores,negativeScores,3));
		uiManager.FinishGame ();
		AssignNegativeScores ();
		//End of rounds, back to selection
		SceneManager.UnloadSceneAsync (actualSceneIndex);
		menuManager.BackToMain ();
	}

	private IEnumerator NextRound(){
		powerUpManager.NotifyLevelFinished ();
		yield return StartCoroutine(uiManager.ShowActualScores(scores,negativeScores,3));
		AssignNegativeScores ();
		SceneManager.UnloadSceneAsync (actualSceneIndex);
		if (actualSceneIndex + 1 >= SceneManager.sceneCountInBuildSettings) {
			actualSceneIndex = 0;
		}
		if (actualSceneIndex+1  == 2) {
			AkSoundEngine.SetSwitch ("InGameEvents", "LevelBasic", menuManager.gameObject);
		}
		GameStart (actualSceneIndex+1, numberOfRounds);
	}
	private void AssignNegativeScores(){
		for (int i = 0; i < scores.Count; i++) {
			scores [i] -= negativeScores [i];
			negativeScores [i] = 0;
		}
	}

	public IEnumerator CharSelection(){
		//This is maded to wait one frame and doesn't capture the actual GetKeyDown
		yield return null;
		charSelection = true;
		if (players [0].GetComponent<PlayerPreview> ().charPreviewPos == 0) {
			menuManager.GoNextPreview (players [0].GetComponent<PlayerPreview> ());
		}
		for (int i = 0; i < players.Count; i++) {
			StartCoroutine(players [i].GetComponent<PlayerMovement> ().CharSelection ());
		}
	}

	public void GameStart(int sceneIndex, int numberOfRounds){
		this.numberOfRounds = numberOfRounds;
		switch (players.Count)
		{
			case 2:
				isTwoPlayersGame = true;
				AkSoundEngine.SetSwitch("AmountOfPlayers","Two",menuManager.gameObject);
			break;
			case 3:
				isTwoPlayersGame = false;
				AkSoundEngine.SetSwitch("AmountOfPlayers","Three",menuManager.gameObject);
			break;
			case 4:
				isTwoPlayersGame = false;
				AkSoundEngine.SetSwitch("AmountOfPlayers","Four",menuManager.gameObject);
			break;
		}
		AkSoundEngine.SetSwitch ("StateOfMusic","InGame", menuManager.gameObject);
		AkSoundEngine.SetSwitch("InGameEvents","LevelBasic",menuManager.gameObject);
		AkSoundEngine.PostEvent("InGameMusic",gameObject);
		AkSoundEngine.PostEvent("Countdown",gameObject);

		StartCoroutine (OnGameStart (sceneIndex));
	}
	private IEnumerator OnGameStart(int sceneIndex){
		actualSceneIndex = sceneIndex;
		bool loadStarted=false;
		if (!loadStarted) {
			SceneManager.LoadScene (sceneIndex,LoadSceneMode.Additive);
			loadStarted = true;
			yield return null;
		}

		GameObject arrowPointer = GameObject.FindWithTag ("ArrowPointer");
		if (arrowPointer != null) {
			arrowPointer.GetComponent<ArrowPointer> ().SetPlayers (players);
		}

		Transform spawnPointsParent;
		spawnPointsParent = GameObject.Find ("SpawnPoints").transform;
		spawnPoints.Clear ();
		foreach (Transform spawnPoint in spawnPointsParent) {
			spawnPoints.Add (spawnPoint);
		}
		for (int i = 0; i < players.Count; i++) {
			int randomSpawn = Random.Range (0, (spawnPoints.Count - 1));
			players [i].GetComponent<PlayerMovement>().ToSpawnPoint(spawnPoints [randomSpawn].position);
			players [i].SetActive (true);
			spawnPoints.RemoveAt (randomSpawn);
		}
		
		Time.timeScale = 0;
		menuManager.ShowCountdown (countdownBeforePlay);
		uiManager.StartGame (players);
		powerUpManager.NotifyLevelStart ();
		yield return new WaitForSecondsRealtime (countdownBeforePlay);
		Time.timeScale = 1;
		//Lasers
		lasersManager = GameObject.Find("Lasers").GetComponent<LasersManager>();
		if (players.Count == 2) {
			lasersManager.StartLasers (onlyTwoPlayersGameSuddenDeathTime);
		}
	}
	public void PressStartButton(){
		pressStart = true;
	}

	private void OnPressStart(){
		for (int i = 0; i < InputManager.Devices.Count; i++) {
			if (InputManager.Devices[i].GetControl(InputControlType.Start).WasPressed) {
				GameObject newPlayer = CreatePlayer (i);
				players.Add (newPlayer);
				menuManager.StartPressed ();
				pressStart = false;
			}
		}
	}

	public void LasersComming(){
		menuManager.ShowLasersAdvice ();
	}

	private void OnCharSelection(){
		for (int i = 0; i < InputManager.Devices.Count; i++) {
			//Detect new players entering the game
			if (InputManager.Devices [i].GetControl (InputControlType.Start).WasPressed) {
				bool inputUsed = false;
				for (int j = 0; j < players.Count; j++) {
					PlayerInput currentPlayerInput = players [j].GetComponent<PlayerInput> ();
					if (currentPlayerInput.GetInputNumber () == i) {
						inputUsed = true;
					}
				}
				if (!inputUsed && players.Count<4) {
					GameObject newPlayer = CreatePlayer (i);
					AkSoundEngine.PostEvent ("NewPlayer",gameObject);
					menuManager.GoNextPreview(newPlayer.GetComponent<PlayerPreview> ());
					StartCoroutine (newPlayer.GetComponent<PlayerMovement> ().CharSelection ());
					players.Add (newPlayer);
				}
			}

			if (InputManager.Devices [i].Action1.WasPressed && (InputManager.Devices [i].Name !="GlobalKeyboard")) {
				for (int j = 0; j < players.Count; j++) {
					PlayerInput currentPlayerInput = players [j].GetComponent<PlayerInput> ();
					if (currentPlayerInput.GetInputNumber () == i) {
						bool result = menuManager.SelectPreview (players [j].GetComponent<PlayerPreview> (), currentPlayerInput);
						if (result) {
							playersReady++;
							PlayerPreview playerPreview = players [j].GetComponent<PlayerPreview> ();
							players [playerPreview.playerNumber - 1].GetComponent<Animator> ().runtimeAnimatorController = animators [playerPreview.charPreviewPos - 1];
						}
						if (playersReady > 1 && playersReady == players.Count) {
							FinishPlayersSelection ();
							menuManager.CharacterSelectionFinished ();
						}
					}
				}
			}

			//Cancel button is pressed
			if (InputManager.Devices[i].Action2.WasPressed) {
				bool unselect = false;
				for (int j = 0; j < players.Count; j++) {
					PlayerInput currentPlayerInput = players [j].GetComponent<PlayerInput> ();
					PlayerPreview currentPlayerPreview = players [j].GetComponent<PlayerPreview> ();
					if (currentPlayerInput.GetInputNumber () == i && currentPlayerPreview.selected) {
						menuManager.UnselectPreview (currentPlayerPreview);
						playersReady--;
						unselect = true;
					}
				}
				if (!unselect) {
					menuManager.GoBack ();
				}
			}
		}

	}

	public IEnumerator RoundSelection(){
		bool waiting = true;
		while (waiting) {
			for (int i = 0; i < InputManager.Devices.Count; i++) {
				//Cancel button is pressed
				if (InputManager.Devices[i].Action2) {
					menuManager.GoBack ();
					waiting = false;
				}
			}
			yield return null;
		}
	}

	public void StopCharSelection(){
		ClearPlayerSelectionValues ();
		FinishPlayersSelection ();
	}

	void ClearPlayerSelectionValues(){
		for (int i = 0; i < players.Count; i++) {
			players [i].GetComponent<PlayerPreview> ().selected = false;
			scores [i] = 0;
			negativeScores [i] = 0;
		}
		playersReady=0;
		charSelection = false;
	}

	private GameObject CreatePlayer(int inputNumber){
		GameObject newPlayer =Instantiate(playerPrefab);
		PlayerPreview pp = newPlayer.AddComponent<PlayerPreview> ();
		int playerNumber = (players.Count + 1);
		newPlayer.name = ("Player" + (playerNumber));
		PlayerInput pi = newPlayer.AddComponent<PlayerInput>();
		pi.SetInputNumber (inputNumber);
		pp.SetPreview(playerNumber,0);
		scores.Add (0);
		negativeScores.Add (0);
		return newPlayer;
	}

	private void FinishPlayersSelection(){
		ClearPlayerSelectionValues ();
		for (int i = 0; i < players.Count; i++) {
			players [i].GetComponent<PlayerMovement> ().StopCharSelection();
		}
	}

	public int GetPlayerOneInputNumber(){
		return players [0].GetComponent<PlayerInput> ().GetInputNumber ();
	}

	public LasersManager GetLaserManager(){
		return lasersManager;
	}
	//Previews Managment
	public void GetNextUnusedPlayer(PlayerPreview actualPreview){
		menuManager.GoNextPreview (actualPreview);

	}
	public void GetPreviousUnusedPlayer(PlayerPreview actualPreview){
		menuManager.GoPreviousPreview (actualPreview);

	}
	//Previews Managment
}