using Unity.Collections;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveTorque = 10f;
    public float moveForce = 10f;
    public float airMoveForce = 10f;
    public float angVel = 10f;

    [Header("Jump Settings")]
    public float jumpForce = 7f;
    public float jumpTriggerDelay = 0.5f;
    public float groundCheckDistanceForce = 1.1f;
    public LayerMask groundLayer;
    private int jumpsLeft = 2;
    public int maxJumps = 2;

    [Header("Dash Settings")]
    public float dashForce = 20f;
    public float dashRegenTime = 1f; // Time required to regenerate one dash
    private float dashRegenTimer = 0f;
    private bool dashRequested = false;
    public int maxDashes = 2; // Maximum number of dashes
    private int dashesLeft = 2; // Number of dashes available

    [Header("Gravity Settings")]
    public Vector3 gravityDirection = Vector3.down;
    private Vector3 prevGravityDirection = Vector3.down;
    public float gravityStrength = 9.81f;

    [Header("Slam Settings")]
    public float slamForce = 15f;
    private bool canSlam = true;

    private bool canJump = false;
    private Rigidbody rb;
    private bool jumpRequested = false;
    private bool isGrounded;
    public float jumpResetCooldown = 0.5f;
    private float jumpResetTimer = 0f;
    private float disableCamColider = 0f;
    private bool camColider = true;
    private float jumpTriggerTimer = 0f;
    private bool levelComplete = false;
    public CheckPoint checkPoint;
    public CinemachineCamera virtualCamera;

    [Header("Dynamic Jump Regen Settings")]
    public string[] disallowedRegenTags;
    public string[] allowRegenTags; // New array for allow regen tags
    private int disallowedRegenCollisionCount = 0;

    private bool canMove = false; // New variable to control player movement
    private float timer = 0f; // Timer to track time after starting
private float groundedTimer = 0f;
    
    private bool disableTimer = false;
    
    [Header("UI Elements")]
    public Image[] dashSprites; // Array to hold dash UI images
    public Image[] jumpSprites; // Array to hold jump UI images
    public Image slamSprite;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.maxAngularVelocity = angVel;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (virtualCamera == null)
        {
            virtualCamera = FindObjectOfType<CinemachineCamera>();
        }
    }

    public UniversalValues uniVals;

    private void Start()
    {
        // Initialize dash and jump UI based on max values
        UpdateDashUI();
        UpdateJumpUI();
        UpdateSlamUI();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !uniVals.paused && !uniVals.levelComplete && !levelComplete)
        {
            uniVals.paused = true;
            Time.timeScale = 0;
            uniVals.Paused.gameObject.SetActive(true);
            uniVals.HUD.gameObject.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Input.GetKeyDown(KeyCode.Escape) && uniVals.paused && !uniVals.levelComplete && !levelComplete)
        {
            uniVals.paused = false;
            Time.timeScale = 1;
            uniVals.Paused.gameObject.SetActive(false);
            uniVals.HUD.gameObject.SetActive(true);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (jumpTriggerTimer > 0)
        {
            jumpTriggerTimer -= Time.deltaTime;
        }

        if (canMove)
        {
            if (Input.GetButtonDown("Jump") && jumpsLeft > 0 && jumpTriggerTimer <= 0)
            {
                jumpRequested = true;
                jumpTriggerTimer = jumpTriggerDelay;
            }

            if (Input.GetKeyDown(KeyCode.LeftShift) && dashesLeft > 0)
            {
                dashRequested = true;
                dashesLeft--;
            }

            if (jumpResetTimer > 0)
            {
                jumpResetTimer -= Time.deltaTime;
            }
            if (disableCamColider >= 0)
            {
                disableCamColider -= Time.deltaTime;
            }
            if (disableCamColider < 0)
            {
                var colliderExtension = virtualCamera.GetComponent<CinemachineDeoccluder>();
                if (colliderExtension != null)
                {
                    colliderExtension.enabled = true;
                }
                disableCamColider = 0;
            }

            // Update the timer
            if (!disableTimer)
            {
                timer += Time.deltaTime;
            }

            if (groundedTimer > 0)
            {
                groundedTimer -= Time.deltaTime;
            }
            else
            {
                isGrounded = false;
            }

            // Check for slam input
            if (Input.GetKeyDown(KeyCode.LeftControl) && canSlam)
            {
                Slam();
            }
        }
        uniVals.ActiveTimer.text = FormatTime(timer);
        UpdateDashUI();
        UpdateJumpUI();
        UpdateSlamUI();
    }

    
    private void UpdateDashUI()
    {
        for (int i = 0; i < dashSprites.Length; i++)
        {
            if (i < dashesLeft)
            {
                dashSprites[i].enabled = true; // Show dash sprite
            }
            else
            {
                dashSprites[i].enabled = false; // Hide dash sprite
            }
        }
    }
    private void UpdateSlamUI()
    {
        
            if (canSlam)
            {
                slamSprite.enabled = true; // Show dash sprite
            }
            else
            {
                slamSprite.enabled = false; // Hide dash sprite
            }
        
    }

    private void UpdateJumpUI()
    {
        for (int i = 0; i < jumpSprites.Length; i++)
        {
            if (i < jumpsLeft)
            {
                jumpSprites[i].enabled = true; // Show jump sprite
            }
            else
            {
                jumpSprites[i].enabled = false; // Hide jump sprite
            }
        }
    }
    private void Slam()
    {
        // Calculate the downward direction based on gravity
        Vector3 relativeDown = gravityDirection.normalized;

        // Apply the slam force in the downward direction
        rb.AddForce(relativeDown * slamForce, ForceMode.Impulse);

        // Halve the horizontal momentum
        Vector3 horizontalVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, relativeDown);
        rb.linearVelocity -= horizontalVelocity * 0.5f;

        // Disable slam until grounded again
        canSlam = false;
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        int milliseconds = Mathf.FloorToInt((time * 100) % 100);

        return string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
    }

    private void FixedUpdate()
    {
        ApplyGravity();
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 forward = Vector3.ProjectOnPlane(Camera.main.transform.forward, gravityDirection).normalized;
        Vector3 right = Vector3.ProjectOnPlane(Camera.main.transform.right, gravityDirection).normalized;
        Vector3 moveDirection = (right * v + -forward * h).normalized;

        rb.AddTorque(moveDirection * moveTorque);
        if (canMove)
        {
            Vector3 forceDirection = (forward * v + right * h).normalized;
            rb.AddForce(forceDirection * (IsGroundedForce() ? moveForce : airMoveForce));

            if (jumpRequested)
            {
                Vector3 relativeUp = -gravityDirection.normalized;
                float verticalSpeed = Vector3.Dot(rb.linearVelocity, relativeUp);
                if (verticalSpeed < 0)
                {
                    Vector3 downwardComponent = Vector3.Project(rb.linearVelocity, relativeUp);
                    rb.linearVelocity -= downwardComponent;
                }
                rb.AddForce(relativeUp * jumpForce, ForceMode.Impulse);
                jumpRequested = false;
                jumpsLeft--;
                canSlam = true; // Re-enable slam when jumping
                jumpResetTimer = jumpResetCooldown;
            }

            if (dashRequested)
            {
                Vector3 dashDirection;
                if (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f)
                {
                    dashDirection = (Camera.main.transform.forward * v + Camera.main.transform.right * h).normalized;
                }
                else
                {
                    dashDirection = Camera.main.transform.forward.normalized;
                }
                rb.AddForce(dashDirection * dashForce, ForceMode.Impulse);
                dashRequested = false;
            }
        }

        // Regenerate dash over time while grounded
        if (isGrounded && dashesLeft < maxDashes)
        {
            dashRegenTimer += Time.deltaTime;
            if (dashRegenTimer >= dashRegenTime)
            {
                dashesLeft++;
                dashRegenTimer = 0f;
            }
        }
    }

    public Transform worldUpOverrideTransform;

    private void LateUpdate()
    {
        if (virtualCamera != null)
        {
            if (gravityDirection != prevGravityDirection)
            {
                
                var colliderExtension = virtualCamera.GetComponent<CinemachineDeoccluder>();
                if (colliderExtension != null)
                {
                    colliderExtension.enabled = false;
                }
            }
            else
            {
                var colliderExtension = virtualCamera.GetComponent<CinemachineDeoccluder>();
                if (colliderExtension != null)
                {
                    colliderExtension.enabled = true;
                }
            }
        }
        Vector3 lerpGravityDirection = Vector3.Slerp(prevGravityDirection, gravityDirection, Time.deltaTime * 5f);
        worldUpOverrideTransform.rotation = Quaternion.FromToRotation(Vector3.up, -lerpGravityDirection);
        prevGravityDirection = lerpGravityDirection;
    }

    private void ApplyGravity()
    {
        rb.AddForce(gravityDirection * gravityStrength, ForceMode.Acceleration);
    }

    private bool IsCollisionWithDisallowedTag(Collision collision)
    {
        foreach (string tag in disallowedRegenTags)
        {
            if (collision.gameObject.CompareTag(tag))
            {
                // Check for allow regen tag
                foreach (string allowTag in allowRegenTags)
                {
                    if (collision.gameObject.CompareTag(allowTag))
                    {
                        return false; // Allow regen tag overrides disallow regen tag
                    }
                }
                return true;
            }
        }
        return false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (IsCollisionWithDisallowedTag(collision))
        {
            disallowedRegenCollisionCount++;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (IsCollisionWithDisallowedTag(collision))
        {
            disallowedRegenCollisionCount = Mathf.Max(0, disallowedRegenCollisionCount - 1);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if ((groundLayer & (1 << collision.gameObject.layer)) != 0)
        {
            
            if (jumpResetTimer <= 0 && disallowedRegenCollisionCount == 0)
            {
                jumpsLeft = maxJumps;
                isGrounded = true;
                groundedTimer = .5f;
                canSlam = true; // Re-enable slam when grounded
            }
        }
    }

    private bool IsGroundedForce()
    {
        return Physics.Raycast(transform.position, gravityDirection, groundCheckDistanceForce, groundLayer);
    }

    public void SetGravity(Vector3 newGravityDirection, float newGravityStrength)
    {
        gravityDirection = newGravityDirection;
        gravityStrength = newGravityStrength;

        if (virtualCamera != null)
        {
            Vector3 relativeUp = -gravityDirection;
            Quaternion gravityAlignment = Quaternion.FromToRotation(Vector3.up, relativeUp);
            Vector3 relativeForward = gravityAlignment * Vector3.forward;
            virtualCamera.transform.rotation = Quaternion.LookRotation(relativeForward, relativeUp);
        }
    }

    public LayerMask resetLayer;
    public LayerMask checkPointLayer;
    public LayerMask goalLayer;
    public LayerMask timerLayer;
    public LayerMask startTriggerLayer; // Layer for the start trigger

    private void OnTriggerEnter(Collider other)
    {
        if (timerLayer == (timerLayer | (1 << other.gameObject.layer)))
        {
            disableTimer = true;
        }
        if (goalLayer == (goalLayer | (1 << other.gameObject.layer)))
        {
            uniVals.finalScore = timer;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            uniVals.HUD.gameObject.SetActive(false);
            uniVals.LevelComplete.gameObject.SetActive(true);
            uniVals.FinalTimeText.text = FormatTime(timer);
            levelComplete = true;
            Time.timeScale = 0;
        }
        if (checkPointLayer == (checkPointLayer | (1 << other.gameObject.layer)))
        {
            checkPoint = other.gameObject.GetComponent<CheckPoint>();
        }

        if (resetLayer == (resetLayer | (1 << other.gameObject.layer)))
        {
            transform.position = checkPoint.transform.position + checkPoint.offsetVector;
            Rigidbody playerRigidbody = GetComponent<Rigidbody>();
            if (playerRigidbody != null)
            {
                Vector3 velocity = playerRigidbody.linearVelocity;
                Quaternion rotation = Quaternion.FromToRotation(gravityDirection, checkPoint.gravityDirection);
                velocity = rotation * velocity;
                float projection = Vector3.Dot(velocity, checkPoint.gravityDirection);
                velocity = checkPoint.gravityDirection * projection;
                playerRigidbody.linearVelocity = velocity;
            }
            SetGravity(checkPoint.gravityDirection, checkPoint.gravityStrength);
            var colliderExtension = virtualCamera.GetComponent<CinemachineDeoccluder>();
            if (colliderExtension != null)
            {
                colliderExtension.enabled = false;
            }

            disableCamColider = 1f;
        }

        // Check for collision with the start trigger layer
        if (startTriggerLayer == (startTriggerLayer | (1 << other.gameObject.layer)))
        {
            canMove = true; // Enable player movement

            // Check if the object has the "LevelStart" tag
            if (other.gameObject.CompareTag("LevelStart"))
            {
                timer = 0f; // Start the timer
            }
        }
    }

    public void ResetLevel()
    {
        Time.timeScale = 1;
        // Get the name of the current active scene
        string currentSceneName = SceneManager.GetActiveScene().name;

        // Load the current scene to reset the level
        SceneManager.LoadScene(currentSceneName);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        for (int i = 0; i < jumpsLeft; i++)
        {
            Gizmos.DrawSphere(transform.position + Vector3.up * (i + 1) * 0.5f, 0.25f);
        }

        Gizmos.color = Color.red;
        for (int i = 0; i < dashesLeft; i++)
        {
            Gizmos.DrawSphere(transform.position + Vector3.right * (i + 1) * 0.5f, 0.25f);
        }
    }
}
