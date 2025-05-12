using UnityEngine;

public class SimpleCameraFollow : MonoBehaviour
{
    public Transform target; // The Kart to follow
    public Vector3 offset = new Vector3(0f, 3f, -6f); // Distance from kart
    public float smoothSpeed = 0.125f; // How quickly the camera catches up

    void LateUpdate() // Best for camera movement after physics
    {
        if (target == null)
        {
            Debug.LogWarning("Camera target not set!");
            return;
        }

        Vector3 desiredPosition = target.position + target.TransformDirection(offset); // Offset relative to kart's rotation
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        transform.LookAt(target.position + target.up * 1.0f); // Look at a point slightly above the kart's base
    }
}