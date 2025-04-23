using UnityEngine;

public class PortalTextureSetup : MonoBehaviour
{

    public Camera portalCamera;
    public Material portalMaterial;

    public Transform portal;
    public Transform otherPortal;

    // For stereo rendering
    private Camera leftEyeCamera;
    private Camera rightEyeCamera;
    private RenderTexture leftEyeTexture;
    private RenderTexture rightEyeTexture;

    void Start()
    {
        if (UnityEngine.XR.XRSettings.enabled)
        {
            SetupStereoPortalCameras();
        }
        else
        {
            // Your existing mono camera setup
            if (portalCamera.targetTexture != null)
            {
                portalCamera.targetTexture.Release();
            }
            portalCamera.targetTexture = new RenderTexture(Screen.width, Screen.height, 24);
            portalMaterial.mainTexture = portalCamera.targetTexture;
        }
    }

    void SetupStereoPortalCameras()
    {
        // Create cameras for each eye
        leftEyeCamera = Instantiate(portalCamera, portalCamera.transform.position, portalCamera.transform.rotation);
        rightEyeCamera = Instantiate(portalCamera, portalCamera.transform.position, portalCamera.transform.rotation);

        // Disable the original camera
        portalCamera.enabled = false;

        // Create render textures for each eye
        leftEyeTexture = new RenderTexture(Screen.width, Screen.height, 24);
        rightEyeTexture = new RenderTexture(Screen.width, Screen.height, 24);

        leftEyeCamera.targetTexture = leftEyeTexture;
        rightEyeCamera.targetTexture = rightEyeTexture;

        // Create a custom shader material that uses both textures
        // You'll need a specialized shader that combines both textures
        portalMaterial.SetTexture("_LeftEyeTexture", leftEyeTexture);
        portalMaterial.SetTexture("_RightEyeTexture", rightEyeTexture);
    }

    void LateUpdate()
    {
        if (UnityEngine.XR.XRSettings.enabled)
        {
            // Update camera positions based on the VR eye positions
            UpdateStereoPortalCameras();
        }
    }

    void UpdateStereoPortalCameras()
    {
        // Get eye positions and rotations from the VR system
        Vector3 leftEyePos = Camera.main.transform.TransformPoint(UnityEngine.XR.InputTracking.GetLocalPosition(UnityEngine.XR.XRNode.LeftEye));
        Vector3 rightEyePos = Camera.main.transform.TransformPoint(UnityEngine.XR.InputTracking.GetLocalPosition(UnityEngine.XR.XRNode.RightEye));

        Quaternion eyeRotation = Camera.main.transform.rotation; // Both eyes share the same rotation

        // Apply portal transform for left eye
        UpdatePortalCameraTransform(leftEyeCamera, leftEyePos, eyeRotation);

        // Apply portal transform for right eye
        UpdatePortalCameraTransform(rightEyeCamera, rightEyePos, eyeRotation);

        // Manually render the cameras if they're not set to auto-render
        if (!leftEyeCamera.enabled)
        {
            leftEyeCamera.Render();
        }

        if (!rightEyeCamera.enabled)
        {
            rightEyeCamera.Render();
        }
    }

    // Helper method that applies the portal logic to a specific camera
    void UpdatePortalCameraTransform(Camera portalCam, Vector3 eyePosition, Quaternion eyeRotation)
    {
        // Calculate local position relative to the other portal
        Vector3 eyeOffsetFromPortal = eyePosition - otherPortal.position;

        // Position the portal camera
        portalCam.transform.position = portal.position + eyeOffsetFromPortal;

        // Get the full relative rotation between portals
        Quaternion portalToOtherPortalRotation = Quaternion.Inverse(otherPortal.rotation) * portal.rotation;

        // Apply the combined rotation
        portalCam.transform.rotation = portal.rotation * portalToOtherPortalRotation * Quaternion.Inverse(otherPortal.rotation) * eyeRotation;

        // Optional: Set up oblique projection for proper portal clipping
        // This helps avoid rendering objects that are behind the portal surface
        Plane portalPlane = new Plane(-portal.forward, portal.position);
        Vector4 clipPlane = new Vector4(portalPlane.normal.x, portalPlane.normal.y, portalPlane.normal.z, portalPlane.distance);
        Vector4 clipPlaneCameraSpace = Matrix4x4.Transpose(Matrix4x4.Inverse(portalCam.worldToCameraMatrix)) * clipPlane;

        portalCam.projectionMatrix = portalCam.CalculateObliqueMatrix(clipPlaneCameraSpace);
    }
}