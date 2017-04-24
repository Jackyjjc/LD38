using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace jackyjjc {
	public class Block : MonoBehaviour {

		public BlockType blockType;

		public void SelfDestruct(Game game) {
			GetComponent<Animator> ().SetTrigger ("BlockDestroyed");
			Destroy (gameObject, 1.5f);
			AudioSource audio = GetComponent<AudioSource> ();
			audio.clip = game.explode;
			audio.PlayDelayed(1.2f);
		}

		public void Land(Game game) {
			AudioSource audio = GetComponent<AudioSource> ();
			audio.PlayOneShot (game.landd);
		}
	}
}