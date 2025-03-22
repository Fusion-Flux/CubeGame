using Unity.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveTorque = 10f;
    public float moveForce = 10f;
    public float airMoveForce = 10f;
    public float angVel = 10f;

    [Header("Jump Settings")]
    public float jumpForce = 7f;
    // A customizable delay between jump triggers (in seconds)
    public float jumpTriggerDelay = 0.5f;
    // Existing variables for jump reset on landing (if desired)
    public float groundCheckDistanceForce = 1.1f;
    public LayerMask groundLayer;
    private int jumpsLeft = 0;
    public int maxJumps = 2;

    [Header("Dash Settings")]
    public float dashForce = 20f;
    public float dashCooldown = 1f;
    private float dashTimer = 0f;
    private bool dashRequested = false;

    [Header("Gravity Settings")]
    public Vector3 gravityDirection = Vector3.down;
    private Vector3 prevGravityDirection = Vector3.down;
    public float gravityStrength = 9.81f;

    private bool canJump = false;
    private Rigidbody rb;
    private bool jumpRequested = false;
    private bool isGrounded;
    // This cooldown is used after a jump is executed (and for resetting jumps on collision)
    public float jumpResetCooldown = 0.5f;
    private float jumpResetTimer = 0f;
    private float disableCamColider = 0f;
    private bool camColider = true;

    // New timer for enforcing delay between jump triggers
    private float jumpTriggerTimer = 0f;

    public CheckPoint checkPoint;
    public CinemachineCamera virtualCamera;
    
    [Header("Dynamic Jump Regen Settings")]
    // Add any tags here in the Inspector to disallow jump regeneration when colliding with objects with these tags.
    public string[] disallowedRegenTags;
    // Internal counter to track collisions with objects that should block jump regeneration.
    private int disallowedRegenCollisionCount = 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.maxAngularVelocity = angVel;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Ensure the virtual camera is assigned
        if (virtualCamera == null)
        {
            virtualCamera = FindObjectOfType<CinemachineCamera>();
        }
    }

    public UniversalValues uniVals;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !uniVals.paused && !uniVals.levelComplete)
        {
            uniVals.paused = true;
            Time.timeScale = 0;
            uniVals.Paused.gameObject.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
        }
        else if (Input.GetKeyDown(KeyCode.Escape) && uniVals.paused && !uniVals.levelComplete)
        {
            uniVals.paused = false;
            Time.timeScale = 1;
            uniVals.Paused.gameObject.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
        }

        // Update the jump trigger timer
        if (jumpTriggerTimer > 0)
        {
            jumpTriggerTimer -= Time.deltaTime;
        }

        // Only allow jump input if the delay has passed
        if (Input.GetButtonDown("Jump") && jumpsLeft > 0 && jumpTriggerTimer <= 0)
        {
            jumpRequested = true;
            jumpTriggerTimer = jumpTriggerDelay;
        }

        // Dash input: press Left Shift to dash if cooldown is complete
        if (Input.GetKeyDown(KeyCode.LeftShift) && dashTimer <= 0)
        {
            dashRequested = true;
            dashTimer = dashCooldown;
        }
        if (dashTimer > 0)
        {
            dashTimer -= Time.deltaTime;
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
    }

    private void FixedUpdate()
    {
        ApplyGravity();
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // Calculate camera's forward/right relative to gravity for movement torque and force
        Vector3 forward = Vector3.ProjectOnPlane(Camera.main.transform.forward, gravityDirection).normalized;
        Vector3 right = Vector3.ProjectOnPlane(Camera.main.transform.right, gravityDirection).normalized;
        Vector3 moveDirection = (right * v + -forward * h).normalized;

        rb.AddTorque(moveDirection * moveTorque);

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
            jumpResetTimer = jumpResetCooldown;
        }

        // Modified dash logic: dash relative to movement direction while preserving vertical look
        if (dashRequested)
        {
            Vector3 dashDirection;
            if (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f)
            {
                // Use the camera's full forward (which includes vertical look) along with its right vector
                dashDirection = (Camera.main.transform.forward * v + Camera.main.transform.right * h).normalized;
            }
            else
            {
                // Fallback dash direction (includes vertical look)
                dashDirection = Camera.main.transform.forward.normalized;
            }
            rb.AddForce(dashDirection * dashForce, ForceMode.Impulse);
            dashRequested = false;
        }
    }

    public Transform worldUpOverrideTransform; // assign this in the Inspector

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
        // Interpolate the gravity direction for a smoother camera up adjustment
        Vector3 lerpGravityDirection = Vector3.Slerp(prevGravityDirection, gravityDirection, Time.deltaTime * 5f);
        worldUpOverrideTransform.rotation = Quaternion.FromToRotation(Vector3.up, -lerpGravityDirection);
        // Update the previous gravity direction for the next frame
        prevGravityDirection = lerpGravityDirection;
    }

    private void ApplyGravity()
    {
        rb.AddForce(gravityDirection * gravityStrength, ForceMode.Acceleration);
    }

    // Helper function to check if the collision is with an object that should block regen.
    private bool IsCollisionWithDisallowedTag(Collision collision)
    {
        foreach (string tag in disallowedRegenTags)
        {
            if (collision.gameObject.CompareTag(tag))
            {
                return true;
            }
        }
        return false;
    }

    // Update the counter when collisions with disallowed objects begin
    private void OnCollisionEnter(Collision collision)
    {
        if (IsCollisionWithDisallowedTag(collision))
        {
            disallowedRegenCollisionCount++;
        }
    }

    // Update the counter when collisions with disallowed objects end
    private void OnCollisionExit(Collision collision)
    {
        if (IsCollisionWithDisallowedTag(collision))
        {
            disallowedRegenCollisionCount = Mathf.Max(0, disallowedRegenCollisionCount - 1);
        }
    }

    // Only reset jump count if colliding with valid ground and not colliding with any disallowed objects.
    private void OnCollisionStay(Collision collision)
    {
        if ((groundLayer & (1 << collision.gameObject.layer)) != 0)
        {
            isGrounded = true;
            if (jumpResetTimer <= 0 && disallowedRegenCollisionCount == 0)
                jumpsLeft = maxJumps;
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

        // Optionally, adjust the camera immediately upon gravity change
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

    private void OnTriggerEnter(Collider other)
    {
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
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        for (int i = 0; i < jumpsLeft; i++)
        {
            Gizmos.DrawSphere(transform.position + Vector3.up * (i + 1) * 0.5f, 0.25f);
        }
    }
}
