using Unity.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveTorque = 10f;
    public float moveForce = 10f;
    public float airMoveForce = 10f;
    public float angVel = 10f;

    [Header("Jump Settings")]
    public float jumpForce = 7f;
    public float groundCheckDistance = 1.1f;
    public float groundCheckDistanceForce = 1.1f;
    public LayerMask groundLayer;

    [Header("Coyote Time Settings")]
    public float coyoteTime = 0.2f;

    [Header("Slam Settings")]
    public float slamForce = 20f;

    [Header("Camera Settings")]
    public Transform cameraTransform;
    public CheckPoint checkPoint;
    public float cameraDistance = 5f;
    public float cameraHeight = 2f;
    public float cameraSensitivity = 2f;
    public float minYAngle = -90f;
    public float maxYAngle = 90f;

    [Header("Gravity Settings")]
    private Vector3 gravityDirection = Vector3.down;
    private Vector3 prevGravityDirection = Vector3.down;
    private float gravityStrength = 9.81f;

    private Quaternion cameraRotation = Quaternion.identity;
    private float pitch = 0f;
    private Vector3 relativeForward;

    public bool slamming = false;
    private bool canJump = false;
    private Rigidbody rb;
    private bool jumpRequested = false;
    private bool isGrounded;
    private int jumpsLeft = 2;
    public int maxJumps = 2;
    private float jumpResetCooldown = 0.5f;
    private float jumpResetTimer = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.maxAngularVelocity = angVel;
        relativeForward = cameraTransform.forward;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
        }else if (Input.GetKeyDown(KeyCode.Escape) && uniVals.paused && !uniVals.levelComplete)
        {
            uniVals.paused = false;
            Time.timeScale = 1;
            uniVals.Paused.gameObject.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
        }
        // --- Mouse Input for Camera ---
        float mouseX = Input.GetAxis("Mouse X") * cameraSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * cameraSensitivity;

        pitch = Mathf.Clamp(pitch - mouseY, minYAngle, maxYAngle);
        cameraRotation = Quaternion.Euler(0f, mouseX, 0f) * cameraRotation;
        cameraRotation = Quaternion.Euler(pitch, cameraRotation.eulerAngles.y, 0f);

        if (Input.GetButtonDown("Jump") && jumpsLeft > 0 && !jumpRequested)
        {
            jumpRequested = true;
        }

        if (jumpResetTimer > 0)
        {
            jumpResetTimer -= Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        ApplyGravity();
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, gravityDirection).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, gravityDirection).normalized;
        Vector3 moveDirection = (right * v + -forward * h).normalized;

        rb.AddTorque(moveDirection * moveTorque);

        Vector3 forceDirection = (forward * v + right * h).normalized;
        rb.AddForce(forceDirection * (IsGroundedForce() ? moveForce : airMoveForce));

        if (jumpRequested)
        {
            rb.AddForce(-gravityDirection * jumpForce, ForceMode.Impulse);
            jumpRequested = false;
            jumpsLeft--;
            jumpResetTimer = jumpResetCooldown;
        }

        if (isGrounded && slamming)
        {
            slamming = false;
        }

        if (!isGrounded && Input.GetKey(KeyCode.LeftControl) && !slamming)
        {
            rb.AddForce(gravityDirection * slamForce, ForceMode.Impulse);
            slamming = true;
        }
    }

    private void LateUpdate()
    {
        Vector3 lerpGravityDirection = Vector3.Slerp(prevGravityDirection, gravityDirection, Time.deltaTime * 5f);
        Vector3 relativeUp = -lerpGravityDirection;
        Quaternion gravityAlignment = Quaternion.FromToRotation(Vector3.up, relativeUp);
        Vector3 relativeForward = gravityAlignment * Vector3.forward;
        Quaternion finalRotation = Quaternion.LookRotation(relativeForward, relativeUp) * cameraRotation;

        cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, finalRotation, Time.deltaTime * 5.0f);

        Vector3 targetPos = transform.position;
        Vector3 offset = finalRotation * new Vector3(0f, cameraHeight, -cameraDistance);
        cameraTransform.position = targetPos + offset;
        cameraTransform.LookAt(targetPos, relativeUp);

        prevGravityDirection = lerpGravityDirection;
    }

    private void ApplyGravity()
    {
        rb.AddForce(gravityDirection * gravityStrength, ForceMode.Acceleration);
    }

    private void OnCollisionStay(Collision collision)
    {
        if ((groundLayer & (1 << collision.gameObject.layer)) != 0)
        {
            isGrounded = true;
            if (jumpResetTimer <= 0)
                jumpsLeft = maxJumps;
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
        return Physics.Raycast(transform.position, gravityDirection, groundCheckDistanceForce, groundLayer);
    }

    public void SetGravity(Vector3 newGravityDirection, float newGravityStrength)
    {
        gravityDirection = newGravityDirection;
        gravityStrength = newGravityStrength;
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
        }
        if (resetLayer == (resetLayer | (1 << other.gameObject.layer)))
        {
            
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
