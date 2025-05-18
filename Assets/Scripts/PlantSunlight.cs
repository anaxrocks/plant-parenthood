using UnityEngine;

public class PlantSunlight : MonoBehaviour
{
    [Header("Sunlight Settings")]
    public float sunlightLevel = 0f;
    public float maxSunlightLevel = 100f;
    public float minSunlightLevel = 0f;

    [Header("Plant Type Settings")]
    [Tooltip("0 = Shade plant, 1 = Partial sun, 2 = Full sun")]
    public PlantType plantType = PlantType.PartialSun;

    [Header("Sunlight Requirements")]
    [Range(0f, 100f)] public float optimalLightLevelLow = 30f;
    [Range(0f, 100f)] public float optimalLightLevelHigh = 70f;

    [Header("Health Impact")]
    public float healthImpactRate = 2f;    // How quickly light conditions affect health
    public PlantHealth plantHealth;        // Reference to plant health component

    [Header("Debug")]
    public bool showDebugMessages = true;

    // Enum to define plant types based on sunlight needs
    public enum PlantType
    {
        Shade,      // Prefers low light (0-30%)
        PartialSun, // Prefers moderate light (30-70%)
        FullSun     // Prefers bright light (70-100%)
    }

    private void Start()
    {
        // Find the PlantHealth component if not assigned
        if (plantHealth == null)
        {
            plantHealth = GetComponent<PlantHealth>();
            if (plantHealth == null && showDebugMessages)
            {
                Debug.LogWarning("No PlantHealth component found. Add one to track plant health.");
            }
        }

        // Set optimal light levels based on plant type
        SetOptimalLightLevelsByPlantType();
    }

    private void Update()
    {
        // Only update health if we have a PlantHealth component
        if (plantHealth != null)
        {
            UpdatePlantHealthBasedOnLight();
        }

        // Debug information
        if (showDebugMessages && Time.frameCount % 300 == 0) // Only show every 300 frames to reduce spam
        {
            string lightStatus = GetLightStatus();
            Debug.Log($"Plant light status: {lightStatus} ({sunlightLevel:F1}%)");
        }
    }

    // Use this method to increase sunlight exposure (call from a light source or day/night system)
    public void ReceiveSunlight(float amount)
    {
        sunlightLevel += amount;
        sunlightLevel = Mathf.Clamp(sunlightLevel, minSunlightLevel, maxSunlightLevel);
    }

    // Sets the current sunlight level directly (useful for day/night cycle)
    public void SetSunlightLevel(float level)
    {
        sunlightLevel = Mathf.Clamp(level, minSunlightLevel, maxSunlightLevel);
    }

    // Configure optimal light levels based on plant type
    private void SetOptimalLightLevelsByPlantType()
    {
        switch (plantType)
        {
            case PlantType.Shade:
                optimalLightLevelLow = 0f;
                optimalLightLevelHigh = 30f;
                break;

            case PlantType.PartialSun:
                optimalLightLevelLow = 30f;
                optimalLightLevelHigh = 70f;
                break;

            case PlantType.FullSun:
                optimalLightLevelLow = 70f;
                optimalLightLevelHigh = 100f;
                break;
        }
    }

    // Update plant health based on current light conditions
    private void UpdatePlantHealthBasedOnLight()
    {
        if (sunlightLevel < optimalLightLevelLow)
        {
            // Too little light - decrease health
            float deficit = optimalLightLevelLow - sunlightLevel;
            float healthImpact = deficit * healthImpactRate * Time.deltaTime / 100f;
            plantHealth.DecreaseHealth(healthImpact);
        }
        else if (sunlightLevel > optimalLightLevelHigh)
        {
            // Too much light - decrease health
            float excess = sunlightLevel - optimalLightLevelHigh;
            float healthImpact = excess * healthImpactRate * Time.deltaTime / 100f;
            plantHealth.DecreaseHealth(healthImpact);
        }
        else
        {
            // Optimal light - increase health
            float healthImprovement = healthImpactRate * Time.deltaTime / 5f;
            plantHealth.IncreaseHealth(healthImprovement);
        }
    }

    // Get a text description of current light conditions
    public string GetLightStatus()
    {
        if (sunlightLevel < optimalLightLevelLow)
        {
            return "Not enough light";
        }
        else if (sunlightLevel > optimalLightLevelHigh)
        {
            return "Too much light";
        }
        else
        {
            return "Optimal light";
        }
    }
}