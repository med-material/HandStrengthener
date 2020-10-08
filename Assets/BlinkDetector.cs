using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Tobii.Gaming;
using System;
using System.IO;

public enum EyeState {
    EyesOpen,
    EyesClosed,
    Unintialized
}

public enum DetectorState {
    Started,
    Stopped
}

public class BlinkDetector : MonoBehaviour
{
    private EyeState eyeState = EyeState.Unintialized;
    private DetectorState state = DetectorState.Stopped;

    private float duration = 0f;
    private string timestamp = "Unavailable";

    [Serializable]
    public class OnBlink : UnityEvent<InputData> { }
    public OnBlink onBlink;

    private int blinkNo = 0;

    // TODO, get where people are looking?

    private LoggingManager loggingManager;

    void Start() {
        loggingManager = GameObject.Find("LoggingManager").GetComponent<LoggingManager>();
    }

    void Update()
    {
        if (state == DetectorState.Started) {
            // If eyes are OPEN
            Debug.Log(TobiiAPI.GetGazePoint().IsRecent(0.1f));
            Debug.Log("eyestate: " + Enum.GetName(typeof(EyeState), eyeState));
            if (TobiiAPI.GetGazePoint().IsRecent(0.1f))
            {
                if (eyeState == EyeState.EyesClosed || eyeState == EyeState.Unintialized) {
                    eyeState = EyeState.EyesOpen;
                    LogEyeOpen();
                    blinkNo++;
                    InputData inputData = new InputData();
                    inputData.validity = InputValidity.Accepted;
                    inputData.type = InputType.BlinkDetection;
                    inputData.confidence = 1f;
                    inputData.inputNumber = blinkNo;
                    onBlink.Invoke(inputData);
                }
                duration = 0f;
            } else
            {
                if (eyeState == EyeState.EyesOpen || eyeState == EyeState.Unintialized) {
                    eyeState = EyeState.EyesClosed;
                    timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff");
                    LogEyeClose();
                }
                duration += Time.deltaTime;
            }
        }
    }

    private void LogEyeOpen() {
        loggingManager.Log("BlinkLog", "Event", "EyeOpening");
        loggingManager.Log("BlinkLog", "BlinkNo", blinkNo);
        loggingManager.Log("BlinkLog", "DurationClosed_s", duration);
        loggingManager.SaveLog("BlinkLog");
        loggingManager.ClearLog("BlinkLog");
    }

    private void LogEyeClose() {
        //loggingManager.Log("BlinkLog", "TimestampEye", timestamp);
        loggingManager.Log("BlinkLog", "Event", "EyeClosing");
        loggingManager.Log("BlinkLog", "BlinkNo", blinkNo);
        loggingManager.SaveLog("BlinkLog");
        loggingManager.ClearLog("BlinkLog");
    }

    public void StartBlinkDetection() {
        state = DetectorState.Started;
    }

    public void StopBlinkDetection() {
        state = DetectorState.Stopped;
    }
}