using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    BlinkDetection,
    FabInput
}

public class InputManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
