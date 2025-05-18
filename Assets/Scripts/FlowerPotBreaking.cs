using UnityEngine;

public class FlowerPotBreaking : MonoBehaviour
{
    [Header("Prefab References")]
    [Tooltip("The broken pot prefab that will replace this intact pot")]
    public GameObject brokenPotPrefab;

    [Header("Destruction Settings")]
    [Tooltip("Minimum impact force required to break the pot")]
    public float breakForceThreshold = 1.5f;

    [Tooltip("Optional audio clip to play when pot breaks")]
    public AudioClip breakSound;

    public AudioSource audioSource;
    private Rigidbody rb;
    private bool isBroken = false;

    private void Start()
    {
        // Get or add required components
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 2.0f;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Prevent multiple breaks
        if (isBroken) return;

        // Calculate impact force
        float impactForce = collision.relativeVelocity.magnitude;

        // Debug info
        Debug.Log("Collision detected with force: " + impactForce);

        // Check if impact force exceeds our threshold
        if (impactForce >= breakForceThreshold)
        {
            BreakPot(collision.contacts[0].point);
        }
    }

    private void BreakPot(Vector3 impactPoint)
    {
        isBroken = true;

        // Play sound if available
        if (breakSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(breakSound);
        }

        // Instantiate broken pot at the same position and rotation
        GameObject brokenPot = Instantiate(brokenPotPrefab, transform.position, transform.rotation);

        // Apply forces to broken pieces
        ApplyForcesToBrokenPieces(brokenPot, impactPoint);

        // Hide or destroy the original pot
        Destroy(gameObject);
    }

    private void ApplyForcesToBrokenPieces(GameObject brokenPot, Vector3 impactPoint)
    {
        // Get all child rigidbodies (broken pieces)
        Rigidbody[] pieces = brokenPot.GetComponentsInChildren<Rigidbody>();

        foreach (Rigidbody piece in pieces)
        {
            // Calculate direction from impact point
            Vector3 direction = (piece.position - impactPoint).normalized;

            // Add random variation to make the break feel more natural
            direction += new Vector3(
                Random.Range(-0.3f, 0.3f),
                Random.Range(0.1f, 0.5f),  // Slight upward bias
                Random.Range(-0.3f, 0.3f)
            );

            // Apply explosion force
            float force = Random.Range(2.0f, 5.0f);
            piece.AddForce(direction * force, ForceMode.Impulse);

            // Add some random torque for rotation
            piece.AddTorque(
                Random.Range(-1.0f, 1.0f),
                Random.Range(-1.0f, 1.0f),
                Random.Range(-1.0f, 1.0f),
                ForceMode.Impulse
            );
        }
    }
}