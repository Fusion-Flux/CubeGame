using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CollisionSound : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip collisionClip;       // The sound to play on collisions
    public float volume = 1.0f;           // Volume of the collision sound
    
    [Header("Collision Settings")]
    public float minCollisionVelocity = 2f;  // Minimum relative velocity to trigger a collision sound
    public float collisionCooldown = 0.2f;   // Time in seconds to wait between collision sounds

    private AudioSource audioSource;
    private float lastCollisionTime;

    [Header("Pitch Variation Settings")]
    public float minPitch = 0.9f;
    public float maxPitch = 1.1f;
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check relative velocity magnitude for a significant impact
        if (collision.relativeVelocity.magnitude >= minCollisionVelocity)
        {
            // Only play if enough time has passed since the last collision sound
            if (Time.time - lastCollisionTime >= collisionCooldown)
            {
                audioSource.PlayOneShot(collisionClip, volume);
                lastCollisionTime = Time.time;
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        // If you want repeated collision sounds while the object is in contact,
        // check for a 'spike' in relative velocity or impulse in OnCollisionStay.
        
        float impulseMagnitude = 0f;
        
        // collision.impulse can contain multiple contact points; sum them up:
        foreach (var contact in collision.contacts)
        {
            // There's no direct impulse per contact point in older Unity versions, 
            // but for demonstration we might estimate using relativeVelocity again or 
            // use collision.impulse.magnitude in new versions
        }

        // Or simply check relative velocity again (though it may be very small while staying)
        if (collision.relativeVelocity.magnitude >= minCollisionVelocity)
        {
            // Use cooldown to avoid spamming
            if (Time.time - lastCollisionTime >= collisionCooldown)
            {
                
                float randomPitch = Random.Range(minPitch, maxPitch);
                audioSource.pitch = randomPitch;
                audioSource.PlayOneShot(collisionClip, volume);
                lastCollisionTime = Time.time;
            }
        }
    }
}
