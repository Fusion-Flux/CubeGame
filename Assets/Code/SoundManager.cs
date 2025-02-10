using UnityEngine;
using System.Collections.Generic;
public class SoundManager : MonoBehaviour
{
    [Header("Audio Source")]
    public AudioSource audioSource;  
    
    [Header("Material Audio Clips")]
    public AudioClip groundClip;
    public AudioClip metalImpactClip;
    public AudioClip glassImpactClip;

    [Header("Pitch Variation Settings")]
    public float minPitch = 0.9f;
    public float maxPitch = 1.1f;

    private float defaultPitch;
    
    // Dictionary mapping material names to audio clips
    private Dictionary<string, AudioClip> materialSounds;

    private void Start()
    {
        // Setup dictionary. Make sure the keys match your actual PhysicMaterial names.
        materialSounds = new Dictionary<string, AudioClip>
        {
            { "SterileGround", groundClip },
            { "Metal", metalImpactClip },
            { "Glass", glassImpactClip }
        };
        
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // Remember the default pitch so we can reset it after playing each sound
        if (audioSource != null)
        {
            defaultPitch = audioSource.pitch;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Get the PhysicMaterial of the object we collided with
        PhysicsMaterial otherMaterial = collision.collider.sharedMaterial;

        if (otherMaterial != null)
        {
            string materialName = otherMaterial.name;

            // Check if the dictionary contains a clip for this material
            if (materialSounds.ContainsKey(materialName))
            {
                // Optionally vary volume by collision strength
                float collisionStrength = collision.relativeVelocity.magnitude;
                float volume = 1;

                float randomPitch = Random.Range(minPitch, maxPitch);
                audioSource.pitch = randomPitch;
                // Play the clip
                audioSource.PlayOneShot(materialSounds[materialName], volume);
                //audioSource.pitch = defaultPitch;
            }
        }
    }
}
