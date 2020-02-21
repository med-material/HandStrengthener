using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using System;

public class LoggingManager : MonoBehaviour
{

    string filepath;
    string filename = "keysequencedata";
    string sep = ",";

    private Dictionary<string, List<string>> logCollection;

    // Start is called before the first frame update
    void Start()
    {
        filepath = Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
        logCollection = new Dictionary<string, List<string>>();
        logCollection["Date"] = new List<string>();
        logCollection["Timestamp"] = new List<string>();
        logCollection["Event"] = new List<string>();
        logCollection["KeyCode"] = new List<string>();
        logCollection["SequenceTime_ms"] = new List<string>();
        logCollection["TimeSinceLastKey_ms"] = new List<string>();
        logCollection["KeyOrder"] = new List<string>();
        logCollection["KeyType"] = new List<string>();
        logCollection["ExpectedKey1"] = new List<string>();
        logCollection["ExpectedKey2"] = new List<string>();
        logCollection["SequenceNumber"] = new List<string>();
        logCollection["SequenceComposition"] = new List<string>();
        logCollection["SequenceSpeed"] = new List<string>();
        logCollection["SequenceValidity"] = new List<string>();
        logCollection["SequenceType"] = new List<string>();
        logCollection["SequenceWindowClosure"] = new List<string>();
        logCollection["TargetFabInputRate"] = new List<string>();
        logCollection["TargetRecognitionRate"] = new List<string>();
        logCollection["StartPolicyReview"] = new List<string>();
        logCollection["Trials"] = new List<string>();
        logCollection["InterTrialIntervalSeconds"] = new List<string>();
        logCollection["InputWindowSeconds"] = new List<string>();
        logCollection["GameState"] = new List<string>();
        logCollection["GamePolicy"] = new List<string>();
        logCollection["CurrentRecognitionRate"] = new List<string>();
        logCollection["FabAlarmFixationPoint"] = new List<string>();
        logCollection["FabAlarmVariability"] = new List<string>();
        logCollection["CurrentFabRate"] = new List<string>();
        logCollection["CurrentFabAlarm"] = new List<string>();
        logCollection["InputConfidence"] = new List<string>();
        logCollection["InputValidity"] = new List<string>();
        logCollection["InputType"] = new List<string>();
        logCollection["InputNumber"] = new List<string>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void onKeySequenceLogReady(SequenceData sequenceData, InputData inputData) {
        logCollection["InputConfidence"].Add(inputData.confidence.ToString());
        logCollection["InputValidity"].Add(System.Enum.GetName(typeof(InputValidity), inputData.validity));
        logCollection["InputType"].Add(System.Enum.GetName(typeof(InputType), inputData.type));
        logCollection["InputNumber"].Add(inputData.inputNumber.ToString());
       foreach (string key in sequenceData.keySequenceLogs.Keys)
        {
            logCollection[key].AddRange(sequenceData.keySequenceLogs[key]);
            //Debug.Log(sequenceData.keySequenceLogs[key].Count);
        }
        FillKeys();
    }

    public void onMotorImageryLogReady(InputData inputData) {
        logCollection["Event"].Add("InputEvent");
        logCollection["Date"].Add(System.DateTime.Now.ToString("yyyy-MM-dd"));
        logCollection["Timestamp"].Add(System.DateTime.Now.ToString("HH:mm:ss.ffff"));
        logCollection["InputConfidence"].Add(inputData.confidence.ToString());
        logCollection["InputValidity"].Add(System.Enum.GetName(typeof(InputValidity), inputData.validity));
        logCollection["InputType"].Add(System.Enum.GetName(typeof(InputType), inputData.type));
        logCollection["InputNumber"].Add(inputData.inputNumber.ToString());
        FillKeys();
    }

    public void onGameStateChanged(GameData gameData) {
        logCollection["Event"].Add(System.Enum.GetName(typeof(GameState), gameData.gameState));
        logCollection["Date"].Add(System.DateTime.Now.ToString("yyyy-MM-dd"));
        logCollection["Timestamp"].Add(System.DateTime.Now.ToString("HH:mm:ss.ffff"));
        logCollection["TargetFabInputRate"].Add(gameData.fabInputRate.ToString());
        logCollection["TargetRecognitionRate"].Add(gameData.recognitionRate.ToString());
        logCollection["StartPolicyReview"].Add(gameData.startPolicyReview.ToString());
        logCollection["Trials"].Add(gameData.trials.ToString());
        logCollection["InterTrialIntervalSeconds"].Add(gameData.interTrialIntervalSeconds.ToString());
        logCollection["InputWindowSeconds"].Add(gameData.inputWindowSeconds.ToString());
        logCollection["GameState"].Add(System.Enum.GetName(typeof(GameState), gameData.gameState));
        logCollection["FabAlarmFixationPoint"].Add(gameData.noInputReceivedFabAlarm.ToString());
        logCollection["FabAlarmVariability"].Add(gameData.fabAlarmVariability.ToString());
        FillKeySequenceColumns();
        FillKeys();

        if (gameData.gameState == GameState.Stopped) {
            SendLogs();
        }
    }

    public void FillKeySequenceColumns() {
        logCollection["KeyCode"].Add("NA");
        logCollection["SequenceTime_ms"].Add("NA");
        logCollection["TimeSinceLastKey_ms"].Add("NA");
        logCollection["KeyOrder"].Add("NA");
        logCollection["KeyType"].Add("NA");
        logCollection["ExpectedKey1"].Add("NA");
        logCollection["ExpectedKey2"].Add("NA");
        logCollection["SequenceNumber"].Add("NA");
        logCollection["SequenceComposition"].Add("NA");
        logCollection["SequenceSpeed"].Add("NA");
        logCollection["SequenceValidity"].Add("NA");
        logCollection["SequenceType"].Add("NA");
        logCollection["SequenceWindowClosure"].Add("NA");
    }

    public void onGamePolicyChanged(GamePolicyData gamePolicyData) {
        logCollection["Event"].Add("NewPolicy" + System.Enum.GetName(typeof(GamePolicy), gamePolicyData.gamePolicy));
        logCollection["Date"].Add(System.DateTime.Now.ToString("yyyy-MM-dd"));
        logCollection["Timestamp"].Add(System.DateTime.Now.ToString("HH:mm:ss.ffff"));
        logCollection["GamePolicy"].Add(System.Enum.GetName(typeof(GamePolicy), gamePolicyData.gamePolicy));
        FillKeySequenceColumns();
        FillKeys();
    }

    public void onGameDecision(GameDecisionData decisionData) {
        logCollection["Event"].Add("Decision" + System.Enum.GetName(typeof(InputTypes), decisionData.decision));
        logCollection["Date"].Add(System.DateTime.Now.ToString("yyyy-MM-dd"));
        logCollection["Timestamp"].Add(System.DateTime.Now.ToString("HH:mm:ss.ffff"));
        logCollection["CurrentRecognitionRate"].Add(decisionData.currentRecogRate.ToString());
        logCollection["CurrentFabRate"].Add(decisionData.currentRecogRate.ToString());
        logCollection["CurrentFabAlarm"].Add(decisionData.currentFabAlarm.ToString());
        // TODO: Whenever there is a GameDecision, we need to "Backfill" KeySequences.
        FillKeySequenceColumns();
        FillKeys();
    }

    public void onInputWindowChanged(InputWindowState windowState) {
        logCollection["Event"].Add("InputWindow"+System.Enum.GetName(typeof(InputWindowState), windowState));
        logCollection["Date"].Add(System.DateTime.Now.ToString("yyyy-MM-dd"));
        logCollection["Timestamp"].Add(System.DateTime.Now.ToString("HH:mm:ss.ffff"));
        // TODO: Whenever there is a GameDecision, we need to "Backfill" KeySequences.
        FillKeySequenceColumns();
        FillKeys();
    }

    public void FillKeys() {
       foreach (string key in logCollection.Keys)
        {
            if (logCollection[key].Count < logCollection["Event"].Count) {
                string value;
                if (logCollection[key].Count > 0) {
                    value = logCollection[key][logCollection[key].Count-1];
                } else {
                    value = "NA";
                }
                var amount = logCollection["Event"].Count - logCollection[key].Count;
                if (amount > 0) {
                    for(int i = 0; i < amount; i++)
                    {
                        logCollection[key].Add(value);
                    }
                }
            }
        }
    }

    public void SendLogs() {
        LogToDisk();
        // TODO: Send to MySQL server.
    }

    public void LogToDisk() {
        if (logCollection["Event"].Count == 0) {
            Debug.Log("Nothing to log, returning..");
            return;
        }

        Debug.Log("Saving " + logCollection["Event"].Count + " Rows to " + filepath);
        string dest = filepath + "\\" + filename + "_" + System.DateTime.Now.ToString("yyyy-MM-dd-HH_mm_ss") + ".csv";

        // Log Header
        string[] keys = new string[logCollection.Keys.Count];
        logCollection.Keys.CopyTo(keys, 0);
        string dbCols = string.Join(sep, keys).Replace("\n", string.Empty);

        using (StreamWriter writer = File.AppendText(dest))
        {
            writer.WriteLine(dbCols);
        }

        // Create a string with the data
        List<string> dataString = new List<string>();
        for (int i = 0; i < logCollection["Event"].Count; i++)
        {
            List<string> row = new List<string>();
            foreach (string key in logCollection.Keys)
            {
                row.Add(logCollection[key][i]);
            }
            dataString.Add(string.Join(sep, row.ToArray()) + sep);
        }

        foreach (var log in dataString)
        {
            using (StreamWriter writer = File.AppendText(dest))
            {
                writer.WriteLine(log.Replace("\n", string.Empty));
            }
        }

        // Clear logCollection
       foreach (string key in logCollection.Keys)
        {
            
            logCollection[key].Clear();
        }
    }




}
