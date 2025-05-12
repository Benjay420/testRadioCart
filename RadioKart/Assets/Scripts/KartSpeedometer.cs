using UnityEngine;
using TMPro; // Required for TextMeshPro UI elements

public class KartSpeedometer : MonoBehaviour
{
    public Rigidbody kartRigidbody; // Assign your Kart's Rigidbody
    
    [Tooltip("UI Text element to display the speed.")]
    public TextMeshProUGUI speedDisplayText; // Assign your TextMeshProUGUI element

    [Tooltip("Current speed of the kart in Unity units per second (m/s).")]
    public float currentSpeedUnitsPerSecond; 

    [Tooltip("Current speed of the kart in kilometers per hour (km/h).")]
    public float currentSpeedKmh;

    // Conversion factor from meters/sec (Unity units/sec) to km/h
    private const float MPS_TO_KMH = 3.6f; 

    void Start()
    {
        if (kartRigidbody == null)
        {
            kartRigidbody = GetComponent<Rigidbody>();
        }

        if (kartRigidbody == null)
        {
            Debug.LogError("KartSpeedometer: Rigidbody component not found or not assigned! Please assign the Kart's Rigidbody.");
            enabled = false; 
            return;
        }

        if (speedDisplayText == null)
        {
            Debug.LogError("KartSpeedometer: Speed Display Text (TextMeshProUGUI) not assigned in the Inspector!");
            // You might choose to disable the script or just not update the UI if the text element isn't assigned.
            // For now, we'll let it continue, but it won't update the UI.
        }
    }

    void Update()
    {
        if (kartRigidbody != null)
        {
            currentSpeedUnitsPerSecond = kartRigidbody.linearVelocity.magnitude;
            currentSpeedKmh = currentSpeedUnitsPerSecond * MPS_TO_KMH;

            // Update the UI Text element if it's assigned
            if (speedDisplayText != null)
            {
                speedDisplayText.text = "Speed: " + currentSpeedKmh.ToString("F1") + " km/h";
            }
            else
            {
                // Fallback to Debug.Log if UI text isn't assigned, so you still see the speed
                Debug.Log("Kart Speed: " + currentSpeedKmh.ToString("F1") + " km/h (UI Text not assigned)");
            }
        }
    }
}
