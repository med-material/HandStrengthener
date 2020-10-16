using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SoundState {
    RejectSound,
    CorrectSound,
    None
}

public class SoundManager : MonoBehaviour
{
    [SerializeField]
    private AudioClip correctSound;
    [SerializeField]
    private AudioClip wrongSound;
    AudioSource audioSource;
    private SoundState soundState = SoundState.None;
    private bool madeDecision = false;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void OnGameDecision(GameDecisionData decisionData) {
        if (decisionData.decision == TrialType.AccInput) {
            audioSource.PlayOneShot(correctSound,0.75f);
            soundState = SoundState.CorrectSound;
        } else if (decisionData.decision == TrialType.FabInput) {
            audioSource.PlayOneShot(correctSound,0.75f);
            soundState = SoundState.CorrectSound;
        } else {
            soundState = SoundState.RejectSound;
            audioSource.PlayOneShot(wrongSound,1f);
            soundState = SoundState.None;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
