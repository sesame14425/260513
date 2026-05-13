using UnityEngine;

public class ToolController : MonoBehaviour
{
    [Header("Tool Settings")]
    public float radius = 0.05f;
    public float moveSpeed = 2.0f;

    [Header("Reference")]
    public DeformableBody deformable;
    public Transform visualMesh;

    [Header("Haptic & Proxy Settings")]
    public Vector3 reactionForce;
    [Range(0.1f, 10f)] public float stiffness = 1.0f;
    [Range(0f, 10f)] public float logicBackdrive = 1.5f;
    public float maxBackdriveSpeed = 1.0f;
    [Header("Material Stiffness (Proxy-Spring)")]
    public float materialStiffnessNormal = 1.0f; // stiffness for normal tissue
    public float materialStiffnessTumor = 3.0f;  // stiffness for tumor tissue (3x)
    [Range(0f, 10f)] public float proxyDamping = 0.6f; // b: device-proxy damping
    [Range(0f, 1f)] public float deformForceGain = 0.2f; // 10~30% deformation add-on

    [Header("Hybrid Control")]
    public float virtualMass = 1.0f;
    public float maxLogicSpeed = 2.0f;

    private Vector3 logicPosition;
    private Vector3 inputVelocity;
    private Vector3 logicVelocity;
    private Vector3 lastLogicPosition;
    public float commandFollow = 18f;

    private Vector3 desiredPosition;
    private Vector3 lastDesiredPosition;
    private Vector3 desiredVelocity;

    public SphereCollider toolCollider;   // 指到自己的 SphereCollider
    public Collider tissueCollider;       // 指到組織的 MeshCollider
    public int projectionIterations = 3;  // 多次投影更穩
    public float projectionSlop = 0.0005f;

    public float desiredSmoothing = 10f; // 越大越快跟隨

    [Header("壓入深度限制")]
    public float maxIndentation = 1f; // 最大下壓深度(世界座標)

    private Vector3 lastPenetrationNormal = Vector3.up;
    private bool hasPenetrationNormal = false;
    private Vector3 deformForce = Vector3.zero;
    private float currentProxyStiffness = 1.0f; // runtime stiffness based on contact material

    void Start()
    {
        logicPosition = transform.position;
        desiredPosition = logicPosition;
        lastDesiredPosition = desiredPosition;
        lastLogicPosition = logicPosition;
        inputVelocity = Vector3.zero;
        logicVelocity = Vector3.zero;
        currentProxyStiffness = Mathf.Max(1e-5f, stiffness);
    }

    void Update()
    {
        HandleInput();

        float dt = Mathf.Max(1e-6f, Time.deltaTime);

        desiredVelocity = (desiredPosition - lastDesiredPosition) / dt;
        lastDesiredPosition = desiredPosition;

        ApplyHybridMotion(dt);

        // 讓邏輯控制點也受到反作用，避免僅視覺彈開
        ApplyLogicBackdrive();

        ResolvePenetrationProjection();

        // 以投影後的邏輯點位移計算速度，確保 proxy 速度與接觸投影一致
        logicVelocity = (logicPosition - lastLogicPosition) / dt;
        lastLogicPosition = logicPosition;

        UpdateProxyReactionForce();
        SendToDeformable();
        UpdateVisualProxy();
    }

    void HandleInput()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        float up = 0f;
        if (Input.GetKey(KeyCode.E)) up += 1f;
        if (Input.GetKey(KeyCode.Q)) up -= 1f;

        Vector3 inputDir = new Vector3(h, up, v);
        inputVelocity = inputDir * moveSpeed;

        Vector3 targetDesired = desiredPosition + inputVelocity * Time.deltaTime;
        float t = 1f - Mathf.Exp(-desiredSmoothing * Time.deltaTime);
        desiredPosition = Vector3.Lerp(desiredPosition, targetDesired, t);
    }

    void ApplyHybridMotion(float dt)
    {
        currentProxyStiffness = GetProxyStiffness();

        Vector3 springForce = currentProxyStiffness * (desiredPosition - logicPosition);
        Vector3 dampingForce = proxyDamping * (desiredVelocity - logicVelocity);
        Vector3 accel = (springForce + dampingForce) / Mathf.Max(1e-5f, virtualMass);

        logicVelocity += accel * dt;
        if (maxLogicSpeed > 0f)
            logicVelocity = Vector3.ClampMagnitude(logicVelocity, maxLogicSpeed);

        logicPosition += logicVelocity * dt;
        transform.position = logicPosition;
    }

    void ApplyLogicBackdrive()
    {
        return; // 先關掉，等調參數
        if (currentProxyStiffness <= 1e-5f) return;
        if (reactionForce.sqrMagnitude <= 1e-8f) return;

        Vector3 backdriveVelocity = reactionForce * (logicBackdrive / currentProxyStiffness);
        if (maxBackdriveSpeed > 0f)
            backdriveVelocity = Vector3.ClampMagnitude(backdriveVelocity, maxBackdriveSpeed);

        logicPosition += backdriveVelocity * Time.deltaTime;
        transform.position = logicPosition;
    }

    void ResolvePenetrationProjection()
    {
        if (toolCollider == null || tissueCollider == null) return;

        bool overlappedThisFrame = false;

        for (int it = 0; it < projectionIterations; it++)
        {
            Vector3 dir;
            float dist;

            bool overlapped = Physics.ComputePenetration(
                toolCollider, toolCollider.transform.position, toolCollider.transform.rotation,
                tissueCollider, tissueCollider.transform.position, tissueCollider.transform.rotation,
                out dir, out dist
            );

            if (!overlapped) break;

            overlappedThisFrame = true;
            lastPenetrationNormal = dir; // dir = outward normal
            hasPenetrationNormal = true;

            Vector3 correction = dir * (dist + projectionSlop);
            logicPosition += correction;
            transform.position = logicPosition;
        }

        if (hasPenetrationNormal)
        {
            Vector3 inward = -lastPenetrationNormal;
            float depth = Vector3.Dot(desiredPosition - logicPosition, inward);
            if (depth > maxIndentation)
            {
                desiredPosition -= inward * (depth - maxIndentation);
            }
        }
    }

    void SendToDeformable()
    {
        if (deformable == null) return;

        deformable.externalPos = logicPosition;
        deformable.externalRadius = radius;
        deformable.externalVelocity = logicVelocity;
    }

    void UpdateProxyReactionForce()
    {
        currentProxyStiffness = GetProxyStiffness();
        Vector3 springForce = currentProxyStiffness * (desiredPosition - logicPosition);
        Vector3 dampingForce = proxyDamping * (desiredVelocity - logicVelocity);
        Vector3 proxyForce = springForce + dampingForce;
        reactionForce = proxyForce + deformForceGain * deformForce;
    }

    float GetProxyStiffness()
    {
        bool isTumor = false;
        if (deformable != null)
        {
            isTumor = deformable.IsTumorAtWorldPoint(logicPosition);
        }

        float targetStiffness = isTumor ? materialStiffnessTumor : materialStiffnessNormal;
        if (targetStiffness <= 1e-5f) targetStiffness = stiffness;
        return Mathf.Max(1e-5f, targetStiffness);
    }

    void UpdateVisualProxy()
    {
        if (visualMesh == null) return;

        float stiffnessToUse = Mathf.Max(1e-5f, currentProxyStiffness);
        Vector3 rawOffset = reactionForce * (1.0f / stiffnessToUse);

        float maxDist = radius * 2.0f;
        Vector3 clampedOffset = Vector3.ClampMagnitude(rawOffset, maxDist);

        visualMesh.position = Vector3.Lerp(visualMesh.position, logicPosition + clampedOffset, 0.2f);
    }

    // Receive deformation-based force from DeformableBody (small add-on only).
    public void SetDeformForce(Vector3 force)
    {
        deformForce = force;
    }

    // Legacy alias: kept for compatibility but only updates deformation add-on.
    [Obsolete("Use SetDeformForce; reactionForce is computed from proxy-spring in ToolController.")]
    public void SetReactionForce(Vector3 force)
    {
        deformForce = force;
    }
}
