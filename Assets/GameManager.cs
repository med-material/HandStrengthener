using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public struct GameData {
    public int trials;
    public float interTrialIntervalSeconds;
    public float inputWindowSeconds;
    public GameState gameState;
    public float noInputReceivedFabAlarm;
    public float fabAlarmVariability;
}

public class Mechanism {
    public string name = "";
    public TrialType trialType;
    public float rate = -1f;
    public int trialsLeft = -1;
    public int trials = -1;
    public UrnEntryBehavior behavior;
}

public class GameDecisionData {
    public TrialType decision;
    public float currentFabAlarm;
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
     AssistSuccess,
     AssistFail,
     ExplicitSham
}

public class GameManager : MonoBehaviour
{

    [Header("Trial Setup")]
	[Tooltip("The total number of trials is calculated from the trial counts set here.")]
    public int rejTrials = 5;
    public int accTrials = 10;
    public int fabTrials = 5;
    public int assistSuccessTrials = 0;
    public int assistFailTrials = 0;
    public int explicitShamTrials = 0;

    private int trialsTotal = -1;
    private int currentTrial = -1;
    private TrialType trialResult = TrialType.RejInput;
    private TrialType trialGoal = TrialType.RejInput;

    private Dictionary<string, Mechanism> mechanisms = new Dictionary<string, Mechanism>();

    [Header("FabInput Settings")]
    [Tooltip("When should the fabrication fire.")]
    [SerializeField]
    private float noInputReceivedFabAlarm = 0.5f; // fixed alarm in seconds relative to input window, at what point should we try and trigger fab input.
    [SerializeField]
    private float fabAlarmVariability = 0.5f; //added delay variability to make the alarm unpredictable.
    private float currentFabAlarm = 0f;
    private bool alarmFired = false;
    private int fabInputNumber = 0;


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
        SetupMechanisms();
        SetupUrn();
        LogMeta();
    }

    private void SetupMechanisms() {
        mechanisms["AccInput"] = new Mechanism {
            name = "AccInput",
            trialType = TrialType.AccInput,
            rate = 0f,
            trials = accTrials,
            trialsLeft = accTrials,
            behavior = UrnEntryBehavior.Persist
        };

        mechanisms["FabInput"] = new Mechanism {
            name = "FabInput",
            trialType = TrialType.FabInput,
            rate = 0f,
            trials = fabTrials,
            trialsLeft = fabTrials,
            behavior = UrnEntryBehavior.Persist
        };

        mechanisms["RejInput"] = new Mechanism {
            name = "RejInput",
            trialType = TrialType.RejInput,
            rate = 0f,
            trials = rejTrials,
            trialsLeft = rejTrials,
            behavior = UrnEntryBehavior.Override
        };
        mechanisms["AssistSuccess"]  = new Mechanism {
            name = "AssistSuccess",
            trialType = TrialType.AssistSuccess,
            rate = 0f,
            trials = assistSuccessTrials,
            trialsLeft = assistSuccessTrials,
            behavior = UrnEntryBehavior.Persist
        };
        mechanisms["AssistFail"]  = new Mechanism {
            name = "AssistFail",
            trialType = TrialType.AssistFail,
            rate = 0f,
            trials = assistFailTrials,
            trialsLeft = assistFailTrials,
            behavior = UrnEntryBehavior.Persist
        };
        mechanisms["ExplicitSham"]  = new Mechanism {
            name = "ExplicitSham",
            trialType = TrialType.ExplicitSham,
            rate = 0f,
            trials = explicitShamTrials,
            trialsLeft = explicitShamTrials,
            behavior = UrnEntryBehavior.Persist
        };
    }

    private void SetupUrn() {
        trialsTotal = 0;
        foreach (KeyValuePair<string, Mechanism> pair in mechanisms) {
            var m = pair.Value;
            urn.AddUrnEntryType(m.name, m.behavior, m.trials);
            trialsTotal += m.trials;
        }

        urn.NewUrn();
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
            {"Event", eventLabel},
            {"InputWindow", System.Enum.GetName(typeof(InputWindowState), inputWindow)},
            {"InputWindowOrder", inputIndex},
            {"InterTrialTimer", interTrialTimer},
            {"InputWindowTimer", inputWindowTimer},
            {"GameState", System.Enum.GetName(typeof(GameState), gameState)},
            {"CurrentFabAlarm", currentFabAlarm},
        };

        foreach (KeyValuePair<string, Mechanism> pair in mechanisms) {
            var m = pair.Value;
            gameLog[m.name + "TrialsLeft"] = m.trialsLeft;
            gameLog[m.name + "Rate"] = m.rate;
        }

        if (eventLabel == "GameDecision") {
            gameLog["TrialGoal"] = trialGoal;
            gameLog["TrialResult"] = trialResult;
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
                    InputData fabInputData = new InputData {
                        validity = InputValidity.Accepted,
                        type = InputType.FabInput,
                        inputNumber = fabInputNumber
                    };
                    MakeInputDecision(fabInputData, false);
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
            gameData.trials = trialsTotal;
            gameData.interTrialIntervalSeconds = interTrialIntervalSeconds;
            gameData.inputWindowSeconds = inputWindowSeconds;
            gameData.gameState = gameState;
            gameData.noInputReceivedFabAlarm = noInputReceivedFabAlarm;
            gameData.fabAlarmVariability = fabAlarmVariability;
            return gameData;
    }

    void OnApplicationQuit() {
        if(gameState != GameState.Stopped) {
            EndGame();
        }
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
        loggingManager.ClearAllLogs();
    }

    public void CalculateRecogRate() {
        var entriesLeft = urn.GetEntriesLeft();

        foreach (KeyValuePair<string, Mechanism> pair in mechanisms) {
            var m = pair.Value;
            m.trialsLeft = entriesLeft[m.name];
        }
        
        var entryResults = urn.GetEntryResults();

        foreach (KeyValuePair<string, Mechanism> pair in mechanisms) {
            var m = pair.Value;
            m.rate = (float) entryResults[m.name] / (float) trialsTotal;
        }

        currentTrial = urn.GetIndex();
    }

    public void OnInputReceived(InputData inputData) {
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
        trialResult = TrialType.RejInput;
        
        if (inputData != null) {
            if (inputData.type == InputType.FabInput) {
                if (trialGoal == TrialType.FabInput) {
                    trialResult = TrialType.FabInput;
                    CloseInputWindow();
                } else if (trialGoal == TrialType.ExplicitSham) {
                    trialResult = TrialType.ExplicitSham;
                    CloseInputWindow();
                }
            } else if (trialGoal == TrialType.AccInput) {
                if (inputData.validity == InputValidity.Accepted) {
                    trialResult = TrialType.AccInput;
                    CloseInputWindow();
                } else {
                    trialResult = TrialType.RejInput;
                }
            } else if (trialGoal == TrialType.RejInput) {
                trialResult = TrialType.RejInput;
                // ignore the input.
            } else if (trialGoal == TrialType.AssistSuccess) {
                if (inputData.validity == InputValidity.Accepted) {
                    trialResult = TrialType.AssistSuccess;
                    CloseInputWindow();
                } else {
                    trialResult = TrialType.RejInput;
                }
            } else if (trialGoal == TrialType.AssistFail) {
                trialResult = TrialType.AssistFail;
                // ignore the input.
            }
        } else if (windowExpired) {
                CloseInputWindow();
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
