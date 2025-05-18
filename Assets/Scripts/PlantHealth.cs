using UnityEngine;
using UnityEngine.Events;

public class PlantHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Range(0f, 100f)] public float health = 100f;
    public float maxHealth = 100f;
    public float minHealth = 0f;

    [Header("Growth Settings")]
    public bool enableGrowth = true;
    public float growthRate = 0.1f;     // Base growth rate per second
    public float maxGrowthStage = 5f;   // Maximum growth stage
    public float currentGrowthStage = 0f;

    [Header("Visual Feedback")]
    public bool updateVisuals = true;
    public Transform plantModel;        // Reference to the plant model to scale
    public float minScale = 0.2f;       // Scale at minimum growth
    public float maxScale = 1.0f;       // Scale at maximum growth
    public Material healthyMaterial;    // Optional materials for different health states
    public Material unhealthyMaterial;
    public Renderer plantRenderer;      // Reference to the renderer to change materials

    [Header("Events")]
    public UnityEvent onPlantDeath;     // Event triggered when plant dies
    public UnityEvent onMaxGrowth;      // Event triggered when plant reaches max growth

    [Header("Health Factors Weights")]
    [Range(0f, 1f)] public float sunlightWeight = 0.35f;
    [Range(0f, 1f)] public float waterWeight = 0.35f;
    [Range(0f, 1f)] public float touchWeight = 0.3f;

    [Header("Debug")]
    public bool showDebugMessages = true;
    public bool showDetailedHealthInfo = false;

    // References to other plant components
    private PlantWatering wateringSystem;
    private PlantSunlight sunlightSystem;
    private PlantTouch touchSystem;

    // Status tracking
    private bool isDead = false;
    private bool reachedMaxGrowth = false;
    private float lastDebugTime = 0f;

    private void Start()
    {
        // Find required components
        wateringSystem = GetComponent<PlantWatering>();
        sunlightSystem = GetComponent<PlantSunlight>();
        touchSystem = GetComponent<PlantTouch>();

        if (plantRenderer == null && plantModel != null)
        {
            plantRenderer = plantModel.GetComponent<Renderer>();
        }

        // Initialize plant size based on current growth
        UpdatePlantVisuals();

        // Log warnings for missing components
        if (showDebugMessages)
        {
            if (wateringSystem == null)
                Debug.LogWarning("No PlantWatering component found on " + gameObject.name);

            if (sunlightSystem == null)
                Debug.LogWarning("No PlantSunlight component found on " + gameObject.name);

            if (touchSystem == null)
                Debug.LogWarning("No PlantTouch component found on " + gameObject.name);
        }
    }

    private void Update()
    {
        // Skip updates if plant is dead
        if (isDead) return;

        // Calculate health based on conditions
        CalculateOverallHealth();

        // Handle growth when conditions are good
        HandleGrowth();

        // Update visual representation
        if (updateVisuals)
        {
            UpdatePlantVisuals();
        }

        // Check for death condition
        if (health <= minHealth && !isDead)
        {
            PlantDied();
        }

        // Debug information
        if (showDebugMessages && Time.time - lastDebugTime > 10f)
        {
            lastDebugTime = Time.time;
            Debug.Log($"Plant health: {health:F1}%, Growth stage: {currentGrowthStage:F1}/{maxGrowthStage}");

            if (showDetailedHealthInfo)
            {
                string waterStatus = wateringSystem != null ? GetWaterStatus() : "No water system";
                string lightStatus = sunlightSystem != null ? sunlightSystem.GetLightStatus() : "No light system";
                string touchStatus = touchSystem != null ? touchSystem.GetTouchStatus() : "No touch system";

                Debug.Log($"Water: {waterStatus}, Light: {lightStatus}, Touch: {touchStatus}");
            }
        }
    }

    // Calculate overall health based on water, sunlight, and touch
    private void CalculateOverallHealth()
    {
        // Base decay if we don't have the necessary systems
        float healthChange = -0.05f * Time.deltaTime;

        // Calculate health contribution from all factors
        float waterFactor = EvaluateWaterFactor();
        float sunlightFactor = EvaluateSunlightFactor();
        float touchFactor = EvaluateTouchFactor();

        // Weight and combine the factors (normalize weights if they don't sum to 1)
        float totalWeight = sunlightWeight + waterWeight + touchWeight;
        if (totalWeight > 0)
        {
            float normalizedSunWeight = sunlightWeight / totalWeight;
            float normalizedWaterWeight = waterWeight / totalWeight;
            float normalizedTouchWeight = touchWeight / totalWeight;

            healthChange = (
                (sunlightFactor * normalizedSunWeight) +
                (waterFactor * normalizedWaterWeight) +
                (touchFactor * normalizedTouchWeight)
            ) * Time.deltaTime;
        }

        // Apply health change
        health = Mathf.Clamp(health + healthChange, minHealth, maxHealth);
    }

    // Evaluate water factor (-1 to +1, where negative is harmful and positive is beneficial)
    private float EvaluateWaterFactor()
    {
        if (wateringSystem == null) return 0f;

        float waterLevel = wateringSystem.waterLevel;
        float optimalLow = wateringSystem.optimalWaterLevelLow;
        float optimalHigh = wateringSystem.optimalWaterLevelHigh;

        // Check if water level is in optimal range
        if (waterLevel >= optimalLow && waterLevel <= optimalHigh)
        {
            // Optimal water - positive health effect (+0.2)
            return 0.2f;
        }
        else if (waterLevel < optimalLow)
        {
            // Too dry - negative health effect (up to -0.5)
            float drynessSeverity = 1 - (waterLevel / optimalLow);
            return -0.5f * drynessSeverity;
        }
        else
        {
            // Too wet - negative health effect (up to -0.5)
            float wetnessSeverity = (waterLevel - optimalHigh) / (wateringSystem.maxWaterLevel - optimalHigh);
            return -0.5f * wetnessSeverity;
        }
    }

    // Evaluate sunlight factor (-1 to +1)
    private float EvaluateSunlightFactor()
    {
        if (sunlightSystem == null) return 0f;

        float sunlightLevel = sunlightSystem.sunlightLevel;
        float optimalLow = sunlightSystem.optimalLightLevelLow;
        float optimalHigh = sunlightSystem.optimalLightLevelHigh;

        // Check if sunlight is in optimal range
        if (sunlightLevel >= optimalLow && sunlightLevel <= optimalHigh)
        {
            // Optimal light - positive health effect (+0.2)
            return 0.2f;
        }
        else if (sunlightLevel < optimalLow)
        {
            // Too dark - negative health effect (up to -0.5)
            float darknessSeverity = 1 - (sunlightLevel / optimalLow);
            return -0.5f * darknessSeverity;
        }
        else
        {
            // Too bright - negative health effect (up to -0.5)
            float brightnessSeverity = (sunlightLevel - optimalHigh) / (sunlightSystem.maxSunlightLevel - optimalHigh);
            return -0.5f * brightnessSeverity;
        }
    }

    // Evaluate touch factor (-1 to +1)
    private float EvaluateTouchFactor()
    {
        if (touchSystem == null) return 0f;

        // Scale based on touch intensity and whether plant likes being touched
        float touchIntensity = touchSystem.touchIntensity / touchSystem.maxTouchIntensity;
        if (touchIntensity <= 0) return 0f; // No touch effect

        if (touchSystem.likesBeeingTouched)
        {
            // Plant enjoys touch - positive health effect (up to +0.3)
            return 0.3f * touchIntensity;
        }
        else
        {
            // Plant doesn't like touch - negative health effect (up to -0.3)
            return -0.3f * touchIntensity;
        }
    }

    // Get a text description of current water conditions
    public string GetWaterStatus()
    {
        if (wateringSystem == null) return "No watering system";

        float level = wateringSystem.waterLevel;
        float optimalLow = wateringSystem.optimalWaterLevelLow;
        float optimalHigh = wateringSystem.optimalWaterLevelHigh;

        if (level < optimalLow)
        {
            return "Too dry";
        }
        else if (level > optimalHigh)
        {
            return "Too wet";
        }
        else
        {
            return "Optimal moisture";
        }
    }

    // Handle plant growth when conditions are good
    private void HandleGrowth()
    {
        if (!enableGrowth || reachedMaxGrowth) return;

        // Only grow if health is good
        if (health > maxHealth * 0.7f)
        {
            float growthAmount = growthRate * (health / maxHealth) * Time.deltaTime;
            currentGrowthStage = Mathf.Min(currentGrowthStage + growthAmount, maxGrowthStage);

            // Check if max growth reached
            if (currentGrowthStage >= maxGrowthStage && !reachedMaxGrowth)
            {
                reachedMaxGrowth = true;
                if (onMaxGrowth != null)
                {
                    onMaxGrowth.Invoke();
                }

                if (showDebugMessages)
                {
                    Debug.Log("Plant reached maximum growth stage!");
                }
            }
        }
    }

    // Update visual representation of the plant
    private void UpdatePlantVisuals()
    {
        if (plantModel != null)
        {
            // Scale plant based on growth
            float growthPercent = Mathf.Clamp01(currentGrowthStage / maxGrowthStage);
            float currentScale = Mathf.Lerp(minScale, maxScale, growthPercent);
            plantModel.localScale = new Vector3(currentScale, currentScale, currentScale);
        }

        // Update material based on health
        if (plantRenderer != null && healthyMaterial != null && unhealthyMaterial != null)
        {
            if (health > maxHealth * 0.5f)
            {
                plantRenderer.material = healthyMaterial;
            }
            else
            {
                plantRenderer.material = unhealthyMaterial;
            }
        }
    }

    // Plant death handling
    private void PlantDied()
    {
        isDead = true;
        if (showDebugMessages)
        {
            Debug.Log("Plant has died!");
        }

        if (onPlantDeath != null)
        {
            onPlantDeath.Invoke();
        }
    }

    // Public methods for external systems to affect health
    public void IncreaseHealth(float amount)
    {
        if (isDead) return;
        health = Mathf.Min(health + amount, maxHealth);
    }

    public void DecreaseHealth(float amount)
    {
        if (isDead) return;
        health = Mathf.Max(health - amount, minHealth);
    }

    // Reset the plant (useful for restarting)
    public void ResetPlant()
    {
        isDead = false;
        reachedMaxGrowth = false;
        health = maxHealth;
        currentGrowthStage = 0f;
        UpdatePlantVisuals();
    }
}