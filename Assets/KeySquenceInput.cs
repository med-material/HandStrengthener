using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using System.IO;

public class SequenceData {
    public int sequenceNumber;
    public SequenceType sequenceType;
    public SequenceComposition sequenceComposition;
    public SequenceSpeed sequenceSpeed;
    public SequenceValidity sequenceValidity;
    public SequenceWindowClosure sequenceWindowClosure;
    public int keyCount;
    public Dictionary<string, List<string>> keySequenceLogs;
}

public enum SequenceType {
    KLHJ, // KL + H + J
    TYUI, // T + YI + U
    TRWE,  // TR + W + E
    XCVB, // X + C + VB
}

public enum SequenceState {
    Playing,
    Stopped
}

public enum SequenceComposition {
    Correct,
    Mistyped
}

public enum SequenceSpeed {
    Slow,
    Fast
}

public enum SequenceValidity {
    Accepted,
    Rejected,
}

public enum SequenceWindowClosure {
    Open,
    ClosedByDeadzone,
    ClosedByInputThreshold,
}


public class KeySquenceInput : MonoBehaviour
{

    [SerializeField]
    private float sequenceTimeLimit_ms = 1.5f; // the longest time that the sequence may take (500 ms time limit)
    [SerializeField]
    private float deadzoneTimeLimit_ms = 1f; // the time it takes before we consider an input to belong to a new sequence.

    [SerializeField]
    private SequenceType keyboardSequence = SequenceType.KLHJ;
    private SequenceState sequenceState = SequenceState.Stopped;
    private SequenceWindowClosure sequenceWindowClosure = SequenceWindowClosure.Open;

    private Dictionary<string, List<string>> keySequenceLogs; // Here we collect how fast people pressed the buttons
    private Dictionary<string, List<string>> currentKeySequenceLogs;
    //private Dictionary<string, List<string>> keysToPress;

    private KeyCode[,] keysToPress;
    // 1. 2-dimension Array which dictates how keys ought to be pressed and by how big margins.
    // 1. 2 keys defined in one row means they must be pressed simultaneously.
    // 1.   key_1,  key_2 (can be KeyCode.None)
    // 2.   key_1,  key_2 (can be KeyCode.None)
    // 3.   key_1,  key_2 (can be KeyCode.None)

    private float time_ms = 0f;
    private float timeSinceLastPress_ms = 0f;
    private float sequenceTime_ms = 0f;
    private float deadzoneTime_ms = 0f;
    

    private KeyCode lastKey;
    private KeyCode lastKey2;

    string filepath;
    string filename = "keysequencedata";
    string sep = ",";
    int sequenceNumber = 0;



    [Serializable]
    public class OnKeySequenceFinished : UnityEvent<SequenceData, InputData> { }
    public OnKeySequenceFinished onKeySequenceFinished;

    [Serializable]
    public class OnInputFinished : UnityEvent<InputData> { }
    public OnInputFinished onInputFinished;

    [Serializable]
    public class OnKeyDown : UnityEvent<KeyCode> { }
    public OnKeyDown onKeyDown;

    private LoggingManager loggingManager;

    // Start is called before the first frame update
    void Start()
    {
        loggingManager = GameObject.Find("LoggingManager").GetComponent<LoggingManager>();
        LogMeta();
        sequenceNumber = 0;
        lastKey = KeyCode.None;
        CreateNewSequenceLogs();
        
        /*if (keyboardSequence == SequenceType.HKJL) {
            keysToPress = new KeyCode[4,2]; // 3 sequences, up to 2 keys simultaneously.
            keysToPress[0,0] = KeyCode.H; // In Slot 0 and 1 we check for both H or K keys 
            keysToPress[0,1] = KeyCode.K;
            keysToPress[1,0] = KeyCode.H;
            keysToPress[1,1] = KeyCode.K;
            keysToPress[2,0] = KeyCode.J;
            keysToPress[2,1] = KeyCode.None;
            keysToPress[3,0] = KeyCode.L;
            keysToPress[3,1] = KeyCode.None;
        }*/

        if (keyboardSequence == SequenceType.KLHJ) {
            keysToPress = new KeyCode[4,2]; // 3 sequences, up to 2 keys simultaneously.
            keysToPress[0,0] = KeyCode.K; // In Slot 0 and 1 we check for both H or K keys 
            keysToPress[0,1] = KeyCode.L;
            keysToPress[1,0] = KeyCode.K;
            keysToPress[1,1] = KeyCode.L;
            keysToPress[2,0] = KeyCode.H;
            keysToPress[2,1] = KeyCode.None;
            keysToPress[3,0] = KeyCode.J;
            keysToPress[3,1] = KeyCode.None;
        }

        if (keyboardSequence == SequenceType.TYUI) { // T + YU + I
            keysToPress = new KeyCode[4,2]; // 3 sequences, up to 2 keys simultaneously.
            keysToPress[0,0] = KeyCode.T; // In Slot 0 and 1 we check for both H or K keys 
            keysToPress[0,1] = KeyCode.None;
            keysToPress[1,0] = KeyCode.Y;
            keysToPress[1,1] = KeyCode.I;
            keysToPress[2,0] = KeyCode.Y;
            keysToPress[2,1] = KeyCode.I;
            keysToPress[3,0] = KeyCode.U;
            keysToPress[3,1] = KeyCode.None;
        }

        if (keyboardSequence == SequenceType.TRWE) { // SR + W + E
            keysToPress = new KeyCode[4,2]; // 3 sequences, up to 2 keys simultaneously.
            keysToPress[0,0] = KeyCode.T; // In Slot 0 and 1 we check for both H or K keys 
            keysToPress[0,1] = KeyCode.R;
            keysToPress[1,0] = KeyCode.T;
            keysToPress[1,1] = KeyCode.R;
            keysToPress[2,0] = KeyCode.W;
            keysToPress[2,1] = KeyCode.None;
            keysToPress[3,0] = KeyCode.E;
            keysToPress[3,1] = KeyCode.None;
        }        

        if (keyboardSequence == SequenceType.XCVB) { // SR + W + E
            keysToPress = new KeyCode[4,2]; // 3 sequences, up to 2 keys simultaneously.
            keysToPress[0,0] = KeyCode.X; // In Slot 0 and 1 we check for both H or K keys 
            keysToPress[0,1] = KeyCode.None;
            keysToPress[1,0] = KeyCode.C;
            keysToPress[1,1] = KeyCode.None;
            keysToPress[2,0] = KeyCode.V;
            keysToPress[2,1] = KeyCode.B;
            keysToPress[3,0] = KeyCode.V;
            keysToPress[3,1] = KeyCode.B;
        }   

    }

    private void LogMeta() {
        Dictionary<string, object> metaLog = new Dictionary<string, object>() {
            {"SequenceTimeLimit_ms", sequenceTimeLimit_ms},
            {"SequenceDeadzoneTimeLimit_ms", deadzoneTimeLimit_ms},
            {"SequenceType", System.Enum.GetName(typeof(SequenceType), keyboardSequence)},
        };
        loggingManager.Log("Meta", metaLog);
    }

    public void CreateNewSequenceLogs() {
        currentKeySequenceLogs = new Dictionary<string, List<string>>();
        currentKeySequenceLogs["Date"] = new List<string>();
        currentKeySequenceLogs["Timestamp"] = new List<string>();
        currentKeySequenceLogs["Event"] = new List<string>();
        currentKeySequenceLogs["KeyCode"] = new List<string>();
        currentKeySequenceLogs["SequenceTime_ms"] = new List<string>();
        currentKeySequenceLogs["TimeSinceLastKey_ms"] = new List<string>();
        currentKeySequenceLogs["KeyOrder"] = new List<string>();
        currentKeySequenceLogs["KeyType"] = new List<string>();
        currentKeySequenceLogs["ExpectedKey1"] = new List<string>();
        currentKeySequenceLogs["ExpectedKey2"] = new List<string>();
        currentKeySequenceLogs["SequenceNumber"] = new List<string>();
        currentKeySequenceLogs["SequenceComposition"] = new List<string>();
        currentKeySequenceLogs["SequenceSpeed"] = new List<string>();
        currentKeySequenceLogs["SequenceValidity"] = new List<string>();
        currentKeySequenceLogs["SequenceType"] = new List<string>();
        currentKeySequenceLogs["SequenceWindowClosure"] = new List<string>();
    }

    void Update() {
        time_ms += Time.deltaTime;
        deadzoneTime_ms += Time.deltaTime;
        Debug.Log("sequenceState: " + System.Enum.GetName(typeof(SequenceState), sequenceState));
        if(sequenceState == SequenceState.Playing) {
            sequenceWindowClosure = SequenceWindowClosure.Open;
            sequenceTime_ms += Time.deltaTime;
            timeSinceLastPress_ms += Time.deltaTime;

            // If we have enough keys to assess whether the sequence can be validated, do so.
            if (currentKeySequenceLogs["Event"].Count == keysToPress.GetLength(0)) {
                sequenceWindowClosure = SequenceWindowClosure.ClosedByInputThreshold;
                SequenceData sequenceData = CheckCapturedKeys();
                InputData inputData = new InputData();
                inputData.validity = InputValidity.Rejected;
                inputData.confidence = 0;
                inputData.inputNumber = sequenceData.sequenceNumber;
                inputData.type = InputType.KeySequence;
                if (sequenceData.sequenceValidity == SequenceValidity.Accepted) {
                    inputData.validity = InputValidity.Accepted;
                    inputData.confidence = 1;
                }
                //if (state == SequenceValidity.Accepted) {
                onKeySequenceFinished.Invoke(sequenceData, inputData);
                onInputFinished.Invoke(inputData);
                //}
                sequenceState = SequenceState.Stopped;

            } else if (deadzoneTime_ms > deadzoneTimeLimit_ms) {
                sequenceWindowClosure = SequenceWindowClosure.ClosedByDeadzone;
               Debug.Log("No key pressed for " + deadzoneTimeLimit_ms + "seconds, sequence stopped.");
                SequenceData sequenceData = CheckCapturedKeys();
                InputData inputData = new InputData();
                inputData.validity = InputValidity.Rejected;
                inputData.confidence = 0;
                inputData.inputNumber = sequenceData.sequenceNumber;
                inputData.type = InputType.KeySequence;
                //if (state == SequenceValidity.Accepted) {
                onKeySequenceFinished.Invoke(sequenceData, inputData);
                onInputFinished.Invoke(inputData);
                //}
                sequenceState = SequenceState.Stopped;
            }
        } else {
          sequenceTime_ms = 0f;  
          timeSinceLastPress_ms = 0f;
        }
    }


    void OnGUI()
    {
        Event e = Event.current;
        if (e == null) {
            return;
        }
        if (e.isKey)
        {
            if (e.keyCode == KeyCode.None) {
                return;
            }
            if (Event.current.type == EventType.KeyDown) {
                if (e.keyCode == lastKey || e.keyCode == lastKey2) {
                    // If we detect a new key, but its the same as the previous key, then discard it.
                    return;
                }
               Debug.Log("Key is " + e.keyCode.ToString());
                // TODO: Log EventType.KeyUp too
               Debug.Log("Detected key code: " + e.keyCode + " time:" + time_ms);
                currentKeySequenceLogs["Date"].Add(System.DateTime.Now.ToString("yyyy-MM-dd"));
                currentKeySequenceLogs["Timestamp"].Add(System.DateTime.Now.ToString("HH:mm:ss.ffff"));
                currentKeySequenceLogs["Event"].Add("KeyDown");
                currentKeySequenceLogs["KeyCode"].Add(e.keyCode.ToString());
                currentKeySequenceLogs["SequenceTime_ms"].Add(sequenceTime_ms.ToString());
                currentKeySequenceLogs["TimeSinceLastKey_ms"].Add(timeSinceLastPress_ms.ToString());
                timeSinceLastPress_ms = 0f;
                sequenceState = SequenceState.Playing;
                deadzoneTime_ms = 0f;
                lastKey2 = lastKey;
                lastKey = e.keyCode;
                onKeyDown.Invoke(e.keyCode);
            }
        }
    }

    private SequenceData CheckCapturedKeys() {
        //if (currentKeySequenceLogs["Event"].Count == 0) {
            // no sequence available, dont do anything.
            //return;
        //}
        SequenceData sequenceData = new SequenceData();
        sequenceData.sequenceValidity = SequenceValidity.Accepted;
        sequenceData.sequenceSpeed = SequenceSpeed.Fast;
        sequenceData.sequenceComposition = SequenceComposition.Correct;
        sequenceData.sequenceType = keyboardSequence;
        sequenceData.sequenceWindowClosure = sequenceWindowClosure;
        sequenceData.sequenceNumber = sequenceNumber;
        sequenceData.keyCount = currentKeySequenceLogs["Event"].Count;

        // populate currentKeySequenceLogs with WrongKey values.
        for (int j = 0; j < currentKeySequenceLogs["Event"].Count; j++) {
           Debug.Log("Populating for Key: " + currentKeySequenceLogs["KeyCode"][j].ToString());
            currentKeySequenceLogs["KeyOrder"].Add("NA");
            currentKeySequenceLogs["KeyType"].Add("WrongKey");
            currentKeySequenceLogs["ExpectedKey1"].Add("NA");
            currentKeySequenceLogs["ExpectedKey2"].Add("NA");
        }

        for (int i = 0; i < keysToPress.GetLength(0); i++) {
            if (i >= currentKeySequenceLogs["KeyCode"].Count) {
                break;
            }

            // for each i, we need to check if the first key pressed, matches either keysToPress[i,0] or [i,1]
           Debug.Log("Checking Key: " + currentKeySequenceLogs["KeyCode"][i]);
           Debug.Log("i = " + i + ", keysToPress: " + keysToPress.GetLength(0) + " currentKeySequenceLogs: " + currentKeySequenceLogs["KeyCode"].Count);
            if (currentKeySequenceLogs["KeyCode"][i] == keysToPress[i,0].ToString() || currentKeySequenceLogs["KeyCode"][i] == keysToPress[i,1].ToString()) {
                currentKeySequenceLogs["KeyOrder"][i] = i.ToString();
                currentKeySequenceLogs["KeyType"][i]  = "CorrectKey";
                currentKeySequenceLogs["ExpectedKey1"][i] = keysToPress[i,0].ToString();
                currentKeySequenceLogs["ExpectedKey2"][i] = keysToPress[i,1].ToString();
            } else {
                // if any keys do not match the desired key, reject it.
                sequenceData.sequenceComposition = SequenceComposition.Mistyped;
                sequenceData.sequenceValidity = SequenceValidity.Rejected;
                currentKeySequenceLogs["KeyOrder"][i] = "NA";
                currentKeySequenceLogs["KeyType"][i] = "WrongKey";
                currentKeySequenceLogs["ExpectedKey1"][i] = keysToPress[i,0].ToString();
                currentKeySequenceLogs["ExpectedKey2"][i] = keysToPress[i,1].ToString();
            }
            
        }

        // If the sequence was played too slowly, reject it.
       Debug.Log("sequenceTime_ms: " + sequenceTime_ms + ", sequenceTimeLimit_ms: " + sequenceTimeLimit_ms);
        if (sequenceTime_ms > sequenceTimeLimit_ms) {
            sequenceData.sequenceSpeed = SequenceSpeed.Slow;
            sequenceData.sequenceValidity = SequenceValidity.Rejected;
        }

        // If the sequence contains too many keys, reject it.
        if (currentKeySequenceLogs["Event"].Count > keysToPress.GetLength(0)) {
            sequenceData.sequenceComposition = SequenceComposition.Mistyped;
            sequenceData.sequenceValidity = SequenceValidity.Rejected;
        } else if (currentKeySequenceLogs["Event"].Count < keysToPress.GetLength(0)) {
            sequenceData.sequenceSpeed = SequenceSpeed.Slow;
            sequenceData.sequenceValidity = SequenceValidity.Rejected;
        }

        for (int j = 0; j < currentKeySequenceLogs["Event"].Count; j++) {
            currentKeySequenceLogs["SequenceNumber"].Add(sequenceData.sequenceNumber.ToString());
            currentKeySequenceLogs["SequenceComposition"].Add(System.Enum.GetName(typeof(SequenceComposition), sequenceData.sequenceComposition));
            currentKeySequenceLogs["SequenceSpeed"].Add(System.Enum.GetName(typeof(SequenceSpeed), sequenceData.sequenceSpeed));
            currentKeySequenceLogs["SequenceValidity"].Add(System.Enum.GetName(typeof(SequenceValidity), sequenceData.sequenceValidity));
            currentKeySequenceLogs["SequenceType"].Add(System.Enum.GetName(typeof(SequenceType), sequenceData.sequenceType));
            currentKeySequenceLogs["SequenceWindowClosure"].Add(System.Enum.GetName(typeof(SequenceWindowClosure), sequenceData.sequenceWindowClosure));
        }
        currentKeySequenceLogs["Event"].Add("KeySequenceStopped");
        currentKeySequenceLogs["Date"].Add(System.DateTime.Now.ToString("yyyy-MM-dd"));
        currentKeySequenceLogs["Timestamp"].Add(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff"));
        currentKeySequenceLogs["KeyCode"].Add("NA");
        currentKeySequenceLogs["SequenceTime_ms"].Add(sequenceTime_ms.ToString());
        currentKeySequenceLogs["TimeSinceLastKey_ms"].Add(timeSinceLastPress_ms.ToString());
        currentKeySequenceLogs["KeyOrder"].Add("NA");
        currentKeySequenceLogs["KeyType"].Add("NA");
        currentKeySequenceLogs["SequenceNumber"].Add(sequenceData.sequenceNumber.ToString());
        currentKeySequenceLogs["SequenceComposition"].Add(System.Enum.GetName(typeof(SequenceComposition), sequenceData.sequenceComposition));
        currentKeySequenceLogs["SequenceSpeed"].Add(System.Enum.GetName(typeof(SequenceSpeed), sequenceData.sequenceSpeed));
        currentKeySequenceLogs["SequenceValidity"].Add(System.Enum.GetName(typeof(SequenceValidity), sequenceData.sequenceValidity));
        currentKeySequenceLogs["SequenceType"].Add(System.Enum.GetName(typeof(SequenceType), sequenceData.sequenceType));
        currentKeySequenceLogs["SequenceWindowClosure"].Add(System.Enum.GetName(typeof(SequenceWindowClosure), sequenceData.sequenceWindowClosure));
        currentKeySequenceLogs["ExpectedKey1"].Add("NA");
        currentKeySequenceLogs["ExpectedKey2"].Add("NA");
        
        for (int k = 0; k < currentKeySequenceLogs["Event"].Count; k++) {
                loggingManager.Log("KeyLog", "Timestamp", currentKeySequenceLogs["Timestamp"][k]);
                loggingManager.Log("KeyLog", "Event", currentKeySequenceLogs["Event"][k]);
                loggingManager.Log("KeyLog", "KeyCode", currentKeySequenceLogs["KeyCode"][k]);
                loggingManager.Log("KeyLog", "SequenceTime_ms", currentKeySequenceLogs["SequenceTime_ms"][k]);
                loggingManager.Log("KeyLog", "TimeSinceLastKey_ms", currentKeySequenceLogs["TimeSinceLastKey_ms"][k]);
                loggingManager.Log("KeyLog", "KeyOrder", currentKeySequenceLogs["KeyOrder"][k]);
                loggingManager.Log("KeyLog", "KeyType", currentKeySequenceLogs["KeyType"][k]);
                loggingManager.Log("KeyLog", "ExpectedKey1", currentKeySequenceLogs["ExpectedKey1"][k]);
                loggingManager.Log("KeyLog", "ExpectedKey2", currentKeySequenceLogs["ExpectedKey2"][k]);
                loggingManager.Log("KeyLog", "SequenceNumber", currentKeySequenceLogs["SequenceNumber"][k]);
                loggingManager.Log("KeyLog", "SequenceComposition", currentKeySequenceLogs["SequenceComposition"][k]);
                loggingManager.Log("KeyLog", "SequenceSpeed", currentKeySequenceLogs["SequenceSpeed"][k]);
                loggingManager.Log("KeyLog", "SequenceValidity", currentKeySequenceLogs["SequenceValidity"][k]);
                loggingManager.Log("KeyLog", "SequenceType", currentKeySequenceLogs["SequenceType"][k]);
                loggingManager.Log("KeyLog", "SequenceWindowClosure", currentKeySequenceLogs["SequenceWindowClosure"][k]);
        }

        loggingManager.SaveLog("KeyLog");
        loggingManager.ClearLog("KeyLog");

        sequenceNumber++;

        sequenceData.keySequenceLogs = new Dictionary<string, List<string>>(currentKeySequenceLogs);

       /*foreach (string key in currentKeySequenceLogs.Keys)
        {
            keySequenceLogs[key].AddRange(currentKeySequenceLogs[key]);
            Debug.Log("Key: " + key + ", Count: " + keySequenceLogs[key].Count.ToString());
        }*/

       /*foreach (string key in currentKeySequenceLogs.Keys)
        {
            Debug.Log("Key: " + key + ", Count: " + currentKeySequenceLogs[key].Count.ToString());
            currentKeySequenceLogs[key].Clear();
        }*/
        CreateNewSequenceLogs();
        return sequenceData;
    }

    // LOGGING 


    /*public void LogKeySequence() {
        if (keySequenceLogs["Event"].Count == 0) {
           Debug.Log("Nothing to log, returning..");
            return;
        }

       Debug.Log("Saving " + keySequenceLogs["Event"].Count + " Rows to " + filepath);
        sequenceNumber = 0;
        string dest = filepath + "\\" + filename + "_" + System.DateTime.Now.ToString("HH_mm_ss") + ".csv";

        // Log Header
        string[] keys = new string[keySequenceLogs.Keys.Count];
        keySequenceLogs.Keys.CopyTo(keys, 0);
        string dbCols = string.Join(sep, keys).Replace("\n", string.Empty);

        using (StreamWriter writer = File.AppendText(dest))
        {
            writer.WriteLine(dbCols);
        }

        // Create a string with the data
        List<string> dataString = new List<string>();
        for (int i = 0; i < keySequenceLogs["Event"].Count; i++)
        {
            List<string> row = new List<string>();
            foreach (string key in keySequenceLogs.Keys)
            {
                row.Add(keySequenceLogs[key][i]);
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

        // Clear keySequenceLogs
       foreach (string key in keySequenceLogs.Keys)
        {
            
            keySequenceLogs[key].Clear();
        }
    }*/

}