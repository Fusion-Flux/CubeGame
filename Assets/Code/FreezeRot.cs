using UnityEngine;

public class FreezeRot: MonoBehaviour
{
    void LateUpdate()
        {
            // Keep world rotation fixed (e.g., identity, or some constant rotation)
            transform.rotation = Quaternion.identity;
        }
}
