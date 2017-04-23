using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.SceneManagement;

namespace jackyjjc {
	public class Game : MonoBehaviour {
		// ====================== All State Variables =====================
		public GameObject screenDimming;
		private GameState currentGameState;
		private bool finishedInit;

		// Variables for camera control
		public float cameraZoomSpeed = 1;
		public float cameraTargetSize = 1;

		// ====================== Main Menu State Variables =======================
		public GameObject titleScreen;
		public GameObject[] tutorials;
		public int currentTutorialIndex;

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
			"- Successfully land at least 5 pods." +
			"- Have a score of 90 or more.",
		};
			
		private int currentLevel;
		private ILevel currentLevelObject;
		private ILevel[] levels = new ILevel[] {
			new Level1()
		};

		// ====================== endgame variables ========================
		public GameObject endGameScreen;
		private Text titleText;
		private Text BodyText;
		private Text finalScoreText;

		// ====================== Gameplay variables ========================
		public static readonly float PLANET_GRAVITY = 10f;
		public static readonly float ROTATION_SPEED = 50f;
		public static readonly float BLOCK_LANDING_PUSH = 5f;
		public static readonly float PLANET_ROTATE_ANGLE_PER_SECOND = 0.8f;
		public static readonly float MAX_ALLOWED_ANGLE = 20f;
		public static readonly float MAX_ALLOWED_SIDEWAY_ANGLE = 40f;

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
		private int _score;
		internal int Score {
			get { return _score; }
			set { 
				_score = value; 
				this.scoreText.text = "Score: " + _score;
			}
		}
		private BlockType upNext;

		// UI
		public GameObject popUpTextContainer;
		private GameObject goalTextPrefab;
		private GameObject goalListPanel;
		private Text[] goalList;
		private GameObject popUpTextPrefab;
		private Text goalText;
		private Text scoreText;
		private GameObject infoPanel;
		private Image upNextImage;
		private Text upNextDescription;
		private Text remainingText;

		// Block types
		internal BlockType[] blockTypes;

		void Start () {
			// Initialise variables and load things
			planetRadius = planet.GetComponent<CircleCollider2D> ().bounds.size.x / 2;
			blockPrefab = Resources.Load<GameObject>("Prefabs/Block");
			blockSize = blockPrefab.GetComponent<BoxCollider2D>().size.x * 1.5f;

			blockTypes = new BlockType[] {
				new BlockType("Basic Pod (Blue)", 1, 10, new Color(27/255f, 98/255f, 1f), Resources.Load<Sprite>("Images/block_male"), ""),
				new BlockType("Basic Pod (Pink)", 1, 10, new Color(1f, 85/255f, 85/255f), Resources.Load<Sprite>("Images/block_female"), "")
			};

			// Initialise all variables
			this.finishedInit = false;
			this.currentGameState = GameState.MAIN_MENU;
			this.scoreText = this.gameplayUI.transform.FindChild ("GoalPanel/ScoreText").GetComponent<Text> ();
			this.Score = 0;

			// Init main menu state
			this.titleScreen.SetActive (true);
			this.screenDimming.SetActive (true);
			foreach (GameObject go in tutorials) {
				go.SetActive (false);
			}
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

			// Init end game state
			this.endGameScreen.SetActive(false);
			this.titleText = endGameScreen.transform.FindChild ("Title").GetComponent<Text> ();
			this.finalScoreText = endGameScreen.transform.FindChild ("ScoreText").GetComponent<Text> ();
			this.BodyText = endGameScreen.transform.FindChild ("Body").GetComponent<Text> ();

			// Init gameplay state
			this.gameplayUI.SetActive(false);
			this.toBeRemove = new HashSet<GameObject> ();
			currentSpawnHeight = blockSize * 3 + planetRadius;
			cameraTargetSize = Camera.main.orthographicSize;
			this.numBlocksLanded = new Dictionary<BlockType, int> ();
			foreach (BlockType type in blockTypes) {
				numBlocksLanded [type] = 0;
			}
			this.goalListPanel = this.gameplayUI.transform.FindChild("GoalPanel").gameObject;
			this.goalTextPrefab = Resources.Load<GameObject> ("Prefabs/GoalText");
			this.popUpTextPrefab = Resources.Load<GameObject> ("Prefabs/PopUpTextParent");
			this.goalText = this.gameplayUI.transform.FindChild ("GoalPanel/GoalText").GetComponent<Text> ();
			this.infoPanel = this.gameplayUI.transform.FindChild ("InfoPanel").gameObject;
			this.upNextImage = this.infoPanel.transform.FindChild ("Panel/Image").GetComponent<Image> ();
			this.upNextDescription = this.infoPanel.transform.FindChild ("Panel/Text").GetComponent<Text> ();
			this.remainingText = this.infoPanel.transform.FindChild ("RemainingText").GetComponent<Text> ();

			this.finishedInit = true;
		}

		void Update () {
			if (finishedInit) {
				if (currentGameState == GameState.MAIN_MENU) {
					MainMenuUpdate ();
				} else if (currentGameState == GameState.INTRO) {
					UpdateIntro ();
				} else if (currentGameState == GameState.LEVEL_SWITCH) {
					UpdateLevelSwitch ();
				} else if (currentGameState == GameState.GAMEPLAY_STATE) {
					UpdateGameplay ();
				} else if (currentGameState == GameState.GAME_END) {
					UpdateEndGame ();
				}
			}

			// Camera always updates
			if (Mathf.Abs (Camera.main.orthographicSize - cameraTargetSize) > float.Epsilon) {
				Camera.main.orthographicSize = Mathf.MoveTowards (Camera.main.orthographicSize, cameraTargetSize, cameraZoomSpeed * Time.deltaTime);
			}
		}

		void FixedUpdate() {
			if (finishedInit && (currentGameState == GameState.GAMEPLAY_STATE || currentGameState == GameState.GAME_END)) {
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
					DestroyBlock (go);
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

		public void Tutorial() {
			titleScreen.SetActive (false);
			Input.ResetInputAxes ();
			currentTutorialIndex = 0;
			tutorials [currentTutorialIndex].SetActive (true);
		}

		private void MainMenuUpdate() {
			if (!titleScreen.activeSelf && Input.GetKeyUp("space")) {
				tutorials [currentTutorialIndex].SetActive (false);
				currentTutorialIndex++;
				if (currentTutorialIndex >= tutorials.Length) {
					titleScreen.SetActive (true);
					currentTutorialIndex = 0;
				} else {
					tutorials [currentTutorialIndex].SetActive (true);
				}
			}
		}

		// ============================ Intro ============================
		private void StartIntro() {
			finishedInit = false;
			currentGameState = GameState.INTRO;
			introScreen.SetActive (true);
			Input.ResetInputAxes ();
			this.currentIntroLine = 0;
			this.currentIntroCharacter = 0;
			this.introDialogText.text = "";
			this.cameraZoomSpeed = 0.2f;
			this.skipPrompt.text = "Press 'S' to skip intro";
			finishedInit = true;
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
			this.cameraZoomSpeed = 1f;
			StartLevelSwitch ();
		}

		// ============================ level Switch ==============================
		private void StartLevelSwitch() {
			this.finishedInit = false;
			this.screenDimming.SetActive (true);
			this.levelSwitchScreen.SetActive (true);
			this.levelText.text = "Level " + (this.currentLevel + 1);
			this.goalText.text = "Level " + (this.currentLevel + 1) + " goals: ";
			this.finishedInit = true;
			currentGameState = GameState.LEVEL_SWITCH;
		}

		private void UpdateLevelSwitch() {
			// display level outline
			briefingText.text = briefingTexts[this.currentLevel];
			this.currentLevelObject = levels [this.currentLevel];
			this.currentLevelObject.Init ();
			if (Input.GetKeyUp ("space")) {
				this.screenDimming.SetActive (false);
				this.levelSwitchScreen.SetActive (false);
				StartGamePlay ();
			}
		}

		// ============================ Gameplay ==============================
		private void StartGamePlay() {
			this.finishedInit = false;
			this.gameplayUI.SetActive (true);
			this.goalList = null;
			UpdateGoalList ();
			UpdateUpNext ();
			this.finishedInit = true;
			currentGameState = GameState.GAMEPLAY_STATE;
		}

		private void UpdateGameplay() {
			// If player doesn't control any block, spawn one
			if (playerControlBlock == null) {
				SpawnBlock();
			}

			if (currentSpawnHeight - currentHighest <= 1.2f * blockSize) {
				//need to scale up the spawn height
				currentSpawnHeight += 1.2f * blockSize;
				cameraTargetSize = currentSpawnHeight;
			} else if (currentHighest > float.Epsilon && currentSpawnHeight - currentHighest >= 2.2f) {
				// need to scale down the spawn height
				currentSpawnHeight -= 1.2f * blockSize;
				cameraTargetSize = currentSpawnHeight;
			}
		}

		private void SpawnBlock() {
			if (upNext == null) {
				LevelEnd ();
				return;
			}

			GameObject newBlock = Instantiate (blockPrefab, new Vector3(0, currentSpawnHeight, 0), Quaternion.identity);
			newBlock.transform.RotateAround(Vector3.zero, Vector3.forward, Random.Range(0, 360));

			newBlock.GetComponent<Block> ().blockType = upNext;
			newBlock.GetComponent<Rigidbody2D> ().mass = upNext.mass;
			newBlock.GetComponent<SpriteRenderer> ().sprite = upNext.sprite;

			playerControlBlock = newBlock;
			landingSpeed = blockDefaultFallingSpeed;

			UpdateUpNext ();
		}

		public void ActiveBlockCollision(Collision2D collision) {
			GameObject activeBlock = collision.otherCollider.gameObject;
			GameObject collidee = collision.collider.gameObject;

			// Player cannot control this block anymore because it just landed
			playerControlBlock = null;

			bool landedSuccessfully = true;
			bool isPerfect = false;

			if (!collidee.name.Equals ("Planet")) {
				Vector3 activeBlockDirection = (planet.transform.position - activeBlock.transform.position).normalized;
				Vector3 activeBlockCollideeDirection = (activeBlock.transform.position - collidee.transform.position).normalized;
				float angle = Vector3.Angle (-activeBlockDirection, activeBlockCollideeDirection);
				landedSuccessfully = angle < MAX_ALLOWED_SIDEWAY_ANGLE;
				if (landedSuccessfully) {
					// rotate to the correct
					Vector3 collideeDirection = (planet.transform.position - collidee.transform.position).normalized;
					float correctAngle = Vector3.Angle (activeBlockDirection, collideeDirection);
					if (correctAngle > 0 && correctAngle < 1) {
						activeBlock.transform.RotateAround(planet.transform.position, Vector3.forward, -correctAngle);
						isPerfect = true;
					}
				}
			}

			if (landedSuccessfully) {
				Destroy (activeBlock.GetComponent<ActiveBlockCollider> ());
				activeBlock.GetComponent<Rigidbody2D> ().constraints = RigidbodyConstraints2D.None;
				activeBlock.transform.SetParent (landedBlocks.transform);

				float height = CalculateBlockHeight (activeBlock);
				if (height > currentHighest) {
					currentHighest = height;
				}

				BlockType blockType = activeBlock.GetComponent<Block> ().blockType;
				numBlocksLanded [blockType] += 1;

				int scoreMultiplier = 1;
				if (isPerfect) {
					scoreMultiplier++;
					CreatePopupText (activeBlock.transform.position, activeBlock.transform.rotation, "Perfect! x2", blockType.color);
				}
				Score = Score + blockType.score * scoreMultiplier;

				// Update the level goal list
				UpdateGoalList ();

				if (currentLevelObject.isAllGoalsSatisfied (this)) {
					LevelEnd ();
				}
			} else {
				DestroyBlock (activeBlock);
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

		private Text CreatePopupText(Vector3 gameLocation, Quaternion rotation, string text, Color color) {
			Text popup = CreatePopupText (gameLocation, rotation, text);
			popup.color = color;
			return popup;
		}

		private Text CreatePopupText(Vector3 gameLocation, Quaternion rotation, string text) {
			GameObject popUpText = Instantiate<GameObject> (popUpTextPrefab, this.popUpTextContainer.transform);
			popUpText.transform.position = Camera.main.WorldToScreenPoint(gameLocation);
			popUpText.transform.rotation = rotation;
			popUpText.GetComponentInChildren<Text> ().text = text;
			return popUpText.GetComponentInChildren<Text>();
		}

		private void DestroyBlock(GameObject block) {
			BlockType blockType = block.GetComponent<Block> ().blockType;
			numBlocksLanded [blockType] -= 1;
			Score = Score - blockType.score;
			Destroy (block);
		}

		private void UpdateUpNext() {
			this.remainingText.text = currentLevelObject.GetNumRemainingBlocks() + " pods remaining\nUp next:";

			upNext = currentLevelObject.getNextBlock (this);
			if (upNext != null) {
				infoPanel.SetActive (true);
				upNextImage.sprite = upNext.sprite;
				upNextDescription.text = upNext.name
					+ (upNext.description.Length == 0 ? "" : "\n" + upNext.description)
					+ "\nMass: " + upNext.mass
					+ "\nScore: " + upNext.score;
			} else {
				infoPanel.SetActive (false);
			}
		}

		private void LevelEnd() {
			bool isPassed = currentLevelObject.isAllGoalsSatisfied (this);
			if (isPassed) {
				if (currentLevel >= levels.Length - 1) {
					StartEndGame (true);
				} else {
					// TODO advance level;
				}
			} else {
				// game ends
				StartEndGame(false);
			}
		}

		// ============================ endScreen ==============================
		private void StartEndGame(bool isWon) {
			this.finishedInit = false;
			this.screenDimming.SetActive (true);
			this.endGameScreen.SetActive (true);
			this.titleText.text = (isWon ? "You've won!" : "Try Again!");
			this.finalScoreText.text = "Your final score is: " + Score;
			this.BodyText.text = "Thank you very much for spending time playing my game! Let me know your high score or any feedbacks you might have! You can comment on the itch.io page or @jackyjjc on Twitter.";
			this.finishedInit = true;
			currentGameState = GameState.GAME_END;
		}

		private void UpdateEndGame() {
			if (Input.GetKeyUp ("space")) {
				SceneManager.LoadScene (SceneManager.GetActiveScene ().buildIndex);
			} else if (Input.GetKeyUp ("s")) {
				if (this.screenDimming.activeSelf) {
					this.screenDimming.SetActive (false);
					this.endGameScreen.SetActive (false);
				} else {
					this.screenDimming.SetActive (true);
					this.endGameScreen.SetActive (true);
				}
			}
		}
	}

	public class BlockType {
		public readonly string name;
		public readonly string description;
		public readonly int mass;
		public readonly int score;
		public readonly Color color;
		public readonly Sprite sprite;

		public BlockType(string name, int mass, int score, Color color, Sprite sprite, string description) {
			this.name = name;
			this.mass = mass;
			this.color = color;
			this.score = score;
			this.sprite = sprite;
			this.description = description;
		}
	}

	public enum GameState {
		MAIN_MENU,
		INTRO,
		LEVEL_SWITCH,
		GAMEPLAY_STATE,
		GAME_END
	}

	public abstract class ILevel {
		public abstract int numGoals ();
		public abstract string[] renderGoalText(Game game);
		public abstract bool isGoalSatisfied (Game game, int i);
		protected abstract BlockType _getNextBlock (Game game);

		private int _numRemaining = 0; 
		public int GetNumRemainingBlocks () {
			return _numRemaining;
		}
		protected void SetNumRemainingBlocks (int numRemaining) {
			_numRemaining = numRemaining;
		}

		public BlockType getNextBlock (Game game) {
			if (_numRemaining > 0) {
				_numRemaining--;
				return _getNextBlock (game);
			}
			return null;
		}

		public virtual void Init () {}

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
				string.Format ("- Land {0} / 5 pods", game.numBlocksLanded.Values.Sum ()),
				string.Format ("- Score {0} >= 90", game.Score)
			};
		}

		public override void Init() {
			base.SetNumRemainingBlocks (10);
		}

		protected override BlockType _getNextBlock (Game game)
		{
			return game.blockTypes [Mathf.FloorToInt (Random.Range (0, 2))];
		}

		public override bool isGoalSatisfied(Game game, int index) {
			switch (index) {
			case 0:
				return game.numBlocksLanded.Values.Sum () >= 5;
			case 1:
				return game.Score >= 90;
			default:
				return false;
			}
		}

		public override int numGoals() {
			return 2;
		}
	}
}
