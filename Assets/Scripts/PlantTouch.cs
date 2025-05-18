using UnityEngine;

public class PlantTouch : MonoBehaviour
{
    [Header("Touch Settings")]
    public bool isTouchSensitive = true;    // Whether the plant responds to touch
    public float touchResponseTime = 10f;   // Time in seconds plant "remembers" touch
    public bool likesBeeingTouched = false; // Whether touch is beneficial or harmful

    [Header("Touch Effect")]
    public float touchIntensity = 0f;       // Current touch effect (0-100)
    public float maxTouchIntensity = 100f;  // Maximum touch effect value
    public float touchDecayRate = 5f;       // How quickly touch effect decays per second
    public float healthImpactRate = 1f;     // How quickly touch affects health

    [Header("Visual Feedback")]
    public bool showTouchEffect = true;
    public Transform touchResponseObject;   // Optional object that shows touch response
    public float touchVisualIntensity = 0.2f; // Visual scale change when touched

    [Header("Components")]
    public PlantHealth plantHealth;

    [Header("Debug")]
    public bool showDebugMessages = true;

    // Track touch state
    private float lastTouchTime = -100f;
    private Vector3 originalScale = Vector3.one;

    private void Start()
    {
        // Find PlantHealth component if not assigned
        if (plantHealth == null)
        {
            plantHealth = GetComponent<PlantHealth>();
        }

        // Store original scale of the response object
        if (touchResponseObject != null)
        {
            originalScale = touchResponseObject.localScale;
        }
    }

    private void Update()
    {
        // Decrease touch effect over time
        if (touchIntensity > 0)
        {
            touchIntensity -= touchDecayRate * Time.deltaTime;
            touchIntensity = Mathf.Max(0, touchIntensity);

            // Apply health impact
            if (plantHealth != null)
            {
                float healthChange = healthImpactRate * Time.deltaTime * (touchIntensity / maxTouchIntensity);

                if (likesBeeingTouched)
                {
                    plantHealth.IncreaseHealth(healthChange);
                }
                else
                {
                    plantHealth.DecreaseHealth(healthChange);
                }
            }
        }

        // Update visual feedback
        UpdateTouchVisuals();
    }

    // Call this when plant is touched
    public void Touch(float intensity = 20f)
    {
        if (!isTouchSensitive) return;

        lastTouchTime = Time.time;
        touchIntensity = Mathf.Min(touchIntensity + intensity, maxTouchIntensity);

        if (showDebugMessages)
        {
            string response = likesBeeingTouched ? "enjoys" : "is stressed by";
            Debug.Log($"Plant touched! This plant {response} being touched.");
        }
    }

    // Update visual feedback based on touch state
    private void UpdateTouchVisuals()
    {
        if (!showTouchEffect || touchResponseObject == null) return;

        float touchEffect = touchIntensity / maxTouchIntensity;

        if (touchEffect > 0)
        {
            // Apply visual effect based on whether plant likes being touched
            float scaleFactor = likesBeeingTouched ?
                1f + (touchEffect * touchVisualIntensity) :
                1f - (touchEffect * touchVisualIntensity);

            touchResponseObject.localScale = originalScale * scaleFactor;
        }
        else
        {
            // Reset to original scale
            touchResponseObject.localScale = originalScale;
        }
    }

    // Used for mouse interaction (can be called from raycasts)
    public void OnMouseDown()
    {
        Touch();
    }

    // Used for detecting physical collisions (for VR interaction, etc.)
    private void OnCollisionEnter(Collision collision)
    {
        // Optional: check tags to restrict what can touch the plant
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Hand"))
        {
            Touch();
        }
    }

    // Get a text description of current touch status
    public string GetTouchStatus()
    {
        if (touchIntensity <= 0)
        {
            return "Not touched recently";
        }
        else if (touchIntensity < 30f)
        {
            return "Lightly touched";
        }
        else if (touchIntensity < 70f)
        {
            return "Moderately touched";
        }
        else
        {
            return "Frequently touched";
        }
    }
}