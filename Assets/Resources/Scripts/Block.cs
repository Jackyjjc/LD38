using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace jackyjjc {
	public class Block : MonoBehaviour {

		public BlockType blockType;

		public void SelfDestruct() {
			GetComponent<Animator> ().SetTrigger ("BlockDestroyed");
			Destroy (gameObject, 1.5f);
		}
	}
}