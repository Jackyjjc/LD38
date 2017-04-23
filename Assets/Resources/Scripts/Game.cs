using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace jackyjjc {
	public class Game : MonoBehaviour {
		// ====================== All State Variables =====================
		public GameObject screenDimming;
		private GameState currentGameState;

		// Variables for camera control
		public float cameraZoomSpeed = 1;
		public float cameraTargetSize = 1;

		// ====================== Main Menu State Variables =======================
		public GameObject titleScreen;

		// ====================== Intro State Variables ===================
		public GameObject introScreen;
		private Text skipPrompt;
		private Text introDialogText;
		private int currentIntroLine;
		private int currentIntroCharacter;
		private static readonly string[] introLines = new string[] {
			"Scout: Central, Yes... We have finally found a habitable planet!!",
			"Central: Awesome! We will start to send our people over! We can't even fit a pin here anymore.",
			"Scout: Wait... But.. This planet is very small.",
			"Central: *beep* *beep* *beep*",
			"Scout: Central!!?"
		};

		// ====================== Level Switch variables ========================
		public GameObject levelSwitchScreen;
		private Text levelText;
		private Text briefingText;
		private static readonly string[] briefingTexts = new string[] {
			"Our employer started sending migrants over to this planet even though we think this planet is too small. Sigh...I guess we have to prepare the landing of the first batch of the migrants." +
			"\n\nGoal:\n" +
			"- Successfully land at least 5 pods.",
		};
			
		private int currentLevel;
		private ILevel currentLevelObject;
		private ILevel[] levels = new ILevel[] {
			new Level1()
		};

		// ====================== Gameplay variables ========================
		public static readonly float PLANET_GRAVITY = 10f;
		public static readonly float ROTATION_SPEED = 50f;
		public static readonly float BLOCK_LANDING_PUSH = 5f;
		public static readonly float PLANET_ROTATE_ANGLE_PER_SECOND = 0.8f;
		public static readonly float MAX_ALLOWED_ANGLE = 20f;

		private float blockDefaultFallingSpeed = 0.3f;
		private float blockAccelerateSpeed = 5f;

		// Variables from the inspector
		public GameObject planet;
		public GameObject landedBlocks;
		public GameObject gameplayUI;
		public bool debug = true;

		private GameObject blockPrefab;
		private float blockSize;
		private float planetRadius;

		private GameObject playerControlBlock;
		private float landingSpeed;
		private float currentSpawnHeight;
		private float currentHighest;

		private HashSet<GameObject> toBeRemove;

		// Game stats
		internal Dictionary<BlockType, int> numBlocksLanded;

		// UI
		private GameObject goalTextPrefab;
		private GameObject goalListPanel;
		private Text[] goalList;

		// Block types
		private static readonly BlockType[] blockTypes = new BlockType[] {
			new BlockType("foundation", 5, Color.red),
			new BlockType("room", 2, Color.blue),
			new BlockType("electriciy", 1, Color.yellow)
		};

		void Start () {
			// Initialise variables and load things
			planetRadius = planet.GetComponent<CircleCollider2D> ().bounds.size.x / 2;
			blockPrefab = Resources.Load<GameObject>("Prefabs/Block");
			blockSize = blockPrefab.GetComponent<BoxCollider2D>().size.x;

			// Initialise all variables
			this.currentGameState = GameState.MAIN_MENU;

			// Init main menu state
			this.titleScreen.SetActive (true);
			this.screenDimming.SetActive (true);
			this.titleScreen.transform.FindChild ("MainMenu/NewGame").GetComponent<Selectable>().Select();
			Camera.main.orthographicSize = 8;

			// Init intro state
			this.introScreen.SetActive(false);
			this.introDialogText = introScreen.transform.FindChild ("Dialog/DialogText").GetComponent<Text>();
			this.skipPrompt = introScreen.transform.FindChild ("Dialog/SkipPrompt").GetComponent<Text>();


			// Init level switch state
			this.levelSwitchScreen.SetActive(false);
			this.currentLevel = 0;
			this.levelText = levelSwitchScreen.transform.FindChild ("LevelText").GetComponent<Text> ();
			this.briefingText = levelSwitchScreen.transform.FindChild ("BriefingText").GetComponent<Text> ();

			// Init gameplay state
			this.gameplayUI.SetActive(false);
			this.toBeRemove = new HashSet<GameObject> ();
			currentSpawnHeight = blockSize * 4 + planetRadius;
			cameraTargetSize = Camera.main.orthographicSize;
			this.numBlocksLanded = new Dictionary<BlockType, int> ();
			foreach (BlockType type in blockTypes) {
				numBlocksLanded [type] = 0;
			}
			this.goalListPanel = this.gameplayUI.transform.FindChild("GoalPanel/").gameObject;
			this.goalTextPrefab = Resources.Load<GameObject> ("Prefabs/GoalText");
		}

		void Update () {
			if (currentGameState == GameState.INTRO) {
				UpdateIntro ();
			} else if (currentGameState == GameState.LEVEL_SWITCH) {
				UpdateLevelSwitch ();
			} else if (currentGameState == GameState.GAMEPLAY_STATE) {
				UpdateGameplay ();
			}

			// Camera always updates
			if (Mathf.Abs (Camera.main.orthographicSize - cameraTargetSize) > float.Epsilon) {
				Camera.main.orthographicSize = Mathf.MoveTowards (Camera.main.orthographicSize, cameraTargetSize, cameraZoomSpeed * Time.deltaTime);
			}
		}

		void FixedUpdate() {
			if (currentGameState == GameState.GAMEPLAY_STATE) {
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
					numBlocksLanded [go.GetComponent<Block> ().blockType] -= 1;
					Destroy (go);
				}
				toBeRemove.Clear ();

				UpdateGoalList ();

				// Player control object have different physics to make things easier
				if (playerControlBlock != null) {

					// Apply landing force
					Vector3 gravityDirection = (planet.transform.position - playerControlBlock.transform.position).normalized;

					// landing speed is a constant
					if (Input.GetKey ("down") || Input.GetKey("space")) {
						landingSpeed += blockAccelerateSpeed * Time.deltaTime;
					}
					//Debug.Log ("fallilng speed is " + landingSpeed);
					playerControlBlock.transform.position += gravityDirection * landingSpeed * Time.deltaTime;

					// Apply rotation
					playerControlBlock.transform.RotateAround(planet.transform.position, Vector3.forward, -1 * Input.GetAxis("Horizontal") * ROTATION_SPEED * Time.deltaTime);		
				}

				// Planet rotation 'animation' always happens
				planet.transform.RotateAround (Vector3.zero, Vector3.forward, - PLANET_ROTATE_ANGLE_PER_SECOND * Time.deltaTime);
				landedBlocks.transform.RotateAround (Vector3.zero, Vector3.forward, - PLANET_ROTATE_ANGLE_PER_SECOND * Time.deltaTime);
			}
		}

		private float CalculateBlockHeight(GameObject block) {
			// calculate the distance between the block and the center of the planet
			return Vector3.Distance(block.transform.position, planet.transform.position);
		}

		// ============================= Main Menu ================================
		public void StartGame() {
			// Hide the main menu, zoom the camera
			titleScreen.SetActive(false);
			cameraTargetSize = 4;
			screenDimming.SetActive (false);
			StartIntro ();
		}

		public void Exit() {
			Application.Quit();
		}

		// ============================ Intro ============================
		private void StartIntro() {
			currentGameState = GameState.INTRO;
			introScreen.SetActive (true);
			Input.ResetInputAxes ();
			this.currentIntroLine = 0;
			this.currentIntroCharacter = 0;
			this.introDialogText.text = "";
			this.cameraZoomSpeed = 0.2f;
			this.skipPrompt.text = "Press 'S' to skip intro";
		}

		private void UpdateIntro() {
			if (Input.GetKeyUp ("s")) {
				// skip intro
				Camera.main.orthographicSize = cameraTargetSize;
				ExitIntro();
				return;
			}

			if (Input.GetKeyUp ("space")) {
				if (currentIntroCharacter >= introLines [currentIntroLine].Length) {
					currentIntroLine++;
					currentIntroCharacter = 0;
					introDialogText.text = "";
					this.skipPrompt.text = "";
				} else {
					introDialogText.text += introLines [currentIntroLine].Substring (currentIntroCharacter);
					currentIntroCharacter = introLines [currentIntroLine].Length;
				}
				return;
			}

			if (currentIntroLine >= introLines.Length) {
				// finish intro
				ExitIntro();
				return;
			} else if (currentIntroCharacter < introLines [currentIntroLine].Length) {
				introDialogText.text += introLines [currentIntroLine] [currentIntroCharacter];
				currentIntroCharacter++;
			} else {
				this.skipPrompt.text = "Press 'Space' to continue";
			}
		}

		private void ExitIntro() {
			introScreen.SetActive (false);
			currentGameState = GameState.LEVEL_SWITCH;
			this.cameraZoomSpeed = 1f;
			StartLevelSwitch ();
		}

		// ============================ level Switch ==============================
		private void StartLevelSwitch() {
			this.screenDimming.SetActive (true);
			this.levelSwitchScreen.SetActive (true);
			this.levelText.text = "Level " + (this.currentLevel + 1);
		}

		private void UpdateLevelSwitch() {
			// display level outline
			briefingText.text = briefingTexts[this.currentLevel];
			this.currentLevelObject = levels [this.currentLevel];
			if (Input.GetKeyUp ("space")) {
				this.screenDimming.SetActive (false);
				this.levelSwitchScreen.SetActive (false);
				currentGameState = GameState.GAMEPLAY_STATE;
				StartGamePlay ();
			}
		}

		// ============================ Gameplay ==============================
		private void StartGamePlay() {
			this.gameplayUI.SetActive (true);
			UpdateGoalList ();
		}

		private void UpdateGameplay() {
			// If player doesn't control any block, spawn one
			if (playerControlBlock == null) {
				SpawnBlock();
			}

			if (currentSpawnHeight - currentHighest <= 2.5 * blockSize) {
				//need to scale up the spawn height
				currentSpawnHeight += 1.5f * blockSize;
				cameraTargetSize = currentSpawnHeight;
			} else if (currentSpawnHeight - planetRadius - currentHighest >= 3.5f) {
				// need to scale down the spawn height
				currentSpawnHeight -= 1.5f * blockSize;
				cameraTargetSize = currentSpawnHeight;
			}
		}

		private void SpawnBlock() {
			GameObject newBlock = Instantiate (blockPrefab, new Vector3(0, currentSpawnHeight, 0), Quaternion.identity);
			newBlock.transform.RotateAround(Vector3.zero, Vector3.forward, Random.Range(0, 360));

			BlockType blockType = blockTypes[Mathf.FloorToInt(Random.Range (0, blockTypes.Length))];
			newBlock.GetComponent<Block> ().blockType = blockType;
			newBlock.GetComponent<Rigidbody2D> ().mass = blockType.mass;

			playerControlBlock = newBlock;
			landingSpeed = blockDefaultFallingSpeed;
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

			numBlocksLanded [activeBlock.GetComponent<Block> ().blockType] += 1;

			// Update the level goal list
			UpdateGoalList();

			if (currentLevelObject.isAllGoalsSatisfied (this)) {
				// Go to the next level
				// If there is no next leve then win
				if (currentLevel >= levels.Length - 1) {
					// there is no more level, you win
					Debug.Log("win");
				} else {
					// go to next level
				}
			}
		}

		private void UpdateGoalList() {
			string[] goalTexts = this.currentLevelObject.renderGoalText (this);
			if (goalList == null) {
				goalList = new Text[goalTexts.Length];
				for (int i = 0; i < goalTexts.Length; i++) {
					goalList [i] = Instantiate<GameObject>(goalTextPrefab, goalListPanel.transform).GetComponent<Text>();
				}
			}

			for (int i = 0; i < goalTexts.Length; i++) {
				goalList [i].text = goalTexts [i];
				if (this.currentLevelObject.isGoalSatisfied (this, i)) {
					goalList[i].color = Color.green;
				} else {
					goalList [i].color = Color.white;
				}
			}
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

	public enum GameState {
		MAIN_MENU,
		INTRO,
		LEVEL_SWITCH,
		GAMEPLAY_STATE,
		WIN,
		LOSS
	}

	public abstract class ILevel {
		public abstract int numGoals ();
		public abstract string[] renderGoalText(Game game);
		public abstract bool isGoalSatisfied (Game game, int i);

		public bool isAllGoalsSatisfied(Game game) {
			bool result = true;
			int nGoals = numGoals ();
			for(int i = 0; i < nGoals; i++) {
				result &= isGoalSatisfied(game, i);
			}
			return result;
		}
	}

	public class Level1 : ILevel {
		public override string[] renderGoalText(Game game) {
			return new string[] {
				string.Format ("- Landed {0} / {1} pods", game.numBlocksLanded.Values.Sum (), 5)
			};
		}

		public override bool isGoalSatisfied(Game game, int index) {
			switch (index) {
			case 0:
				return game.numBlocksLanded.Values.Sum () >= 5; 
			default:
				return false;
			}
		}

		public override int numGoals() {
			return 1;
		}
	}
}
