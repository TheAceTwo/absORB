using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cinemachine;

public class MoteScript : MonoBehaviour {
    Renderer rend;
    List<MoteScript> Motes;

    // Universal mote variables
    public bool IsPlayer; // Check if the mote is a player mote
    public Rigidbody2D rb;
    public float moteSize = 1.5f; // The size of the mote, used to calculate the actual size of the mote when the game starts
    
    // Variables for Gravity
    private bool enableGravity = true;
    private float GConstant = 0.0005f; // The gravitational constant for the attraction between all motes
    private Vector2 gravityForce; // used also in InitVelocity()

    // Variables for OutOfBounds()
    private float OOBDrag = 0.005f; // The value of the drag force applied by the rigidbody when the mote is outside the set boundaries defined below
    private float OOBForce = 0.3f; // The amount of force applied to the rigidbody when the mote is outside the set boundaries defined below
    private float xBoundary = 20; private float yBoundary = 20; // The coordinates used in OutOfBounds()
    
    // Player mote specific variables
    public GameObject motePrefab; // Reference to the mote prefab
    private float moteSpawnDistance = 0.15f; // The distance to spawn the cloned mote away from the player mote
    private float moteSpawnSize = 0.2f; // The size to set the cloned mote to
    private float moteSpawnForce = 0.02f; // The amount of force to apply to the cloned mote
    private float playerLaunchForce = 1.2f; // Originally I was using the moteSpawnForce to push the player mote away with the same force - like how equal and opposite reactions work in real life - but that didn't give me the effect I was looking for
    private float holdTime;

    // Sound variables
    public AudioClip CollisionSound;
    public AudioClip LVLCompleteSound;
    public AudioSource CollisionSFX;
    public AudioSource LVLCompleteSFX;

    // Pause and Time
    private bool Paused = false;
    private bool allowTimeScaler = true;
    private Vector3 storedMousePos;
    private float gameSpeedTime = 1f;
    private float prevMousePosY;

    // Variables for inital velocity
    public float VUp = 0.0f;
    public float VDown = 0.0f;
    public float VRight = 0.0f;
    public float VLeft = 0.0f;

    // Variables for Color()
    public Material bluemat; // Blue material used to designate motes that the player can absorb
    public Material redmat; // Red material used to designate motes that the player cannot absorb
    public GameObject playerMote; // gets the player mote for Color()
    private float playerMoteSize; // the variable for the size of the player gameobject for Color()

    // Variables for CinemachineZoom() on Windows
    private bool allowZoom = true;
    private float sizeChangeAmount = 0.5f; // The amount to change the camera's size by
    private float zoomSpeed = 2f; // The speed at which to zoom in/out
    private CinemachineVirtualCamera virtualCamera; // The virtual camera to zoom
    private float targetSize = 5f; // The target size of the camera

    //Variables for winning/losing
    private bool biggerThanPlayer = false;
    private bool enableBiggestFinder;
    private bool tutorialCompleted = true;

    // Variables for UI
    public GameObject Canvas;
    private bool allowMenus = true;
    private bool uiSpawned = false;
    public GameObject pauseMenu;
    public GameObject PlayerTooSmallUI;
    public GameObject PlayerAbsorbedUI;
    public GameObject LvlCompleteUI;

    void Awake() {
        MotesList.Motes.Add(this); // Add the mote to the list of motes
        Motes = MotesList.Motes; // Reference to the motes list
        Canvas = GameObject.Find("Canvas"); // Find the canvas
        playerMote = GameObject.Find("Player"); // Find the gameobject with the IsPlayer flag set to true
        StartCoroutine(InitVelocity()); // Start the inital velocity 
        if (IsPlayer) { // if we don't check for a player, every time a new mote is spawned, the camera resets itself and gives you virtual wiplash
            // Check for the virtual camera so we can zoom in/out with it in CinemachineZoom()
            virtualCamera = GameObject.FindGameObjectWithTag("vcam").GetComponent<CinemachineVirtualCamera>(); // get the virtual camera component
            if(virtualCamera != null) { // if the camera isn't null
                virtualCamera.m_Lens.OrthographicSize = 5; // The size of the orthographic camera. 5 seems to be a reasonable default
                targetSize = virtualCamera.m_Lens.OrthographicSize; 
            }

            if (Motes == null){ // if the list is null
                Motes = new List<MoteScript>(); // Create the list 
                Motes.Add(this); // add this mote to the list
            }
            StartCoroutine(WaitForSizeLoad()); // coroutine below
            // StartCoroutine(AllowSounds());
            TutorialChecker();
        }
    }
    void StartSounds() {
        CollisionSFX = gameObject.AddComponent<AudioSource>();
        CollisionSFX.clip = CollisionSound;
        LVLCompleteSFX = gameObject.AddComponent<AudioSource>();
        LVLCompleteSFX.clip = LVLCompleteSound;
    }
    // IEnumerator AllowSounds() {
    //     yield return new WaitForSecondsRealtime(2);
    //     allowCollisonSound = true;
    // }
    IEnumerator InitVelocity() {
        yield return new WaitForSecondsRealtime(1); // wait for a second
        rb.AddForce(transform.up * VUp, ForceMode2D.Impulse); // force applied up
        rb.AddForce(transform.right * VRight, ForceMode2D.Impulse); // force applied right
        rb.AddForce(-transform.right * VLeft, ForceMode2D.Impulse); // force applied left
        rb.AddForce(-transform.up * VRight, ForceMode2D.Impulse); // force applied down
        rb.AddForce(2 * (-gravityForce)); // launch the motes away form each other at the start
        
    }
    IEnumerator WaitForSizeLoad() { // use a coroutine to enable size checking after a certain amount of time. without this, the game ends whenever a level starts in any built version
        yield return new WaitForSecondsRealtime(5f); // wait for seconds
        enableBiggestFinder = true; // enable the checker
    }

    void OnDisable() { // when the mote is disabled or deleted
        Motes.Remove(this); // If the mote this script is attached to is disabled, then remove it from the list
    }

    void FixedUpdate() {
        OutOfBounds(); // pushes the motes back to the center of the scene if they are out of bounds

        //Gravity
        if (enableGravity) {
            foreach (MoteScript attractor in Motes) {
                if (attractor != this) {
                    Gravity(attractor);
                }
            }
        }
    }
    void TutorialChecker() { // check if the tutorial has been completed
        if (this.GetComponent<TutorialScript>().isActiveAndEnabled == true) { // if the tutorialScript is active
            tutorialCompleted = this.GetComponent<TutorialScript>().tutorialCompleted; // the value variable tutorialCompleted should match with the one in TutorialScript
        } else if (this.GetComponent<TutorialScript>().isActiveAndEnabled == false) { // if the script is not active.
            tutorialCompleted = true; // default to true because that means the level is not a tutorial level
        }
    }
    void Update() {
        transform.localScale = new Vector3(moteSize * 0.01f, moteSize * 0.01f, 1f); // Calculates the size of the sprite based on the variable moteSize
        if(IsPlayer){
            TutorialChecker();
            Player(); // I put the player stuff under Update instead of FixedUpdate because when it was under FixedUpdate, it would spawn a new mote every frame that the mouse was pressed down because its state gets refreshed every frame and thought it was getting pressed each frame instead of held from when it was initally pressed
            if (enableBiggestFinder && tutorialCompleted) {
                BiggestFinder();
            }
            CinemachineZoom(); // its all the way down at the bottom. have fun scrolling :)
        } if (!IsPlayer) {
            Color(); // Change the color based on player size
        }
    }
    void Player() {
        if(moteSize > (1.5f * moteSpawnSize)) { // Makes sure that the player mote can't get too small
            if (Input.GetKeyDown(KeyCode.Escape)) { // Check if the escape key is pressed
                if (Paused) {
                    MenuClosed(false);
                } else if (!Paused) {
                    MenuOpened();
                }
            }
            if(!Paused){
                if (Input.GetMouseButton(0)) {
                    holdTime += Time.deltaTime;
                }
                if (Input.GetMouseButtonDown(0)) {
                    storedMousePos = Input.mousePosition;
                }
                if (holdTime < 0.5f) {
                    if (Input.GetMouseButtonUp(0)) {
                        MoteDispenser();
                        holdTime = 0.0f;
                    }
                } else { 
                    TimeScaler();
                    if (Input.GetMouseButtonUp(0)) {
                        holdTime = 0.0f;
                    }
                }
            }
        } else {
            PlayerTooSmall();
        }
    }
    void TimeScaler() { // if the mouse button is pressed down then tell the function to change the time scale as if the player were dragging a slider up or down
        if (allowTimeScaler) {
            float currentMousePositionY = Input.mousePosition.y; // set the y position variable to the current y position of the mouse
            if (currentMousePositionY > prevMousePosY) { // if the current y position is greater than the one from the previous frame
                if (Time.timeScale < 10f) { // clamp the time scale at a maximum of 10
                    Time.timeScale += 0.1f; // increase the time scale by .1 every frame
                } else if (Time.timeScale > 10f) {Time.timeScale = 10f;} // if the time scale is greater than 10, set it back to 10 so that it can't be too high
            } else if (currentMousePositionY < prevMousePosY) { // if the current y position is less than the one from the previous frame
                if (Time.timeScale > 0.2f) { // clamp the time scale at a minimum of 0.2
                    Time.timeScale -= 0.1f; // decrease the time scale by .1 every frame
                } else if (Time.timeScale < 0.2f) {Time.timeScale = 0.2f;} // if the time scale is less than 0.2 then set it back to 0.2 so that it can't get too small
            }
            prevMousePosY = currentMousePositionY; // set the previous mouse position to the current mouse position so that the function will function properly next frame
            
            /*
            reference for Time.timeScale : https://docs.unity3d.com/ScriptReference/Time-timeScale.html
            time refers to the time at the beginnning of the frame and timeScale refers to how fast time passes, so when you put those two together you get the scale for how fast time passes in the scene
            so a Time.timeScale of 1 means the game is moving at its original speed while a Time.timeScale of 1.5 means that time is moving 1.5 times faster than usual
            */
        }
    }
    void MoteDispenser() {
        // Rotates the player mote such that its x axis is always facing the mouse
        Vector3 mousePos = Input.mousePosition;
        Vector3 objectPos = Camera.main.WorldToScreenPoint (transform.position);
        mousePos.x = mousePos.x - objectPos.x;
        mousePos.y = mousePos.y - objectPos.y;
        float angle = Mathf.Atan2(mousePos.y, mousePos.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
        // I figured out how to rotate the gameobject twords the mouse from the following link: https://answers.unity.com/questions/10615/rotate-objectweapon-towards-mouse-cursor-2d.html

        // Spawns a new mote and sets the size
        Vector3 spawnPosition = transform.position + (transform.right * ((moteSize*0.1f)+moteSpawnDistance));
        GameObject newMote = Instantiate(motePrefab, spawnPosition, Quaternion.identity); // Spawns the new mote with the spawnPosition
        MoteScript moteScript = newMote.GetComponent<MoteScript>(); // Gets the script componenet of the gameobject
        moteScript.moteSize = moteSpawnSize; // Sets the moteSize float of that script to the moteSpawnSize float from this script

        // Apply force to the new mote
        Vector2 forceDirection = (mousePos - transform.position).normalized; // Determines the direction to apply forces and normalizes which turns it into a unit vector
        Rigidbody2D moteRigidbody = newMote.GetComponent<Rigidbody2D>(); // Gets the Rigidbody of the cloned mote so I can apply force to it
        moteRigidbody.AddForce(forceDirection * moteSpawnForce, ForceMode2D.Impulse); // Add force to the rigidbody in the direction determined by forceDirection

        // Apply force to the player mote's rigidbody and decrease the size of the player mote
        rb.AddForce((-forceDirection * (playerLaunchForce * moteSize)), ForceMode2D.Force);
        moteSize -= moteSpawnSize;

        // References for ForceMode2D: https://docs.unity3d.com/ScriptReference/ForceMode2D.html
        // I chose to use Impulse on the cloned mote because it dosen't need to accumulate any speed from forces while the player mote does.
    }
    void BiggestFinder(){
        transform.localScale = new Vector3(moteSize * 0.01f, moteSize * 0.01f, 1f);
        playerMoteSize = this.moteSize;
        bool playerIsBiggest = Motes.TrueForAll(x => x.biggerThanPlayer == false);
        if (playerIsBiggest) {
            LevelComplete();
        }
        // TrueForAll: https://stackoverflow.com/questions/17897728/how-to-use-trueforall#:~:text=bool%20alltrue%20%3D%20listOfBools.TrueForAll(b%20%3D%3E%20b)%3B
    }
    public void LevelComplete() {
        if ((!uiSpawned)) {
            Time.timeScale = 1;
            allowTimeScaler = false;
            allowMenus = false;
            uiSpawned = true;
            LVLCompleteSFX.Play();
            Instantiate(LvlCompleteUI, Canvas.transform.position, Quaternion.identity, Canvas.transform);
        }
        SaveLevel();
    }
    public void MenuOpened() {
        if (allowMenus) {
            Instantiate(pauseMenu, Canvas.transform.position, Quaternion.identity, Canvas.transform);
            Time.timeScale = 0f; 
            Paused = true;
        }
    }
    public void MenuClosed(bool goToMenu) { // add in the goToMenu variable because calling the return to menu function seperately in SceneLoader causes it to just do nothing at all. p;robably due to the fact that the object is destroyed before return to the menu is called
        Destroy(GameObject.FindGameObjectWithTag("PauseMenu"));
        Time.timeScale = gameSpeedTime; // set the time back to how it was before the pause menu was opened
        Paused = false; // pause boolean
        if (goToMenu) { // that variable we added because of the loading problem
            SceneManager.LoadScene(0); // the main menu in my game is denoted by id 0. I could also load it by name with SceneManager.LoadScene("MainMenu");
        }
    }
    void PlayerTooSmall() { // called when the player clicks too many times and runs out of mass
        if (allowMenus) {
            playerMote.SetActive(false);
            if (!uiSpawned) {
                uiSpawned = true; // set the boolean to true so that the UI doesn't duplicate itself
                Instantiate(PlayerTooSmallUI, Canvas.transform.position, Quaternion.identity, Canvas.transform); // spawn the PlayerTooSmallUI
            }
        }
    }
    void PlayerMoteAbsorbed() { // called if the player runs into a larger mote
        if (allowMenus) {
            if (!uiSpawned) {
                uiSpawned = true; // prevent the UI from duping itself
                Instantiate(PlayerAbsorbedUI, Canvas.transform.position, transform.rotation, Canvas.transform); // spawn the PlayerAbsorbedUI
            }
        }
    }
    void OutOfBounds() { // Defines what will happen when the mote is past a certain distance (xBoundary & yBoundary) in each direction.
        if (-xBoundary > rb.transform.position.x) { // if the mote's coordinates are larger than the boundary variables
            rb.AddForce(Vector2.right*(OOBForce)/moteSize); // add force back twords the middle of the level
            rb.drag = OOBDrag; // add a drag to the mote so that it dosen't start accelerating
        } else if (xBoundary < rb.transform.position.x) {
            rb.AddForce(Vector2.left*(OOBForce)/moteSize);
            rb.drag = OOBDrag;
        } else if (-yBoundary > rb.transform.position.y) {
            rb.AddForce(Vector2.up*(OOBForce)/moteSize);
            rb.drag = OOBDrag;
        } else if (yBoundary < rb.transform.position.y) {
            rb.AddForce(Vector2.down*(OOBForce)/moteSize);
            rb.drag = OOBDrag;
        } else {
            rb.drag = 0.00f; // Universal drag constant. All motes will always feel this drag on their rigidbody while inside the defined boundaries. I don't want to set it to a variable because there is no reason it should ever not be 0 unless I want to change how the entire game feels
        }
    }
    void Gravity(MoteScript affectedObject) { // Calculate and apply a simulated gravitational force to all motes
        Vector2 direction = rb.position - affectedObject.rb.position; // Calculates the direction of the gravity by subtracting the position of the affected rigidbody from the current position
        float distance = direction.magnitude; // the distance is the magnitude of the direction vector
        float gForceStrength = GConstant * (rb.mass * affectedObject.rb.mass) / Mathf.Pow(distance, 2); // Gravitational Force = The Gravitational Constant x First Object's Mass x Second Object's Mass / Distance between the two objects squared.  
        gravityForce = direction.normalized * gForceStrength; // added together before being applied so that we can use the inverse of the gravitational force in the InitVelocity coroutine
        affectedObject.rb.AddForce(gravityForce); // Applying "gravity" force
    }

    void Absorb(MoteScript otherMote) { // The Absorb function basically eats the smaller object when they collide
        if (moteSize > otherMote.moteSize) { // Absorb the other mote if this mote is bigger
            if (otherMote.name == playerMote.name) { // If the other mote is the player mote
                moteSize += otherMote.moteSize;
                Destroy(otherMote.gameObject);
                PlayerMoteAbsorbed(); // absorb the player mote. its different from absorbing other motes cause the level has to end when the player had been absorbed
            } else if (otherMote.name != playerMote.name) { // If the other mote is not the player mote
                moteSize += otherMote.moteSize;
                Destroy(otherMote.gameObject);
            }
        } else if (moteSize == otherMote.moteSize){ // handles collisions where both motes are the same size
            if (otherMote.name == playerMote.name) {
                PlayerMoteAbsorbed();
            }
            Destroy(gameObject);
        }
    }
    void OnCollisionEnter2D(Collision2D collision) { // Gets the size of the other mote and calls in Absorb() when two motes collide
        if (collision.gameObject.CompareTag("absorbable")) { // compare the tags. later versions of the game have different types of motes
            CollisionSFX.Play();
            MoteScript otherMote = collision.gameObject.GetComponent<MoteScript>(); // Get the other mote's script, which holds all the information about the other mote
            Absorb(otherMote); // call Absorb()
        }
    }
    void Color() { // Compare the moteSize of this gameobject to that of the player's mote
        rend = GetComponent<Renderer>(); // get the renderer component for Color()
        if (playerMote != null) {
            MoteScript playerMoteScript = playerMote.GetComponent<MoteScript>(); // Get the MoteScript component from the player's mote gameobject
            playerMoteSize = playerMoteScript.moteSize; // Get the size of the player's mote
            
            // Compare the size of the mote and the player, then change colors accordingly
            if (moteSize >= playerMoteSize) {
                // rend.material.Lerp(bluemat, redmat, 100f);
                rend.material = redmat;
                biggerThanPlayer = true;
            } else if (moteSize < playerMoteSize) {
                // rend.material.Lerp(redmat, bluemat, 100f);
                rend.material = bluemat;
                biggerThanPlayer = false;
            }
            // note that using Lerp causes everything to break and die, so don't un-comment those unless you wanna try and fix them somehow 
                // thanks past self :)

        } else {rend.material = redmat;}
    }

    void SaveLevel() { // save the level's completion state to a persistent data file. 
        if (SceneManager.GetActiveScene().name != "Tutorial") {
            int currentLevelInt = int.Parse(SceneManager.GetActiveScene().name); // turn the string into an int so we can use it in the next line

            if (currentLevelInt == 1) {LevelSaver.lvl1 = true;}
            if (currentLevelInt == 2) {LevelSaver.lvl2 = true;}
            if (currentLevelInt == 3) {LevelSaver.lvl3 = true;}
            if (currentLevelInt == 4) {LevelSaver.lvl4 = true;}
            if (currentLevelInt == 5) {LevelSaver.lvl5 = true;}
            if (currentLevelInt == 6) {LevelSaver.lvl6 = true;}
            if (currentLevelInt == 7) {LevelSaver.lvl7 = true;}
            if (currentLevelInt == 8) {LevelSaver.lvl8 = true;}
            if (currentLevelInt == 9) {LevelSaver.lvl9 = true;}
            if (currentLevelInt == 10) {LevelSaver.lvl10 = true;}
            if (currentLevelInt == 11) {LevelSaver.lvl11 = true;}
            if (currentLevelInt == 12) {LevelSaver.lvl12 = true;}
            if (currentLevelInt == 13) {LevelSaver.lvl13 = true;}
            if (currentLevelInt == 14) {LevelSaver.lvl14 = true;}
            if (currentLevelInt == 15) {LevelSaver.lvl15 = true;}
            if (currentLevelInt == 16) {LevelSaver.lvl16 = true;}
            if (currentLevelInt == 17) {LevelSaver.lvl17 = true;}
            if (currentLevelInt == 18) {LevelSaver.lvl18 = true;}
            if (currentLevelInt == 19) {LevelSaver.lvl19 = true;}
            if (currentLevelInt == 20) {LevelSaver.lvl20 = true;}
            if (currentLevelInt == 21) {LevelSaver.lvl21 = true;}
            if (currentLevelInt == 22) {LevelSaver.lvl22 = true;}
            if (currentLevelInt == 23) {LevelSaver.lvl23 = true;}
            if (currentLevelInt == 24) {LevelSaver.lvl24 = true;}
        } else {
            LevelSaver.lvl0 = true;
        }
    }
    void CinemachineZoom() {
        if (!Paused && allowZoom) {
            Vector2 scrollDelta = Input.mouseScrollDelta; // make it a variable for ease of access
            if (scrollDelta.y != 0) { // if you're scrolling
                targetSize -= sizeChangeAmount * scrollDelta.y; // change the size variable based on the scroll delta vector
                targetSize = Mathf.Clamp(targetSize, 0.5f, 45f); // Clamp the target size to a reasonable range
            }
            virtualCamera.m_Lens.OrthographicSize = Mathf.Lerp(virtualCamera.m_Lens.OrthographicSize, targetSize, zoomSpeed * Time.deltaTime); // finally lerp the size of the virtual camera (the one controlling the main camera) based on the target size and the zoom speed
            // this was a very difficult solution to find, but it was crucial that the camera wasn't super jumpy, so here's where i found how to change the Cinemachine's virtual camera size: https://docs.unity3d.com/Packages/com.unity.cinemachine@2.3/api/Cinemachine.LensSettings.html#:~:text=System.Single-,OrthographicSize,-When%20using%20an. it really would have been nice to know that is was so simple to begin with.
        }
    }
}