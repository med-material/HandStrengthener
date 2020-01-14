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

public enum InputTypes {
    AcceptAllInput,
    FabInput,
    RejectAllInput,
}

public class GameManager : MonoBehaviour
{
    
    public enum GamePolicy {
        StrictOperation, // this is equivalent to BCI.
        MeetDesignGoals, // this is equivalent to Fab.Input.
    }

    public enum FabInputRate {
        NoFabInput,
        FabInput10,
        FabInput20,
        FabInput30,
    }

    public enum RecognitionRate {
        Rate20,
        Rate40,
        Rate60
    }

    // TODO for the future: Fixate fabricated input 1 second, 2 second, 3 second after the input attempt.
    //public enum FabInputDistance {
    //    TwoSecs,
    //    FiveSecs,
    //    OneSecs,
    //}

    private List<InputTypes> designedInputOrder;
    private List<InputTypes> actualInputOrder;
    private List<SequenceData> CurrentSequences;
    
    [SerializeField]
    private float fabInputRate = 0.1f; // percentage, value between 0 - 1 (rounded up)

    [SerializeField]
    private float recognitionRate = 0.4f; // percentage, value between 0 - 1
    // Start is called before the first frame update

    [SerializeField]
    private float noInputReceivedFabAlarm = 0.5f; // fixed alarm in seconds relative to input window, at what point should we try and trigger fab input.
    private bool inputReceivedThisWindow = false;

    [SerializeField]
    private float startPolicyReview = 0.2f; // percentage of trials which should pass before we start reviewing policy.

    [SerializeField]
    private int trials = 20;

    private InputTypes currentInputDecision = InputTypes.RejectAllInput;

    private int currentTrial;

    private GamePolicy gamePolicy = GamePolicy.StrictOperation;

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
    public class GameDecision : UnityEvent<InputTypes> { }
    public GameDecision gameDecision;

    [Serializable]
    public class OnInputWindowChanged : UnityEvent<InputWindowState> { }
    public OnInputWindowChanged onInputWindowChanged;

    [Serializable]
    public class OnGameTimeUpdate : UnityEvent<GameTimers> { }
    public OnGameTimeUpdate onGameTimeUpdate;

    void Start()
    {
        designedInputOrder = new List<InputTypes>();
        actualInputOrder = new List<InputTypes>();
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
                    onInputWindowChanged.Invoke(inputWindow);
                } else if (interTrialTimer > interTrialIntervalSeconds) {
                    EndGame();
                }
            } else if (inputWindow == InputWindowState.Open) {
                inputWindowTimer += Time.deltaTime;

                if (inputReceivedThisWindow == false && noInputReceivedFabAlarm > inputWindowSeconds) {
                    // Fire fabricated input (if scheduled).
                    MakeInputDecision(null, false);
                } else if (inputWindowTimer > inputWindowSeconds) {
                    // The input window expired
                    MakeInputDecision(null, true);
                }
            }
            GameTimers gameTimers = new GameTimers();
            gameTimers.interTrialTimer = interTrialTimer;
            gameTimers.inputWindowTimer = inputWindowTimer;
            onGameTimeUpdate.Invoke(gameTimers);
        }
    }

    public GameData createGameData() {
            GameData gameData = new GameData();
            gameData.fabInputRate = fabInputRate;
            gameData.recognitionRate = recognitionRate;
            gameData.startPolicyReview = startPolicyReview;
            gameData.trials = trials;
            gameData.interTrialIntervalSeconds = interTrialIntervalSeconds;
            gameData.anticipationzone = anticipationzone;
            gameData.inputWindowSeconds = inputWindowSeconds;
            gameData.gameState = gameState;
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

        Debug.Log("fabTrials: " + fabTrials + ", ended: " + fabTrialsEnded + ", new target amount: " + fabTrialsTarget);
        Debug.Log("accTrials: " + accTrials + ", ended: " + accTrialsEnded + ", new target amount: " + accTrialsTarget);

        for (int i = 0; i < fabTrialsTarget; i++) {
            designedInputOrder.Add(InputTypes.FabInput);
        }
        for (int i = 0; i < accTrialsTarget; i++) {
            designedInputOrder.Add(InputTypes.AcceptAllInput);
        }

        var remainingTrials = trials - trialsEnded;
        Debug.Log("Remaining Trials: " + remainingTrials);
        if (remainingTrials < 0) {
            Debug.LogError("Negative Remaining Trials! (trials: " + trials + ", trialsEnded: " + trialsEnded + ")");
        }
        for (int i = 0; i < remainingTrials; i++) {
            designedInputOrder.Add(InputTypes.RejectAllInput);
        }

        Utils.Shuffle(designedInputOrder);

        string designedInputString = "";
        foreach (var i in designedInputOrder) {
            designedInputString += System.Enum.GetName(typeof(InputTypes), i) + " ";
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

    public void ReviewPolicy() {
            // Calculate rej/acc rates.
            int actualAcc = actualInputOrder.Count(c => c == InputTypes.AcceptAllInput);
            float actualRecognitionRate = (float) actualAcc / (float) actualInputOrder.Count;

            int designedAcc = designedInputOrder.Count(c => c == InputTypes.AcceptAllInput);
            int designedRej = designedInputOrder.Count(c => c == InputTypes.RejectAllInput);
            Debug.Log("actualRecognitionRate: " + Math.Round(actualRecognitionRate, 1) + ", recRate: " + recognitionRate);

            if (Math.Round(actualRecognitionRate, 1) != recognitionRate) {
                gamePolicy = GamePolicy.MeetDesignGoals;
            } else {
                gamePolicy = GamePolicy.StrictOperation;
            }
            Debug.Log("Game Policy: " + System.Enum.GetName(typeof(GamePolicy), gamePolicy));
    }

    public void OnSequenceReceived(SequenceData sequenceData) {
        if (inputWindow == InputWindowState.Closed) {
            // ignore the input. The keySequencer will still log that the input has happened.
            return;
        } else {
            // This clears the noInput alarm which otherwise would trigger a decision.
            inputReceivedThisWindow = true;
            MakeInputDecision(sequenceData);
        }
    }

    public void CloseInputWindow() {
        // update the window state.
        inputWindow = InputWindowState.Closed;
        inputWindowTimer = 0f;
        inputReceivedThisWindow = false;
        onInputWindowChanged.Invoke(inputWindow);

        // store the input decision.
        actualInputOrder.Add(currentInputDecision);
        gameDecision.Invoke(currentInputDecision);
        UpdateDesignedInputOrder();

        int startPolicyReviewTrial = (int) Math.Floor((trials * startPolicyReview));
        if (actualInputOrder.Count >= startPolicyReviewTrial) {
            ReviewPolicy();
        }

    }

    public void MakeInputDecision(SequenceData sequenceData = null, bool windowExpired = false) {

        // TODO: Handle invalid sequences (too short sequences)

        // if this is in response to receiving an input;
        // then we evaluate according to this. accept/reject
        if (sequenceData != null) {
            if (gamePolicy == GamePolicy.StrictOperation) {
                    if (designedInputOrder[actualInputOrder.Count] == InputTypes.FabInput) {
                        currentInputDecision = InputTypes.FabInput;
                    } else if (sequenceData.sequenceValidity == SequenceValidity.Accepted) {
                        currentInputDecision  = InputTypes.AcceptAllInput;
                    } else if (sequenceData.sequenceValidity == SequenceValidity.Rejected) {
                        currentInputDecision  = InputTypes.RejectAllInput;
                    }
            } else if (gamePolicy == GamePolicy.MeetDesignGoals) {
                if (designedInputOrder[actualInputOrder.Count] == InputTypes.AcceptAllInput) {
                    currentInputDecision = InputTypes.AcceptAllInput;
                } else if (designedInputOrder[actualInputOrder.Count] == InputTypes.RejectAllInput) {
                    currentInputDecision = InputTypes.RejectAllInput;
                } else if (designedInputOrder[actualInputOrder.Count] == InputTypes.FabInput) {
                    currentInputDecision = InputTypes.FabInput;
                }
            }
        } else if (sequenceData == null && windowExpired) {
            // if this is in response to that the input window has expired,
            // then we submit a Rejection.
            currentInputDecision = InputTypes.RejectAllInput;
        } else if (sequenceData == null && designedInputOrder[actualInputOrder.Count] == InputTypes.FabInput) {
            // if this is in response to an alarm that we dont receive any input,
            // then we evaluate fab. input.
            currentInputDecision = InputTypes.FabInput;
        }
        CloseInputWindow();
        return;
    }

}
