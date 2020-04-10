using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
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
    MotorImagery
}

public class GameDecisionData {
    public string decision;
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
    Stopped,
}

//public enum InputTypes {
//    AcceptAllInput,
//    FabInput,
//    RejectAllInput,
    //assistedInput
//}

public struct GamePolicyData {
    public GamePolicy gamePolicy;
}

public enum GamePolicy {
    StrictOperation, // this is equivalent to BCI.
    MeetDesignGoals, // this is equivalent to Fab.Input.
}

public class GameManager : MonoBehaviour
{

    // TODO for the future: Fixate fabricated input 1 second, 2 second, 3 second after the input attempt.
    //public enum FabInputDistance {
    //    TwoSecs,
    //    FiveSecs,
    //    OneSecs,
    //}

    [Serializable]
    public struct UrnInput {
        public float acceptInput;
        public float fabInput;
        public float rejectInput;
        //public int assistedInput;
        //public int assistedEnv;
    }

    public UrnInput urnInput;


    private List<string> designedInputOrder;
    private List<string> actualInputOrder;
    private List<SequenceData> CurrentSequences;
    
    [SerializeField]
    private float fabInputRate = 0.1f; // percentage, value between 0 - 1 (rounded up)

    [SerializeField]
    private float recognitionRate = 0.4f; // percentage, value between 0 - 1
    // Start is called before the first frame update
    private float actualRecognitionRate = -1f;

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

    private string currentInputDecision = "rejectInput";

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

    void Start()
    {
        designedInputOrder = new List<string>();
        actualInputOrder = new List<string>();
        UpdateDesignedInputOrder();

    }

    // Update is called once per frame
    void Update()
    {
        if (gameState == GameState.Running) {
            if (inputWindow == InputWindowState.Closed) {
                interTrialTimer += Time.deltaTime;
                if (interTrialTimer > interTrialIntervalSeconds && actualInputOrder.Count < trials) {
                    interTrialTimer = 0f;
                    inputWindow = InputWindowState.Open;
                    setFabAlarmVariability();
                    onInputWindowChanged.Invoke(inputWindow);
                } else if (interTrialTimer > interTrialIntervalSeconds) {
                    EndGame();
                }
            } else if (inputWindow == InputWindowState.Open) {
                //Debug.Log("inputwindow is open");
                inputWindowTimer += Time.deltaTime;
                if (inputWindowTimer > currentFabAlarm && alarmFired == false) {
                    Debug.Log("inputWindowTimer exceeded currentFabAlarm.");
                    // Fire fabricated input (if scheduled).
                    MakeInputDecision(null, false);
                    alarmFired = true;
                } else if (inputWindowTimer > inputWindowSeconds) {
                    Debug.Log("inputWindow expired.");
                    // The input window expired
                    MakeInputDecision(null, true);
                    alarmFired = false;
                }
            }
            GameTimers gameTimers = new GameTimers();
            gameTimers.interTrialTimer = interTrialTimer;
            gameTimers.inputWindowTimer = inputWindowTimer;
            onGameTimeUpdate.Invoke(gameTimers);
        }
    }

    public void setFabAlarmVariability() {
        currentFabAlarm = UnityEngine.Random.Range(noInputReceivedFabAlarm-fabAlarmVariability, noInputReceivedFabAlarm+fabAlarmVariability);
        Debug.Log("currentFabAlarm set to: " + currentFabAlarm);
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
        //List<int> stats = new List<int>();
        //foreach(var field in typeof(UrnInput).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)) {
        //    stats.Add(field.GetValue(urnInput))
        //}

        Dictionary<string, int> trialTypesEnded = new Dictionary<string, int>();
        
        foreach(var field in typeof(UrnInput).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)) {
            trialTypesEnded[field.Name] = actualInputOrder.Count(c => c == field.Name);
        }

        //int fabTrialsEnded = actualInputOrder.Count(c => c == "fabInput");
        //int accTrialsEnded = actualInputOrder.Count(c => c == "acceptInput");
        //int rejTrialsEnded = actualInputOrder.Count(c => c == "rejectInput");

        Dictionary<string, int> newTrialTargets = new Dictionary<string, int>();
        foreach(var field in typeof(UrnInput).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)) {
            newTrialTargets[field.Name] = ((int) Math.Floor((float) trials * fabInputRate)) - trialTypesEnded[field.Name];
        }


        foreach (KeyValuePair<string, int> trialtype in trialTypesEnded)
        {
            Debug.Log("TrialType = " + trialtype.Key + ", No. of trials ended = " + trialtype.Value);
        }

        foreach (KeyValuePair<string, int> trialtype in newTrialTargets)
        {
            Debug.Log("TrialType = " + trialtype.Key + ", New allocated trials = " + trialtype.Value);
        }

        //int fabTrials = (int) Math.Floor((float) trials * fabInputRate);
        //int accTrials = (int) Math.Floor((float) trials * recognitionRate);
        //int fabTrialsTarget = fabTrials - fabTrialsEnded;
        //int accTrialsTarget = accTrials - accTrialsEnded;

        int remainingTrials = trials - trialsEnded;

        //Debug.Log("fabTrials: " + fabTrials + ", ended: " + fabTrialsEnded + ", new target amount: " + fabTrialsTarget);
        //Debug.Log("accTrials: " + accTrials + ", ended: " + accTrialsEnded + ", new target amount: " + accTrialsTarget);

        foreach(var field in typeof(UrnInput).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)) {
            for (int i = 0; i < newTrialTargets[field.Name]; i++) {
                designedInputOrder.Add(field.Name);
            }
        }

        //for (int i = 0; i < fabTrialsTarget; i++) {
        //    designedInputOrder.Add("fabInput");
        //}

        //for (int i = 0; i < accTrialsTarget; i++) {
        //    if (designedInputOrder.Count < remainingTrials) {
        //        designedInputOrder.Add("acceptInput");
        //    }
        //}

        Debug.Log("Remaining Trials: " + remainingTrials);
        if (remainingTrials < 0) {
            Debug.LogError("Negative Remaining Trials! (trials: " + trials + ", trialsEnded: " + trialsEnded + ")");
        }
        //int rejTrialsTarget = remainingTrials - (fabTrialsTarget + accTrialsTarget);
        //for (int i = 0; i < rejTrialsTarget; i++) {
        //    designedInputOrder.Add("rejectInput");
        //}

        Utils.Shuffle(designedInputOrder);

        string designedInputString = "";
        foreach (var i in designedInputOrder) {
            designedInputString += i + " ";
        }
        Debug.Log("DesignedInputOrder updated: [ " + designedInputString + " ] Count: " + designedInputOrder.Count);
    }

    public void RunGame() {
        gameState = GameState.Running;
        GameData gameData = createGameData();
        onGameStateChanged.Invoke(gameData);

    }

    public void EndGame() {
        interTrialTimer = 0f;
        if (inputWindow == InputWindowState.Open) {
            CloseInputWindow();
        }
        gameState = GameState.Stopped;
        GameData gameData = createGameData();
        onGameStateChanged.Invoke(gameData);
    }

    public void CalculateRecogRate() {
        int actualAcc = actualInputOrder.Count(c => c == "acceptInput");
        actualRecognitionRate = (float) actualAcc / (float) actualInputOrder.Count;

        // TODO: Calculate FabInput Rate

        int designedAcc = designedInputOrder.Count(c => c == "acceptInput");
        int designedRej = designedInputOrder.Count(c => c == "rejectInput");
        Debug.Log("actualRecognitionRate: " + Math.Round(actualRecognitionRate, 1) + ", recRate: " + recognitionRate);
    }

    public void ReviewPolicy() {
            // Calculate rej/acc rates.
            if (Math.Round(actualRecognitionRate, 1) != recognitionRate) {
                gamePolicy = GamePolicy.MeetDesignGoals;
            } else {
                gamePolicy = GamePolicy.StrictOperation;
            }
            Debug.Log("Game Policy: " + System.Enum.GetName(typeof(GamePolicy), gamePolicy));
    }

    public void OnInputReceived(InputData inputData) {
        if (inputWindow == InputWindowState.Closed) {
            // ignore the input. The keySequencer will still log that the input has happened.
            return;
        } else {
            // This clears the noInput alarm which otherwise would trigger a decision.
            Debug.Log("Received sequence: " + inputData.inputNumber);
            MakeInputDecision(inputData);
        }
    }

    public void CloseInputWindow() {
        // update the window state.
        inputWindow = InputWindowState.Closed;
        inputWindowTimer = 0f;
        onInputWindowChanged.Invoke(inputWindow);

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
        Debug.Log("designedInputOrder: " + designedInputOrder.Count);
        Debug.Log("actualInputOrder: " + actualInputOrder.Count);
        Debug.Log("Decision: " + currentInputDecision);
        UpdateDesignedInputOrder();


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

        // TODO: Handle invalid sequences (too short sequences)

        // if this is in response to receiving an input;
        // then we evaluate according to this. accept/reject
        if (inputData != null) {
            if (gamePolicy == GamePolicy.StrictOperation) {
                    if (designedInputOrder.First() == "fabInput") {
                        currentInputDecision = "fabInput";
                        Debug.Log("Case: StrictOperation, Awaiting Fabricated Input.");
                    } else if (inputData.validity == InputValidity.Accepted) {
                        currentInputDecision  = "acceptInput";
                        Debug.Log("Case: StrictOperation, Correct Sequence Played.");
                        CloseInputWindow();
                    } else if (inputData.validity == InputValidity.Rejected) {
                        currentInputDecision  = "rejectInput";
                        Debug.Log("Case: StrictOperation, Input Incorrect."); // + System.Enum.GetName(typeof(SequenceSpeed), sequenceData.sequenceSpeed) + ", " + System.Enum.GetName(typeof(SequenceComposition), sequenceData.sequenceComposition));
                    }
            } else if (gamePolicy == GamePolicy.MeetDesignGoals) {
                if (designedInputOrder.First() == "acceptInput") {
                    if (inputData.validity == InputValidity.Accepted) {
                        currentInputDecision = "acceptInput";
                        CloseInputWindow();
                    } else if (inputData.validity == InputValidity.Rejected) {
                        // Recycles the AcceptAllInput
                        currentInputDecision = "rejectInput";
                    }
                    Debug.Log("Case: MeetDesignGoals, We should Accept this input if it is valid.");
                    Debug.Log("InputValidity: " + System.Enum.GetName(typeof(InputValidity), inputData.validity));
                } else if (designedInputOrder.First() == "rejectInput") {
                    currentInputDecision = "rejectInput";
                    Debug.Log("Case: MeetDesignGoals, We should Reject this input no matter what.");
                } else if (designedInputOrder.First() == "fabInput") {
                    currentInputDecision = "fabInput";
                    Debug.Log("Case: MeetDesignGoals, We should Fabricate input no matter what.");
                }
            }
        } else if (inputData == null && windowExpired) {
            // if this is in response to that the input window has expired,
            // then we submit a Rejection.
            currentInputDecision = "rejectInput";
            Debug.Log("Case: Input Window Expired, Rejecting.");
            CloseInputWindow();
        } else if (inputData == null && designedInputOrder.First() == "fabInput") {
            // if this is in response to an alarm that we dont receive any input,
            // then we evaluate fab. input.
            currentInputDecision = "fabInput";
            Debug.Log("Case: Fabricated Input Fired by Alarm.");
            CloseInputWindow();
        }
        return;
    }

}
