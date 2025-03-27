using UnityEngine;

public class GravityTrigger : MonoBehaviour
{
    public Vector3 gravityDirection = Vector3.down;
    public float gravityStrength = 9.81f;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the trigger is the player
        PlayerController playerController = other.GetComponent<PlayerController>();
        if (playerController != null)
        {
            // Apply the gravity data to the player
            playerController.SetGravity(gravityDirection, gravityStrength);
        }
    }
}