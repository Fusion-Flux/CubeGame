using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Force applied to the sphere for movement")]
    public float moveTorque = 10f;
    public float moveForce = 10f;
    public float angVel = 10f;

    [Header("Jump Settings")]
    [Tooltip("Force/impulse applied when jumping")]
    public float jumpForce = 7f;

    [Tooltip("Which layers are considered 'ground'")]
    public LayerMask groundLayer;

    [Header("Slam Settings")]
    [Tooltip("Downward acceleration while 'slamming' in mid-air")]
    public float slamForce = 20f;

    [Header("Camera Settings")]
    [Tooltip("The Transform of the camera that will follow/orbit the player")]
    public Transform cameraTransform;

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

    private float currentYaw = 0f;
    private float currentPitch = 0f;

    private Rigidbody rb;

    // Tracks how many "valid ground" contacts we currently have
    private int groundContactCount = 0;
    // True whenever groundContactCount > 0
    private bool isGrounded = false;

    public bool slamming = false;
    // We store whether jump was requested this frame
    private bool jumpRequested = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.maxAngularVelocity = angVel;
        
        // (Optional) Lock and hide the cursor for a more seamless experience
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // --- Mouse Input for Camera ---
        float mouseX = Input.GetAxis("Mouse X") * cameraSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * cameraSensitivity;

        currentYaw += mouseX;          // Yaw (horizontal turn)
        currentPitch -= mouseY;        // Pitch (vertical turn)
        currentPitch = Mathf.Clamp(currentPitch, minYAngle, maxYAngle);

        // --- Keyboard Input for Jump ---
        // Now we check isGrounded instead of a raycast-based method
        if (Input.GetButtonDown("Jump") && isGrounded && !jumpRequested)
        {
            jumpRequested = true;
        }
    }

    private void FixedUpdate()
    {
        // --- Keyboard Input for Movement ---
        float h = Input.GetAxis("Horizontal");  // A/D or Left/Right
        float v = Input.GetAxis("Vertical");    // W/S or Up/Down

        // Camera-aligned directions: forward & right
        Vector3 forward = cameraTransform.forward;
        Vector3 right   = cameraTransform.right;

        // We don't want our sphere to move up/down when pressing forward/back
        forward.y = 0f;
        right.y   = 0f;

        forward.Normalize();
        right.Normalize();

        // Calculate final movement direction based on WASD relative to the camera
        Vector3 moveDirection = (right * v + -forward * h).normalized;

        // Apply torque to roll the sphere
        rb.AddTorque(moveDirection * moveTorque);

        // If on the ground, apply extra force for more direct control
        if (isGrounded)
        {
            Vector3 forceDirection = (forward * v + right * h).normalized;
            rb.AddForce(forceDirection * moveForce);
        }

        // --- Apply Jump if Requested ---
        if (jumpRequested)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpRequested = false;
        }

        // If we have landed while slamming, reset slamming
        if (isGrounded && slamming)
        {
            slamming = false;
        }

        // --- Ground Slam (hold Ctrl to slam downward if in air) ---
        if (!isGrounded && Input.GetKey(KeyCode.LeftControl) && !slamming)
        {
            rb.AddForce(Vector3.down * slamForce, ForceMode.Impulse);
            slamming = true;
        }
    }

    private void LateUpdate()
    {
        // Build a rotation from our current yaw & pitch
        Quaternion camRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

        // Sphere (or player's) position is our camera target
        Vector3 targetPos = transform.position;

        // Position the camera behind and above the sphere
        Vector3 offset = new Vector3(0f, cameraHeight, -cameraDistance);
        cameraTransform.position = targetPos + camRotation * offset;

        // Make the camera look at the sphere (slightly above center)
        cameraTransform.LookAt(targetPos + Vector3.up * cameraHeight);
    }

    /// <summary>
    /// Called when this object starts colliding with another.
    /// We check if it's the ground layer and has an upward-ish normal.
    /// If so, increment our groundContactCount.
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        // 1) Check if the collider is in the ground layer.
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            // 2) Check if at least one contact normal is "floor-like" (normal.y > 0.5).
            bool foundGroundContact = false;
            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.normal.y > 0.5f)
                {
                    foundGroundContact = true;
                    break;
                }
            }

            // 3) If ground contact is found, increment the counter.
            if (foundGroundContact)
            {
                groundContactCount++;
                isGrounded = (groundContactCount > 0);
            }
        }
    }

    /// <summary>
    /// Called when this object stops colliding with another.
    /// If we leave a ground object, decrement our groundContactCount.
    /// </summary>
    private void OnCollisionExit(Collision collision)
    {
        // 1) Check if the collider is in the ground layer.
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            // Only decrement if we actually counted it on collision enter.
            groundContactCount = Mathf.Max(groundContactCount - 1, 0);
            isGrounded = (groundContactCount > 0);
        }
    }
}
