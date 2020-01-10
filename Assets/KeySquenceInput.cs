using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using System.IO;

public class KeySquenceInput : MonoBehaviour
{

    public enum KeyboardSequenceType {
     HKJL, // HK + J + L
     TYUI, // T + YU + I
     SRWES  // SR + W + ES
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

    [SerializeField]
    private float sequenceTimeLimit_ms = 1.5f; // the longest time that the sequence may take (500 ms time limit)
    [SerializeField]
    private float deadzoneTimeLimit_ms = 1f; // the time it takes before we consider an input to belong to a new sequence.

    [SerializeField]
    private KeyboardSequenceType keyboardSequence = KeyboardSequenceType.HKJL;
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

    string filepath;
    string filename = "keysequencedata";
    string sep = ",";
    int sequenceNumber = 0;

    [Serializable]
    public class OnKeySequenceAccepted : UnityEvent<SequenceValidity> { }
    public OnKeySequenceAccepted onKeySequenceAccepted;

    [Serializable]
    public class OnKeyDown : UnityEvent<KeyCode> { }
    public OnKeyDown onKeyDown;

    // Start is called before the first frame update
    void Start()
    {
        filepath = Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
        lastKey = KeyCode.None;
        keySequenceLogs = new Dictionary<string, List<string>>();
        keySequenceLogs["Date"] = new List<string>();
        keySequenceLogs["Timestamp"] = new List<string>();
        keySequenceLogs["Event"] = new List<string>();
        keySequenceLogs["KeyCode"] = new List<string>();
        keySequenceLogs["SequenceTime_ms"] = new List<string>();
        keySequenceLogs["TimeSinceLastKey_ms"] = new List<string>();
        keySequenceLogs["KeyOrder"] = new List<string>();
        keySequenceLogs["KeyType"] = new List<string>();
        keySequenceLogs["ExpectedKey1"] = new List<string>();
        keySequenceLogs["ExpectedKey2"] = new List<string>();
        keySequenceLogs["SequenceNumber"] = new List<string>();
        keySequenceLogs["SequenceComposition"] = new List<string>();
        keySequenceLogs["SequenceSpeed"] = new List<string>();
        keySequenceLogs["SequenceValidity"] = new List<string>();
        keySequenceLogs["SequenceType"] = new List<string>();
        keySequenceLogs["SequenceWindowClosure"] = new List<string>();

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
        
        if (keyboardSequence == KeyboardSequenceType.HKJL) {
            keysToPress = new KeyCode[4,2]; // 3 sequences, up to 2 keys simultaneously.
            keysToPress[0,0] = KeyCode.H; // In Slot 0 and 1 we check for both H or K keys 
            keysToPress[0,1] = KeyCode.K;
            keysToPress[1,0] = KeyCode.H;
            keysToPress[1,1] = KeyCode.K;
            keysToPress[2,0] = KeyCode.J;
            keysToPress[2,1] = KeyCode.None;
            keysToPress[3,0] = KeyCode.L;
            keysToPress[3,1] = KeyCode.None;
        }

        if (keyboardSequence == KeyboardSequenceType.TYUI) { // T + YU + I
            keysToPress = new KeyCode[4,2]; // 3 sequences, up to 2 keys simultaneously.
            keysToPress[0,0] = KeyCode.T; // In Slot 0 and 1 we check for both H or K keys 
            keysToPress[0,1] = KeyCode.None;
            keysToPress[1,0] = KeyCode.Y;
            keysToPress[1,1] = KeyCode.U;
            keysToPress[2,0] = KeyCode.Y;
            keysToPress[2,1] = KeyCode.U;
            keysToPress[3,0] = KeyCode.I;
            keysToPress[3,1] = KeyCode.None;
        }

        if (keyboardSequence == KeyboardSequenceType.SRWES) { // SR + W + ES
            keysToPress = new KeyCode[5,2]; // 3 sequences, up to 2 keys simultaneously.
            keysToPress[0,0] = KeyCode.S; // In Slot 0 and 1 we check for both H or K keys 
            keysToPress[0,1] = KeyCode.R;
            keysToPress[1,0] = KeyCode.S;
            keysToPress[1,1] = KeyCode.R;
            keysToPress[2,0] = KeyCode.W;
            keysToPress[2,1] = KeyCode.None;
            keysToPress[3,0] = KeyCode.E;
            keysToPress[3,1] = KeyCode.S;
            keysToPress[4,0] = KeyCode.E;
            keysToPress[4,1] = KeyCode.S;
        }        

    }

    void Update() {
        time_ms += Time.deltaTime;
        deadzoneTime_ms += Time.deltaTime;
        Debug.Log("sequenceState: " + System.Enum.GetName(typeof(SequenceState), sequenceState));
        if (sequenceState == SequenceState.Playing) {
            sequenceWindowClosure = SequenceWindowClosure.Open;
            sequenceTime_ms += Time.deltaTime;
            timeSinceLastPress_ms += Time.deltaTime;

            // If we have enough keys to assess whether the sequence can be validated, do so.
            if (currentKeySequenceLogs["Event"].Count == keysToPress.GetLength(0)) {
                sequenceWindowClosure = SequenceWindowClosure.ClosedByInputThreshold;
                SequenceValidity state = CheckCapturedKeys();
                if (state == SequenceValidity.Accepted) {
                    onKeySequenceAccepted.Invoke(state);
                }
                sequenceState = SequenceState.Stopped;

            } else if (deadzoneTime_ms > deadzoneTimeLimit_ms) {
                sequenceWindowClosure = SequenceWindowClosure.ClosedByDeadzone;
                Debug.Log("No key pressed for " + deadzoneTimeLimit_ms + "seconds, sequence stopped.");
                SequenceValidity state = CheckCapturedKeys();
                if (state == SequenceValidity.Accepted) {
                    onKeySequenceAccepted.Invoke(state);
                }
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
                if (e.keyCode == lastKey) {
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
                lastKey = e.keyCode;
                onKeyDown.Invoke(e.keyCode);
            }
        }
    }

    private SequenceValidity CheckCapturedKeys() {
        //if (currentKeySequenceLogs["Event"].Count == 0) {
            // no sequence available, dont do anything.
            //return;
        //}
        SequenceValidity sequenceValidity = SequenceValidity.Accepted;
        SequenceSpeed sequenceSpeed = SequenceSpeed.Fast;
        SequenceComposition sequenceComposition = SequenceComposition.Correct;

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
                sequenceComposition = SequenceComposition.Mistyped;
                sequenceValidity = SequenceValidity.Rejected;
                currentKeySequenceLogs["KeyOrder"][i] = "NA";
                currentKeySequenceLogs["KeyType"][i] = "WrongKey";
                currentKeySequenceLogs["ExpectedKey1"][i] = keysToPress[i,0].ToString();
                currentKeySequenceLogs["ExpectedKey2"][i] = keysToPress[i,1].ToString();
            }
            
        }

        // If the sequence was played too slowly, reject it.
        if (sequenceTime_ms > sequenceTimeLimit_ms) {
            sequenceSpeed = SequenceSpeed.Slow;
            sequenceValidity = SequenceValidity.Rejected;
        }

        // If the sequence contains too many keys, reject it.
        if (currentKeySequenceLogs["Event"].Count > keysToPress.GetLength(0)) {
            sequenceComposition = SequenceComposition.Mistyped;
            sequenceValidity = SequenceValidity.Rejected;
        } else if (currentKeySequenceLogs["Event"].Count < keysToPress.GetLength(0)) {
            sequenceSpeed = SequenceSpeed.Slow;
            sequenceValidity = SequenceValidity.Rejected;
        }

        for (int j = 0; j < currentKeySequenceLogs["Event"].Count; j++) {
            currentKeySequenceLogs["SequenceNumber"].Add(sequenceNumber.ToString());
            currentKeySequenceLogs["SequenceComposition"].Add(System.Enum.GetName(typeof(SequenceComposition), sequenceComposition));
            currentKeySequenceLogs["SequenceSpeed"].Add(System.Enum.GetName(typeof(SequenceSpeed), sequenceSpeed));
            currentKeySequenceLogs["SequenceValidity"].Add(System.Enum.GetName(typeof(SequenceValidity), sequenceValidity));
            currentKeySequenceLogs["SequenceType"].Add(System.Enum.GetName(typeof(KeyboardSequenceType), keyboardSequence));
            currentKeySequenceLogs["SequenceWindowClosure"].Add(System.Enum.GetName(typeof(SequenceWindowClosure), sequenceWindowClosure));
        }
        currentKeySequenceLogs["Event"].Add("KeySequenceStopped");
        currentKeySequenceLogs["Date"].Add(System.DateTime.Now.ToString("yyyy-MM-dd"));
        currentKeySequenceLogs["Timestamp"].Add(System.DateTime.Now.ToString("HH:mm:ss.ffff"));
        currentKeySequenceLogs["KeyCode"].Add("NA");
        currentKeySequenceLogs["SequenceTime_ms"].Add(sequenceTime_ms.ToString());
        currentKeySequenceLogs["TimeSinceLastKey_ms"].Add(timeSinceLastPress_ms.ToString());
        currentKeySequenceLogs["KeyOrder"].Add("NA");
        currentKeySequenceLogs["KeyType"].Add("NA");
        currentKeySequenceLogs["SequenceNumber"].Add(sequenceNumber.ToString());
        currentKeySequenceLogs["SequenceComposition"].Add(System.Enum.GetName(typeof(SequenceComposition), sequenceComposition));
        currentKeySequenceLogs["SequenceSpeed"].Add(System.Enum.GetName(typeof(SequenceSpeed), sequenceSpeed));
        currentKeySequenceLogs["SequenceValidity"].Add(System.Enum.GetName(typeof(SequenceValidity), sequenceValidity));
        currentKeySequenceLogs["SequenceType"].Add(System.Enum.GetName(typeof(KeyboardSequenceType), keyboardSequence));
        currentKeySequenceLogs["SequenceWindowClosure"].Add(System.Enum.GetName(typeof(SequenceWindowClosure), sequenceWindowClosure));
        currentKeySequenceLogs["ExpectedKey1"].Add("NA");
        currentKeySequenceLogs["ExpectedKey2"].Add("NA");

        sequenceNumber++;

       foreach (string key in currentKeySequenceLogs.Keys)
        {
            keySequenceLogs[key].AddRange(currentKeySequenceLogs[key]);
            Debug.Log("Key: " + key + ", Count: " + keySequenceLogs[key].Count.ToString());
        }

       foreach (string key in currentKeySequenceLogs.Keys)
        {
            Debug.Log("Key: " + key + ", Count: " + currentKeySequenceLogs[key].Count.ToString());
            currentKeySequenceLogs[key].Clear();
        }
        return sequenceValidity;
    }


    // LOGGING

    public void LogKeySequence() {
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
    }

}