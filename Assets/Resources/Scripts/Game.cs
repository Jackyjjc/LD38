using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour {

	public static readonly float FULL_CIRCULAR_DEGREE = 360;
	public static readonly float DEGREE_PER_SLOT = 30;
	public static readonly int NUM_SLOTS = Mathf.FloorToInt(FULL_CIRCULAR_DEGREE / DEGREE_PER_SLOT);
	public static readonly float BLOCK_FALLING_SPEED = 0.3f;
	public static readonly float BLOCK_LANDING_SPEED = 5f;

	public GameObject blockPrefab;

	// Variables from the inspector
	public GameObject planet;
	public bool debug = true;

	public GameObject currentlyControllBlock;
	public int currentSlot;

	private int heightOfTallestSlot = 0;
	public List<Block>[] slots;

	public 

	void Start () {
		this.slots = new List<Block>[NUM_SLOTS];
		for (int i = 0; i < NUM_SLOTS; i++) {
			this.slots [i] = new List<Block> ();
		}

		blockPrefab = Resources.Load<GameObject>("Prefabs/Block");
	}
	
	void Update () {
		
		// If there is no currently active block, create one
		if (currentlyControllBlock == null) {
			spawnNewBlock();
		}
	}

	void FixedUpdate() {
		if (currentlyControllBlock == null) {
			return;
		}

		// Apply downward force
		Vector3 direction = (Vector3.zero - currentlyControllBlock.transform.position).normalized;
		currentlyControllBlock.transform.position += direction * BLOCK_FALLING_SPEED * Time.deltaTime;
		if (Input.GetKey ("down")) {
			currentlyControllBlock.transform.position += direction * BLOCK_LANDING_SPEED * Time.deltaTime;
		}

		// Apply rotation
		int inputDir = 0;
		if (Input.GetKey ("left")) {
			inputDir = 1;
		} else if (Input.GetKey ("right")) {
			inputDir = -1;	
		}

		if (currentlyControllBlock != null && inputDir != 0) {
			currentSlot = ((currentSlot + inputDir) + NUM_SLOTS) % NUM_SLOTS;
			currentlyControllBlock.transform.RotateAround(Vector3.zero, Vector3.forward, DEGREE_PER_SLOT * inputDir * Time.deltaTime);
			return;
		}
	}

	public void ActiveBlockCollision(Collision2D collision) {
		GameObject activeBlock = collision.otherCollider.gameObject;
		if (debug && currentlyControllBlock == activeBlock) {
			currentlyControllBlock = null;
		}

		bool landingSucceeded = false;

		GameObject collidee = collision.collider.gameObject;
		Debug.Log ("collideee " + collidee.name + " " + collidee.name.Equals ("Planet"));
		if (collidee.name.Equals ("Planet")) {
			landingSucceeded = true;
		} else {
			if (collidee.GetComponent<Rigidbody2D> ()) {

			}


			Vector2 relativeVelocityDirection = (activeBlock.transform.position - collidee.transform.position).normalized;
			float angle = Vector2.Angle (relativeVelocityDirection, activeBlock.transform.position);
			Debug.Log ("angle is " + angle);
			if (angle > 40f) {
				Debug.Log ("exploded");
				landingSucceeded = false;
			} else {
				landingSucceeded = true;
			}
			// could be collided with another active block or a static block, check ridgid body

		}

		if (landingSucceeded) {
			// Put the block into the slot
			slots [currentSlot].Add (activeBlock.GetComponent<Block>());
			if (slots [currentSlot].Count > heightOfTallestSlot) {
				heightOfTallestSlot = slots [currentSlot].Count;
				// TODO: update camera size and intiial spawn position
			}

			activeBlock.GetComponent<Rigidbody2D> ().bodyType = RigidbodyType2D.Static;
			activeBlock.GetComponent<Rigidbody2D> ().isKinematic = false;
			activeBlock.transform.SetParent (planet.transform);
		} else {
			Destroy (activeBlock);
		}
	}

	public void spawnNewBlock() {
		GameObject newBlock = Instantiate (blockPrefab, new Vector3(0, 5, 0), Quaternion.identity);
		// need to rotate it to a random spot
		int slot =  Mathf.RoundToInt(Random.Range(0, NUM_SLOTS));
		newBlock.transform.RotateAround(Vector3.zero, Vector3.forward, DEGREE_PER_SLOT * slot);
		currentlyControllBlock = newBlock;
	}
}