using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public struct GameData {
    public float fabRate;
    public float accRate;
    public float startPolicyReview;
    public int trials;
    public float interTrialIntervalSeconds;
    public float inputWindowSeconds;
    public GameState gameState;
    public float noInputReceivedFabAlarm;
    public float fabAlarmVariability;
}

public class InputData {
    public InputValidity validity;
    public InputType type;
    public float confidence;
    public int inputNumber;
}

public enum InputValidity {
    Accepted,
    Rejected
}

public enum InputType {
    KeySequence,
    MotorImagery,
    BlinkDetection
}

public class GameDecisionData {
    public TrialType decision;
    public float currentFabAlarm;
    public float accRate;
    public float fabRate;
    public float rejRate;
}

public struct GameTimers {
    public float inputWindowTimer;
    public float interTrialTimer;
}

public enum InputWindowState {
    Closed,
    Open,
}

public enum GameState {
    Running,
    Paused,
    Stopped,
}

public enum TrialType  {
     AccInput,
     FabInput,
     RejInput,
}

public class GameManager : MonoBehaviour
{

    [Header("Trial Setup")]
	[Tooltip("The total number of trials is calculated from the trial counts set here.")]
    public int rejTrials = 5;
    public int accTrials = 10;
    public int fabTrials = 5;

    private float fabRate = -1f;
    private float accRate = -1f;
    private float rejRate = -1f;
    private int rejTrialsLeft = -1;
    private int accTrialsLeft = -1;
    private int fabTrialsLeft = -1;

    private int trialsTotal = -1;
    private int currentTrial = -1;
    private TrialType trialResult = TrialType.RejInput;
    private TrialType trialGoal = TrialType.RejInput;

    [Header("FabInput Settings")]
    [Tooltip("When should the fabrication fire.")]
    [SerializeField]
    private float noInputReceivedFabAlarm = 0.5f; // fixed alarm in seconds relative to input window, at what point should we try and trigger fab input.
    [SerializeField]
    private float fabAlarmVariability = 0.5f; //added delay variability to make the alarm unpredictable.
    private float currentFabAlarm = 0f;
    private bool alarmFired = false;


    [Header("InputWindow Settings")]
    [Tooltip("Length of Window and Inter-trial interval.")]
    [SerializeField]
    private float interTrialIntervalSeconds = 4.5f;
    [SerializeField]
    private float inputWindowSeconds = 1f;
    private float inputWindowTimer = 0.0f;
    private float interTrialTimer = 0.0f;
    private InputWindowState inputWindow = InputWindowState.Closed;
    private int inputIndex = 0;

    private GameState gameState = GameState.Stopped;

    [Serializable]
    public class OnGameStateChanged : UnityEvent<GameData> { }
    public OnGameStateChanged onGameStateChanged;
    [Serializable]
    public class GameDecision : UnityEvent<GameDecisionData> { }
    public GameDecision gameDecision;

    [Serializable]
    public class OnInputWindowChanged : UnityEvent<InputWindowState> { }
    public OnInputWindowChanged onInputWindowChanged;

    [Serializable]
    public class OnGameTimeUpdate : UnityEvent<GameTimers> { }
    public OnGameTimeUpdate onGameTimeUpdate;

    private LoggingManager loggingManager;
    private UrnModel urn;

    void Start()
    {
        loggingManager = GameObject.Find("LoggingManager").GetComponent<LoggingManager>();
        urn = GetComponent<UrnModel>();
        SetupUrn();
        LogMeta();
    }

    private void SetupUrn() {
        urn.AddUrnEntryType("FabInput", UrnEntryBehavior.Persist, fabTrials);
        urn.AddUrnEntryType("AccInput", UrnEntryBehavior.Persist, accTrials);
        urn.AddUrnEntryType("RejInput", UrnEntryBehavior.Override, rejTrials);

        urn.NewUrn();

        trialsTotal = rejTrials + accTrials + fabTrials;
        currentTrial = 0;
    }

    private void LogMeta() {
        Dictionary<string, object> metaLog = new Dictionary<string, object>() {
            {"FabInputTrials", fabTrials},
            {"AccInputTrials", accTrials},
            {"RejInputTrials", rejTrials},
            {"Trials", trialsTotal},
            {"InterTrialInterval_sec", interTrialIntervalSeconds},
            {"InputWindow_sec", inputWindowSeconds},
            {"noInputReceivedFabAlarm_sec", noInputReceivedFabAlarm},
            {"FabAlarmVariability_sec", fabAlarmVariability},
        };
        loggingManager.Log("Meta", metaLog);
    }

    private void LogEvent(string eventLabel) {
        Dictionary<string, object> gameLog = new Dictionary<string, object>() {
            {"Timestamp", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff")},
            {"Event", eventLabel},
            {"InputWindow", System.Enum.GetName(typeof(InputWindowState), inputWindow)},
            {"InputWindowOrder", inputIndex},
            {"InterTrialTimer", interTrialTimer},
            {"InputWindowTimer", inputWindowTimer},
            {"GameState", System.Enum.GetName(typeof(GameState), gameState)},
            {"CurrentAcceptRate", accRate},
            {"CurrentFabRate", fabRate},
            {"CurrentRejectRate", rejRate},
            {"AccInputTrialsLeft", accTrialsLeft},
            {"FabInputTrialsLeft", fabTrialsLeft},
            {"RejInputTrialsLeft", rejTrialsLeft},
            {"CurrentFabAlarm", currentFabAlarm},
        };

        if (eventLabel == "GameDecision") {
            gameLog["TrialResult"] = trialResult;
            gameLog["CurrentDesignGoal"] = trialGoal;
        } else {
            gameLog["TrialResult"] = "NA";
        }

        loggingManager.Log("Game", gameLog);
    }

    // Update is called once per frame
    void Update()
    {
        if (gameState == GameState.Running) {
            if (inputWindow == InputWindowState.Closed) {
                alarmFired = false;
                interTrialTimer += Time.deltaTime;
                if (interTrialTimer > interTrialIntervalSeconds && currentTrial < trialsTotal) {
                    interTrialTimer = 0f;
                    inputWindow = InputWindowState.Open;
                    SetFabAlarmVariability();
                    onInputWindowChanged.Invoke(inputWindow);
                    LogEvent("InputWindowChange");
                } else if (interTrialTimer > interTrialIntervalSeconds) {
                    EndGame();
                }
            } else if (inputWindow == InputWindowState.Open) {
                //Debug.Log("inputwindow is open");
                inputWindowTimer += Time.deltaTime;
                if (inputWindowTimer > currentFabAlarm && alarmFired == false) {
                   //Debug.Log("inputWindowTimer exceeded currentFabAlarm.");
                    // Fire fabricated input (if scheduled).
                    MakeInputDecision(null, false);
                    alarmFired = true;
                } else if (inputWindowTimer > inputWindowSeconds) {
                   //Debug.Log("inputWindow expired.");
                    // The input window expired
                    MakeInputDecision(null, true);
                    alarmFired = false;
                }
            }
        }
        GameTimers gameTimers = new GameTimers();
        gameTimers.interTrialTimer = interTrialTimer;
        gameTimers.inputWindowTimer = inputWindowTimer;
        onGameTimeUpdate.Invoke(gameTimers);
    }

    public void SetFabAlarmVariability() {
        currentFabAlarm = UnityEngine.Random.Range(noInputReceivedFabAlarm-fabAlarmVariability, noInputReceivedFabAlarm+fabAlarmVariability);
    }

    public GameData createGameData() {
            GameData gameData = new GameData();
            gameData.fabRate = fabRate;
            gameData.accRate = accRate;
            //gameData.startPolicyReview = startPolicyReview;
            gameData.trials = trialsTotal;
            gameData.interTrialIntervalSeconds = interTrialIntervalSeconds;
            gameData.inputWindowSeconds = inputWindowSeconds;
            gameData.gameState = gameState;
            gameData.noInputReceivedFabAlarm = noInputReceivedFabAlarm;
            gameData.fabAlarmVariability = fabAlarmVariability;
            return gameData;
    }

    public void RunGame() {
        CalculateRecogRate();
        gameState = GameState.Running;
        GameData gameData = createGameData();
        onGameStateChanged.Invoke(gameData);
        LogEvent("GameRunning");
    }

    public void EndGame() {
        interTrialTimer = 0f;
        if (inputWindow == InputWindowState.Open) {
            CloseInputWindow();
        }
        gameState = GameState.Stopped;
        GameData gameData = createGameData();
        onGameStateChanged.Invoke(gameData);
        LogEvent("GameStopped");
        loggingManager.SaveLog("Game");
        loggingManager.SaveLog("Sample");
        loggingManager.SaveLog("Meta");
    }

    public void CalculateRecogRate() {
        var entriesLeft = urn.GetEntriesLeft();
        fabTrialsLeft = entriesLeft["FabInput"];
        accTrialsLeft = entriesLeft["AccInput"];
        rejTrialsLeft = entriesLeft["RejInput"];
        
        var entryResults = urn.GetEntryResults();

        accRate = (float) entryResults["AccInput"] / (float) trialsTotal;
        fabRate = (float) entryResults["FabInput"] / (float) trialsTotal;
        rejRate = (float) entryResults["RejInput"] / (float) trialsTotal;
        currentTrial = urn.GetIndex();
    }

    public void OnInputReceived(InputData inputData) {
        Debug.Log("input Data received");
        if (inputWindow == InputWindowState.Closed) {
            // ignore the input.
            return;
        } else {
            MakeInputDecision(inputData);
        }
    }

    public void CloseInputWindow() {
        // update the window state.
        inputWindow = InputWindowState.Closed;
        interTrialTimer -= (inputWindowSeconds - inputWindowTimer);
        inputWindowTimer = 0f;
        onInputWindowChanged.Invoke(inputWindow);
        LogEvent("InputWindowChange");

        // store the input decision.
        urn.SetEntryResult(System.Enum.GetName(typeof(TrialType), trialResult));

        CalculateRecogRate();
        // Send Decision Data
        GameDecisionData gameDecisionData = new GameDecisionData();
        gameDecisionData.accRate = accRate;
        gameDecisionData.fabRate = fabRate;
        gameDecisionData.rejRate = rejRate;
        gameDecisionData.currentFabAlarm = currentFabAlarm;
        gameDecisionData.decision = trialResult;
        gameDecision.Invoke(gameDecisionData);
        LogEvent("GameDecision");
       ////Debug.Log("designedInputOrder: " + designedInputOrder.Count);
       ////Debug.Log("actualInputOrder: " + actualInputOrder.Count);
       ////Debug.Log("Decision: " + System.Enum.GetName(typeof(InputTypes), currentInputDecision));
        //UpdateDesignedInputOrder();
        inputIndex++;
    }

    public void MakeInputDecision(InputData inputData = null, bool windowExpired = false) {
        string entry = urn.ReadEntry();
        trialGoal = (TrialType) System.Enum.Parse(typeof(TrialType), entry);
        if (inputData != null) {
            if (trialGoal == TrialType.AccInput) {
                if (inputData.validity == InputValidity.Accepted) {
                    trialResult = TrialType.AccInput;
                } else {
                    trialResult = TrialType.RejInput;
                }
                CloseInputWindow();
            } else if (trialGoal == TrialType.RejInput) {
                trialResult = TrialType.RejInput;
                // ignore the input.
            }
        } else {
            if (trialGoal == TrialType.FabInput) {
                trialResult = TrialType.FabInput;
                CloseInputWindow();
            } else if (windowExpired) {
                trialResult = TrialType.RejInput;
                CloseInputWindow();
            }
        }
    }

    public void PauseTrial() {
        gameState = GameState.Paused;
    }

    public void ResetTrial() {
        inputWindowTimer = 0f;
        interTrialTimer = 0.001f;
        inputWindow = InputWindowState.Closed;
    }

    public void ResumeTrial() {
        gameState = GameState.Running;
    }

    public void SetInputWindowSeconds(float time) {
        inputWindowSeconds = time;
        GameData gameData = createGameData();
        onGameStateChanged.Invoke(gameData);        
    }

    public void SetInterTrialSeconds(float time) {
        interTrialIntervalSeconds = time;
        GameData gameData = createGameData();
        onGameStateChanged.Invoke(gameData);
    }

}
