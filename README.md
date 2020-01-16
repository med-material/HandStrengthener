# Hand Strengther
![Hand Strengthener Interface](https://raw.githubusercontent.com/med-material/HandStrengthener/master/hand-strengthener.png)

## About
The hand strengthener is an application for rehabilitation of hand opening and closening in stroke patients. The hand-strengthener features the following:
* Ability to use and log keyboard sequences ´HK+J+L´, ´T+YU+I´ or ´SR+W+ES´ as input (to mimick BCI-like input).
* Show a Virtual 3D hand strengthener which can be squeezed.
* Log all key events.
* Fabricate Input and Emulate "Rejected Input" (to simulate BCI-like input). 

## Roadmap
In the long-term the hand strengthener will connect to BCI and EMG equipment, to receive input from these classifiers, and inject input if no input is registered.

## Contributors
Done at Aalborg University.   
- **Bastian ILSO** - _Developer_ - [MED Material](https://github.com/med-material)

## Jan2020 Experimental Procedure
_Final Procedure Design applies only to participant 302, 303 and 304_

GameManager settings:
```
FabInput = 0.0 / 0.1 / 0.2 / 0.3
Recognition Rate = 0.4
Trials = 20
InputWindowSeconds = 2
InterTrialIntervalSeconds = 2.5
FabricationAlarm = 1 (when should fabricated input be activated)
FabricationAlarmVariability = 0.25 (which means Fab.Input. happens between 0.75 - 1.25 seconds)
```

KeyInputSequencer settings **(New Settings, applied to P. 302, 303, 304, introduced after the new practice mode)**:
```
Deadzone = 0.75
SequenceTimeLimit_ms = 1.5
```
KeyInputSequencer settings **(Old Settings, applied to other participants, this made sequencing too hard)**:
```
Deadzone = 0.4
SequenceTimeLimit_ms = 1.3
```

1. The facilitator uses a [randomizer](https://www.random.org/integer-sets/), to choose order of conditions and order of key sequences.  **TODO: For the future, we should make sure that this is counter-balanced.**
1. The participant is welcomed and given a demographical GDPR sheet where we ask for Age, Gender, Name and assign the participant a participant number.
1. First, the participant is introduced to the task. "_In this task, you are to press a sequence of keys in order to make a ball squeeze. A black bar will move and when it reaches the green area, you are allowed to press the sequence, but only once. You will be trying with 4 different sequence of keys. The first one is HK (at the same time), J and then L. Now you will be given some opportunity to practice it._
1. The facilitator informs the participant to hold the keys with the same 4 fingers at all time, so as to not loose track of what keys he is pressing. **(Applied only to P.302, 303, 304, other participants could press the keys as they wish but only with one hand.)**
1. The practice application is opened and the participant get to practice the key sequence. The practice application tells the participant whether the participant was slow or mistyped the sequence. The participant gets to practice until he can perform the sequences reliably at the desired speed **(Applied only to P.302, 303, 304, other participants were allowed to train before playing but received no feedback on the speed/correctness and so had less clue as to what constituted a correct input.)**
1. The facilitator asks whether the participant is ready and then switches over to the real application.
1. The facilitator starts a video recording and starts the test. The facilitator covers the keyboard so the participant cannot look down at the keys the participant is pressing. **(Video recordings only available for participants 101, 402, 210, 404, 108, 602, 603, 604)**
1. Behind the scenes, our program chooses to either fabricate input, accept the input (if it is correct), or reject the input.
1. After 20 trials, the participant is given a survey, to answer how much he felt frustrated and how much he felt in control of the ball squeezing. **(Survey only consistent for 101, 402, 210, 404, 108, 602, 603, 604)**
1. Then the participant proceeds to try the next key sequence. After all four sequences, and answering the survey 4 times, a new survey is given to the participant for the whole experiment. **(consistent for all participants)**
1. The facilitator should now make sure to rename the CSV files and video recordings to include the participant's number.
1. The participant is then in a short audio-recorded qualitative interview, about his perception of controlling the ball squeezing. What he suspected was affecting his performance and what was on his mind when answering control/frustration survey questions. Then the participant is debriefed about the experiments true purpose, and told about the injection of input, as well as the purposeful rejection of input. **(audio recordings available for 302j, 302c, 303, 206, 101, 402, 210, 404, 601, 108, 303, 604)**

## Technical details
### Logged Data
Data is currently logged locally into a ´keysequencedata´ csv file.
 * **Date**: date you started the application (e.g. 01/29/19)
 * **Timestamp**: time you started the application (e.g. 03:01:17.1234)
 * **Event**: Some event in the application fx "KeyDown" or "KeySequenceStopped".
 * **KeyCode**: For KeyDown events, the Keycode specifies what key was pressed (Fx "K").
 * **SequenceTime_ms**: For Sequences, specifies how much time has passed in the sequence.
 * **TimeSinceLastKey_ms**: For Sequences, how much time has passed since last key was pressed (reset at beginning of sequence).
 * **KeyOrder**: Where in the expected sequence order, the detected key is. The value is "NA" if the Key appeared in a wrong or in an unexpected order.
 * **KeyType**: "CorrectKey" or "WrongKey" - depending on whether the keyCode matches up with the ExpectedKey1/ExpectedKey2 or not.
 * **ExpectedKey1**: Which Key was used to match with KeyCode sessions timer, has values such as "H".
 * **ExpectedKey2**: Which Other Key was used to match with KeyCode sessions timer, has values such as "H". ExpectedKey1 and 2 has values when simultaneous keypresses are expected.
 * **SequenceNumber**: An identifier for which sequence this is (Sequence 1, 2, 3).
 * **SequenceComposition**: Whether the Composition of the sequence was correct or mistyped.
 * **SequenceSpeed**: Whether the sequence was made fast enough, before deadzone time kicked in.
 * **SequenceValidity**: Whether the sequence was accepted or rejected.
 * **SequenceType**: What sequence the player was asked to play, fx "HKJL".
 * **SequenceWindowClosure**: What caused the sequence to end, can either be "Open" (never), "ClosedByInputThreshold" or "ClosedByDeadzone".
