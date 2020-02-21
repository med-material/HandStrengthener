using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.UI;

public class DisplayData : MonoBehaviour
{	
  	[SerializeField]
    private Text bciStatus;

	[SerializeField]
	private Text miText;

	[SerializeField]
	private Text eventText;

	private OpenBCIInput controller;

    void Start()
    {
		controller = GameObject.Find("InputManager").GetComponent<OpenBCIInput>();	
    }

	public void updateBCIStatus(string newStatus, string subStatus) {
		bciStatus.text = newStatus + " (" + subStatus + ") ";
	}

	public void OnMotorImageryDetected(MotorImageryEvent value)  {
		StartCoroutine("showMI", value);
	}

	public void OnBCIEvent(float value) {
		eventText.text = value.ToString();
	}

	private IEnumerator showMI(MotorImageryEvent value) {
		miText.text = Enum.GetName(typeof(MotorImageryEvent), value);
		yield return new WaitForSeconds(0.15f);
		//miText.text = "";
		yield return null;
	}

}
