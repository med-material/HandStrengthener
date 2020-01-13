using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProgressIndication : MonoBehaviour
{
    [SerializeField]
    private RectTransform inputWindow;
    
    [SerializeField]
    private RectTransform positionPusher;

    [SerializeField]
    private RectTransform progressBar;

    private float inputWindowDuration;
    private float interTrialDuration;
    private float progressBarDuration;
    private float progressBarTime = 0f; // goes from 0 to 1?


    // Start is called before the first frame update
    void Start()
    {
        positionPusher.sizeDelta = new Vector2(0f, positionPusher.sizeDelta.y);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnGameTimeUpdate(GameTimers gameTimers) {
        // Update Progress Indication
        if (gameTimers.inputWindowTimer > 0f) {
            float progress = (interTrialDuration + gameTimers.inputWindowTimer) / progressBarDuration;
            float newPosition = progress * progressBar.sizeDelta.x;
            positionPusher.sizeDelta = new Vector2(newPosition, positionPusher.sizeDelta.y);
        } else if (gameTimers.interTrialTimer > 0f) {
            float progress = gameTimers.interTrialTimer / progressBarDuration;
            float newPosition = progress * progressBar.sizeDelta.x;
            positionPusher.sizeDelta = new Vector2(newPosition, positionPusher.sizeDelta.y);
        }
    }

    public void OnGameStateChanged(GameData gameData) {
        // Set InputWindow, InputTime indication.
        progressBarDuration = gameData.interTrialIntervalSeconds + gameData.inputWindowSeconds;
        inputWindowDuration = gameData.inputWindowSeconds;
        interTrialDuration = gameData.interTrialIntervalSeconds;

        // Calculate visual size of input window.
        float inputWindowRatio = inputWindowDuration / progressBarDuration;
        float newInputWindowSize = inputWindowRatio * progressBar.sizeDelta.x;
        inputWindow.sizeDelta = new Vector2(newInputWindowSize, inputWindow.sizeDelta.y);
    }
}
