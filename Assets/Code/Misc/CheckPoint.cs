using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    public Vector3 offsetVector;
    public Vector3 gravityDirection = Vector3.down;
    public float gravityStrength = 9.81f;

    private void OnDrawGizmosSelected()
    {
        // Calculate the position of the offset vector relative to the object's position
        Vector3 offsetPosition = transform.position + offsetVector;

        // Draw a line from the object's position to the offset position
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, offsetPosition);

        // Draw a sphere at the offset position to make it more visible
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(offsetPosition, 0.1f);
    }
}