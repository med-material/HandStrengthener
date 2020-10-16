using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrangeBehavior : MonoBehaviour
{

    [SerializeField]
    Material wrongMat;
    Material correctMat;

    [SerializeField]
    Animator anim;

    private Renderer renderer;

    // Start is called before the first frame update
    void Start()
    {
        renderer = GetComponent<Renderer>();
        correctMat = renderer.material;
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.eulerAngles = new Vector3(this.transform.eulerAngles.x, this.transform.eulerAngles.y+0.05f, this.transform.eulerAngles.z);
    }

    public void onGameDecision(GameDecisionData decisionData) {
        if (decisionData.decision == TrialType.AccInput) {
            ActivateSuccessFeedback();
            //Debug.Log("Showing Feedback from Real Input.");
        } else if (decisionData.decision == TrialType.FabInput) {
            ActivateSuccessFeedback();
            //Debug.Log("Showing Feedback from Fabricated Input.");
        } else {
            StartCoroutine("Wrong");
        }

    }

    public void ActivateSuccessFeedback() {
        StartCoroutine("Squeeze");
        //anim.Play("ballSqueeze");

    }

    public void ActivateBioFeedback() {
        anim.Play("SmallSqueeze");
    }

    IEnumerator Wrong() {
        renderer.material = wrongMat;
        yield return new WaitForSeconds(0.5f);
        renderer.material = correctMat;
    }
    
    // TODO: Present a stimuli

    IEnumerator Squeeze() {
        //transform.localScale = new Vector3(0.5f,1f,1f);
        yield return new WaitForSeconds(0.1f);
        anim.Play("Squeezed");
        //Reset();
    }

    public void Reset() {
        transform.localScale = new Vector3(1f,1f,1f);
    }
}
