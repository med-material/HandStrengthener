using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrnEntryType {
    public string name;
    public UrnEntryBehavior behavior = UrnEntryBehavior.Override;
    public int count = -1;
}

public enum UrnEntryBehavior {
    Override,
    Persist
}

public class UrnModel : MonoBehaviour
{



    // EntryTypes contain the different types of entries the urn generates the order from.
    private Dictionary<string,UrnEntryType> entryTypes = new Dictionary<string, UrnEntryType>();
    // DesignedOrder becomes smaller as entries are drawn from the urn.
    private List<string> designedOrder = new List<string>();
    // ResultedOrder contains the result when entries are drawn from the urn.
    private List<string> resultedOrder = new List<string>();

    private int index = -1;

    public void NewUrn() {
        index = 0;
        CreateNewOrder();
    }

    public void AddUrnEntryType(string name, UrnEntryBehavior behavior, int count) {
        var entryType = new UrnEntryType();
        entryType.name = name;
        entryType.behavior = behavior;
        entryType.count = count;
        entryTypes.Add(entryType.name, entryType);
    }

    public int GetIndex() {
        return index;
    }

    public string ReadEntry() {
        if (index >= designedOrder.Count) {
            return null;
        }
        return designedOrder[index];
    }

    public void SetEntryResult(string result) {
        resultedOrder[index] = result;

        if (result == designedOrder[index]) {
            index++;
            return;
        } else {
            string entryName = designedOrder[index];
            if (entryTypes[entryName].behavior == UrnEntryBehavior.Persist) {
                OverrideEntryInOrder(entryName);
            }
            index++;
            Debug.Log("New Resulted Order: " + ListToString(resultedOrder));
            Debug.Log("New Designed Order: " + ListToString(designedOrder));
        }
    }

    public Dictionary<string,int> GetEntriesLeft() {
        var entriesLeft = new Dictionary<string, int>();

        foreach (KeyValuePair<string, UrnEntryType> pair in entryTypes) {
            var entryType = pair.Value;
            entriesLeft[entryType.name] = 0;
        }

        foreach(string name in designedOrder) {
            entriesLeft[name]++;
        }

        foreach(string name in resultedOrder) {
            if (name != "None") {
                if (entriesLeft[name] != 0) entriesLeft[name]--;
            }
        }

        return entriesLeft;
    }

    public Dictionary<string,int> GetEntryResults() {
        var entryResults = new Dictionary<string, int>();

        foreach (KeyValuePair<string, UrnEntryType> pair in entryTypes) {
            var entryType = pair.Value;
            entryResults[entryType.name] = 0;
        }

        foreach(string name in resultedOrder) {
            if (name != "None") {
                entryResults[name]++;
            }
        }
        return entryResults;
    }

    private void CreateNewOrder() {
        designedOrder.Clear();

        if (entryTypes.Count == 0) {
            Debug.LogError("No urn entry types registered. Use AddUrnEntryType() to register new entry types.");
            return;
        }

        // Add the desired amount of entries to designed order.
        foreach (KeyValuePair<string, UrnEntryType> pair in entryTypes) {
            var entryType = pair.Value;
            for (int i = 0; i < entryType.count; i++) {
                designedOrder.Add(entryType.name);
                resultedOrder.Add("None");
            }
        }

        // Shuffle the items.
        Utils.Shuffle(designedOrder);
        Debug.Log("New Designed Order: " + ListToString(designedOrder));
    }

    private void OverrideEntryInOrder(string entryName) {
        string name;
        for (int i = index; i < designedOrder.Count; i++) {
            name = designedOrder[i];
            if (entryTypes[name].behavior == UrnEntryBehavior.Override) {
                Debug.Log("Overwriting " + name + " with " + entryName + "at position " + i.ToString());
                designedOrder[i] = entryName;
                return;
            }
        }
        Debug.LogWarning("Warning: No more override entries available.");
        return;
    }

    private string ListToString(List<string> theList) {
        string theString = "";
        foreach (var i in theList) {
            theString += i + " ";
        }
        return ("[ " + theString + " ] (" + theList.Count + ")");
    }

}
