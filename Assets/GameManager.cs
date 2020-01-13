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
    public float anticipationzone;
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
    InputWindowExpired,
}

public class GameManager : MonoBehaviour
{
    
    public enum GamePolicy {
        FreeOperation,
        MeetDesignGoals,
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
    private float startPolicyReview = 0.2f; // percentage of trials which should pass before we start reviewing policy.

    [SerializeField]
    private int trials = 20;

    private int currentTrial;

    private GamePolicy gamePolicy = GamePolicy.FreeOperation;

    private InputWindowState inputWindow = InputWindowState.Open;

    [SerializeField]
    private float interTrialIntervalSeconds = 4.5f;
    [SerializeField]
    private float inputWindowSeconds = 1f;
    private float inputWindowTimer = 0.0f;
    [SerializeField]
    private float anticipationzone = 0.5f;
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
        if (inputWindow == InputWindowState.Closed) {
            interTrialTimer += Time.deltaTime;
            if (interTrialTimer > interTrialIntervalSeconds && actualInputOrder.Count < trials) {
                interTrialTimer = 0f;
                inputWindow = InputWindowState.Open;
                onInputWindowChanged.Invoke(inputWindow);
            } else if (interTrialTimer > interTrialIntervalSeconds) {
                interTrialTimer = 0f;
                inputWindow = InputWindowState.Closed;
                onInputWindowChanged.Invoke(inputWindow);
                gameState = GameState.Stopped;
                GameData gameData = createGameData();
                onGameStateChanged.Invoke(gameData);
            }
        } else if (inputWindow == InputWindowState.Open) {
            inputWindowTimer += Time.deltaTime;
            if (inputWindowTimer > inputWindowSeconds) {
                inputWindow = InputWindowState.Closed;
                inputWindowTimer = 0f;
                actualInputOrder.Add(InputTypes.InputWindowExpired);
                onInputWindowChanged.Invoke(inputWindow);
            }
        }
        GameTimers gameTimers = new GameTimers();
        gameTimers.interTrialTimer = interTrialTimer;
        gameTimers.inputWindowTimer = inputWindowTimer;
        onGameTimeUpdate.Invoke(gameTimers);
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

    public void DecideResult() {

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
                gamePolicy = GamePolicy.FreeOperation;
            }
            Debug.Log("Game Policy: " + System.Enum.GetName(typeof(GamePolicy), gamePolicy));
    }

    public void OnSequenceReceived(SequenceData sequenceData) {
        if (inputWindow == InputWindowState.Closed) {
            // ignore the input. The keySequencer will still log that the input has happened.
            return;
        } else {
            int startPolicyReviewTrial = (int) Math.Floor((trials * startPolicyReview));
            if (actualInputOrder.Count >= startPolicyReviewTrial) {
                ReviewPolicy();
            }
            InputTypes decision = InputTypes.RejectAllInput;
            if (gamePolicy == GamePolicy.FreeOperation) {
                if (designedInputOrder[actualInputOrder.Count] == InputTypes.FabInput) {
                    decision = InputTypes.FabInput;
                } else if (sequenceData.sequenceValidity == SequenceValidity.Accepted) {
                    decision = InputTypes.AcceptAllInput;
                } else if (sequenceData.sequenceValidity == SequenceValidity.Rejected) {
                    decision = InputTypes.RejectAllInput;
                }
            } else if (gamePolicy == GamePolicy.MeetDesignGoals) {
                if (designedInputOrder[actualInputOrder.Count] == InputTypes.AcceptAllInput) {
                    decision = InputTypes.AcceptAllInput;
                } else if (designedInputOrder[actualInputOrder.Count] == InputTypes.RejectAllInput) {
                    decision = InputTypes.RejectAllInput;
                } else if (designedInputOrder[actualInputOrder.Count] == InputTypes.FabInput) {
                    decision = InputTypes.FabInput;
                }
            }
            actualInputOrder.Add(decision);
            gameDecision.Invoke(decision);
            
            UpdateDesignedInputOrder();

            inputWindow = InputWindowState.Closed;
            onInputWindowChanged.Invoke(inputWindow);
        }
    }

}
