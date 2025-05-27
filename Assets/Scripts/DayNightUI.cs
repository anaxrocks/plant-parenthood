using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class DayNightUI : MonoBehaviour
{
    [Header("UI References")]
    public Canvas vrCanvas;
    public Image backgroundBar;
    public Image sunIcon;
    public Image moonIcon;
    public Image progressFill;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI cycleSpeedText;
    
    [Header("Day/Night Cycle Reference")]
    public DayNightCycle dayNightCycle;
    
    [Header("Visual Settings")]
    public Color dayBarColor = new Color(0.8f, 0.9f, 1f, 0.8f);
    public Color nightBarColor = new Color(0.1f, 0.1f, 0.3f, 0.8f);
    public Color sunsetBarColor = new Color(1f, 0.6f, 0.2f, 0.8f);
    public Color progressDayColor = new Color(1f, 0.9f, 0.4f);
    public Color progressNightColor = new Color(0.3f, 0.3f, 0.8f);
    
    [Header("Animation")]
    public float iconBobAmount = 10f;
    public float iconBobSpeed = 2f;
    public AnimationCurve sunMovementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("VR Positioning")]
    public Transform vrCamera;
    public Vector3 uiOffset = new Vector3(0, -0.3f, 1.5f);
    public bool followPlayer = true;
    public float followSmoothness = 2f;
    
    private RectTransform sunIconRect;
    private RectTransform moonIconRect;
    private RectTransform progressBarRect;
    private Vector3 sunStartPos;
    private Vector3 moonStartPos;
    private float barWidth;
    
    private void Start()
    {
        InitializeUI();
        SetupVRPositioning();
    }
    
    private void InitializeUI()
    {
        // Get references if not assigned
        if (dayNightCycle == null)
            dayNightCycle = FindObjectOfType<DayNightCycle>();
            
        if (vrCamera == null)
            vrCamera = Camera.main.transform;
            
        // Get RectTransforms for positioning
        if (sunIcon != null)
        {
            sunIconRect = sunIcon.GetComponent<RectTransform>();
            sunStartPos = sunIconRect.anchoredPosition;
        }
        
        if (moonIcon != null)
        {
            moonIconRect = moonIcon.GetComponent<RectTransform>();
            moonStartPos = moonIconRect.anchoredPosition;
        }
        
        if (progressFill != null)
        {
            progressBarRect = progressFill.GetComponent<RectTransform>();
            barWidth = progressBarRect.rect.width;
        }
    }
    
    private void SetupVRPositioning()
    {
        if (vrCanvas != null)
        {
            // Set up canvas for VR
            vrCanvas.renderMode = RenderMode.WorldSpace;
            vrCanvas.worldCamera = Camera.main;
            
            // Scale for VR comfort
            transform.localScale = Vector3.one * 0.001f; // Adjust scale as needed
        }
    }
    
    private void Update()
    {
        if (dayNightCycle == null) return;
        
        UpdateUIPosition();
        UpdateTimeDisplay();
        UpdateProgressBar();
        UpdateIconPositions();
        UpdateBarColors();
        UpdateIconVisibility();
    }
    
    private void UpdateUIPosition()
    {
        if (!followPlayer || vrCamera == null) return;
        
        // Calculate target position relative to camera
        Vector3 targetPosition = vrCamera.position + vrCamera.TransformDirection(uiOffset);
        Vector3 targetRotation = vrCamera.eulerAngles;
        targetRotation.x = 0; // Keep UI horizontal
        targetRotation.z = 0;
        
        // Smoothly move UI
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSmoothness);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(targetRotation), Time.deltaTime * followSmoothness);
    }
    
    private void UpdateTimeDisplay()
    {
        if (timeText != null) {
            string time24 = dayNightCycle.GetTimeString();

            if (TimeSpan.TryParse(time24, out TimeSpan time)) {
                string ampm = time.Hours >= 12 ? "PM" : "AM";
                int hour12 = time.Hours % 12;
                if (hour12 == 0) hour12 = 12;

                string formatted = $"{hour12:D2}:{time.Minutes:D2} {ampm}";
                timeText.text = formatted;
            } else {
                timeText.text = time24; // fallback in case parsing fails
            }
        }

        
        if (cycleSpeedText != null)
        {
            float cycleMinutes = dayNightCycle.dayDuration / 60f;
            cycleSpeedText.text = $"Cycle: {cycleMinutes:F1}min";
        }
    }
    
    private void UpdateProgressBar()
    {
        if (progressFill == null) return;
        
        // Update fill amount based on time of day
        progressFill.fillAmount = dayNightCycle.timeOfDay;
        
        // Update progress bar color
        Color currentColor = GetProgressColorForTime();
        progressFill.color = currentColor;
    }
    
    private void UpdateIconPositions()
    {
        float timeOfDay = dayNightCycle.timeOfDay;
        
        // Calculate positions along the bar
        float sunProgress = GetSunProgress(timeOfDay);
        float moonProgress = GetMoonProgress(timeOfDay);
        
        // Update sun position
        if (sunIconRect != null)
        {
            float sunX = Mathf.Lerp(-barWidth * 0.5f, barWidth * 0.5f, sunProgress);
            float sunY = sunStartPos.y + Mathf.Sin(Time.time * iconBobSpeed) * iconBobAmount;
            
            // Add curve for sun arc movement
            float arcHeight = sunMovementCurve.Evaluate(sunProgress) * 30f;
            sunY += arcHeight;
            
            sunIconRect.anchoredPosition = new Vector2(sunX, sunY);
        }
        
        // Update moon position
        if (moonIconRect != null)
        {
            float moonX = Mathf.Lerp(-barWidth * 0.5f, barWidth * 0.5f, moonProgress);
            float moonY = moonStartPos.y + Mathf.Sin(Time.time * iconBobSpeed + Mathf.PI) * iconBobAmount * 0.5f;
            
            // Moon follows opposite arc
            float arcHeight = sunMovementCurve.Evaluate(1f - moonProgress) * 20f;
            moonY += arcHeight;
            
            moonIconRect.anchoredPosition = new Vector2(moonX, moonY);
        }
    }
    
    private float GetSunProgress(float timeOfDay)
    {
        // Sun is visible from 0.2 to 0.8 (dawn to dusk)
        if (timeOfDay < 0.2f)
            return 0f;
        else if (timeOfDay > 0.8f)
            return 1f;
        else
            return (timeOfDay - 0.2f) / 0.6f;
    }
    
    private float GetMoonProgress(float timeOfDay)
    {
        // Moon is visible from 0.7 to 0.3 (wrapping around midnight)
        if (timeOfDay < 0.3f)
            return (timeOfDay + 0.3f) / 0.6f;
        else if (timeOfDay > 0.7f)
            return (timeOfDay - 0.7f) / 0.6f;
        else
            return 0f;
    }
    
    private void UpdateBarColors()
    {
        if (backgroundBar == null) return;
        
        float timeOfDay = dayNightCycle.timeOfDay;
        Color targetColor;
        
        // Determine background color based on time
        if (timeOfDay > 0.2f && timeOfDay < 0.3f) // Dawn
        {
            float t = (timeOfDay - 0.2f) / 0.1f;
            targetColor = Color.Lerp(nightBarColor, sunsetBarColor, t);
        }
        else if (timeOfDay >= 0.3f && timeOfDay <= 0.7f) // Day
        {
            float t = Mathf.Abs(0.5f - timeOfDay) * 2f; // 0 at noon, 1 at dawn/dusk
            targetColor = Color.Lerp(dayBarColor, sunsetBarColor, t * 0.3f);
        }
        else if (timeOfDay > 0.7f && timeOfDay < 0.8f) // Dusk
        {
            float t = (timeOfDay - 0.7f) / 0.1f;
            targetColor = Color.Lerp(sunsetBarColor, nightBarColor, t);
        }
        else // Night
        {
            targetColor = nightBarColor;
        }
        
        backgroundBar.color = targetColor;
    }
    
    private Color GetProgressColorForTime()
    {
        float timeOfDay = dayNightCycle.timeOfDay;
        
        if (timeOfDay > 0.25f && timeOfDay < 0.75f) // Day
        {
            return progressDayColor;
        }
        else // Night
        {
            return progressNightColor;
        }
    }
    
    private void UpdateIconVisibility()
    {
        float timeOfDay = dayNightCycle.timeOfDay;
        float sunIntensity = GetSunVisibility(timeOfDay);
        float moonIntensity = GetMoonVisibility(timeOfDay);
        
        // Update sun visibility
        if (sunIcon != null)
        {
            Color sunColor = sunIcon.color;
            sunColor.a = sunIntensity;
            sunIcon.color = sunColor;
            
            // Add glowing effect during peak hours
            float glowIntensity = Mathf.Max(0, (sunIntensity - 0.5f) * 2f);
            sunIcon.transform.localScale = Vector3.one * (1f + glowIntensity * 0.2f);
        }
        
        // Update moon visibility
        if (moonIcon != null)
        {
            Color moonColor = moonIcon.color;
            moonColor.a = moonIntensity;
            moonIcon.color = moonColor;
        }
    }
    
    private float GetSunVisibility(float timeOfDay)
    {
        if (timeOfDay < 0.2f || timeOfDay > 0.8f)
            return 0f;
        else if (timeOfDay < 0.25f)
            return (timeOfDay - 0.2f) / 0.05f; // Fade in
        else if (timeOfDay > 0.75f)
            return (0.8f - timeOfDay) / 0.05f; // Fade out
        else
            return 1f; // Full visibility
    }
    
    private float GetMoonVisibility(float timeOfDay)
    {
        if (timeOfDay > 0.3f && timeOfDay < 0.7f)
            return 0f;
        else if (timeOfDay < 0.1f || timeOfDay > 0.9f)
            return 1f; // Peak night
        else if (timeOfDay <= 0.3f)
            return (0.3f - timeOfDay) / 0.2f; // Fade out at dawn
        else
            return (timeOfDay - 0.7f) / 0.2f; // Fade in at dusk
    }
    
    // Public methods for interaction
    public void SetTimeOfDay(float time)
    {
        if (dayNightCycle != null)
            dayNightCycle.SetTimeOfDay(time);
    }
    
    public void ToggleFollowPlayer()
    {
        followPlayer = !followPlayer;
    }
    
    public void SetUIOffset(Vector3 offset)
    {
        uiOffset = offset;
    }
}