using UnityEngine;

public class PlantWatering : MonoBehaviour
{
    [Header("Water Settings")]
    public float waterLevel = 50f;
    public float maxWaterLevel = 100f;
    public float minWaterLevel = 0f;
    public float waterPerParticle = 0.5f;
    public float evaporationRate = 2f; // per second

    [Header("Plant Type Settings")]
    [Tooltip("0 = Drought-resistant, 1 = Normal, 2 = Water-loving")]
    public PlantWaterType plantWaterType = PlantWaterType.Normal;

    [Header("Water Requirements")]
    [Range(0f, 100f)] public float optimalWaterLevelLow = 30f;
    [Range(0f, 100f)] public float optimalWaterLevelHigh = 70f;

    [Header("Visual Feedback")]
    public bool showVisualFeedback = true;
    public GameObject dryIndicator;
    public GameObject optimalIndicator;
    public GameObject overWateredIndicator;

    [Header("Debug")]
    public bool showDebugMessages = true;

    // Reference to plant health component
    private PlantHealth plantHealth;

    // Time tracking for debug messages
    private float lastDebugTime = 0f;

    // Enum to define plant types based on water needs
    public enum PlantWaterType
    {
        DroughtResistant, // Prefers dry conditions (0-30%)
        Normal,           // Prefers moderate moisture (30-70%)
        WaterLoving       // Prefers wet conditions (70-100%)
    }

    private void Start()
    {
        // Find plant health component if not assigned
        plantHealth = GetComponent<PlantHealth>();

        // Set optimal water levels based on plant type
        SetOptimalWaterLevelsByPlantType();

        // Initialize indicators
        UpdateVisualIndicators();
    }

    private void Update()
    {
        // Reduce water level over time (evaporation)
        waterLevel -= evaporationRate * Time.deltaTime;
        waterLevel = Mathf.Clamp(waterLevel, minWaterLevel, maxWaterLevel);

        // Update visual indicators
        if (showVisualFeedback)
        {
            UpdateVisualIndicators();
        }

        // Debug information
        if (showDebugMessages && Time.time - lastDebugTime > 10f)
        {
            lastDebugTime = Time.time;
            string waterStatus = GetWaterStatus();
            Debug.Log($"Plant water level: {waterLevel:F1}% - {waterStatus}");
        }
    }

    // Called automatically when particles hit this collider
    void OnParticleCollision(GameObject other)
    {
        // You can check tag or other if you have multiple particle systems
        if (other.CompareTag("WaterParticles"))
        {
            waterLevel += waterPerParticle;
            waterLevel = Mathf.Clamp(waterLevel, minWaterLevel, maxWaterLevel);

            if (showDebugMessages)
            {
                Debug.Log("Plant watered. Current level: " + waterLevel);
            }
        }
    }

    // Configure optimal water levels based on plant type
    private void SetOptimalWaterLevelsByPlantType()
    {
        switch (plantWaterType)
        {
            case PlantWaterType.DroughtResistant:
                optimalWaterLevelLow = 10f;
                optimalWaterLevelHigh = 40f;
                break;

            case PlantWaterType.Normal:
                optimalWaterLevelLow = 30f;
                optimalWaterLevelHigh = 70f;
                break;

            case PlantWaterType.WaterLoving:
                optimalWaterLevelLow = 60f;
                optimalWaterLevelHigh = 90f;
                break;
        }
    }

    // Update visual indicators based on water level
    private void UpdateVisualIndicators()
    {
        if (!showVisualFeedback) return;

        bool isDry = waterLevel < optimalWaterLevelLow;
        bool isOptimal = waterLevel >= optimalWaterLevelLow && waterLevel <= optimalWaterLevelHigh;
        bool isOverwatered = waterLevel > optimalWaterLevelHigh;

        // Update indicators if they exist
        if (dryIndicator != null) dryIndicator.SetActive(isDry);
        if (optimalIndicator != null) optimalIndicator.SetActive(isOptimal);
        if (overWateredIndicator != null) overWateredIndicator.SetActive(isOverwatered);
    }

    // Get a text description of current water conditions
    public string GetWaterStatus()
    {
        if (waterLevel < optimalWaterLevelLow)
        {
            return "Too dry";
        }
        else if (waterLevel > optimalWaterLevelHigh)
        {
            return "Overwatered";
        }
        else
        {
            return "Optimal moisture";
        }
    }

    // Public method to add water directly (for UI buttons or external systems)
    public void AddWater(float amount)
    {
        waterLevel += amount;
        waterLevel = Mathf.Clamp(waterLevel, minWaterLevel, maxWaterLevel);
    }

    // Public method to set water level directly
    public void SetWaterLevel(float level)
    {
        waterLevel = Mathf.Clamp(level, minWaterLevel, maxWaterLevel);
    }
}