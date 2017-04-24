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

		private int currentLevel;
		private ILevel currentLevelObject;
		private ILevel[] levels = new ILevel[] {
			new Level1(),
			new Level2(),
			new Level3()
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
		public static readonly int RAYCAST_LAYER_MASK = 1 << 8;

		private float blockDefaultFallingSpeed = 0.5f;
		private float blockAccelerateSpeed = 5f;
		private float fallinSpeedMax = 1f;
		private float spawnHeightMultiplierMax = 1.5f;

		// Variables from the inspector
		public GameObject planet;
		public GameObject landedBlocks;
		public GameObject limboBlocks;
		public GameObject gameplayUI;
		public AudioClip explode;
		public AudioClip landd;
		public bool debug = true;

		private GameObject blockPrefab;
		private float blockSize;
		private float planetRadius;

		private GameObject playerControlBlock;
		private float landingSpeed;
		private float currentSpawnHeight;
		private float currentHighest;
		internal int currentHighestNumBlocks;

		private bool endlessMode;

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
		public Image audioButton;
		public Sprite audioOn;
		public Sprite audioOff;

		// Block types
		internal BlockType[] blockTypes;

		void Start () {
			// Initialise variables and load things
			planetRadius = planet.GetComponent<CircleCollider2D> ().bounds.size.x / 2;
			blockPrefab = Resources.Load<GameObject>("Prefabs/Block");
			blockSize = blockPrefab.GetComponent<BoxCollider2D>().size.x * 1.5f;

			blockTypes = new BlockType[] {
				new BlockType("Basic Living Pod (Blue)", 2, 10, new Color(27/256f, 98/256f, 1f), Resources.Load<Sprite>("Images/block_male"), ""),
				new BlockType("Basic Living Pod (Pink)", 2, 10, new Color(1f, 85/256f, 85/256f), Resources.Load<Sprite>("Images/block_female"), ""),
				new BlockType("Basic Shop (Green)", 3, 10, new Color(152/256f, 245/256f, 126/256f), Resources.Load<Sprite>("Images/shop_green"), "10 extra score for each level higher")
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
			currentSpawnHeight = blockSize * 4f + planetRadius;
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
			AudioListener.volume = 1f;
			audioButton.sprite = audioOn;
			this.endlessMode = false;

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
				// Apply gravity to all landed objects and clean them up
				Rigidbody2D[] landedBlocksRigidBody = landedBlocks.GetComponentsInChildren<Rigidbody2D>();
				foreach(Rigidbody2D rigidBody in landedBlocksRigidBody) {
					GameObject go = rigidBody.gameObject;
					Vector3 gravityDirection = (planet.transform.position - go.transform.position).normalized;
					if (rigidBody.IsSleeping () || (rigidBody.velocity.magnitude < 0.01f)) {
						float angle = Vector3.Angle (-gravityDirection, go.transform.rotation * Vector3.up);
						if (angle > MAX_ALLOWED_ANGLE) {
							DestroyBlock (go);
							continue;
						}
					}

					rigidBody.AddForce (gravityDirection * PLANET_GRAVITY);
				}

				// Apply gravity to to-be-destroyed blocks
				Rigidbody2D[] limboBlocksRigidBody = limboBlocks.GetComponentsInChildren<Rigidbody2D>();
				foreach(Rigidbody2D body in limboBlocksRigidBody) {
					Vector3 gravityDirection = (planet.transform.position - body.transform.position).normalized;
					body.AddForce (gravityDirection * PLANET_GRAVITY);
				}

				// recalcualte the heighest
				if (limboBlocksRigidBody.Length > 0) {
					currentHighest = landedBlocksRigidBody.Max (c => CalculateBlockHeight (c.gameObject));
					UpdateHighestAll ();
				}

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
			StartLevelSwitch (0);
		}

		// ============================ level Switch ==============================
		private void StartLevelSwitch(int nextLevel) {
			this.finishedInit = false;
			this.screenDimming.SetActive (true);
			this.levelSwitchScreen.SetActive (true);

			if (!endlessMode) {
				this.currentLevel = nextLevel;
				this.levelText.text = "Level " + (this.currentLevel + 1);
				this.goalText.text = "Level " + (this.currentLevel + 1) + " goals: ";
				this.currentLevelObject = levels [this.currentLevel];
			} else {
				this.currentLevel = 0;
				this.levelText.text = "Endless Mode";
				this.goalText.text = "";
				this.currentLevelObject = new Endless ();
			}

			this.currentLevelObject.Init (this);
			briefingText.text = currentLevelObject.GetBriefingText ();
			this.finishedInit = true;
			currentGameState = GameState.LEVEL_SWITCH;
		}

		private void UpdateLevelSwitch() {
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
			if (goalList != null) {
				foreach (Text goal in goalList) {
					Destroy (goal.gameObject);
				}
			}
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

			if (currentSpawnHeight - currentHighest <= spawnHeightMultiplierMax * blockSize) {
				//need to scale up the spawn height
				currentSpawnHeight += spawnHeightMultiplierMax * blockSize;
				cameraTargetSize = currentSpawnHeight - 0.5f;
			} else if (currentHighest > float.Epsilon && currentSpawnHeight - currentHighest >= spawnHeightMultiplierMax + 1) {
				// need to scale down the spawn height
				currentSpawnHeight -= spawnHeightMultiplierMax * blockSize;
				cameraTargetSize = currentSpawnHeight - 0.5f;
			}
		}

		private void SpawnBlock() {
			if (!endlessMode && upNext == null) {
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

			Destroy (activeBlock.GetComponent<ActiveBlockCollider> ());
			activeBlock.GetComponent<Rigidbody2D> ().constraints = RigidbodyConstraints2D.None;
			if (landedSuccessfully) {
				activeBlock.transform.SetParent (landedBlocks.transform);
				activeBlock.GetComponent<Block> ().Land (this);

				float height = CalculateBlockHeight (activeBlock);
				if (height > currentHighest) {
					currentHighest = height;
				}
				int activeBlockHeight = UpdateHighest (activeBlock);

				BlockType blockType = activeBlock.GetComponent<Block> ().blockType;
				numBlocksLanded [blockType] += 1;

				int basicScore = blockType.score;
				if (blockType.name.Equals ("Basic Shop (Green)")) {
					basicScore = basicScore + 10 * activeBlockHeight;
				}

				int scoreMultiplier = 1;
				if (isPerfect) {
					scoreMultiplier++;
					Vector3 randomPos = activeBlock.transform.position + (Quaternion.AngleAxis (Random.Range (0, 360), Vector3.forward) * Vector3.up).normalized * 0.25f;
					CreatePopupText (randomPos, activeBlock.transform.rotation, "Perfect! x2", blockType.color);
				}
				Score = Score + basicScore * scoreMultiplier;

				// Update the level goal list
				UpdateGoalList ();

				if (!endlessMode && currentLevelObject.isAllGoalsSatisfied (this)) {
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
			block.transform.SetParent (limboBlocks.transform);
			BlockType blockType = block.GetComponent<Block> ().blockType;
			numBlocksLanded [blockType] -= 1;
			Score = Score - blockType.score;
			block.GetComponent<Block> ().SelfDestruct (this);
		}

		private void UpdateUpNext() {
			if (endlessMode) {
				this.remainingText.text = "Up next:";
			} else {
				this.remainingText.text = currentLevelObject.GetNumRemainingBlocks () + " more pods remaining\nUp next:";
			}

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
					StartLevelSwitch (currentLevel + 1);
				}
			} else {
				// game ends
				StartEndGame(false);
			}
		}

		private int UpdateHighest(GameObject go) {
			Vector3 gravityDirection = (planet.transform.position - go.transform.position).normalized;
			RaycastHit2D[] hits = Physics2D.RaycastAll (go.transform.position, gravityDirection, Vector3.Distance (go.transform.position, Vector3.zero), RAYCAST_LAYER_MASK);
			int height = hits.Length + 1;
			//Debug.Log ("height: " + height);
			if (height > currentHighestNumBlocks) {
				currentHighestNumBlocks = height;
			}
			return height;
		}

		private void UpdateHighestAll() {
			HashSet<GameObject> done = new HashSet<GameObject> ();
			foreach (Transform go in landedBlocks.transform) {
				if (done.Contains (go.gameObject)) {
					continue;
				}

				Vector3 gravityDirection = (planet.transform.position - go.transform.position).normalized;
				RaycastHit2D[] hits = Physics2D.RaycastAll (go.transform.position, gravityDirection, Vector3.Distance (go.transform.position, Vector3.zero), RAYCAST_LAYER_MASK);
				int height = hits.Length + 1;
				if (height > currentHighestNumBlocks) {
					currentHighestNumBlocks = height;
				}
				foreach (RaycastHit2D hit in hits) {
					done.Add(hit.collider.gameObject);
				}
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
			} else if (Input.GetKeyUp ("c")) {
				this.screenDimming.SetActive (false);
				this.endGameScreen.SetActive (false);
				endlessMode = true;
				StartLevelSwitch (0);
			}
		}

		public void ToggleVolume() {
			if (AudioListener.volume > float.Epsilon) {
				AudioListener.volume = 0f;
				audioButton.sprite = audioOff;
			} else {
				AudioListener.volume = 1f;
				audioButton.sprite = audioOn;
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

	public class Utils {
		public static void Suffle(BlockType[] array) {
			for (int i = 0; i < array.Length; i++) {
				BlockType temp = array[i];
				int randomIndex = Mathf.FloorToInt(Random.Range(i, array.Length));
				array[i] = array[randomIndex];
				array[randomIndex] = temp;
			}
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
		public abstract string GetBriefingText();
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
			} else if (_numRemaining == -1) {
				// endless mode special
				return _getNextBlock (game);
			}
			return null;
		}

		public virtual void Init (Game game) {}

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
				string.Format ("- Score {0} >= 70", game.Score)
			};
		}

		public override void Init(Game game) {
			base.SetNumRemainingBlocks (8);
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
				return game.Score >= 70;
			default:
				return false;
			}
		}

		public override int numGoals() {
			return 2;
		}

		public override string GetBriefingText() {
			return "Our employer started sending migrants over to this planet even though we think this planet is too small. Sigh...I guess we have to prepare the landing of the first batch of the migrants." +
				"\n\nGoal:\n" +
				"- Successfully land at least 5 pods in total\n" +
				"- Have a score of 90 or more";
		}
	}

	public class Level2 : ILevel {
		public override string[] renderGoalText(Game game) {
			return new string[] {
				string.Format ("- Land {0} / 10 pods", game.numBlocksLanded.Values.Sum ()),
				string.Format ("- Score {0} >= 150", game.Score),
				string.Format ("- A stack with height of 4 (Current highest {0})", game.currentHighestNumBlocks)
			};
		}

		public override void Init(Game game) {
			base.SetNumRemainingBlocks (8);
		}

		protected override BlockType _getNextBlock (Game game)
		{
			return game.blockTypes [Mathf.FloorToInt (Random.Range (0, 2))];
		}

		public override bool isGoalSatisfied(Game game, int index) {
			switch (index) {
			case 0:
				return game.numBlocksLanded.Values.Sum () >= 10;
			case 1:
				return game.Score >= 150;
			case 2: 
				return game.currentHighestNumBlocks >= 4;
			default:
				return false;
			}
		}

		public override int numGoals() {
			return 2;
		}

		public override string GetBriefingText() {
			return "Nice job landing those pods! Now, they want us to make higher ones because the to-be residents went to protests for better views." +
			"\n\nGoal:\n" +
			"- Successfully land at least 10 pods in total\n" +
			"- Have a score of 150 or more\n" +
			"- Have at least a stack of 4 pods";
		}
	}


	public class Level3 : ILevel {
		public override string[] renderGoalText(Game game) {
			return new string[] {
				string.Format ("- Land {0} / 15 pods", game.numBlocksLanded.Values.Sum ()),
				string.Format ("- Score {0} >= 270", game.Score),
				string.Format ("- Land {0} / 2 shops", game.numBlocksLanded[game.blockTypes[2]])
			};
		}

		private int index;
		BlockType[] blocks;

		public override void Init(Game game) {
			int numRemaining = 8;

			this.index = 0;
			List<BlockType> blockList = new List<BlockType> ();
			blockList.Add (game.blockTypes [2]);
			blockList.Add (game.blockTypes [2]);
			blockList.Add (game.blockTypes [2]);
			for (int i = 0; i < numRemaining - 3; i++) {
				blockList.Add (game.blockTypes [Mathf.FloorToInt (Random.Range (0, 2))]);
			}
			blocks = blockList.ToArray ();
			Utils.Suffle (blocks);
			base.SetNumRemainingBlocks (numRemaining);
		}

		protected override BlockType _getNextBlock (Game game)
		{
			BlockType result = blocks[index];
			index++;
			return result;
		}

		public override bool isGoalSatisfied(Game game, int index) {
			switch (index) {
			case 0:
				return game.numBlocksLanded.Values.Sum () >= 15;
			case 1:
				return game.Score >= 270;
			case 2: 
				return game.numBlocksLanded[game.blockTypes[2]] >= 2;
			default:
				return false;
			}
		}

		public override int numGoals() {
			return 3;
		}

		public override string GetBriefingText() {
			return "Gah! These people are always complaining! Now they want some shops! You should place them high as well." +
				"\n\nGoal:\n" +
				"- Successfully land at least 15 pods in total\n" +
				"- Have a score of 270 or more\n" +
				"- Land at least 2 shops";
		}
	}
	public class Endless : ILevel {
		public override string[] renderGoalText(Game game) {
			return new string[] {};
		}
			
		public override void Init(Game game) {
			base.SetNumRemainingBlocks (-1);
		}

		protected override BlockType _getNextBlock (Game game)
		{
			return game.blockTypes [Mathf.FloorToInt (Random.Range (0, game.blockTypes.Length))];
		}

		public override bool isGoalSatisfied(Game game, int index) {
			return false;
		}

		public override int numGoals() {
			return 0;
		}

		public override string GetBriefingText() {
			return "Endless Mode!!";
		}
	}
}
