﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
public class MenuManager : GenericSingletonClass<MenuManager> {
	public List<GameObject> menuPlayerPreview = new List<GameObject>();

	GameManager gameManager;
	GameObject pressStart;
	GameObject mainMenu;
	GameObject characterSelect;
	GameObject mapSelect;
	new void Awake(){
		base.Awake ();
	}
	void Start () {
		gameManager = GameManager.Instance;
		pressStart = transform.Find ("PressStart").gameObject;
		mapSelect = transform.Find ("MapSelect").gameObject;
		mainMenu = transform.Find ("MainMenu").gameObject;
		characterSelect = transform.Find ("CharacterSelect").gameObject;
		for (int i = 0; i < menuPlayerPreview.Count; i++) {
			menuPlayerPreview[i] = characterSelect.transform.Find ("Character" + (i + 1)).gameObject;
		}
		gameManager.PressStartButton ();
	}

	public void StartPressed(){
		pressStart.SetActive (false);
		mainMenu.SetActive (true);
	}

	public void CharacterSelectionFinished(){
		characterSelect.SetActive (false);
		gameManager.GameStart("Map1");
	}

	public void PlayButton(){
		mainMenu.SetActive (false);
		characterSelect.SetActive (true);
		gameManager.CharSelection ();
	}

	public void BackToMain(){
		mainMenu.SetActive (true);

	}
	public void SetImagePreview(PlayerPreview playerPreview){
		menuPlayerPreview [playerPreview.playerNumber-1].GetComponent<Image> ().sprite = playerPreview.charPreview;
	}
	public string GetCharacterFromPreview(int playerNumber){
		return menuPlayerPreview [playerNumber].GetComponent<Image> ().sprite.name;
	}
}