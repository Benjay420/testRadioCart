using UnityEngine;
using System.Collections.Generic; // N�dvendig hvis du bruger lister andre steder

// Kr�ver, at du har opsat Input Manager med en akse kaldet "Drift" (f.eks. p� Left Shift)
// Kr�ver, at du har oprettet Tags: "FinishLine", "Checkpoint1", "Checkpoint2", "SpeedBoost" osv.

public class KartController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider wheelFL; // Forreste venstre
    public WheelCollider wheelFR; // Forreste h�jre
    public WheelCollider wheelRL; // Bagerste venstre
    public WheelCollider wheelRR; // Bagerste h�jre

    [Header("Kart Movement")]
    public float motorForce = 30000f;   // Juster denne kraftigt baseret p� Rigidbody Mass!
    public float steerAngleMax = 30f;   // Max styrevinkel i grader
    public float brakeForce = 80000f;   // Bremsekraft

    [Header("Drifting Settings")]
    public bool enableDrifting = true;  // Tillad drift?
    [Range(0.1f, 1f)] public float driftStiffnessMultiplier = 0.5f; // Lavere = mere drift (sidev�rts greb reduktion)
    public float minSpeedToDrift = 10f; // Minimum hastighed for at drifte
    public float minTurnForDrift = 0.3f; // Minimum styreinput (0 til 1) for at drifte

    [Header("Speed Boost Settings")]
    public float boostMultiplier = 2.0f; // Hvor meget hurtigere (2.0 = dobbelt kraft)
    public float boostDuration = 3.0f; // Varighed af boost i sekunder

    [Header("Audio Settings")]
    public AudioSource kartAudioSource; // AudioSource komponent p� karten
    public AudioClip collisionSound;    // Lydfil for kollision
    public float minPitch = 0.7f;       // Minimum motor pitch ved lav fart
    public float maxPitch = 1.5f;       // Maksimum motor pitch ved h�j fart
    public float maxSpeedEstimateForPitch = 50f; // Estimeret topfart til pitch beregning (JUSTER DENNE!)
    public float minCollisionForce = 5f;  // Minimum kraft for at spille kollisionslyd

    [Header("Visual Wheels")]
    public Transform visualWheelFL; // Transform for det visuelle forreste venstre hjul
    public Transform visualWheelFR; // Transform for det visuelle forreste h�jre hjul
    public Transform visualWheelRL; // Transform for det visuelle bagerste venstre hjul
    public Transform visualWheelRR; // Transform for det visuelle bagerste h�jre hjul

    [Header("Game Logic References")]
    public LapManager lapManager;       // Reference til LapManager scriptet (p� GameManager objektet)

    // Private variabler
    private Rigidbody kartRigidbody;
    private float currentMotorTorque = 0f;
    private float currentSteerAngle = 0f;
    private bool isBraking = false;

    private WheelFrictionCurve originalSidewaysFrictionRL;
    private WheelFrictionCurve originalSidewaysFrictionRR;
    private bool isCurrentlyDrifting = false;

    private bool isBoosting = false;
    private float boostTimer = 0f;
    private float originalMotorForce;

    void Start()
    {
        // F� fat i n�dvendige komponenter
        kartRigidbody = GetComponent<Rigidbody>();
        if (kartRigidbody == null)
        {
            Debug.LogError("KartController: Rigidbody component mangler!");
            enabled = false; // Deaktiver script hvis Rigidbody mangler
            return;
        }

        kartAudioSource = GetComponent<AudioSource>();
        // (Du kan tilf�je en lignende fejlbesked hvis AudioSource mangler, men det er valgfrit)

        // Gem oprindelige v�rdier
        originalMotorForce = motorForce;
        if (wheelRL != null && wheelRR != null) // Sikkerhedstjek
        {
             originalSidewaysFrictionRL = wheelRL.sidewaysFriction;
             originalSidewaysFrictionRR = wheelRR.sidewaysFriction;
        }
        else {
            Debug.LogError("Bag-hjulcolliders (wheelRL eller wheelRR) er ikke tildelt i Inspectoren!");
            enableDrifting = false; // Deaktiver drift hvis hjul mangler
        }

        // Valgfrit: Find LapManager automatisk hvis ikke sat i inspector
        // if (lapManager == null)
        // {
        //     lapManager = FindObjectOfType<LapManager>();
        //     if (lapManager == null) Debug.LogWarning("KartController: Kunne ikke finde LapManager i scenen.");
        // }
    }

    void Update() // K�rer hver frame - godt til input, lyd, visuelt
    {
        HandleAudioPitch();
        UpdateVisualWheels();
    }

    void FixedUpdate() // K�rer synkront med fysik - godt til kr�fter og bev�gelse
    {
        // --- Input ---
        float verticalInput = Input.GetAxis("Vertical");       // W/S eller Op/Ned
        float horizontalInput = Input.GetAxis("Horizontal");   // A/D eller Venstre/H�jre
        bool driftInput = enableDrifting && Input.GetButton("Drift"); // "Drift" akse fra Input Manager
        isBraking = Input.GetKey(KeyCode.Space);             // Eller din valgte bremseknap

        // --- Boost H�ndtering ---
        if (isBoosting)
        {
            boostTimer -= Time.fixedDeltaTime; // T�l ned
            if (boostTimer <= 0f)
            {
                isBoosting = false;
                motorForce = originalMotorForce; // S�t kraften tilbage til normal
                Debug.Log("Speed boost ended.");
            }
        }

        // --- Drift H�ndtering ---
        bool wantsToDrift = driftInput
                            && Mathf.Abs(horizontalInput) > minTurnForDrift
                            && kartRigidbody.linearVelocity.magnitude > minSpeedToDrift
                            && !isBraking // Drifter normalt ikke mens man bremser h�rdt
                            && IsGrounded(); // Tjek om vi er p� jorden

        if (wantsToDrift && !isCurrentlyDrifting)
        {
            isCurrentlyDrifting = true;
            StartDrift();
        }
        else if (!wantsToDrift && isCurrentlyDrifting)
        {
            isCurrentlyDrifting = false;
            StopDrift();
        }

        // --- Anvend Kr�fter ---
        // Motor
        currentMotorTorque = verticalInput * motorForce; // Bruger den (muligvis boostede) motorForce

        // Deaktiver motor hvis vi bremser og k�rer fremad, eller hvis vi drifter (valgfrit, juster for f�lelse)
        if (isBraking && verticalInput > 0) {
            currentMotorTorque = 0;
        }
        // if (isCurrentlyDrifting) {
        //     currentMotorTorque *= 0.9f; // Lille reduktion under drift?
        // }

        // S�rg for at hjul er tildelt f�r vi bruger dem
        if (wheelRL != null) wheelRL.motorTorque = currentMotorTorque;
        if (wheelRR != null) wheelRR.motorTorque = currentMotorTorque;

        // Styring
        currentSteerAngle = horizontalInput * steerAngleMax;
        // if (isCurrentlyDrifting) {
        //     // Juster evt styring under drift
        //     // f.eks. currentSteerAngle *= 1.1f;
        // }
        if (wheelFL != null) wheelFL.steerAngle = currentSteerAngle;
        if (wheelFR != null) wheelFR.steerAngle = currentSteerAngle;

        // Bremsning
        float currentBrakeForce = isBraking ? brakeForce : 0f;
        // Hvis vi drifter og trykker bremse, brems m�ske mindre? (Valgfrit)
        // if (isCurrentlyDrifting && isBraking) {
        //     currentBrakeForce *= 0.5f;
        // }

        if (wheelFL != null) wheelFL.brakeTorque = currentBrakeForce;
        if (wheelFR != null) wheelFR.brakeTorque = currentBrakeForce;
        if (wheelRL != null) wheelRL.brakeTorque = currentBrakeForce;
        if (wheelRR != null) wheelRR.brakeTorque = currentBrakeForce;
    }

    // --- Hj�lpefunktioner ---

    void StartDrift()
    {
        if (wheelRL == null || wheelRR == null) return; // Ekstra sikkerhed
        // Debug.Log("Starting Drift");

        // Kopier den originale kurve og modificer stiffness
        WheelFrictionCurve sidewaysFrictionRL = wheelRL.sidewaysFriction;
        WheelFrictionCurve sidewaysFrictionRR = wheelRR.sidewaysFriction;

        sidewaysFrictionRL.stiffness *= driftStiffnessMultiplier;
        sidewaysFrictionRR.stiffness *= driftStiffnessMultiplier;

        // Anvend den modificerede kurve
        wheelRL.sidewaysFriction = sidewaysFrictionRL;
        wheelRR.sidewaysFriction = sidewaysFrictionRR;
    }

    void StopDrift()
    {
         if (wheelRL == null || wheelRR == null) return; // Ekstra sikkerhed
        // Debug.Log("Stopping Drift");

        // Gendan den originale friktion gemt i Start()
        wheelRL.sidewaysFriction = originalSidewaysFrictionRL;
        wheelRR.sidewaysFriction = originalSidewaysFrictionRR;
    }

    bool IsGrounded()
    {
        // Simpel check - returnerer true hvis mindst et baghjul r�rer jorden
        // Forbedring: Tjek evt. alle 4 hjul eller brug Raycast nedad
        bool grounded = (wheelRL != null && wheelRL.isGrounded) || (wheelRR != null && wheelRR.isGrounded);
        return grounded;
    }

    void HandleAudioPitch()
    {
        if (kartAudioSource != null && kartRigidbody != null)
        {
            // Beregn hastighedsprocent (0 til 1, ca.)
            float speedPercent = Mathf.Clamp01(kartRigidbody.linearVelocity.magnitude / maxSpeedEstimateForPitch);

            // S�t pitch baseret p� hastighed, brug Lerp for bl�d overgang
            kartAudioSource.pitch = Mathf.Lerp(minPitch, maxPitch, speedPercent);
        }
    }

    void UpdateVisualWheels()
    {
        // Opdater hvert visuelt hjul til at matche sin WheelCollider
        UpdateSingleWheel(wheelFL, visualWheelFL);
        UpdateSingleWheel(wheelFR, visualWheelFR);
        UpdateSingleWheel(wheelRL, visualWheelRL);
        UpdateSingleWheel(wheelRR, visualWheelRR);
    }

    void UpdateSingleWheel(WheelCollider collider, Transform visualWheel)
    {
        // Stop hvis et af objekterne mangler
        if (collider == null || visualWheel == null) return;

        Vector3 position;
        Quaternion rotation;
        // F� den aktuelle position og rotation fra WheelCollider
        collider.GetWorldPose(out position, out rotation);

        // Anvend position og rotation p� den visuelle model
        visualWheel.position = position;
        visualWheel.rotation = rotation;
    }

    // --- Kollisioner og Triggers ---

    void OnTriggerEnter(Collider other)
    {
        // Speed Boost Pickup
        if (other.CompareTag("SpeedBoost") && !isBoosting)
        {
            Debug.Log("Picked up Speed Boost!");
            isBoosting = true;
            boostTimer = boostDuration;
            motorForce = originalMotorForce * boostMultiplier; // Anvend boost kraft

            // Deaktiver pickup objektet (g�r det usynligt)
            other.gameObject.SetActive(false);
            // Bem�rk: Du skal bruge en anden mekanisme (f.eks. en timer p� pickup'en selv)
            // for at f� den til at blive aktiv igen efter et stykke tid.
            return; // G� ud for at undg� at tjekke andre tags for samme objekt
        }

        // Lap & Checkpoint Logic (Kr�ver LapManager)
        if (lapManager != null)
        {
            if (other.CompareTag("FinishLine"))
            {
                lapManager.KartHitFinishLine();
            }
            else if (other.CompareTag("Checkpoint1"))
            {
                lapManager.KartHitCheckpoint(1);
            }
            else if (other.CompareTag("Checkpoint2"))
            {
                lapManager.KartHitCheckpoint(2);
            }
            // Tilf�j flere 'else if' for Checkpoint3, Checkpoint4 osv. her...
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Kollisionslyd
        // Tjek om lyden og source findes, og om kraften er stor nok
        if (collisionSound != null && kartAudioSource != null && collision.relativeVelocity.magnitude > minCollisionForce)
        {
            // Afspil lyden �n gang
            // Juster volumen baseret p� kraften for mere dynamik (valgfrit)
            float volumeScale = Mathf.Clamp01(collision.relativeVelocity.magnitude / (minCollisionForce * 5f)); // Skaler op til 5x min force
            kartAudioSource.PlayOneShot(collisionSound, volumeScale);
            // Debug.Log("Collision detected with force: " + collision.relativeVelocity.magnitude);
        }
    }
}