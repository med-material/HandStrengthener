using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public struct GameData {
    public float fabInputRate;
    public float recognitionRate;
    public float startPolicyReview;
    public int trials;
    public float interTrialIntervalSeconds;
    public float inputWindowSeconds;
    public GameState gameState;
    public float noInputReceivedFabAlarm;
    public GamePolicy gamePolicy;
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
    public InputTypes decision;
    public float currentFabAlarm;
    public float currentRecogRate;
    public float currentFabRate;
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

public enum InputTypes {
    AcceptAllInput,
    FabInput,
    RejectAllInput,
}

public enum SetupType {
    Recog100,
    Fab0,
    Fab15,
    Fab30,
    Fab50,
}

public struct GamePolicyData {
    public GamePolicy gamePolicy;
}

public enum GamePolicy {
    StrictOperation, // this is equivalent to BCI.
    MeetDesignGoals, // this is equivalent to Fab.Input.
    LooseOperation // this just accepts whatever input comes in regardless of validity.
}

public class GameManager : MonoBehaviour
{

    // TODO for the future: Fixate fabricated input 1 second, 2 second, 3 second after the input attempt.
    //public enum FabInputDistance {
    //    TwoSecs,
    //    FiveSecs,
    //    OneSecs,
    //}

    private List<InputTypes> designedInputOrder;
    private List<InputTypes> actualInputOrder;
    private List<SequenceData> CurrentSequences;
    
    private int inputIndex = 0;

    
    private float fabInputRate = 0.1f; // percentage, value between 0 - 1 (rounded up)

    [SerializeField]
    private SetupType setupType = SetupType.Fab0;

    
    private float recognitionRate = 0.4f; // percentage, value between 0 - 1
    // Start is called before the first frame update
    private float actualRecognitionRate = -1f;
    private float actualFabRate = -1f;

    [SerializeField]
    private float noInputReceivedFabAlarm = 0.5f; // fixed alarm in seconds relative to input window, at what point should we try and trigger fab input.
    [SerializeField]
    private float fabAlarmVariability = 0.5f; //added delay variability to make the alarm unpredictable.
    private float currentFabAlarm = 0f;
    private bool alarmFired = false;

    [SerializeField]
    private float startPolicyReview = 0.2f; // percentage of trials which should pass before we start reviewing policy.

    [SerializeField]
    private int trials = 20;

    private InputTypes currentInputDecision = InputTypes.RejectAllInput;

    private int currentTrial;

    [SerializeField]
    private GamePolicy gamePolicy = GamePolicy.MeetDesignGoals;

    private InputWindowState inputWindow = InputWindowState.Closed;

    [SerializeField]
    private float interTrialIntervalSeconds = 4.5f;
    [SerializeField]
    private float inputWindowSeconds = 1f;
    private float inputWindowTimer = 0.0f;
    private float interTrialTimer = 0.0f;

    private GameState gameState = GameState.Stopped;

    [Serializable]
    public class OnGameStateChanged : UnityEvent<GameData> { }
    public OnGameStateChanged onGameStateChanged;

    [Serializable]
    public class GameDecision : UnityEvent<GameDecisionData> { }
    public GameDecision gameDecision;

    [Serializable]
    public class OnGamePolicyChanged : UnityEvent<GamePolicyData> { }
    public OnGamePolicyChanged onGamePolicyChanged;

    [Serializable]
    public class OnInputWindowChanged : UnityEvent<InputWindowState> { }
    public OnInputWindowChanged onInputWindowChanged;

    [Serializable]
    public class OnGameTimeUpdate : UnityEvent<GameTimers> { }
    public OnGameTimeUpdate onGameTimeUpdate;

    private LoggingManager loggingManager;

    void Start()
    {
        CreateDesignedInputOrder();
        actualInputOrder = new List<InputTypes>();
        //UpdateDesignedInputOrder();
        loggingManager = GameObject.Find("LoggingManager").GetComponent<LoggingManager>();
        LogMeta();

    }

    private void CreateDesignedInputOrder() {
        if (setupType == SetupType.Fab0) {
            fabInputRate = 0f;
            recognitionRate = 0.5f;
            designedInputOrder = new List<InputTypes>() {
                    InputTypes.AcceptAllInput,
                    InputTypes.RejectAllInput,
                    InputTypes.RejectAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.RejectAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.RejectAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.RejectAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.RejectAllInput,
                    InputTypes.RejectAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.RejectAllInput,
                    InputTypes.RejectAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.RejectAllInput,
                    InputTypes.AcceptAllInput
            };
        } else if (setupType == SetupType.Fab15) {
            fabInputRate = 0.15f;
            recognitionRate = 0.5f;
            designedInputOrder = new List<InputTypes>() {
                    InputTypes.AcceptAllInput,
                    InputTypes.RejectAllInput,
                    InputTypes.FabInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.FabInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.RejectAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.RejectAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.RejectAllInput,
                    InputTypes.RejectAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.RejectAllInput,
                    InputTypes.FabInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.RejectAllInput,
                    InputTypes.AcceptAllInput
            };
        } else if (setupType == SetupType.Fab30) {
            fabInputRate = 0.30f;
            recognitionRate = 0.5f;
            designedInputOrder = new List<InputTypes>() {
                    InputTypes.AcceptAllInput,
                    InputTypes.FabInput,
                    InputTypes.RejectAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.FabInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.FabInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.RejectAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.FabInput,
                    InputTypes.FabInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.FabInput,
                    InputTypes.RejectAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.RejectAllInput,
                    InputTypes.AcceptAllInput
            };  
        } else if (setupType == SetupType.Fab50) {
            fabInputRate = 0.5f;
            recognitionRate = 0.5f;
            designedInputOrder = new List<InputTypes>() {
                    InputTypes.AcceptAllInput,
                    InputTypes.FabInput,
                    InputTypes.FabInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.FabInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.FabInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.FabInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.FabInput,
                    InputTypes.FabInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.FabInput,
                    InputTypes.FabInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.FabInput,
                    InputTypes.AcceptAllInput
            };  
        } else if (setupType == SetupType.Recog100) {
            fabInputRate = 0f;
            recognitionRate = 1.0f;
            designedInputOrder = new List<InputTypes>() {
                    InputTypes.AcceptAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.AcceptAllInput,
                    InputTypes.AcceptAllInput
            };  
        }
    }

    private void LogMeta() {
        Dictionary<string, object> metaLog = new Dictionary<string, object>() {
            {"FabInputRate", fabInputRate},
            {"RecognitionRate", recognitionRate},
            {"Trials", trials},
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
            {"InterTrialTimer", interTrialTimer},
            {"InputWindowTimer", inputWindowTimer},
            {"GameState", System.Enum.GetName(typeof(GameState), gameState)},
            {"CurrentRecognitionRate", actualRecognitionRate},
            {"CurrentFabRate", actualFabRate},
            {"CurrentFabAlarm", currentFabAlarm},
        };

        if (eventLabel == "GameDecision") {
            gameLog["CurrentInputDecision"] = currentInputDecision;
            gameLog["CurrentDesignGoal"] = designedInputOrder[inputIndex];// .First();
        } else {
            gameLog["CurrentInputDecision"] = "NA";
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
                if (interTrialTimer > interTrialIntervalSeconds && actualInputOrder.Count < trials) {
                    interTrialTimer = 0f;
                    inputWindow = InputWindowState.Open;
                    setFabAlarmVariability();
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

    public void setFabAlarmVariability() {
        currentFabAlarm = UnityEngine.Random.Range(noInputReceivedFabAlarm-fabAlarmVariability, noInputReceivedFabAlarm+fabAlarmVariability);
        //Debug.Log("currentFabAlarm set to: " + currentFabAlarm);
    }

    public GameData createGameData() {
            GameData gameData = new GameData();
            gameData.fabInputRate = fabInputRate;
            gameData.recognitionRate = recognitionRate;
            gameData.startPolicyReview = startPolicyReview;
            gameData.trials = trials;
            gameData.interTrialIntervalSeconds = interTrialIntervalSeconds;
            gameData.inputWindowSeconds = inputWindowSeconds;
            gameData.gameState = gameState;
            gameData.noInputReceivedFabAlarm = noInputReceivedFabAlarm;
            gameData.fabAlarmVariability = fabAlarmVariability;
            return gameData;
    }

    public void UpdateDesignedInputOrder() {
        designedInputOrder.Clear();
        // TODO: Take actualInputOrder into account.
        // Count the actual input so far.
        int trialsEnded = actualInputOrder.Count;
        int fabTrialsEnded = actualInputOrder.Count(c => c == InputTypes.FabInput);
        int accTrialsEnded = actualInputOrder.Count(c => c == InputTypes.AcceptAllInput);
        int rejTrialsEnded = actualInputOrder.Count(c => c == InputTypes.RejectAllInput);

        int fabTrials = (int) Math.Floor((float) trials * fabInputRate);
        int accTrials = (int) Math.Floor((float) trials * recognitionRate);
        int fabTrialsTarget = fabTrials - fabTrialsEnded;
        int accTrialsTarget = accTrials - accTrialsEnded;

        int remainingTrials = trials - trialsEnded;

       ////Debug.Log("fabTrials: " + fabTrials + ", ended: " + fabTrialsEnded + ", new target amount: " + fabTrialsTarget);
       ////Debug.Log("accTrials: " + accTrials + ", ended: " + accTrialsEnded + ", new target amount: " + accTrialsTarget);

        for (int i = 0; i < fabTrialsTarget; i++) {
            designedInputOrder.Add(InputTypes.FabInput);
        }

        for (int i = 0; i < accTrialsTarget; i++) {
            if (designedInputOrder.Count < remainingTrials) {
                designedInputOrder.Add(InputTypes.AcceptAllInput);
            }
        }

       ////Debug.Log("Remaining Trials: " + remainingTrials);
        if (remainingTrials < 0) {
           ////Debug.LogError("Negative Remaining Trials! (trials: " + trials + ", trialsEnded: " + trialsEnded + ")");
        }
        int rejTrialsTarget = remainingTrials - (fabTrialsTarget + accTrialsTarget);
        for (int i = 0; i < rejTrialsTarget; i++) {
            designedInputOrder.Add(InputTypes.RejectAllInput);
        }

        Utils.Shuffle(designedInputOrder);

        string designedInputString = "";
        foreach (var i in designedInputOrder) {
            designedInputString += System.Enum.GetName(typeof(InputTypes), i) + " ";
        }
        ////Debug.Log("DesignedInputOrder updated: [ " + designedInputString + " ] Count: " + designedInputOrder.Count);
    }

    public void RunGame() {
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
        int actualAcc = actualInputOrder.Count(c => c == InputTypes.AcceptAllInput);
        actualRecognitionRate = (float) actualAcc / (float) actualInputOrder.Count;

        // TODO: Calculate FabInput Rate

        int actualFab = actualInputOrder.Count(c => c == InputTypes.FabInput);
        actualFabRate = (float) actualFab / (float) actualInputOrder.Count;

        int designedAcc = designedInputOrder.Count(c => c == InputTypes.AcceptAllInput);
        int designedRej = designedInputOrder.Count(c => c == InputTypes.RejectAllInput);
       ////Debug.Log("actualRecognitionRate: " + Math.Round(actualRecognitionRate, 1) + ", recRate: " + recognitionRate);
    }

    public void ReviewPolicy() {
            // Calculate rej/acc rates.
            if (Math.Round(actualRecognitionRate, 1) != recognitionRate) {
                gamePolicy = GamePolicy.MeetDesignGoals;
            } else {
                gamePolicy = GamePolicy.StrictOperation;
            }
           ////Debug.Log("Game Policy: " + System.Enum.GetName(typeof(GamePolicy), gamePolicy));
    }

    public void OnInputReceived(InputData inputData) {
        if (inputWindow == InputWindowState.Closed) {
            // ignore the input. The keySequencer will still log that the input has happened.
            return;
        } else {
            // This clears the noInput alarm which otherwise would trigger a decision.
           ////Debug.Log("Received sequence: " + inputData.inputNumber);
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
        actualInputOrder.Add(currentInputDecision);

        CalculateRecogRate();
        // Send Decision Data
        GameDecisionData gameDecisionData = new GameDecisionData();
        gameDecisionData.currentRecogRate = actualRecognitionRate;
        gameDecisionData.currentFabRate = actualRecognitionRate;
        gameDecisionData.currentFabAlarm = currentFabAlarm;
        gameDecisionData.decision = currentInputDecision;
        gameDecision.Invoke(gameDecisionData);
        LogEvent("GameDecision");
       ////Debug.Log("designedInputOrder: " + designedInputOrder.Count);
       ////Debug.Log("actualInputOrder: " + actualInputOrder.Count);
       ////Debug.Log("Decision: " + System.Enum.GetName(typeof(InputTypes), currentInputDecision));
        //UpdateDesignedInputOrder();
        inputIndex++;


        int startPolicyReviewTrial = (int) Math.Floor((trials * startPolicyReview));
        if (actualInputOrder.Count >= startPolicyReviewTrial) {
            //ReviewPolicy();
        } 

        // update Game Policy
        GamePolicyData gamePolData = new GamePolicyData();
        gamePolData.gamePolicy = gamePolicy;
        onGamePolicyChanged.Invoke(gamePolData);
    }

    public void MakeInputDecision(InputData inputData = null, bool windowExpired = false) {
        //Debug.Log(System.Enum.GetName(typeof(InputTypes), designedInputOrder[inputIndex]));
        // TODO: Handle invalid sequences (too short sequences)

        // if this is in response to receiving an input;
        // then we evaluate according to this. accept/reject
        if (inputData != null) {
            if (gamePolicy == GamePolicy.LooseOperation) {
                        currentInputDecision  = InputTypes.AcceptAllInput;
                       ////Debug.Log("Case: LooseOperation, Input Was Received So Accept it.");
                        CloseInputWindow();
            }
            else if (gamePolicy == GamePolicy.StrictOperation) {
                    if (designedInputOrder[inputIndex] == InputTypes.FabInput) {
                        currentInputDecision = InputTypes.FabInput;
                       ////Debug.Log("Case: StrictOperation, Awaiting Fabricated Input.");
                    } else if (inputData.validity == InputValidity.Accepted) {
                        currentInputDecision  = InputTypes.AcceptAllInput;
                       ////Debug.Log("Case: StrictOperation, Correct Sequence Played.");
                        CloseInputWindow();
                    } else if (inputData.validity == InputValidity.Rejected) {
                        currentInputDecision  = InputTypes.RejectAllInput;
                       ////Debug.Log("Case: StrictOperation, Input Incorrect."); // + System.Enum.GetName(typeof(SequenceSpeed), sequenceData.sequenceSpeed) + ", " + System.Enum.GetName(typeof(SequenceComposition), sequenceData.sequenceComposition));
                    }
            } else if (gamePolicy == GamePolicy.MeetDesignGoals) {
                if (designedInputOrder[inputIndex] == InputTypes.AcceptAllInput) {
                    if (inputData.validity == InputValidity.Accepted) {
                        currentInputDecision = InputTypes.AcceptAllInput;
                        CloseInputWindow();
                    }
                   ////Debug.Log("Case: MeetDesignGoals, We should Accept this input.");
                } else if (designedInputOrder[inputIndex] == InputTypes.RejectAllInput) {
                    currentInputDecision = InputTypes.RejectAllInput;
                   ////Debug.Log("Case: MeetDesignGoals, We should Reject this input no matter what.");
                } else if (designedInputOrder[inputIndex] == InputTypes.FabInput) {
                    currentInputDecision = InputTypes.FabInput;
                   ////Debug.Log("Case: MeetDesignGoals, We should Fabricate input no matter what.");
                }
            }
        } else if (inputData == null && designedInputOrder[inputIndex] == InputTypes.FabInput) {
            // if this is in response to an alarm that we dont receive any input,
            // then we evaluate fab. input.
            currentInputDecision = InputTypes.FabInput;
           //Debug.Log("Case: Fabricated Input Fired by Alarm.");
            CloseInputWindow();
        } else if (inputData == null && windowExpired) {
            // if this is in response to that the input window has expired,
            // then we submit a Rejection.
            currentInputDecision = InputTypes.RejectAllInput;
            //Debug.Log("Case: Input Window Expired, Rejecting.");
            CloseInputWindow();
        } 
        return;
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
