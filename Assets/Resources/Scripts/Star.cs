using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace jackyjjc {
	public class Star : MonoBehaviour {

		// Use this for initialization
		void Start () {
			gameObject.GetComponent<Animator> ().Play ("star", -1, Random.Range (0, 1));
		}
		
		// Update is called once per frame
		void Update () {
			
		}
	}
}