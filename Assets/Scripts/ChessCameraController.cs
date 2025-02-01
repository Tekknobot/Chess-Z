using UnityEngine;

public class ChessCameraController : MonoBehaviour
{
    // Set this value if you want a different orthographic size for WebGL builds.
    [SerializeField] private float webBrowserOrthographicSize = 5f;

    private Camera cam;

    void Start()
    {
        // Get the Camera component attached to this GameObject.
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("ChessCameraController: No Camera component found on this GameObject.");
            return;
        }

        // Check if we are running in a browser environment (WebGL)
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            if (cam.orthographic)
            {
                cam.orthographicSize = webBrowserOrthographicSize;
                Debug.Log("ChessCameraController: Set orthographic size to " + webBrowserOrthographicSize + " for WebGL build.");
            }
            else
            {
                Debug.LogWarning("ChessCameraController: Camera is not in orthographic mode.");
            }
        }
    }
}
