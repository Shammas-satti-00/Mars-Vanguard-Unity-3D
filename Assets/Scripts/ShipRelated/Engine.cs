using UnityEngine;
using System;

[Serializable]
public class Engine : Equipment
{
    [Header("Movement")]
    public float moveSpeed = 20f; // Forward speed (computed dynamically)
    public float baseMoveSpeed; // Store the base speed (adjustable by slider)
    [Header("Rotation Speeds")]
    public float pitchRotationSpeed = 50f; // Nose up/down
    public float yawRotationSpeed = 40f; // Turn left/right
    public float rollRotationSpeed = 45f; // Visual roll
    [Header("Airplane Style")]
    public float maxRollAngle = 30f;
    public float maxPitchAngle = 60f;
    public float yawTurnMultiplier = 0.5f;
    [Header("Boost Properties")]
    public float boostMultiplier = 2f; // speed multiplier while boosting
    public float boostDuration = 3f; // total boost "fuel" in seconds
    public bool unlimitedBoost = false;
    [Header("Boost Regeneration")]
    public float regenerationRate = 1f; // fuel per second (when not boosting)
    // ---------- Runtime state ----------
    public bool IsBoosting { get; private set; }
    public float CurrentFuel { get; private set; } // seconds of boost available
    // Transition tracking
    private float currentSpeedMultiplier = 1f; // current interpolated multiplier
    private float targetSpeedMultiplier = 1f; // target multiplier (1 or boostMultiplier)
    // Convenience: current effective forward speed
    public float CurrentSpeed => baseMoveSpeed * currentSpeedMultiplier;
    bool initialized = false;
    // Call once when equipping/spawning the engine (e.g., in Start() or Awake())
    public void InitializeRuntime()
    {
        if (initialized) return;
        baseMoveSpeed = moveSpeed; // Store base speed
        CurrentFuel = boostDuration; // Initialize fuel to full
        // Initial speed update
        moveSpeed = baseMoveSpeed * currentSpeedMultiplier;
        initialized = true;
    }
    // Boost Activation
    public bool ActivateBoost()
    {
        if (!initialized) InitializeRuntime(); // Fallback, but prefer calling earlier
        if (IsBoosting) return true;
        if (unlimitedBoost || CurrentFuel > 0.01f)
        {
            IsBoosting = true;
            targetSpeedMultiplier = boostMultiplier;
            return true;
        }
        return false; // blocked by cooldown or no fuel
    }
    // Boost Deactivation
    public void DeactivateBoost()
    {
        if (!IsBoosting) return;
        IsBoosting = false;
        targetSpeedMultiplier = 1f;
    }
    // Per-frame update (pass Time.deltaTime or Time.fixedDeltaTime)
    public void Tick(float deltaTime)
    {

        // Smooth speed transition
        if (currentSpeedMultiplier != targetSpeedMultiplier)
        {
            float transitionTime = (targetSpeedMultiplier > currentSpeedMultiplier)
                ? 0.5f // Boost ramp-up time
                : 1f; // Boost ramp-down time
            float transitionSpeed = Mathf.Abs(boostMultiplier - 1f) / transitionTime;
            if (targetSpeedMultiplier > currentSpeedMultiplier)
            {
                // Ramping up to boost
                currentSpeedMultiplier = Mathf.Min(targetSpeedMultiplier,
                    currentSpeedMultiplier + transitionSpeed * deltaTime);
            }
            else
            {
                // Ramping down to base speed
                currentSpeedMultiplier = Mathf.Max(targetSpeedMultiplier,
                    currentSpeedMultiplier - transitionSpeed * deltaTime);
            }
        }
        // Always enforce moveSpeed (prevents external overrides from persisting)
        moveSpeed = baseMoveSpeed * currentSpeedMultiplier;
        if (IsBoosting)
        {
            if (!unlimitedBoost)
            {
                CurrentFuel -= deltaTime;
                if (CurrentFuel <= 0f)
                {
                    CurrentFuel = 0f;
                    DeactivateBoost(); // triggers cooldown
                }
            }
        }
        else
        {
            // Regenerate fuel when not boosting
            if (!unlimitedBoost && CurrentFuel < boostDuration)
            {
                CurrentFuel = Mathf.Min(boostDuration, CurrentFuel + regenerationRate * deltaTime);
            }
        }
    }
    // Helpers
    public bool CanBoost() => unlimitedBoost || CurrentFuel > 0.01f;
    public float FuelPercent => boostDuration > 0f ? CurrentFuel / boostDuration : (unlimitedBoost ? 1f : 0f);
}