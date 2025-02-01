using UnityEngine;
using UnityEngine.UI; // Needed for UI components

public class CameraZoom : MonoBehaviour
{
    // Reference to the main camera (set this in the Inspector or let it default to Camera.main)
    public Camera mainCamera;
    
    // Reference to the UI slider (drag the slider GameObject into this field in the Inspector)
    public Slider zoomSlider;

    void Start()
    {
        // If no camera has been assigned, use the main camera.
        if (mainCamera == null)
            mainCamera = Camera.main;

        // Ensure the camera is in orthographic mode.
        mainCamera.orthographic = true;

        // Configure the slider:
        // Set the minimum and maximum values. 
        // Smaller orthographic size = more zoomed in.
        zoomSlider.minValue = 4f;
        zoomSlider.maxValue = 9f;
        // Set default value to 9 (camera starts zoomed out).
        zoomSlider.value = 9f;

        // Listen for when the slider's value changes.
        zoomSlider.onValueChanged.AddListener(UpdateZoom);
    }

    // This method is called every time the slider's value changes.
    void UpdateZoom(float newZoom)
    {
        // Set the camera's orthographic size based on the slider value.
        mainCamera.orthographicSize = newZoom;
    }
}
