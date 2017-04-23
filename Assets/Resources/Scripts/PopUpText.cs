using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopUpText : MonoBehaviour {

	// Use this for initialization
	void Start () {
		AnimatorClipInfo[] clipInfo = GetComponentInChildren<Animator> ().GetCurrentAnimatorClipInfo (0);
		// Destroy the game object once the animation finishes
		Destroy (gameObject, clipInfo [0].clip.length);
	}
}
