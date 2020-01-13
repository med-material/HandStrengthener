using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

public class ExperimentTimer : MonoBehaviour
{
    private Text timerText;
    private string timerTextTemplate;
    private bool startTimer = false;

    private DateTime experimentStart;
    private DateTime experimentEnd;
    
    // Start is called before the first frame update
    void Start()
    {
        timerText = this.GetComponent<Text>();
        timerTextTemplate = timerText.text;
    }

    // Update is called once per frame
    void Update()
    {
        if (startTimer) {
            TimeSpan timeSpan = System.DateTime.Now.Subtract(experimentStart);
            timerText.text = string.Format("{0:D2}:{1:D2}.{2:D2}", timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
        }
        
    }

    public void onGameStateChanged(GameData gameData) {
        if (gameData.gameState == GameState.Running) {
            StartTimer();
        } else {
            ResetTimer();
        }
    }

    public void StartTimer() {
        startTimer = true;
        experimentStart = System.DateTime.Now;
    }

    public void ResetTimer() {
        startTimer = false;
        timerText.text = timerTextTemplate;
        experimentEnd = System.DateTime.Now;
    }
}

