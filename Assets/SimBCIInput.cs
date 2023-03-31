using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

/*
 * Activate by holding down button.
 * Outputs random raw BCI data from real BCI captured data and an event.
 * 
 */

public class SimBCIInput : MonoBehaviour
{

    private MotorImageryEvent classification = MotorImageryEvent.Rest;
    public float classificationThreshold = 0.7f;

    private int[] consecThresholdBuffer;
    private float[] consecThresholdBufferVal;
    private int consecThresholdIndex = 0;
    public int  consecutiveBufferSize = 8;


    private int testSampleChannelSize;
    private int testSampleCount;
    private int testChannelCount;
    private float confidence;

    private double[,] lastMatrix;

    private RawOpenVibeSignal lastSignal;

    public class RawOpenVibeSignal {

        public int channels;
        public int samples;
        public double[,] signalMatrix;
    }

    public enum BCIState {
        Disconnected,
        Connecting,
        ReceivingHeader,
        ReceivingData
    }

    private BCIState bciState;
    public string motorImageryEvent;
    private int inputNumber = 0;

    public enum BCIProcessingMode {
        SingleThreshold,
        ConsecutiveThreshold,
    }
    public BCIProcessingMode bciProcessingMode = BCIProcessingMode.SingleThreshold;

    [Serializable]
    public class OnBCIStateChanged : UnityEvent<string, string> { }

    [Serializable]
    public class OnBCIMotorImagery : UnityEvent<MotorImageryEvent> { }

    [Serializable]
    public class OnInputFinished : UnityEvent<InputData> { }

    [Serializable]
    public class OnBCIEvent : UnityEvent<float> { }

    public TextAsset BCIInput;
    private string[] confList;
    private int confPosition;
    private int maxConfPosition;
    private float timer = 0f;
    private float waitTime = 0.1f;

    private Dictionary<string, List<string>> BCILogs;

    public OnBCIStateChanged onBCIStateChanged;
    public OnBCIMotorImagery onBCIMotorImagery;
    public OnInputFinished onInputFinished;
    public OnBCIEvent onBCIEvent;

    private static SimBCIInput instance;

    private LoggingManager loggingManager;

    void Start()
    {
        if (instance == null) {
            instance = this;
        }
        DontDestroyOnLoad(this);
        consecThresholdBuffer = new int[consecutiveBufferSize];
        consecThresholdBufferVal = new float[consecutiveBufferSize];
        bciState = BCIState.Disconnected;
        loggingManager = GameObject.Find("LoggingManager").GetComponent<LoggingManager>();
        LogMeta();
        onBCIStateChanged.Invoke(Enum.GetName(typeof(BCIState), bciState), "");
        StartCoroutine("ConnectToBCI");
        inputNumber = 0;
        confList = BCIInput.text.Split('\n');
        maxConfPosition = confList.Length;
        confPosition = UnityEngine.Random.Range(0,maxConfPosition);
    }

    private void LogMeta() {
        Dictionary<string, object> metaLog = new Dictionary<string, object>() {
            {"ConfidenceThreshold", classificationThreshold},
            {"BCIProcessingMode", Enum.GetName(typeof(BCIProcessingMode), bciProcessingMode)},
            {"ConsecutiveThresholdBufferSize", consecutiveBufferSize},
        };
        loggingManager.Log("Meta", metaLog);
    }

    private void LogMotorImageryEvent(MotorImageryEvent miEvent = MotorImageryEvent.Rest, float lastConfidence = -1f) {
        Dictionary<string, object> gameLog = new Dictionary<string, object>() {
            {"Event", Enum.GetName(typeof(MotorImageryEvent), miEvent)},
            {"BCIConfidence", lastConfidence},
            {"BCIState", Enum.GetName(typeof(BCIState), bciState)},
            {"InputNumber", inputNumber},
        };
        loggingManager.Log("Game", gameLog);
        if (bciProcessingMode == BCIProcessingMode.ConsecutiveThreshold) {
            string buffer = "(";
            foreach(float t in consecThresholdBufferVal) {
                buffer += t.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + " ";
            }
            buffer += ")";
            loggingManager.Log("Game", "BCIThresholdBuffer", buffer);
        }
    }

    private void LogStateEvent() {
        Dictionary<string, object> gameLog = new Dictionary<string, object>() {
            {"Event", "BCIStateUpdated"},
            {"BCIState", Enum.GetName(typeof(BCIState), bciState)},
        };
        loggingManager.Log("Game", gameLog);
    }

    private void LogSample(string eventLabel) {
        Dictionary<string, object> sampleLog = new Dictionary<string, object>() {
            {"Event", eventLabel},
            {"BCIConfidence", confidence},
            {"BCIState", Enum.GetName(typeof(BCIState), bciState)},
        };
        loggingManager.Log("Sample", sampleLog);
    }

    void Update()
    {
        if (bciState == BCIState.Disconnected) {
            return;
        }
        timer += Time.deltaTime;
        if (Input.GetKey(KeyCode.V) && timer > waitTime)
        {
            timer = 0f;
            confidence = float.Parse(confList[confPosition], System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
            Debug.Log(confidence);
            if (confPosition < maxConfPosition) {
                confPosition++;
            } else {
                confPosition = 0;
            }
        }
        if (Input.GetKeyUp(KeyCode.V)) {
            confPosition = UnityEngine.Random.Range(0,maxConfPosition);
        }

       if (confidence == -1f) {
           // No Stream available.
           return;
       }
       // Update() runs faster (1/60) than our input data (1/16) arrives.
       // The code below is only run whenever a new value comes in from the BCI side.
       LogSample("Sample");
       InputData inputData = new InputData();
       inputData.confidence = 1 - confidence;
       inputData.type = InputType.MotorImagery;
       MotorImageryEvent newClassification = MotorImageryEvent.Rest;
       inputData.validity = InputValidity.Rejected;
       if (bciProcessingMode == BCIProcessingMode.SingleThreshold) {
           newClassification = ProcessSingleThreshold(confidence);
       } else if (bciProcessingMode == BCIProcessingMode.ConsecutiveThreshold) {
           newClassification = ProcessConsecutiveThreshold(confidence);
       }
       if (newClassification != classification) {
            if (newClassification == MotorImageryEvent.MotorImagery) {
                inputData.validity = InputValidity.Accepted;
                inputNumber++;
            }
           inputData.inputNumber = inputNumber;
           LogMotorImageryEvent(newClassification, confidence);
           onBCIMotorImagery.Invoke(newClassification);
           onInputFinished.Invoke(inputData);
           classification = newClassification;
       }
       if (confidence != 0f) { 
        onBCIEvent.Invoke(confidence);
       }
       
    }

    private MotorImageryEvent ProcessSingleThreshold(float confidence) {
       MotorImageryEvent newClassification = MotorImageryEvent.Rest;
       if (confidence > classificationThreshold) {
           newClassification = MotorImageryEvent.MotorImagery;
       }
       return newClassification;
    }

    private MotorImageryEvent ProcessConsecutiveThreshold(float confidence) {
        MotorImageryEvent newClassification = MotorImageryEvent.Rest;

        // If our confidence value is higher than the threshold, add a 1 to the buffer.
        if (confidence > classificationThreshold) {
            consecThresholdBuffer[consecThresholdIndex] = 1;
            consecThresholdBufferVal[consecThresholdIndex] = confidence;
        } else {
            consecThresholdBuffer[consecThresholdIndex] = 0;
            consecThresholdBufferVal[consecThresholdIndex] = confidence;
        }

        // if all positions in the buffer carry a 1, we have motor imagery.
        if (consecThresholdBuffer.Sum() == consecutiveBufferSize) {
            newClassification = MotorImageryEvent.MotorImagery;
        }

        // Increment our buffer index.
        if (consecThresholdIndex < consecutiveBufferSize-1) {
            consecThresholdIndex++;
        } else {
            consecThresholdIndex = 0;
        }

        return newClassification;
    }

    private IEnumerator ConnectToBCI() {
        bciState = BCIState.Connecting;
        LogStateEvent();
        onBCIStateChanged.Invoke(Enum.GetName(typeof(BCIState), bciState), "Establishing connection to BCI Socket..");     
        yield return new WaitForSeconds(0.5f);
        bciState = BCIState.ReceivingHeader;
        LogStateEvent();
        onBCIStateChanged.Invoke(Enum.GetName(typeof(BCIState), bciState), "Waiting for data..Make sure that Acquisition is paired with PC.");
        yield return null;
    }

}
