using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrangeBehavior : MonoBehaviour
{

    [SerializeField]
    Animator anim;

    // Start is called before the first frame update
    void Start()
    {
        anim.Play("Squeezed");
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.eulerAngles = new Vector3(this.transform.eulerAngles.x, this.transform.eulerAngles.y+0.05f, this.transform.eulerAngles.z);
    }

    public void ActivateSuccessFeedback() {
        //StartCoroutine("Squeeze");
        anim.Play("Squeezed");
        //anim.Play("ballSqueeze");

    }

    // TODO: Present a stimuli

    IEnumerator Squeeze() {
        transform.localScale = new Vector3(0.5f,1f,1f);
        yield return new WaitForSeconds(0.5f);
        Reset();
    }

    public void Reset() {
        transform.localScale = new Vector3(1f,1f,1f);
    }
}
