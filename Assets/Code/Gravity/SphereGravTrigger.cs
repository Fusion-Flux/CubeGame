using UnityEngine;

public class SphereGravTrigger : MonoBehaviour
{
    public float gravityStrength = 9.81f;
    public bool inverted = false;
    private void OnTriggerStay(Collider other)
    {
        // Check if the object entering the trigger is the player
        PlayerController playerController = other.GetComponent<PlayerController>();
        if (playerController != null)
        {
            Vector3 difference = playerController.transform.position - transform.position;
            if (inverted)
                difference = -difference;
            // Apply the gravity data to the player
            playerController.SetGravity(-difference.normalized, gravityStrength);
        }
    }
}