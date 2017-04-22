using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Game : MonoBehaviour {
	public static readonly float PLANET_GRAVITY = 10f;
	public static readonly float ROTATION_SPEED = 50f;
	public static readonly float BLOCK_LANDING_PUSH = 5f;
	public static readonly float PLANET_ROTATE_ANGLE_PER_SECOND = 0.8f;
	public static readonly float MAX_ALLOWED_ANGLE = 20f;

	private float blockDefaultFallingSpeed = 0.3f;
	private float blockAccelerateSpeed = 5f;

	private GameObject blockPrefab;

	// Variables from the inspector
	public GameObject planet;
	private float planetRadius;
	public GameObject landedBlocks;
	public bool debug = true;

	public GameObject playerControlBlock;
	private float landingSpeed;

	private float currentSpawnHeight = 5;
	private float currentHighest;

	HashSet<GameObject> toBeRemove;

	// Variables for camera control
	public float cameraZoomSpeed = 1;
	public float cameraTargetSize = 1;

	// Block types
	public static readonly BlockType[] blockTypes = new BlockType[] {
		new BlockType("foundation", 5, Color.red),
		new BlockType("room", 2, Color.blue),
		new BlockType("electriciy", 1, Color.yellow)
	};

	void Start () {
		this.toBeRemove = new HashSet<GameObject> ();

		planetRadius = planet.GetComponent<CircleCollider2D> ().bounds.size.x / 2;
		blockPrefab = Resources.Load<GameObject>("Prefabs/Block");
		Camera.main.orthographicSize = currentSpawnHeight + 1;
		cameraTargetSize = Camera.main.orthographicSize;
	}
	
	void Update () {
		// If player doesn't control any block, spawn one
		if (playerControlBlock == null) {
			SpawnBlock();
		}

		if (currentSpawnHeight - planetRadius - currentHighest <= 1) {
			//need to scale up the spawn height
			Debug.Log(currentSpawnHeight - planetRadius - currentHighest + " is ");
			currentSpawnHeight += 1;
			cameraTargetSize = currentSpawnHeight + 0.5f;
		} else if (currentSpawnHeight - planetRadius - currentHighest >= 3) {
			// need to scale down the spawn height
			currentSpawnHeight -= 2;
			cameraTargetSize = currentSpawnHeight + 0.5f;
		}

		if (Mathf.Abs (Camera.main.orthographicSize - cameraTargetSize) > float.Epsilon) {
			Camera.main.orthographicSize = Mathf.MoveTowards (Camera.main.orthographicSize, cameraTargetSize, cameraZoomSpeed * Time.deltaTime);
		}
	}

	void SpawnBlock() {
		GameObject newBlock = Instantiate (blockPrefab, new Vector3(0, currentSpawnHeight, 0), Quaternion.identity);
		newBlock.transform.RotateAround(Vector3.zero, Vector3.forward, Random.Range(0, 360));

		BlockType blockType = blockTypes[Mathf.FloorToInt(Random.Range (0, blockTypes.Length))];
		newBlock.GetComponent<SpriteRenderer> ().color = blockType.color;
		newBlock.GetComponent<Rigidbody2D> ().mass = blockType.mass;

		playerControlBlock = newBlock;
		landingSpeed = blockDefaultFallingSpeed;
	}

	void FixedUpdate() {
		// Apply gravity to all landed objects and clean up fallen objects
		Rigidbody2D[] children = landedBlocks.GetComponentsInChildren<Rigidbody2D>();
		foreach(Rigidbody2D rigidBody in children) {
			GameObject go = rigidBody.gameObject;
			Vector3 gravityDirection = (planet.transform.position - go.transform.position).normalized;
			if (rigidBody.IsSleeping () || (rigidBody.velocity.magnitude < 0.01f)) {
				float angle = Vector3.Angle (-gravityDirection, go.transform.rotation * Vector3.up);
				if (angle > MAX_ALLOWED_ANGLE) {
					toBeRemove.Add (go);
					continue;
				}
			}

			rigidBody.AddForce (gravityDirection * PLANET_GRAVITY);
		}



		// recalcualte the heighest
		if (toBeRemove.Count > 0) {
			currentHighest = children.Max (c => CalculateBlockHeight (c.gameObject));
		}

		foreach (var go in toBeRemove) {
			Destroy (go);
		}
		toBeRemove.Clear ();

		// Player control object have different physics to make things easier
		if (playerControlBlock != null) {
			
			// Apply landing force
			Vector3 gravityDirection = (planet.transform.position - playerControlBlock.transform.position).normalized;

			// landing speed is a constant
			if (Input.GetKey ("down")) {
				landingSpeed += blockAccelerateSpeed * Time.deltaTime;
			}
			//Debug.Log ("fallilng speed is " + landingSpeed);
			playerControlBlock.transform.position += gravityDirection * landingSpeed * Time.deltaTime;

			// Apply rotation
			playerControlBlock.transform.RotateAround(planet.transform.position, Vector3.forward, -1 * Input.GetAxis("Horizontal") * ROTATION_SPEED * Time.deltaTime);		
		}

		planet.transform.RotateAround (Vector3.zero, Vector3.forward, - PLANET_ROTATE_ANGLE_PER_SECOND * Time.deltaTime);
		landedBlocks.transform.RotateAround (Vector3.zero, Vector3.forward, - PLANET_ROTATE_ANGLE_PER_SECOND * Time.deltaTime);
	}

	public void ActiveBlockCollision(Collision2D collision) {
		GameObject activeBlock = collision.otherCollider.gameObject;
		GameObject collidee = collision.collider.gameObject;

		// Player cannot control this block anymore because it just landed
		playerControlBlock = null;

		Destroy(activeBlock.GetComponent<ActiveBlockCollider>());
		activeBlock.GetComponent<Rigidbody2D> ().constraints = RigidbodyConstraints2D.None;
		activeBlock.transform.SetParent (landedBlocks.transform);

		float height = CalculateBlockHeight (activeBlock);
		if (height > currentHighest) {
			currentHighest = height;
		}
	}

	public float CalculateBlockHeight(GameObject block) {
		// calculate the distance between the block and the center of the planet
		return Vector3.Distance(block.transform.position, planet.transform.position) - planetRadius;
	}
}

public class BlockType {
	public readonly string name;
	public readonly int mass;
	public readonly Color color;

	public BlockType(string name, int mass, Color color) {
		this.name = name;
		this.mass = mass;
		this.color = color;
	}
}