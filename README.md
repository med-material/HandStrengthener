# Hand Strengther

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

 -----------------  
# Technical details
# Logged Data
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
