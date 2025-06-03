using UnityEngine;
using System.Collections.Generic;

public class DayNightCycle : MonoBehaviour
{
    [Header("Time Settings")]
    public float dayDuration = 180f;    // Duration of a full day in seconds (3 minutes default)
    public float timeOfDay = 0f;        // 0 to 1 (0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset)
    public bool pauseCycleWhenNotPlaying = true;

    [Header("Lighting")]
    public Light directionalLight;      // Main sun light
    public float maxLightIntensity = 1f;
    public Color dayLightColor = Color.white;
    public Color sunsetLightColor = new Color(1f, 0.5f, 0.2f);
    public Color nightLightColor = new Color(0.2f, 0.2f, 0.5f);

    [Header("Skybox")]
    public Material daySkybox;
    public Material nightSkybox;
    public bool blendSkyboxes = true;

    [Header("Plant Interaction")]
    public List<PlantSunlight> plants = new List<PlantSunlight>();
    public float maxSunExposure = 0.5f; // Maximum sunlight exposure per second at noon
    public AnimationCurve sunIntensityCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Debug")]
    public bool showDebugMessages = true;
    private float lastDebugTime = 0f;

    private void Start()
    {
        // Find the directional light if not assigned
        if (directionalLight == null)
        {
            directionalLight = FindObjectOfType<Light>();
            if (directionalLight == null && showDebugMessages)
            {
                Debug.LogWarning("No directional light found in scene. Add one for day/night cycle.");
            }
        }

        // Find plants in the scene if list is empty
        if (plants.Count == 0)
        {
            FindPlantsInScene();
        }

        // Initialize skybox if available
        if (daySkybox != null && nightSkybox != null)
        {
            RenderSettings.skybox = daySkybox;
        }

        // Initialize with current time of day
        UpdateLighting();
    }

    private void Update()
    {
        // Skip updating if cycle is paused
        if (pauseCycleWhenNotPlaying && Time.timeScale == 0)
            return;

        // Update time of day
        timeOfDay += Time.deltaTime / dayDuration;
        if (timeOfDay >= 1f)
            timeOfDay -= 1f;

        // Update lighting based on time of day
        UpdateLighting();

        // Update plant sunlight exposure
        UpdatePlantSunlight();

        // Debug information
        if (showDebugMessages && Time.time - lastDebugTime > 10f)
        {
            lastDebugTime = Time.time;
            Debug.Log($"Time of day: {GetTimeString()}, Sunlight intensity: {GetSunIntensity():F2}");
        }
    }

    // Update lighting based on time of day
    private void UpdateLighting()
    {
        if (directionalLight != null)
        {
            // Set light rotation (0 = east, 90 = south, 180 = west)
            float sunRotation = timeOfDay * 360f;
            directionalLight.transform.rotation = Quaternion.Euler(new Vector3((sunRotation - 90f), 170f, 0));

            // Set light intensity and color
            float intensity = GetSunIntensity();
            directionalLight.intensity = intensity * maxLightIntensity;

            // Update light color
            directionalLight.color = GetLightColorForTime();
        }

        // Update skybox if available
        if (daySkybox != null && nightSkybox != null && blendSkyboxes)
        {
            float skyBlend = Mathf.Clamp01((GetSunIntensity() * 2f) - 0.2f);
            RenderSettings.skybox.Lerp(nightSkybox, daySkybox, skyBlend);
        }
    }

    // Calculate sun intensity based on time of day
    private float GetSunIntensity()
    {
        // Day is from 0.25 to 0.75 (sunrise to sunset)
        float dayProgress = 0f;

        if (timeOfDay > 0.25f && timeOfDay < 0.75f)
        {
            // Map 0.25-0.75 to 0-1
            dayProgress = (timeOfDay - 0.25f) * 2f;

            // Apply curve for smooth transition
            return sunIntensityCurve.Evaluate(1f - Mathf.Abs(dayProgress - 0.5f) * 2f);
        }

        return 0f; // Night time
    }

    // Get light color based on time of day
    private Color GetLightColorForTime()
    {
        if (timeOfDay < 0.25f || timeOfDay > 0.75f)
        {
            // Night time
            return nightLightColor;
        }
        else if ((timeOfDay > 0.2f && timeOfDay < 0.3f) || (timeOfDay > 0.7f && timeOfDay < 0.8f))
        {
            // Sunrise or sunset
            return sunsetLightColor;
        }
        else
        {
            // Day time
            return dayLightColor;
        }
    }

    // Update sunlight for all plants
    private void UpdatePlantSunlight()
    {
        float sunIntensity = GetSunIntensity();
        float sunExposure = sunIntensity * maxSunExposure * Time.deltaTime;

        foreach (var plant in plants)
        {
            if (plant != null)
            {
                // Check if plant is in direct sunlight by raycast
                bool inDirectLight = IsPlantInDirectSunlight(plant);

                if (inDirectLight)
                {
                    plant.ReceiveSunlight(sunExposure * 10f); // Multiply by 100 to match percentage scale
                }
                else
                {
                    // Plants in shade get some ambient light
                    plant.ReceiveSunlight(-0.005f); // 2% of direct sunlight
                }
            }
        }
    }

    // Check if plant is in direct sunlight using raycasting
    private bool IsPlantInDirectSunlight(PlantSunlight plant)
    {
        if (directionalLight == null || plant == null)
            return false;

        // Get ray direction from sun
        Vector3 rayDirection = directionalLight.transform.forward;

        // Cast ray from the plant position upward toward the sun
        RaycastHit hit;
        if (Physics.Raycast(plant.transform.position, -rayDirection, out hit))
        {
            // If we hit something that isn't the plant itself, it's in shadow
            if (hit.transform != plant.transform)
            {
                return false;
            }
        }

        return true;
    }

    // Find all plants with PlantSunlight component in the scene
    public void FindPlantsInScene()
    {
        plants.Clear();
        PlantSunlight[] foundPlants = FindObjectsOfType<PlantSunlight>();
        plants.AddRange(foundPlants);

        if (showDebugMessages)
        {
            Debug.Log($"Found {plants.Count} plants in the scene.");
        }
    }

    // Get current time as a string (HH:MM)
    public string GetTimeString()
    {
        int hours = Mathf.FloorToInt(timeOfDay * 24f);
        int minutes = Mathf.FloorToInt((timeOfDay * 24f - hours) * 60f);
        return $"{hours:00}:{minutes:00}";
    }

    // Manually set time of day (0-1)
    public void SetTimeOfDay(float time)
    {
        timeOfDay = Mathf.Clamp01(time);
        UpdateLighting();
    }

    // Set day duration in minutes
    public void SetDayDurationInMinutes(float minutes)
    {
        dayDuration = minutes * 60f;
    }
}