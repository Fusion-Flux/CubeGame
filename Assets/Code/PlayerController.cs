using Unity.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")] [Tooltip("Force applied to the sphere for movement")]
    public float moveTorque = 10f;

    public float moveForce = 10f;
    public float angVel = 10f;

    [Header("Jump Settings")] [Tooltip("Force/impulse applied when jumping")]
    public float jumpForce = 7f;

    [Tooltip("How far below the sphere we check for the ground")]
    public float groundCheckDistance = 1.1f;

    public float groundCheckDistanceForce = 1.1f;

    [Tooltip("Which layers are considered 'ground'")]
    public LayerMask groundLayer;

    [Header("Coyote Time Settings")] [Tooltip("Duration of coyote time after leaving the ground")]
    public float coyoteTime = 0.2f;

    private float coyoteTimeCounter;

    [Header("Slam Settings")] [Tooltip("Downward acceleration while 'slamming' in mid-air")]
    public float slamForce = 20f;

    [Header("Camera Settings")] [Tooltip("The Transform of the camera that will follow/orbit the player")]
    public Transform cameraTransform;
    
    public CheckPoint checkPoint;
    [Tooltip("Distance behind the sphere at which the camera will be placed")]
    public float cameraDistance = 5f;

    [Tooltip("Height offset for the camera above the sphere")]
    public float cameraHeight = 2f;

    [Tooltip("Mouse sensitivity for camera rotation")]
    public float cameraSensitivity = 2f;

    [Tooltip("Minimum pitch (looking down)")]
    public float minYAngle = -20f;

    [Tooltip("Maximum pitch (looking up)")]
    public float maxYAngle = 60f;
    
    [Header("Gravity Settings")] 
    private Vector3 gravityDirection = Vector3.down;
    private Vector3 prevGravityDirection = Vector3.down;
    private float gravityStrength = 9.81f;

    private Quaternion cameraRotation = Quaternion.identity;
    private float pitch = 0f;
    private Vector3 relativeForward;

    public bool slamming = false;

    private Rigidbody rb;

    // We store whether jump was requested this frame
    private bool jumpRequested = false;

    private bool isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.maxAngularVelocity = angVel;

        // Initialize relativeForward with the camera's initial forward direction
        relativeForward = cameraTransform.forward;

        // (Optional) Lock and hide the cursor for a more seamless experience
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // --- Mouse Input for Camera ---
        float mouseX = Input.GetAxis("Mouse X") * cameraSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * cameraSensitivity;

        // Adjust the pitch based on mouse Y movement
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minYAngle, maxYAngle);

        // Adjust the yaw based on mouse X movement
        cameraRotation = Quaternion.Euler(0f, mouseX, 0f) * cameraRotation;

        // Apply the new rotation
        cameraRotation = Quaternion.Euler(pitch, cameraRotation.eulerAngles.y, 0f);

        // --- Keyboard Input for Jump ---
        if (Input.GetButtonDown("Jump") && (isGrounded || coyoteTimeCounter > 0) && !jumpRequested)
        {
            jumpRequested = true;
        }

        // Decrease coyote time counter
        if (!isGrounded)
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
        else
        {
            coyoteTimeCounter = coyoteTime;
        }
    }

    private void FixedUpdate()
    {
        ApplyGravity();
        // --- Keyboard Input for Movement ---
        float h = Input.GetAxis("Horizontal"); // A/D or Left/Right
        float v = Input.GetAxis("Vertical"); // W/S or Up/Down

        // Camera-aligned directions: forward & right
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        // We don't want our sphere to move up/down when pressing forward/back
        forward = Vector3.ProjectOnPlane(forward, gravityDirection);
        right = Vector3.ProjectOnPlane(right, gravityDirection);

        forward.Normalize();
        right.Normalize();

        // Calculate final movement direction based on WASD relative to the camera
        Vector3 moveDirection = (right * v + -forward * h).normalized;

        // Apply torque to roll the sphere
        rb.AddTorque(moveDirection * moveTorque);

        if (IsGroundedForce())
        {
            // Optionally apply force for additional control
            Vector3 forceDirection = (forward * v + right * h).normalized;
            rb.AddForce(forceDirection * moveForce);
        }

        // --- Apply Jump if Requested ---
        if (jumpRequested)
        {
            rb.AddForce(-gravityDirection * jumpForce, ForceMode.Impulse);
            jumpRequested = false;
            coyoteTimeCounter = 0; // Reset coyote time after jumping
        }

        if (isGrounded && slamming)
        {
            slamming = false;
        }

        // --- Ground Slam (hold Ctrl to slam downward if in air) ---
        // Check if we're in the air and the user is holding the Ctrl key
        if (!isGrounded && Input.GetKey(KeyCode.LeftControl) && !slamming)
        {
            // Add downward force as an acceleration
            rb.AddForce(gravityDirection * slamForce, ForceMode.Impulse);
            slamming = true;
        }
    }

    private void LateUpdate()
    {
        // Interpolate the gravity direction
        Vector3 lerpGravityDirection = Vector3.Slerp(prevGravityDirection, gravityDirection, Time.deltaTime * 5f);
        Vector3 relativeUp = -lerpGravityDirection;

        // Calculate the rotation needed to align with the new gravity direction
        Quaternion gravityAlignment = Quaternion.FromToRotation(Vector3.up, relativeUp);

        // Calculate the relative forward direction
        Vector3 relativeForward = gravityAlignment * Vector3.forward;

        // Calculate the final rotation by combining gravity alignment and camera rotation
        Quaternion finalRotation = Quaternion.LookRotation(relativeForward, relativeUp) * cameraRotation;

        // Smoothly interpolate the camera's rotation
        cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, finalRotation, Time.deltaTime * 5.0f);

        // Directly set the camera's position
        Vector3 targetPos = transform.position;
        Vector3 offset = finalRotation * new Vector3(0f, cameraHeight, -cameraDistance);
        cameraTransform.position = targetPos + offset;

        // Make the camera look at the target position with the correct up direction
        cameraTransform.LookAt(targetPos, relativeUp);

        prevGravityDirection = lerpGravityDirection;
    }


    private void ApplyGravity()
    {
        rb.AddForce(gravityDirection * gravityStrength, ForceMode.Acceleration);
    }

    // Use collision detection to determine if the player is grounded
    private void OnCollisionStay(Collision collision)
    {
        if ((groundLayer & (1 << collision.gameObject.layer)) != 0)
        {
            isGrounded = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if ((groundLayer & (1 << collision.gameObject.layer)) != 0)
        {
            isGrounded = false;
        }
    }

    private bool IsGroundedForce()
    {
        // Raycast straight down from the sphere's position
        // Adjust groundCheckDistance depending on sphere radius
        return Physics.Raycast(transform.position, gravityDirection, groundCheckDistanceForce, groundLayer);
    }

    public void SetGravity(Vector3 newGravityDirection, float newGravityStrength)
    {
        //prevGravityDirection = gravityDirection;
        gravityDirection = newGravityDirection;
        gravityStrength = newGravityStrength;
    }

    public LayerMask resetLayer; // Layer mask for the reset objects
    public LayerMask checkPointLayer;
    private void OnTriggerEnter(Collider other)
    {
        if (checkPointLayer == (checkPointLayer | (1 << other.gameObject.layer)))
        {
            checkPoint = other.gameObject.GetComponent<CheckPoint>();
            //spawnPoint = point.spawnPoint;
        }

        // Check if the object the player collides with is on the "Reset" layer
        if (resetLayer == (resetLayer | (1 << other.gameObject.layer)))
        {
            // Move the player to the reset point
            transform.position = checkPoint.transform.position + checkPoint.offsetVector;
            

            // Reset only the horizontal velocity
            Rigidbody playerRigidbody = GetComponent<Rigidbody>();
            if (playerRigidbody != null)
            {
                Vector3 velocity = playerRigidbody.linearVelocity;
                Vector3 targetGravityDirection = checkPoint.gravityDirection;

                // Calculate the rotation needed to align currentGravityDirection with targetGravityDirection
                Quaternion rotation = Quaternion.FromToRotation(gravityDirection, targetGravityDirection);

                // Apply the rotation to the velocity
                velocity = rotation * velocity;
                float projection = Vector3.Dot(velocity, checkPoint.gravityDirection);
                velocity = checkPoint.gravityDirection * projection;

                playerRigidbody.linearVelocity = velocity;
            }
            SetGravity(checkPoint.gravityDirection,checkPoint.gravityStrength);
        }
    }
}
