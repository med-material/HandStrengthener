using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    
    public enum InputTypes {
        KeyboardSequenceInput,
        FabricatedInput,
        RejectAllInput,
    }

    public enum GameStates {
        StartCountdown,
        InterTrialInterval,
        InputWindow,
    }

    private List<GameStates> gameProcedure;
    private List<InputTypes> inputOrder;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
