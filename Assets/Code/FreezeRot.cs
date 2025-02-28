using UnityEngine;

public class FreezeRot: MonoBehaviour
{
    void Update()
        {
            // Keep world rotation fixed (e.g., identity, or some constant rotation)
            transform.rotation = Quaternion.identity;
        }
}
