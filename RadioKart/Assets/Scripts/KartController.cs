using UnityEngine;

public class KartController : MonoBehaviour
{
    [Header("Wheel Colliders - Skal Tildeles i Inspectoren")]
    public WheelCollider wheelFL; // Forreste venstre
    public WheelCollider wheelFR; // Forreste højre
    public WheelCollider wheelRL; // Bagerste venstre
    public WheelCollider wheelRR; // Bagerste højre

    [Header("Kart Movement Settings")]
    [Tooltip("Kraften der anvendes til acceleration. Juster denne ift. Rigidbody Mass.")]
    public float motorForce = 40000f;
    [Tooltip("Maksimal styrevinkel for forhjulene i grader.")]
    public float maxSteerAngle = 30f;
    [Tooltip("Kraften der anvendes til bremsning.")]
    public float brakeForce = 80000f;

    [Header("Visual Wheels - Skal Tildeles (Selve Cylinder Objekter)")]
    [Tooltip("Transform for selve det visuelle forreste venstre hjul (Cylinder).")]
    public Transform visualWheelFL;
    [Tooltip("Transform for selve det visuelle forreste højre hjul (Cylinder).")]
    public Transform visualWheelFR;
    [Tooltip("Transform for selve det visuelle bagerste venstre hjul (Cylinder).")]
    public Transform visualWheelRL;
    [Tooltip("Transform for selve det visuelle bagerste højre hjul (Cylinder).")]
    public Transform visualWheelRR;

    // Private variabler
    private float currentMotorInput = 0f;
    private float currentSteerInput = 0f;
    private bool isBraking = false;
    private Rigidbody kartRigidbody;


    void Start()
    {
        kartRigidbody = GetComponent<Rigidbody>();
        if (kartRigidbody == null)
        {
            Debug.LogError("KartController mangler en Rigidbody komponent på MyKart!");
            enabled = false;
        }

        if (wheelFL == null || wheelFR == null || wheelRL == null || wheelRR == null)
        {
            Debug.LogError("En eller flere WheelColliders er ikke tildelt i KartController!");
            enabled = false;
        }
        if (visualWheelFL == null || visualWheelFR == null || visualWheelRL == null || visualWheelRR == null)
        {
            Debug.LogWarning("En eller flere VisualWheels er ikke tildelt i KartController. Visuelle hjul vil ikke opdatere.");
        }
    }

    void Update()
    {
        // --- Håndter Input ---
        currentMotorInput = Input.GetAxis("Vertical");
        currentSteerInput = Input.GetAxis("Horizontal");
        isBraking = Input.GetKey(KeyCode.Space);

        // --- Opdater Visuelle Hjul ---
        UpdateAllVisualWheels();
    }

    void FixedUpdate()
    {
        // --- Anvend Motor Kraft ---
        float actualMotorForce = isBraking && currentMotorInput > 0 ? 0f : currentMotorInput * motorForce;
        wheelRL.motorTorque = actualMotorForce;
        wheelRR.motorTorque = actualMotorForce;

        // --- Anvend Styring ---
        float targetSteerAngle = currentSteerInput * maxSteerAngle;
        wheelFL.steerAngle = targetSteerAngle;
        wheelFR.steerAngle = targetSteerAngle;

        // --- Anvend Bremsning ---
        float currentBrakeForce = isBraking ? brakeForce : 0f;
        wheelFL.brakeTorque = currentBrakeForce;
        wheelFR.brakeTorque = currentBrakeForce;
        wheelRL.brakeTorque = currentBrakeForce;
        wheelRR.brakeTorque = currentBrakeForce;
    }

    // --- Hjælpefunktioner til Visuelle Hjul ---
    void UpdateAllVisualWheels()
    {
        UpdateSingleVisualWheel(wheelFL, visualWheelFL);
        UpdateSingleVisualWheel(wheelFR, visualWheelFR);
        UpdateSingleVisualWheel(wheelRL, visualWheelRL);
        UpdateSingleVisualWheel(wheelRR, visualWheelRR);
    }

    void UpdateSingleVisualWheel(WheelCollider physicsCollider, Transform visualWheelTransform) // Bemærk navneændring for klarhed
    {
        if (physicsCollider == null || visualWheelTransform == null)
        {
            return;
        }

        Vector3 position;
        Quaternion rotation;
        physicsCollider.GetWorldPose(out position, out rotation);

        visualWheelTransform.position = position;
        visualWheelTransform.rotation = rotation;
    }
}