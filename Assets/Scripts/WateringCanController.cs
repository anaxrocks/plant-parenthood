using UnityEngine;

public class WateringCanController : MonoBehaviour
{
    [Header("Particle System References")]
    [Tooltip("The particle system that will simulate water")]
    public ParticleSystem waterParticles;

    [Header("Pour Settings")]
    [Tooltip("The angle at which the watering can starts to pour (degrees)")]
    [Range(0, 90)]
    public float minPourAngle = 45f;

    [Tooltip("The angle at which the watering can pours at maximum rate (degrees)")]
    [Range(0, 180)]
    public float maxPourAngle = 90f;

    [Tooltip("The forward direction of the watering can (the direction it pours)")]
    public Vector3 pourDirection = Vector3.forward;

    [Tooltip("The up direction of the watering can when upright")]
    public Vector3 upDirection = Vector3.up;

    [Header("Pour VFX")]
    [Tooltip("Maximum emission rate of particles")]
    public float maxEmissionRate = 50f;

    [Tooltip("Minimum flow rate (even when barely tilted)")]
    public float minEmissionRate = 5f;

    [Header("Debug")]
    [Tooltip("Show debug visualizations")]
    public bool showDebug = true;

    private ParticleSystem.EmissionModule emissionModule;
    private float currentAngle = 0f;
    private bool isPouring = false;

    private void Start()
    {
        // Ensure we have a particle system
        if (waterParticles == null)
        {
            Debug.LogError("No water particle system assigned to WateringCanController!");
            enabled = false;
            return;
        }

        // Get the emission module for control
        emissionModule = waterParticles.emission;

        // Initialize to not pouring
        emissionModule.rateOverTime = 0;
        waterParticles.Stop();

        // Normalize our direction vectors
        pourDirection = pourDirection.normalized;
        upDirection = upDirection.normalized;
    }

    private void Update()
    {
        // Calculate the current tilting angle
        Vector3 currentUp = transform.TransformDirection(upDirection);
        Vector3 worldUp = Vector3.up;

        // Calculate the angle between the watering can's up direction and world up
        currentAngle = Vector3.Angle(currentUp, worldUp);

        // Determine if we should pour based on angle
        if (currentAngle >= minPourAngle)
        {
            if (!isPouring)
            {
                waterParticles.Play();
                isPouring = true;
            }

            // Calculate how much to pour (from min to max angle)
            float pourFactor = Mathf.Clamp01((currentAngle - minPourAngle) / (maxPourAngle - minPourAngle));
            float emissionRate = Mathf.Lerp(minEmissionRate, maxEmissionRate, pourFactor);

            // Update the particle emission rate
            emissionModule.rateOverTime = emissionRate;
        }
        else if (isPouring)
        {
            // Stop pouring
            waterParticles.Stop();
            emissionModule.rateOverTime = 0;
            isPouring = false;
        }

        // Output debug info
        if (showDebug)
        {
            Debug.Log($"Current angle: {currentAngle:F1}° - Pouring: {isPouring}");
        }
    }

    // Visualize pour direction and angles in the editor
    private void OnDrawGizmos()
    {
        if (!showDebug) return;

        // Draw the pour direction
        Gizmos.color = Color.blue;
        Vector3 worldPourDir = transform.TransformDirection(pourDirection);
        Gizmos.DrawRay(waterParticles ? waterParticles.transform.position : transform.position, worldPourDir * 0.2f);

        // Draw the up direction
        Gizmos.color = Color.green;
        Vector3 worldUpDir = transform.TransformDirection(upDirection);
        Gizmos.DrawRay(transform.position, worldUpDir * 0.2f);
    }
}