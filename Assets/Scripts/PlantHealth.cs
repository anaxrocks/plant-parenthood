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
    public float baseGrowthRate = 0.02f;     // Much slower base growth rate
    public int maxGrowthStage = 3;           // 3 stages: Small, Medium, Large
    public int currentGrowthStage = 1;       // Start at stage 1 (small)
    
    [Header("Growth Requirements")]
    [Range(0f, 1f)] public float growthHealthThreshold = 0.8f;    // Need 80% health to grow
    public float timeInOptimalConditions = 0f;                     // Time spent in good conditions
    public float requiredOptimalTime = 30f;                       // 30 seconds of good conditions to advance stage
    public float conditionCheckInterval = 1f;                     // Check conditions every second
    private float lastConditionCheck = 0f;

    [Header("Visual Feedback - Prefabs")]
    public GameObject smallPlantPrefab;      // Stage 1 prefab
    public GameObject mediumPlantPrefab;     // Stage 2 prefab
    public GameObject largePlantPrefab;      // Stage 3 prefab
    
    [Header("Visual Feedback - Materials")]
    public Material healthyMaterial;         // Optional materials for different health states
    public Material unhealthyMaterial;
    public Material stressedMaterial;        // New material for stressed plants
    
    private GameObject currentPlantModel;    // Current active plant model
    private Renderer plantRenderer;          // Current plant renderer

    [Header("Events")]
    public UnityEvent onPlantDeath;          // Event triggered when plant dies
    public UnityEvent onGrowthStageChange;   // Event triggered when plant grows to next stage
    public UnityEvent onMaxGrowth;           // Event triggered when plant reaches max growth

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
    private bool areConditionsOptimal = false;

    private void Start()
    {
        // Find required components
        wateringSystem = GetComponent<PlantWatering>();
        sunlightSystem = GetComponent<PlantSunlight>();
        touchSystem = GetComponent<PlantTouch>();

        // Set up initial plant model
        UpdatePlantModel();

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

        // Check growth conditions periodically
        if (Time.time - lastConditionCheck >= conditionCheckInterval)
        {
            lastConditionCheck = Time.time;
            CheckGrowthConditions();
        }

        // Handle growth progression
        HandleGrowthProgression();

        // Update visual representation
        UpdatePlantVisuals();

        // Check for death condition
        if (health <= minHealth && !isDead)
        {
            PlantDied();
        }

        // Debug information
        if (showDebugMessages && Time.time - lastDebugTime > 5f)
        {
            lastDebugTime = Time.time;
            Debug.Log($"Plant - Health: {health:F1}%, Stage: {currentGrowthStage}/{maxGrowthStage}, Optimal Time: {timeInOptimalConditions:F1}s");

            if (showDetailedHealthInfo)
            {
                string waterStatus = wateringSystem != null ? GetWaterStatus() : "No water system";
                string lightStatus = sunlightSystem != null ? sunlightSystem.GetLightStatus() : "No light system";
                string touchStatus = touchSystem != null ? touchSystem.GetTouchStatus() : "No touch system";

                Debug.Log($"Water: {waterStatus}, Light: {lightStatus}, Touch: {touchStatus}, Conditions Optimal: {areConditionsOptimal}");
            }
        }
    }

    // Check if conditions are optimal for growth
    private void CheckGrowthConditions()
    {
        if (!enableGrowth || reachedMaxGrowth) return;

        // Check if health is high enough
        bool healthGood = health >= (maxHealth * growthHealthThreshold);
        
        // Check if all conditions are in their optimal ranges
        bool waterOptimal = IsWaterOptimal();
        bool lightOptimal = IsLightOptimal();
        bool touchOptimal = IsTouchOptimal();

        // All conditions must be optimal for growth
        areConditionsOptimal = healthGood && waterOptimal && lightOptimal && touchOptimal;

        if (areConditionsOptimal)
        {
            timeInOptimalConditions += conditionCheckInterval;
            if (showDebugMessages && (int)timeInOptimalConditions % 10 == 0)
            {
                Debug.Log($"Plant thriving! Optimal conditions for {timeInOptimalConditions:F0} seconds");
            }
        }
        else
        {
            // Reset timer if conditions aren't optimal
            timeInOptimalConditions = 0f;
            
            if (showDebugMessages)
            {
                string reason = "";
                if (!healthGood) reason += "Low health ";
                if (!waterOptimal) reason += "Bad water ";
                if (!lightOptimal) reason += "Bad light ";
                if (!touchOptimal) reason += "Bad touch ";
                
                if (timeInOptimalConditions > 0)
                    Debug.Log($"Growth halted: {reason.Trim()}");
            }
        }
    }

    // Check if water conditions are optimal
    private bool IsWaterOptimal()
    {
        if (wateringSystem == null) return true; // If no system, don't penalize

        float waterLevel = wateringSystem.waterLevel;
        float optimalLow = wateringSystem.optimalWaterLevelLow;
        float optimalHigh = wateringSystem.optimalWaterLevelHigh;

        return waterLevel >= optimalLow && waterLevel <= optimalHigh;
    }

    // Check if light conditions are optimal
    private bool IsLightOptimal()
    {
        if (sunlightSystem == null) return true; // If no system, don't penalize

        float sunlightLevel = sunlightSystem.sunlightLevel;
        float optimalLow = sunlightSystem.optimalLightLevelLow;
        float optimalHigh = sunlightSystem.optimalLightLevelHigh;

        return sunlightLevel >= optimalLow && sunlightLevel <= optimalHigh;
    }

    // Check if touch conditions are optimal
    private bool IsTouchOptimal()
    {
        if (touchSystem == null) return true; // If no system, don't penalize

        float touchIntensity = touchSystem.touchIntensity;
        
        if (touchSystem.likesBeeingTouched)
        {
            // If plant likes touch, moderate touch is good, too much is bad
            return touchIntensity > 0 && touchIntensity <= (touchSystem.maxTouchIntensity * 0.7f);
        }
        else
        {
            // If plant doesn't like touch, minimal touch is optimal
            return touchIntensity <= (touchSystem.maxTouchIntensity * 0.1f);
        }
    }

    // Handle growth stage progression
    private void HandleGrowthProgression()
    {
        if (!enableGrowth || reachedMaxGrowth || currentGrowthStage >= maxGrowthStage) return;

        // Check if we've been in optimal conditions long enough to grow
        if (timeInOptimalConditions >= requiredOptimalTime)
        {
            GrowToNextStage();
        }
    }

    // Advance to the next growth stage
    private void GrowToNextStage()
    {
        int previousStage = currentGrowthStage;
        currentGrowthStage++;
        timeInOptimalConditions = 0f; // Reset timer for next stage

        if (showDebugMessages)
        {
            Debug.Log($"Plant grew from stage {previousStage} to stage {currentGrowthStage}!");
        }

        // Update the plant model
        UpdatePlantModel();

        // Trigger events
        if (onGrowthStageChange != null)
        {
            onGrowthStageChange.Invoke();
        }

        // Check if we've reached maximum growth
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

    // Update the plant model based on current growth stage
    private void UpdatePlantModel()
    {
        // Destroy current model if it exists
        if (currentPlantModel != null)
        {
            DestroyImmediate(currentPlantModel);
        }

        // Instantiate appropriate prefab based on growth stage
        GameObject prefabToUse = null;
        switch (currentGrowthStage)
        {
            case 1:
                prefabToUse = smallPlantPrefab;
                break;
            case 2:
                prefabToUse = mediumPlantPrefab;
                break;
            case 3:
                prefabToUse = largePlantPrefab;
                break;
            default:
                prefabToUse = smallPlantPrefab;
                break;
        }

        if (prefabToUse != null)
        {
            currentPlantModel = Instantiate(prefabToUse, transform);
            currentPlantModel.transform.localPosition = Vector3.zero;
            currentPlantModel.transform.localRotation = Quaternion.identity;
            
            // Get the renderer from the new model
            plantRenderer = currentPlantModel.GetComponent<Renderer>();
            if (plantRenderer == null)
            {
                plantRenderer = currentPlantModel.GetComponentInChildren<Renderer>();
            }
        }
        else
        {
            if (showDebugMessages)
            {
                Debug.LogWarning($"No prefab assigned for growth stage {currentGrowthStage}");
            }
        }
    }

    // Calculate overall health based on water, sunlight, and touch
    private void CalculateOverallHealth()
    {
        // Base decay if we don't have the necessary systems
        float healthChange = -0.03f * Time.deltaTime; // Slightly slower decay

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
            // Optimal water - positive health effect (+0.15, reduced from 0.2)
            return 0.15f;
        }
        else if (waterLevel < optimalLow)
        {
            // Too dry - negative health effect (up to -0.4)
            float drynessSeverity = 1 - (waterLevel / optimalLow);
            return -0.4f * drynessSeverity;
        }
        else
        {
            // Too wet - stronger negative effect for overwatering
            float wetnessSeverity = (waterLevel - optimalHigh) / (wateringSystem.maxWaterLevel - optimalHigh);
            return -0.6f * wetnessSeverity; // Increased penalty for overwatering
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
            // Optimal light - positive health effect (+0.15, reduced from 0.2)
            return 0.15f;
        }
        else if (sunlightLevel < optimalLow)
        {
            // Too dark - negative health effect (up to -0.4)
            float darknessSeverity = 1 - (sunlightLevel / optimalLow);
            return -0.4f * darknessSeverity;
        }
        else
        {
            // Too bright - stronger negative effect for too much sun
            float brightnessSeverity = (sunlightLevel - optimalHigh) / (sunlightSystem.maxSunlightLevel - optimalHigh);
            return -0.6f * brightnessSeverity; // Increased penalty for too much light
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
            // Plant enjoys moderate touch, but too much is stressful
            if (touchIntensity <= 0.7f)
            {
                return 0.2f * touchIntensity; // Positive effect for moderate touch
            }
            else
            {
                // Too much touch becomes stressful even for plants that like it
                float excessTouch = (touchIntensity - 0.7f) / 0.3f;
                return 0.2f * 0.7f - (0.4f * excessTouch); // Diminishing returns, then negative
            }
        }
        else
        {
            // Plant doesn't like touch - negative health effect that gets worse with more touch
            return -0.4f * touchIntensity; // Increased penalty for unwanted touch
        }
    }

    // Get a text description of current water conditions
    public string GetWaterStatus()
    {
        if (wateringSystem == null) return "No watering system";

        float level = wateringSystem.waterLevel;
        float optimalLow = wateringSystem.optimalWaterLevelLow;
        float optimalHigh = wateringSystem.optimalWaterLevelHigh;

        if (level < optimalLow * 0.5f)
        {
            return "Severely dry";
        }
        else if (level < optimalLow)
        {
            return "Too dry";
        }
        else if (level > optimalHigh * 1.5f)
        {
            return "Severely overwatered";
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

    // Update visual representation of the plant
    private void UpdatePlantVisuals()
    {
        // Update material based on health and conditions
        if (plantRenderer != null && healthyMaterial != null && unhealthyMaterial != null)
        {
            if (health > maxHealth * 0.7f && areConditionsOptimal)
            {
                plantRenderer.material = healthyMaterial;
            }
            else if (health > maxHealth * 0.3f)
            {
                // Use stressed material if available, otherwise unhealthy
                if (stressedMaterial != null && !areConditionsOptimal)
                {
                    plantRenderer.material = stressedMaterial;
                }
                else
                {
                    plantRenderer.material = unhealthyMaterial;
                }
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
        currentGrowthStage = 1;
        timeInOptimalConditions = 0f;
        areConditionsOptimal = false;
        UpdatePlantModel();
    }

    // Public getters for external systems
    public bool IsInOptimalConditions()
    {
        return areConditionsOptimal;
    }

    public float GetGrowthProgress()
    {
        if (reachedMaxGrowth) return 1f;
        
        float stageProgress = (currentGrowthStage - 1) / (float)(maxGrowthStage - 1);
        float timeProgress = timeInOptimalConditions / requiredOptimalTime;
        float currentStageContribution = timeProgress / maxGrowthStage;
        
        return stageProgress + currentStageContribution;
    }
}