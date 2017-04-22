using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveBlockCollider : MonoBehaviour {
	void OnCollisionEnter2D (Collision2D other) {
		GameObject go = GameObject.FindGameObjectWithTag ("GameController");
		go.SendMessage ("ActiveBlockCollision", other);
	}
}
