using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PracticeModeKeyPressed : MonoBehaviour
{
    public Text keyPressedText;
    public Text result;
    
    // Start is called before the first frame update
    void Start()
    {
        keyPressedText = this.GetComponent<Text>();
        keyPressedText.text = "";
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void onKeyDown(KeyCode keyCode) {
        keyPressedText.text += keyCode.ToString();
    }

    public void onKeySequenceFinished(SequenceData sequenceData, InputData inputData) {
        keyPressedText.text = "";
        result.text = System.Enum.GetName(typeof(SequenceComposition), sequenceData.sequenceComposition) + ", " + System.Enum.GetName(typeof(SequenceSpeed), sequenceData.sequenceSpeed);

    }
}
