using UnityEngine;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;

[RequireComponent(typeof(Ship))]
public class ShipAI : MonoBehaviour
{
    [Header("AI Configuration")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float detectionUpdateRate = 0.5f;

    [Header("Combat Settings")]
    [SerializeField] private float shootingRange = 500f;
    [SerializeField] private float alignmentAngle = 35f;
    [SerializeField] private float aimAccuracy = 1f;
    [SerializeField] private float passDistance = 300f;

    [Header("Positioning Settings")]
    [SerializeField] private float offsetRandomness = 50f;
    [SerializeField] private float predictionTime = 1f;

    [Header("Steering Settings")]
    [SerializeField] private float steeringSpeed = 5f;
    [SerializeField] private float rotationMultiplier = 1f;
    [SerializeField] private float maxInputAngle = 30f;
    [SerializeField] private float rollSmoothness = 2f;

    [Header("Weapon Settings")]
    [SerializeField] private float cannonFireRate = 0.15f;
    [SerializeField] private float missileFireRate = 1.5f;

    [Header("AI Difficulty")]
    [SerializeField] private AIDifficulty difficulty = AIDifficulty.Normal;

    [Header("Enemy Class Data")]
    [SerializeField] private EnemyClassData classData;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    public int reward;
    private int baseReward;
    private int perCannonReward;

    // Components
    private Ship ship;
    private EquipmentManager equipmentManager;
    private DamageHandler damageHandler;
    private AStar3D.ECS.ShipPathfindingBridge pathfindingBridge;

    // Target tracking
    private Transform playerTransform;
    private float lastDetectionTime;

    // Player velocity estimation
    private Vector3 playerLastPos;
    private Vector3 playerEstimatedVelocity;

    // AI State
    private enum AIState { Searching, Attacking, Retreating }
    private AIState currentState = AIState.Searching;

    // Steering
    private Vector3 lastInput = Vector3.zero;
    private Vector3 targetPosition;
    private Vector3 randomOffset;
    private float offsetUpdateTimer = 0f;
    private float offsetUpdateInterval = 3f;

    // Retreating
    private Vector3 passPosition;

    // Our own velocity
    private Vector3 currentVelocity = Vector3.zero;

    // Weapon cooldowns
    private float lastCannonFireTime;
    private float lastMissileFireTime;

    // Target Locking
    private Transform targetTransform;
    private float lockOnTime;
    private float currentLockTime;

    // Hardcoded values for lead calculation
    private const float facingBlend = 0.6f;
    private bool canFire = false;

    void Start()
    {
        ship = GetComponent<Ship>();
        equipmentManager = GetComponent<EquipmentManager>();
        damageHandler = GetComponent<DamageHandler>();
        pathfindingBridge = GetComponent<AStar3D.ECS.ShipPathfindingBridge>();
        if (equipmentManager == null)
        {
            Debug.LogError("EquipmentManager component not found on " + gameObject.name);
        }
        if (damageHandler == null)
        {
            Debug.LogError("DamageHandler component not found on " + gameObject.name);
        }
        if (ship == null)
        {
            Debug.LogError("Ship component not found on " + gameObject.name);
        }
        if (pathfindingBridge != null && showDebugInfo)
        {
            Debug.Log("ShipPathfindingBridge found - pathfinding enabled");
        }
        lockOnTime = equipmentManager?.equippedRadar?.lockOnTime ?? 0f;
        if (classData != null)
        {
            canFire = equipmentManager.InitializeEquipmentAI(classData);
        }
        else
        {
            Debug.LogWarning("EnemyClassData not assigned on " + gameObject.name);
        }
        SetDifficultyParameters();
        int numCannons = equipmentManager.GetAllCannonSlots().Length;
        reward = baseReward + numCannons * perCannonReward;
        damageHandler.creditReward = reward;
        UpdateRandomOffset();
        if (showDebugInfo)
        {
            Debug.Log($"ShipAI Start: Difficulty={difficulty}, AimAccuracy={aimAccuracy}, ShootingRange={shootingRange}, Reward={reward} (Cannons={numCannons}) on {gameObject.name}");
        }
    }

    private void SetDifficultyParameters()
    {
        float t = ((int)difficulty - 1f) / 9f;
        aimAccuracy = Mathf.Lerp(0.3f, 1.0f, t);
        alignmentAngle = Mathf.Lerp(10f, 35f, t);
        shootingRange = Mathf.Lerp(300f, 500f, t);
        predictionTime = Mathf.Lerp(0.5f, 1.5f, t);
        steeringSpeed = Mathf.Lerp(3f, 7f, t);
        detectionUpdateRate = Mathf.Lerp(1.0f, 0.2f, t);
        offsetRandomness = Mathf.Lerp(100f, 20f, t);
        cannonFireRate = Mathf.Lerp(1f, 0.1f, t);
        missileFireRate = Mathf.Lerp(7f, 3f, t);
        passDistance = Mathf.Lerp(500f, 700f, t);
        baseReward = classData.rewardCredits;
        perCannonReward = classData.rewardPerCannon;
        if (showDebugInfo)
        {
            Debug.Log($"Set Difficulty Parameters: t={t:F2}, AimAccuracy={aimAccuracy:F2}, DetectionRate={detectionUpdateRate:F2}, BaseReward={baseReward}, PerCannonReward={perCannonReward} on {gameObject.name}");
        }
    }

    void FixedUpdate()
    {
        if (!canFire) return;

        // Periodic detection update
        if (Time.time - lastDetectionTime > detectionUpdateRate)
        {
            DetectPlayer();
            lastDetectionTime = Time.time;
        }

        if (playerTransform == null || damageHandler == null || damageHandler.CurrentHealth <= 0)
        {
            currentState = AIState.Searching;
            return;
        }

        // Estimate player velocity
        Vector3 playerPos = GetPlayerTargetPosition();
        playerEstimatedVelocity = (playerPos - playerLastPos) / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
        playerLastPos = playerPos;

        // Update random offset periodically
        offsetUpdateTimer += Time.fixedDeltaTime;
        if (offsetUpdateTimer >= offsetUpdateInterval)
        {
            UpdateRandomOffset();
            offsetUpdateTimer = 0f;
        }

        // Update AI state machine
        UpdateAIState();

        // Handle target lock-on for radar
        HandleRadarLockOn();

        // Calculate target position based on state (WITH PATHFINDING)
        CalculateTargetPosition();

        // Calculate and apply steering
        Vector3 steeringInput = CalculateSteering(Time.fixedDeltaTime);
        ApplyMovement(steeringInput);

        // Handle weapons
        if (currentState != AIState.Searching)
        {
            HandleWeapons();
        }
    }

    void UpdateRandomOffset()
    {
        randomOffset = new Vector3(
            Random.Range(-offsetRandomness, offsetRandomness),
            Random.Range(-offsetRandomness, offsetRandomness),
            Random.Range(-offsetRandomness, offsetRandomness)
        );

        if (showDebugInfo)
        {
            Debug.Log($"Updated Random Offset: {randomOffset} on {gameObject.name}");
        }
    }

    void DetectPlayer()
    {
        if (playerTransform != null) return;

        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerLastPos = GetPlayerTargetPosition();
            playerEstimatedVelocity = Vector3.zero;
            if (showDebugInfo) Debug.Log($"ShipAI: Player detected by {gameObject.name}");
        }
    }

    Vector3 GetPlayerTargetPosition()
    {
        if (playerTransform == null)
            return Vector3.zero;

        Collider playerCollider = playerTransform.GetComponent<Collider>();
        if (playerCollider != null)
        {
            return playerCollider.bounds.center;
        }

        return playerTransform.position;
    }

    private float prevDotToPlayer = 1f;

    void UpdateAIState()
    {
        Vector3 playerPos = GetPlayerTargetPosition();
        float distanceToPlayer = Vector3.Distance(transform.position, playerPos);
        Vector3 toPlayer = (playerPos - transform.position).normalized;
        float dotToPlayer = Vector3.Dot(transform.forward, toPlayer);

        switch (currentState)
        {
            case AIState.Searching:
                if (playerTransform != null)
                {
                    currentState = AIState.Attacking;

                    // Clear path when starting to attack
                    if (pathfindingBridge != null)
                    {
                        pathfindingBridge.ClearPath();
                    }

                    if (showDebugInfo) Debug.Log($"ShipAI: State -> Attacking on {gameObject.name}");
                }
                break;

            case AIState.Attacking:
                // Only enter Retreating on transition from front to behind
                if (prevDotToPlayer >= 0f && dotToPlayer < 0f)
                {
                    passPosition = transform.position;
                    currentState = AIState.Retreating;

                    // Clear path when starting to retreat
                    if (pathfindingBridge != null)
                    {
                        pathfindingBridge.ClearPath();
                    }

                    if (showDebugInfo) Debug.Log($"ShipAI: State -> Retreating on {gameObject.name}");
                }
                break;

            case AIState.Retreating:
                if (Vector3.Distance(transform.position, passPosition) > passDistance)
                {
                    currentState = AIState.Attacking;

                    // Clear path when returning to attack
                    if (pathfindingBridge != null)
                    {
                        pathfindingBridge.ClearPath();
                    }

                    if (showDebugInfo) Debug.Log($"ShipAI: State -> Attacking on {gameObject.name}");
                }
                break;
        }

        prevDotToPlayer = dotToPlayer;

        if (showDebugInfo && Time.frameCount % 60 == 0)
        {
            Debug.Log($"ShipAI UpdateAIState: CurrentState={currentState}, Distance={distanceToPlayer:F1}, Dot={dotToPlayer:F2} on {gameObject.name}");
        }
    }

    void CalculateTargetPosition()
    {
        Vector3 playerPos = GetPlayerTargetPosition();

        switch (currentState)
        {
            case AIState.Attacking:
                // Calculate direct target (player + prediction + offset)
                Vector3 directTarget = playerPos + PredictPlayerMovement() + randomOffset;

                // Use pathfinding to avoid obstacles
                if (pathfindingBridge != null && pathfindingBridge.enablePathfinding)
                {
                    targetPosition = pathfindingBridge.GetNavigationTarget(directTarget, playerTransform);
                }
                else
                {
                    targetPosition = directTarget;
                }
                break;

            case AIState.Retreating:
                // When retreating, fly straight forward (no pathfinding needed)
                targetPosition = transform.position + (transform.forward * 10000f) + randomOffset;
                break;
        }

        if (showDebugInfo && Time.frameCount % 60 == 0)
        {
            string pathStatus = pathfindingBridge?.IsFollowingPath() == true ? "Following Path" : "Direct";
            Debug.Log($"Calculated Target Position: {targetPosition} ({pathStatus}) for state {currentState} on {gameObject.name}");
        }
    }

    Vector3 PredictPlayerMovement()
    {
        return playerEstimatedVelocity * predictionTime;
    }

    Vector3 CalculateSteering(float dt)
    {
        Vector3 worldError = targetPosition - transform.position;
        float distanceToTarget = worldError.magnitude;
        Vector3 directionToTarget = worldError.normalized;
        Vector3 localDirection = transform.InverseTransformDirection(directionToTarget);

        Vector3 targetInput = Vector3.zero;
        float angleToTarget = Vector3.Angle(Vector3.forward, localDirection);

        float steeringMultiplier = 1f;
        float angleThreshold = 1f;

        if (angleToTarget > angleThreshold)
        {
            Vector3 pitchPlane = new Vector3(0, localDirection.y, localDirection.z).normalized;
            float pitchAngle = Vector3.SignedAngle(Vector3.forward, pitchPlane, Vector3.right);
            targetInput.x = Mathf.Clamp(pitchAngle / maxInputAngle, -1f, 1f) * steeringMultiplier;

            Vector3 yawPlane = new Vector3(localDirection.x, 0, localDirection.z).normalized;
            float yawAngle = Vector3.SignedAngle(Vector3.forward, yawPlane, Vector3.up);
            targetInput.y = Mathf.Clamp(yawAngle / maxInputAngle, -1f, 1f) * steeringMultiplier;

            if (angleToTarget > 15f)
            {
                float rollTarget = -yawAngle / maxInputAngle;
                targetInput.z = Mathf.Clamp(rollTarget, -1f, 1f);
            }
            else
            {
                targetInput.z = 0f;
            }
        }

        Vector3 smoothedInput;
        smoothedInput.x = Mathf.MoveTowards(lastInput.x, targetInput.x, steeringSpeed * dt);
        smoothedInput.y = Mathf.MoveTowards(lastInput.y, targetInput.y, steeringSpeed * dt);
        smoothedInput.z = Mathf.MoveTowards(lastInput.z, targetInput.z, rollSmoothness * dt);

        lastInput = smoothedInput;

        return smoothedInput;
    }

    void ApplyMovement(Vector3 input)
    {
        if (equipmentManager == null || equipmentManager.equippedEngine == null)
        {
            Debug.LogWarning($"ShipAI: No engine equipped on {gameObject.name}");
            return;
        }

        Engine engine = equipmentManager.equippedEngine;

        // Forward movement
        Vector3 desired = transform.forward * engine.moveSpeed;
        currentVelocity = Vector3.Lerp(currentVelocity, desired, Time.fixedDeltaTime * 2f);
        transform.position += currentVelocity * Time.fixedDeltaTime;

        // Rotation
        float pitchSpeed = input.x * engine.pitchRotationSpeed * rotationMultiplier;
        float yawSpeed = input.y * engine.yawRotationSpeed * rotationMultiplier;
        float rollSpeed = input.z * engine.rollRotationSpeed * rotationMultiplier;

        Quaternion deltaRotation = Quaternion.Euler(
            pitchSpeed * Time.fixedDeltaTime,
            yawSpeed * Time.fixedDeltaTime,
            rollSpeed * Time.fixedDeltaTime
        );

        transform.rotation = transform.rotation * deltaRotation;
    }

    void HandleWeapons()
    {
        if (playerTransform == null)
            return;

        Vector3 playerPos = GetPlayerTargetPosition();
        float distanceToPlayer = Vector3.Distance(transform.position, playerPos);
        Vector3 toPlayer = (playerPos - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, toPlayer);
        float dotToPlayer = Vector3.Dot(transform.forward, toPlayer);
        bool playerInFront = dotToPlayer > 0.1f;

        bool inRange = distanceToPlayer <= shootingRange;
        bool aligned = angleToPlayer <= alignmentAngle;

        if (inRange && playerInFront)
        {
            AimCannons(distanceToPlayer);

            if (Time.time - lastCannonFireTime > cannonFireRate)
            {
                equipmentManager.FireAllCannons();
                lastCannonFireTime = Time.time;
            }

            if (aligned && Time.time - lastMissileFireTime > missileFireRate)
            {
                equipmentManager.FireAllLaunchers(playerTransform);
                lastMissileFireTime = Time.time;
            }
        }
        else
        {
            RotateCannonsToDefault();
        }
    }

    private void AimCannons(float distanceToPlayer)
    {
        var cannonSlots = equipmentManager.GetAllCannonSlots();
        if (cannonSlots == null || cannonSlots.Length == 0)
            return;

        Vector3 baseTargetPos = GetPlayerTargetPosition();
        float errorFactor = 1f - aimAccuracy;
        float maxErrorRadius = errorFactor * distanceToPlayer * 0.05f;

        Vector3 vTarget = playerEstimatedVelocity;
        Vector3 biasedVel = Vector3.Lerp(vTarget, playerTransform.forward * vTarget.magnitude, facingBlend);

        foreach (var slot in cannonSlots)
        {
            if (slot.transform == null || slot.FirePoint == null)
                continue;

            Vector3 shooterPos = slot.FirePoint.position;
            Vector3 noisyTarget = baseTargetPos + Random.insideUnitSphere * maxErrorRadius;

            if (TryFirstOrderIntercept(shooterPos, slot.projectileSpeed, noisyTarget, biasedVel, out var lead))
            {
                slot.transform.LookAt(lead);
            }
            else
            {
                slot.transform.LookAt(noisyTarget);
            }
        }
    }

    private void RotateCannonsToDefault()
    {
        var cannonSlots = equipmentManager.GetAllCannonSlots();
        if (cannonSlots == null || cannonSlots.Length == 0)
            return;

        foreach (var slot in cannonSlots)
        {
            if (slot.transform == null)
                continue;

            Vector3 forwardPos = transform.position + transform.forward * 100f;
            slot.transform.LookAt(forwardPos);
        }
    }

    private static bool TryFirstOrderIntercept(
        Vector3 shooterPos, float projectileSpeed,
        Vector3 targetPos, Vector3 targetVel,
        out Vector3 intercept)
    {
        intercept = targetPos;

        float s = projectileSpeed;
        if (s <= 0.01f) return false;

        Vector3 r = targetPos - shooterPos;
        float r2 = r.sqrMagnitude;
        float v2 = targetVel.sqrMagnitude;

        float a = v2 - s * s;
        float b = 2f * Vector3.Dot(r, targetVel);
        float c = r2;

        float t;
        if (Mathf.Abs(a) < 1e-6f)
        {
            if (Mathf.Abs(b) < 1e-6f) t = 0f;
            else t = Mathf.Max(0f, -c / b);
        }
        else
        {
            float disc = b * b - 4f * a * c;
            if (disc < 0f) return false;
            float sqrt = Mathf.Sqrt(disc);

            float t1 = (-b + sqrt) / (2f * a);
            float t2 = (-b - sqrt) / (2f * a);

            t = Mathf.Min(t1, t2);
            if (t < 0f) t = Mathf.Max(t1, t2);
            if (t < 0f) return false;
        }

        intercept = targetPos + targetVel * t;
        return true;
    }

    void HandleRadarLockOn()
    {
        if (equipmentManager.equippedRadar == null || playerTransform == null)
            return;

        Radar radar = equipmentManager.equippedRadar;

        Vector3 playerPos = GetPlayerTargetPosition();
        Vector3 toTarget = playerPos - transform.position;
        float angleToTarget = Vector3.Angle(transform.forward, toTarget);

        if (angleToTarget <= radar.detectionRange)
        {
            if (radar.currentTarget == null)
            {
                radar.currentTarget = playerTransform;
                currentLockTime = 0f;
            }

            if (radar.currentTarget == playerTransform)
            {
                currentLockTime += Time.fixedDeltaTime;
            }
        }
        else
        {
            if (radar.currentTarget != null)
            {
                radar.currentTarget = null;
                currentLockTime = 0f;
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || playerTransform == null) return;

        Vector3 playerPos = GetPlayerTargetPosition();

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, targetPosition);
        Gizmos.DrawWireSphere(targetPosition, 20f);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, playerPos);

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 100f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, shootingRange);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 50f,
            $"State: {currentState}\nDist: {Vector3.Distance(transform.position, playerPos):F1}\nAngle: {Vector3.Angle(transform.forward, (playerPos - transform.position).normalized):F1}°"
        );
#endif
    }
}

public enum AIDifficulty
{
    Easy = 1,
    EasyNormal = 2,
    EasyHard = 3,
    Normal = 4,
    NormalHard = 5,
    Hard = 7,
    Insane = 10
}