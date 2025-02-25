using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MoteScript : MonoBehaviour {
    Renderer rend;
    List<MoteScript> Motes;

    // Universal mote variables
    public bool IsPlayer; // Check if the mote is a player mote
    public Rigidbody2D rb;
    public float moteSize = 1.5f; // The size of the mote, used to calculate the actual size of the mote when the game starts
    
    // Variables for OutOfBounds()
    public bool enableOOB = false;
    private float OOBDrag = 0.005f; // The value of the drag force applied by the rigidbody when the mote is outside the set boundaries defined below
    private float OOBForce = 0.01f; // The amount of force applied to the rigidbody when the mote is outside the set boundaries defined below
    private float xBoundary = 20; private float yBoundary = 20; // The coordinates used in OutOfBounds()
    
    // Player mote specific variables
    public GameObject motePrefab; // Reference to the mote prefab
    private float moteSpawnDistance = 0.15f; // The distance to spawn the cloned mote away from the player mote
    private float moteSpawnSize = 0.2f; // The size to set the cloned mote to
    private float moteSpawnForce = 0.02f; // The amount of force to apply to the cloned mote
    private float playerLaunchForce = 1.2f; // Originally I was using the moteSpawnForce to push the player mote away with the same force - like how equal and opposite reactions work in real life - but that didn't give me the effect I was looking for
    private float holdTime; // the time the left mouse button it held down
    public bool isMultiplayer = true;
    private bool isMoteDispenserRunning = false;

    // Sound variables
    public AudioClip CollisionSound; // clips hold the sound
    public AudioSource CollisionSFX; // sources play the sound

    // Pause and Time
    private bool Paused = false;

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

    //Variables for winning/losing
    private bool biggerThanPlayer = false;

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
        if (IsPlayer) {
            if (Motes == null){ // if the list is null
                Motes = new List<MoteScript>(); // Create the list 
                Motes.Add(this); // add this mote to the list
            }
            StartCoroutine(WaitForSizeLoad()); // coroutine below
        }
    }
    IEnumerator InitVelocity() {
        yield return new WaitForSecondsRealtime(1); // wait for a second
        rb.AddForce(transform.up * VUp, ForceMode2D.Impulse); // force applied up
        rb.AddForce(transform.right * VRight, ForceMode2D.Impulse); // force applied right
        rb.AddForce(-transform.right * VLeft, ForceMode2D.Impulse); // force applied left
        rb.AddForce(-transform.up * VRight, ForceMode2D.Impulse); // force applied down
    }
    IEnumerator WaitForSizeLoad() { // use a coroutine to enable size checking after a certain amount of time. without this, the game ends whenever a level starts in any built version
        yield return new WaitForSecondsRealtime(5f); // wait for seconds
    }

    void OnDisable() { // when the mote is disabled or deleted
        Motes.Remove(this); // If the mote this script is attached to is disabled, then remove it from the list
    }

    void FixedUpdate() {
        OutOfBounds(); // pushes the motes back to the center of the scene if they are out of bounds
    }
    void Update() {
        transform.localScale = new Vector3(moteSize * 0.01f, moteSize * 0.01f, 1f); // Calculates the size of the sprite based on the variable moteSize
        if(IsPlayer){
            Player(); // I put the player stuff under Update instead of FixedUpdate because when it was under FixedUpdate, it would spawn a new mote every frame that the mouse was pressed down because its state gets refreshed every frame and thought it was getting pressed each frame instead of held from when it was initally pressed
        } if (!IsPlayer) {
            Color(); // Change the color based on player size
        }
    }
    void Player()
    { // player specific stuff
        if (moteSize > (1.5f * moteSpawnSize))
        { // Makes sure that the player mote can't get too small
            if (Input.GetKeyDown(KeyCode.Escape))
            { // Check if the escape key is pressed
                if (Paused)
                {
                    MenuClosed(false); // if the game is paused and you press escape, close the pause menu but don't load the main menu
                }
                else if (!Paused)
                {
                    MenuOpened(); // if the game is not paused and you press escape, open the pause menu
                }
            }
            if (!Paused)
            {
                if (Input.GetMouseButton(0) & !isMoteDispenserRunning)
                { // If you click the mouse button
                    StartCoroutine(MoteDispenser()); // Start the coroutine to spawn a new mote
                }
            }
        }
        else
        {
            PlayerTooSmall();
        }
    }
    IEnumerator MoteDispenser()
    {
        isMoteDispenserRunning = true; // Set the flag to true to indicate the coroutine is running
        
        // Rotates the player mote such that its x axis is always facing the mouse
        Vector3 mousePos = Input.mousePosition;
        Vector3 playerPos = Camera.main.WorldToScreenPoint(transform.position);
        mousePos.x = mousePos.x - playerPos.x;
        mousePos.y = mousePos.y - playerPos.y;
        float angle = Mathf.Atan2(mousePos.y, mousePos.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

        // Spawns a new mote and sets the size
        Vector3 spawnPosition = transform.position + (transform.right * ((moteSize * 0.1f) + moteSpawnDistance));
        GameObject newMote = Instantiate(motePrefab, spawnPosition, Quaternion.identity); // Spawns the new mote with the spawnPosition
        MoteScript moteScript = newMote.GetComponent<MoteScript>(); // Gets the script component of the new gameobject
        moteScript.moteSize = moteSpawnSize; // Sets the moteSize float of that script to the moteSpawnSize float from this script

        // Apply force to the new mote
        Vector2 forceDirection = (mousePos - transform.position).normalized; // Determines the direction to apply forces and normalizes which turns it into a unit vector
        Rigidbody2D moteRigidbody = newMote.GetComponent<Rigidbody2D>(); // Gets the Rigidbody of the cloned mote so I can apply force to it
        moteRigidbody.AddForce(forceDirection * moteSpawnForce, ForceMode2D.Impulse); // Add force to the rigidbody in the direction determined by forceDirection

        // Apply force to the player mote's rigidbody and decrease the size of the player mote
        rb.AddForce((-forceDirection * (playerLaunchForce * moteSize)), ForceMode2D.Force);
        moteSize -= moteSpawnSize;
        yield return new WaitForSeconds(0.1f); // Wait for 1 second
        isMoteDispenserRunning = false; // Set the flag to false to indicate the coroutine has finished
    }
    public void MenuOpened() { // callable function for opening the menu
        if (allowMenus) { // if the menu is allowed to be opened right now
            Instantiate(pauseMenu, Canvas.transform.position, Quaternion.identity, Canvas.transform); // instantiate the menu prefab
            Paused = true; // boolean for paused state
        }
    }
    public void MenuClosed(bool goToMenu) { // add in the goToMenu variable because calling the return to menu function seperately in SceneLoader causes it to just do nothing at all. probably due to the fact that the object is destroyed before return to the menu is called
        Destroy(GameObject.FindGameObjectWithTag("PauseMenu"));
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
        if (enableOOB) {
            if (-xBoundary > rb.transform.position.x)
            { // if the mote's coordinates are larger than the boundary variables
                rb.AddForce(Vector2.right * (OOBForce) / moteSize); // add force back twords the middle of the level
                rb.linearDamping = OOBDrag; // add a drag to the mote so that it dosen't start accelerating
            }
            else if (xBoundary < rb.transform.position.x)
            {
                rb.AddForce(Vector2.left * (OOBForce) / moteSize);
                rb.linearDamping = OOBDrag;
            }
            else if (-yBoundary > rb.transform.position.y)
            {
                rb.AddForce(Vector2.up * (OOBForce) / moteSize);
                rb.linearDamping = OOBDrag;
            }
            else if (yBoundary < rb.transform.position.y)
            {
                rb.AddForce(Vector2.down * (OOBForce) / moteSize);
                rb.linearDamping = OOBDrag;
            }
            else
            {
                rb.linearDamping = 0.00f; // Universal drag constant. All motes will always feel this drag on their rigidbody while inside the defined boundaries. I don't want to set it to a variable because there is no reason it should ever not be 0 unless I want to change how the entire game feels
            }
        }
    }

    void Absorb(MoteScript otherMote) { // The Absorb function basically eats the smaller object when they collide
        if (moteSize > otherMote.moteSize) { // Absorb the other mote if this mote is bigger
            if (otherMote.name == playerMote.name) { // If the other mote is the player mote
                moteSize += otherMote.moteSize; // grow
                Destroy(otherMote.gameObject); // destroy the other mote
                PlayerMoteAbsorbed(); // run this function instead of breaking everything (destroying the player object when lots of functions use the player object results in lots of errors as you could probably imagine)
            } else if (otherMote.name != playerMote.name) { // If the other mote is not the player mote, grow and then destroy the other mote
                moteSize += otherMote.moteSize; // grow
                Destroy(otherMote.gameObject); // destroy the other mote
            }
        } else if (moteSize == otherMote.moteSize){ // handles collisions where both motes are the same size
            if (otherMote.name == playerMote.name) { // if you're absorbing the player
                PlayerMoteAbsorbed(); 
            }
            Destroy(gameObject);
        }
    }
    void OnCollisionEnter2D(Collision2D collision)
    { // Gets the size of the other mote and calls in Absorb() when two motes collide
        if (collision.gameObject.CompareTag("absorbable"))
        { // compare the tags. later versions of the game have different types of motes
            if (CollisionSFX != null)
            { // Check if CollisionSFX is assigned
                CollisionSFX.Play(); // play the cool sound effect I got from https://opengameart.org for free.
            }
            else
            {
                Debug.LogWarning("CollisionSFX is not assigned.");
            }
            MoteScript otherMote = collision.gameObject.GetComponent<MoteScript>(); // Get the other mote's script, which holds all the information about the other mote
            Absorb(otherMote); // call Absorb()
        }
    }
    void Color() { // Compare the moteSize of this gameobject to that of the player's mote
        rend = GetComponent<Renderer>(); // get the renderer component for Color()
        if (playerMote != null) { // if player isnt null
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
            // note that using Lerp causes everything to break and die, so don't un-comment those unless you appreciate graphics errors 
            // thanks past self :)

        } else {Debug.Log("a mote is confused as to its size relative to the player"); rend.material = redmat;} // if your comparitive size is neither larger, smaller, or equal to that of the player, stay confused but also be red
    }
}