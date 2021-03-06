﻿using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using InControl;
public class MenuManager : GenericSingletonClass<MenuManager> {

	public List<GameObject> menuPlayerSelector = new List<GameObject>();

	List<Sprite> availableStartKeys = new List<Sprite> ();
	MenuMovementBehaviour menuMovementBehaviour;
	SettingsMenuBehaviour settingsMenuBehaviour;
	UIManager uiManager;
	SettingsDB settingsDB;
	GameManager gameManager;
	GameObject pressStart;
	GameObject menuBackground;
	GameObject mainMenu;
	GameObject settings;
	GameObject characterSelect;
	GameObject credits;
	GameObject mapSelect;
	GameObject roundsSelect;
	GameObject countdown;
	GameObject laserAdvice;
	GameObject acceptGuide;
	GameObject backGuide;
	int possiblePlayers;
	new void Awake(){
		base.Awake ();
		menuMovementBehaviour = GetComponent<MenuMovementBehaviour> ();
		settingsMenuBehaviour = GetComponent<SettingsMenuBehaviour> ();
	}
	void Start () {
		uiManager = UIManager.Instance;
		gameManager = GameManager.Instance;
		settingsDB = SettingsDB.Instance;
		settingsDB.Initialize ();
		pressStart = transform.Find ("PressStart").gameObject;
		menuBackground = transform.Find ("GeneralBackground").gameObject;
		mainMenu = transform.Find ("MainMenu").gameObject;
		credits = transform.Find ("Credits").gameObject;
		settings = transform.Find ("Settings").gameObject;
		mapSelect = transform.Find ("MapSelect").gameObject;
		characterSelect = transform.Find ("CharacterSelect").gameObject;
		roundsSelect = transform.Find ("RoundsSelect").gameObject;
		countdown = transform.Find ("CountdownBox").Find("Countdown").gameObject;
		laserAdvice = transform.Find ("LaserAdvice").gameObject;
		acceptGuide = transform.Find ("Accept").gameObject;
		backGuide = transform.Find ("Back").gameObject;
		gameManager.PressStartButton ();

		AkSoundEngine.SetSwitch ("StateOfMusic","Menu", gameObject);
		AkSoundEngine.SetSwitch ("MenuMusic", "PressStartMenu",gameObject);
		AkSoundEngine.PostEvent ("Menu_music", gameObject);
		Cursor.visible = false;
		mainMenu.transform.Find ("Version").GetComponent<Text> ().text = Application.version;
	}

	public void StartPressed(){
		AkSoundEngine.PostEvent ("PressStart",gameObject);
		pressStart.SetActive (false);
		menuBackground.SetActive (true);
		mainMenu.SetActive (true);
		acceptGuide.SetActive (true);
		menuMovementBehaviour.MainMenuOptionsNavigation(0);
		AkSoundEngine.SetSwitch ("MenuMusic", "MainMenu",gameObject);
	}

	public void CharacterSelectionFinished(){
		for (int i = 0; i < menuPlayerSelector.Count; i++) {
			menuPlayerSelector [i].GetComponent<Image> ().color = Color.black;
			menuPlayerSelector [i].transform.Find ("Character"+(i+1)).gameObject.SetActive (false);
		}
		roundsSelect.SetActive (true);
		menuMovementBehaviour.RoundSelectionNavigation (1);
		StartCoroutine (gameManager.RoundSelection ());
		AkSoundEngine.SetSwitch ("MenuMusic", "KillSelectionMenu",gameObject);
	}
	public void RoundsSelect(){
		//Clear data
		ClearSelectorsData();
		//
		int rounds = int.Parse(EventSystem.current.currentSelectedGameObject.name);
		characterSelect.SetActive (false);
		menuBackground.SetActive (false);
		roundsSelect.SetActive (false);
		acceptGuide.SetActive (false);
		backGuide.SetActive (false);
		StopCoroutine (gameManager.RoundSelection ());

		gameManager.GameStart(1,rounds);
	}

	public void PlayButton(){
		mainMenu.SetActive (false);
		characterSelect.SetActive (true);
		backGuide.SetActive (true);
		menuMovementBehaviour.StopMainMenuSelection ();
		StartCoroutine(gameManager.CharSelection ());
		AkSoundEngine.SetSwitch ("MenuMusic", "CharacterSelectionMenu",gameObject);
	}

	public void Settings(){
		mainMenu.SetActive (false);
		settings.SetActive (true);
		backGuide.SetActive (true);
		acceptGuide.SetActive (true);
		menuMovementBehaviour.StopMainMenuSelection ();
		AkSoundEngine.SetSwitch ("MenuMusic", "CreditsMenu",gameObject);
		settingsMenuBehaviour.StartSettings (settings);
		//StartCoroutine (WaitToExitSettings());
	}
		
	private IEnumerator WaitToExitSettings(){
		bool loop = true;
		while (loop) {
			for (int i = 0; i < InputManager.Devices.Count; i++) {
				if (InputManager.Devices [i].Action1.WasPressed) {
					if (settings.GetComponent<Text> ().text.Equals ("SOUND ON")) {
						settings.GetComponent<Text> ().text = "SOUND OFF";
						PlayerPrefs.SetInt ("SoundOn", 0);
						Camera.main.GetComponent<AkAudioListener> ().enabled = false;
					} else if (settings.GetComponent<Text> ().text.Equals ("SOUND OFF")) {
						settings.GetComponent<Text> ().text = "SOUND ON";
						PlayerPrefs.SetInt ("SoundOn", 1);
						Camera.main.GetComponent<AkAudioListener> ().enabled = true;
					}
				}
				else if (InputManager.Devices [i].Action2.WasPressed) {
					GoBack ();
					loop = false;
				}
			}
			yield return null;
		}
	}


	private void ClearSelectorsData(){
		for (int i = 0; i < menuPlayerSelector.Count; i++) {
			menuPlayerSelector [i].GetComponent<Image> ().color = Color.white;
			menuPlayerSelector [i].transform.Find ("Character"+(i+1)).gameObject.SetActive (true);
			menuPlayerSelector [i].transform.Find ("Selected").gameObject.SetActive (false);
			menuPlayerSelector [i].GetComponent<SelectorBehaviour> ().ClearValues ();
			menuPlayerSelector [i].transform.Find ("KeyMap").gameObject.SetActive (false);
		}
	}

	public void GoBack(){
		if (roundsSelect.activeSelf) {
			characterSelect.SetActive (true);
			roundsSelect.SetActive (false);
			menuMovementBehaviour.StopRoundSelection ();
			ClearSelectorsData ();
			AkSoundEngine.SetSwitch ("MenuMusic", "CharacterSelectionMenu", gameObject);
			StartCoroutine (gameManager.CharSelection ());
		} else if (characterSelect.activeSelf) {
			for (int i = 0; i < menuPlayerSelector.Count; i++) {
				menuPlayerSelector [i].transform.Find ("Selected").gameObject.SetActive (false);
				menuPlayerSelector [i].GetComponent<SelectorBehaviour> ().ClearValues ();
				menuPlayerSelector [i].transform.Find ("KeyMap").gameObject.SetActive (false);
			}
			backGuide.SetActive (false);
			mainMenu.SetActive (true);
			characterSelect.SetActive (false);
			gameManager.StopCharSelection ();
			for (int i = 0; i < menuPlayerSelector.Count; i++) {
				menuPlayerSelector [i].GetComponent<SelectorBehaviour> ().ClearValues ();
			}
			menuMovementBehaviour.MainMenuOptionsNavigation (0);
			AkSoundEngine.SetSwitch ("MenuMusic", "MainMenu", gameObject);
		} else if (credits.activeSelf) {
			mainMenu.SetActive (true);
			credits.SetActive (false);
			AkSoundEngine.SetSwitch ("MenuMusic", "MainMenu", gameObject);
			menuMovementBehaviour.MainMenuOptionsNavigation (1);
		} else if (settings.activeSelf) {
			mainMenu.SetActive (true);
			settings.SetActive (false);
			backGuide.SetActive (false);
			AkSoundEngine.SetSwitch ("MenuMusic", "MainMenu", gameObject);
			menuMovementBehaviour.MainMenuOptionsNavigation (2);
		}
	}

	public void BackToMain(){
		acceptGuide.SetActive (true);
		mainMenu.SetActive (true);
		menuBackground.SetActive (true);
		AkSoundEngine.StopAll (gameObject);
		AkSoundEngine.SetSwitch ("MenuMusic", "MainMenu",gameObject);
		AkSoundEngine.SetState ("StateOfMusic", "Menu");
		AkSoundEngine.PostEvent ("Menu_music", gameObject);
		menuMovementBehaviour.MainMenuOptionsNavigation(0);
	}
	public void ShowCountdown(float time){
		StartCoroutine (Countdown (time));
	}
	private IEnumerator Countdown(float time){
		float currentTime = time;
		countdown.transform.parent.gameObject.SetActive (true);
		Text countdownText = countdown.GetComponent<Text> ();
		while (currentTime >= 0) {
			currentTime -= Time.unscaledDeltaTime;
			int rounded = Mathf.RoundToInt (currentTime);
			if (rounded == 0) {
				countdownText.text = "GO!";
			} else {
				countdownText.text = rounded.ToString ();
			}
			yield return null;
		}
		countdown.transform.parent.gameObject.SetActive (false);
	}
	public void GoNextPreview(PlayerPreview playerPreview){
		int currentPos = playerPreview.charPreviewPos;
		if(currentPos!=0){
			menuPlayerSelector [currentPos - 1].GetComponent<SelectorBehaviour> ().RemoveSelector ((Sprite)Resources.Load<Sprite> ("Selectors/" + playerPreview.playerNumber));
		}
		currentPos++;
		for (int i = 0; i < menuPlayerSelector.Count; i++) {
			if (currentPos > menuPlayerSelector.Count) {
				currentPos = 1;
			}
			SelectorBehaviour selector = menuPlayerSelector [currentPos - 1].GetComponent<SelectorBehaviour> ();
			if (!selector.selected) {
				selector.AddSelector ((Sprite)Resources.Load<Sprite> ("Selectors/" + playerPreview.playerNumber));
				playerPreview.charPreviewPos = currentPos;
				AkSoundEngine.PostEvent ("ChoosingPlayerSoundRight", gameObject);
				break;
			} else {
				currentPos++;
			}
		}
	}

	public void GoPreviousPreview(PlayerPreview playerPreview){
		int currentPos = playerPreview.charPreviewPos;
		if(currentPos!=0){
			menuPlayerSelector [currentPos - 1].GetComponent<SelectorBehaviour> ().RemoveSelector ((Sprite)Resources.Load<Sprite> ("Selectors/" + playerPreview.playerNumber));
		}
		currentPos--;
		for (int i = 0; i < menuPlayerSelector.Count; i++) {
			if (currentPos <= 0) {
				currentPos = menuPlayerSelector.Count;
			}
			SelectorBehaviour selector = menuPlayerSelector [currentPos - 1].GetComponent<SelectorBehaviour> ();
			if (!selector.selected) {
				selector.AddSelector ((Sprite)Resources.Load<Sprite> ("Selectors/" + playerPreview.playerNumber));
				playerPreview.charPreviewPos = currentPos;
				AkSoundEngine.PostEvent ("ChoosingPlayerSoundLeft", gameObject);
				break;
			} else {
				currentPos--;
			}
		}
	}

	public void ShowLasersAdvice(){
		StartCoroutine (ShowLasersAdv ());
	}
	private IEnumerator ShowLasersAdv(){
		laserAdvice.SetActive (true);
		yield return new WaitForSeconds (0.25f);
		laserAdvice.SetActive (false);
		yield return new WaitForSeconds (0.25f);
		laserAdvice.SetActive (true);
		yield return new WaitForSeconds (0.25f);
		laserAdvice.SetActive (false);
	}


	public void Credits(){
		mainMenu.SetActive (false);
		credits.SetActive (true);
		menuMovementBehaviour.StopMainMenuSelection ();
		AkSoundEngine.SetSwitch ("MenuMusic", "CreditsMenu",gameObject);

		StartCoroutine (WaitToExitCredits());
	}
	private IEnumerator WaitToExitCredits(){
		bool loop = true;
		int screenNumber = 1;
		credits.transform.Find (screenNumber.ToString()).gameObject.SetActive (true);
		while (loop) {
			for (int i = 0; i < InputManager.Devices.Count; i++) {
				if (InputManager.Devices [i].AnyButton.WasPressed) {
					screenNumber++;
					if (screenNumber > 3) {
						GoBack ();
						credits.transform.Find ((screenNumber-1).ToString()).gameObject.SetActive (false);
						loop = false;
					} else {
						credits.transform.Find ((screenNumber-1).ToString()).gameObject.SetActive (false);
						credits.transform.Find (screenNumber.ToString()).gameObject.SetActive (true);
					}
				}
			}
			yield return null;
		}
	}

	public void StopLasersAdvice(){
		StopCoroutine (ShowLasersAdv ());
		laserAdvice.SetActive (false);
	}
	public bool SelectPreview(PlayerPreview playerPreview, PlayerInput playerInput){
		SelectorBehaviour selector = menuPlayerSelector [playerPreview.charPreviewPos-1].GetComponent<SelectorBehaviour> ();
		if (!selector.selected) {
			selector.SelectSelector ((Sprite)Resources.Load<Sprite> ("Selectors/" + playerPreview.playerNumber));
			selector.transform.Find ("Selected").gameObject.SetActive (true);
			GameObject keySelector = selector.transform.Find ("KeyMap").gameObject;
			Sprite inputMap = ((Sprite)Resources.Load<Sprite> ("InputMap/" + InputManager.Devices [playerInput.inputNumber].Name));
			if (inputMap == null) {
				inputMap = ((Sprite)Resources.Load<Sprite> ("InputMap/XBox One Controller"));
			}
			keySelector.GetComponent<Image> ().sprite = inputMap;
			keySelector.SetActive (true);
			playerPreview.selected = true;
			AkSoundEngine.PostEvent ("SelectedPlayerSound", gameObject);
			return true;
		} else {
			//Reproducir sonido
			return false;
		}
	}

	public void UnselectPreview(PlayerPreview playerPreview){
		SelectorBehaviour selector = menuPlayerSelector [playerPreview.charPreviewPos-1].GetComponent<SelectorBehaviour> ();
		if (selector.selected) {
			selector.UnselectSelector();
			selector.transform.Find ("Selected").gameObject.SetActive (false);
			GameObject keySelector = selector.transform.Find ("KeyMap").gameObject;
			keySelector.SetActive (false);
			playerPreview.selected = false;
		}
	}

	public void SetPossiblePlayers(int possiblePlayers){
		this.possiblePlayers = possiblePlayers;
		for (int i = 1; i < possiblePlayers+1; i++) {
			availableStartKeys.Add ((Sprite)Resources.Load<Sprite> ("Keys/" + Inputs.Start+i));
		}
	}

	public void ExitGame(){
		Application.Quit ();
	}
}
