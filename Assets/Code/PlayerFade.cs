using UnityEngine;

public class PlayerFade : MonoBehaviour
{
    [Tooltip("Reference to the Cinemachine camera or its transform.")]
    public Transform cameraTransform;
    
    [Tooltip("Distance below which the player is fully faded (alpha = 0).")]
    public float minFadeDistance = 2.0f;
    
    [Tooltip("Distance above which the player is fully visible (alpha = 1).")]
    public float maxFadeDistance = 5.0f;
    
    [Tooltip("Speed at which the fade transitions.")]
    public float fadeSpeed = 2.0f;
    
    private Renderer[] renderers;

    void Start()    
    {
        // Cache all renderers on the player and its children.
        renderers = GetComponentsInChildren<Renderer>();
    }

    void FixedUpdate()
    {
        // Calculate the distance between the camera and the player.
        float distance = Vector3.Distance(cameraTransform.position, transform.position);
        
        // Use InverseLerp to calculate a normalized value:
        // 0 when distance <= minFadeDistance, 1 when distance >= maxFadeDistance.
        float targetAlpha = Mathf.InverseLerp(minFadeDistance, maxFadeDistance, distance);

        // Update each renderer's material alpha.
        foreach (Renderer rend in renderers)
        {
            Material[] mats = rend.materials; // Working with instances to avoid changing shared materials.
            for (int i = 0; i < mats.Length; i++)
            {
                // Retrieve the current color from the HDRP/Lit material.
                Color currentColor = mats[i].GetColor("_BaseColor");
                // Lerp between the current alpha and the target alpha.
                currentColor.a = Mathf.Lerp(currentColor.a, targetAlpha, Time.deltaTime * fadeSpeed);
                mats[i].SetColor("_BaseColor", currentColor);
            }
        }
    }
}