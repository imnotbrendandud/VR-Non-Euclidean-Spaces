using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;
using Unity.XR.Oculus;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
[DefaultExecutionOrder(-30000)]
public class PortalRender : MonoBehaviour
{
    [Tooltip("Where the portal is rendered (entrance to portal).")]
    [SerializeField]
    private MeshRenderer m_portalRenderer;

    [Tooltip("A transform on the local end (entrance) of the portal.")]
    [SerializeField]
    private Transform m_portalLocalObservationPoint;

    [Tooltip("A transform on the remote end (observed by this camera) of the portal corresponding exactly to the local-end point.")]
    [SerializeField]
    private Transform m_portalRemoteObservationPoint;

    [Tooltip("Layer to use for portal cameras to prevent recursive rendering")]
    [SerializeField]
    private LayerMask m_portalCullingMask = -1; // Default to everything

    [Header("Advanced Settings")]
    [Tooltip("Resolution multiplier for portal render textures")]
    [Range(0.1f, 2.0f)]
    [SerializeField]
    private float m_resolutionMultiplier = 1.0f;

    [Tooltip("Near clip plane for portal cameras")]
    [SerializeField]
    private float m_nearClipPlane = 0.01f;

    [Tooltip("Far clip plane for portal cameras")]
    [SerializeField]
    private float m_farClipPlane = 100f;

    [SerializeField]
    [Range(1, 10)]
    private int m_maxRecursionDepth = 5;

    // Private variables
    private Camera m_centerCamera;
    private Camera m_leftCamera;
    private Camera m_rightCamera;
    static private Transform s_leftEyeAnchor;
    static private Transform s_rightEyeAnchor;
    private XROrigin m_xrOrigin;
    static private bool s_eyeAnchorsInitialized = false;
    private bool m_renderTexturesCreated = false;

    private void LateUpdate()
    {
        if (!s_eyeAnchorsInitialized || !m_renderTexturesCreated)
            return;

        if (!m_portalRenderer.isVisible)
            return;

        UpdateEyeAnchors();

        Vector3 leftEye = s_leftEyeAnchor.position;
        Quaternion leftRot = s_leftEyeAnchor.rotation;

        Vector3 rightEye = s_rightEyeAnchor.position;
        Quaternion rightRot = s_rightEyeAnchor.rotation;

        // Start from the eye position
        Vector3 leftPos = leftEye;
        Quaternion leftOrientation = leftRot;

        Vector3 rightPos = rightEye;
        Quaternion rightOrientation = rightRot;

        leftPos = m_portalLocalObservationPoint.TransformPoint(
            m_portalRemoteObservationPoint.InverseTransformPoint(leftPos));
        rightPos = m_portalLocalObservationPoint.TransformPoint(
            m_portalRemoteObservationPoint.InverseTransformPoint(rightPos));

        Quaternion relativeRot = m_portalLocalObservationPoint.rotation *
                                    Quaternion.Inverse(m_portalRemoteObservationPoint.rotation);

        leftOrientation = relativeRot * leftOrientation;
        rightOrientation = relativeRot * rightOrientation;

        m_leftCamera.transform.SetPositionAndRotation(leftPos, leftOrientation);
        m_rightCamera.transform.SetPositionAndRotation(rightPos, rightOrientation);

        SetObliqueProjection(m_leftCamera, m_portalRemoteObservationPoint);
        SetObliqueProjection(m_rightCamera, m_portalRemoteObservationPoint);

        m_leftCamera.projectionMatrix = Camera.main.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
        m_rightCamera.projectionMatrix = Camera.main.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);

        m_leftCamera.nonJitteredProjectionMatrix = Camera.main.GetStereoNonJitteredProjectionMatrix(Camera.StereoscopicEye.Left);
        m_rightCamera.nonJitteredProjectionMatrix = Camera.main.GetStereoNonJitteredProjectionMatrix(Camera.StereoscopicEye.Right);

        UpdateProjectionMatrices();
    }

    private void RenderPortalIteration(int iteration)
    {
        Vector3 leftEye = s_leftEyeAnchor.position;
        Quaternion leftRot = s_leftEyeAnchor.rotation;

        Vector3 rightEye = s_rightEyeAnchor.position;
        Quaternion rightRot = s_rightEyeAnchor.rotation;

        // Start from the eye position
        Vector3 leftPos = leftEye;
        Quaternion leftOrientation = leftRot;

        Vector3 rightPos = rightEye;
        Quaternion rightOrientation = rightRot;

        for (int i = 0; i <= iteration; i++)
        {
            leftPos = m_portalLocalObservationPoint.TransformPoint(
                m_portalRemoteObservationPoint.InverseTransformPoint(leftPos));
            rightPos = m_portalLocalObservationPoint.TransformPoint(
                m_portalRemoteObservationPoint.InverseTransformPoint(rightPos));

            Quaternion relativeRot = m_portalLocalObservationPoint.rotation *
                                     Quaternion.Inverse(m_portalRemoteObservationPoint.rotation);

            leftOrientation = relativeRot * leftOrientation;
            rightOrientation = relativeRot * rightOrientation;
        }

        m_leftCamera.transform.SetPositionAndRotation(leftPos, leftOrientation);
        m_rightCamera.transform.SetPositionAndRotation(rightPos, rightOrientation);

        SetObliqueProjection(m_leftCamera, m_portalRemoteObservationPoint);
        SetObliqueProjection(m_rightCamera, m_portalRemoteObservationPoint);

        m_leftCamera.Render();
        m_rightCamera.Render();
    }

    private void SetObliqueProjection(Camera cam, Transform clipPlaneTransform)
    {
        // Create a plane representing the portal surface
        // The portal plane faces into the portal (normal points away from the visible side)
        Plane p = new Plane(-clipPlaneTransform.forward, clipPlaneTransform.position);

        // Convert the plane to camera space
        Vector4 clipPlaneWorldSpace = new Vector4(p.normal.x, p.normal.y, p.normal.z, p.distance);
        Vector4 clipPlaneCameraSpace =
            Matrix4x4.Transpose(Matrix4x4.Inverse(cam.worldToCameraMatrix)) * clipPlaneWorldSpace;

        // Update the camera's projection matrix to include this oblique clip plane
        Matrix4x4 newProjection = cam.CalculateObliqueMatrix(clipPlaneCameraSpace);
        cam.projectionMatrix = newProjection;
    }

    private void UpdateEyeAnchors()
    {
        // Get latest eye positions from XR system
        InputDevice leftEyeDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftEye);
        InputDevice rightEyeDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightEye);

        if (leftEyeDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 leftPos))
        {
            s_leftEyeAnchor.localPosition = leftPos;
        }

        if (rightEyeDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 rightPos))
        {
            s_rightEyeAnchor.localPosition = rightPos;
        }

        if (leftEyeDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion leftRot))
        {
            s_leftEyeAnchor.localRotation = leftRot;
        }

        if (rightEyeDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rightRot))
        {
            s_rightEyeAnchor.localRotation = rightRot;
        }
    }

    private void UpdateProjectionMatrices()
    {
        if (XRSettings.isDeviceActive)
        {
            // Get projection matrices from the XR system
            m_leftCamera.projectionMatrix = Camera.main.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
            m_rightCamera.projectionMatrix = Camera.main.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);

            // Update non-jittered matrices if needed
            m_leftCamera.nonJitteredProjectionMatrix = Camera.main.GetStereoNonJitteredProjectionMatrix(Camera.StereoscopicEye.Left);
            m_rightCamera.nonJitteredProjectionMatrix = Camera.main.GetStereoNonJitteredProjectionMatrix(Camera.StereoscopicEye.Right);
        }
        else
        {
            // Fallback for non-XR mode (optional)
            Matrix4x4 projMatrix = Matrix4x4.Perspective(
                Camera.main.fieldOfView,
                Camera.main.aspect,
                m_nearClipPlane,
                m_farClipPlane);

            m_leftCamera.projectionMatrix = projMatrix;
            m_rightCamera.projectionMatrix = projMatrix;
            m_leftCamera.nonJitteredProjectionMatrix = projMatrix;
            m_rightCamera.nonJitteredProjectionMatrix = projMatrix;
        }
    }

    private void CreateRenderTarget(Camera camera)
    {
        // Calculate resolution based on the main camera and multiplier
        int width = Mathf.RoundToInt(Camera.main.pixelWidth * m_resolutionMultiplier);
        int height = Mathf.RoundToInt(Camera.main.pixelHeight * m_resolutionMultiplier);

        // Release existing texture if any
        if (camera.targetTexture != null)
        {
            camera.targetTexture.Release();
        }

        // Create new render texture with appropriate format for XR
        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.DefaultHDR);
        //rt.antiAliasing = XRSettings.eyeTextureAntiAliasing;
        rt.vrUsage = VRTextureUsage.OneEye;
        rt.Create();


        camera.targetTexture = rt;
    }

    // Creates a portal-observing camera representing a stereoscopic eye
    private Camera CreateEyeCamera(string name)
    {
        GameObject cameraContainer = new GameObject(name);
        cameraContainer.tag = m_centerCamera.gameObject.tag;
        cameraContainer.transform.parent = transform;
        cameraContainer.transform.localPosition = Vector3.zero;
        cameraContainer.transform.localRotation = Quaternion.identity;

        Camera camera = cameraContainer.AddComponent<Camera>();
        camera.CopyFrom(m_centerCamera);

        // Configure for portal rendering
        camera.enabled = true;
        //camera.stereoTargetEye = StereoTargetEyeMask.None; // We handle stereo rendering manually
        camera.cullingMask = m_portalCullingMask;
        camera.nearClipPlane = m_nearClipPlane;
        camera.farClipPlane = m_farClipPlane;
        camera.depth = m_centerCamera.depth - 1; // Render before main camera

        // Disable post-processing if using URP/HDRP
        // This depends on your render pipeline
        SetupRenderPipelineSettings(camera);

        return camera;
    }

    private void SetupRenderPipelineSettings(Camera camera)
    {
         var additionalCameraData = camera.GetUniversalAdditionalCameraData();
         if (additionalCameraData != null)
            {
                additionalCameraData.renderPostProcessing = false;
                additionalCameraData.antialiasing = AntialiasingMode.None;
            }
    }

    private void InitializeEyeAnchors()
    {
        // Find the XR Rig
        if (!s_eyeAnchorsInitialized)
        {
            m_xrOrigin = FindObjectOfType<XROrigin>();
            if (m_xrOrigin != null)
            {
                // Get the XR camera
                var xrCamera = m_xrOrigin.Camera;
                if (xrCamera == null)
                {
                    Debug.LogError("XR Origin found but it has no Camera component!");
                    return;
                }

                // Create left eye anchor
                GameObject leftEyeObj = new GameObject("LeftEyeAnchor");
                s_leftEyeAnchor = leftEyeObj.transform;
                s_leftEyeAnchor.parent = xrCamera.transform.parent;
                s_leftEyeAnchor.localPosition = Vector3.zero;
                s_leftEyeAnchor.localRotation = Quaternion.identity;

                // Create right eye anchor
                GameObject rightEyeObj = new GameObject("RightEyeAnchor");
                s_rightEyeAnchor = rightEyeObj.transform;
                s_rightEyeAnchor.parent = xrCamera.transform.parent;
                s_rightEyeAnchor.localPosition = Vector3.zero;
                s_rightEyeAnchor.localRotation = Quaternion.identity;

                s_eyeAnchorsInitialized = true;
            }
            else
            {
                Debug.LogError("No XR Origin found in the scene! Portal rendering requires XR Origin.");
            }
        }
    }

    private void InitializePortalCameras()
    {
        // Create cameras for portal rendering
        m_leftCamera = CreateEyeCamera("LeftEyePortalCamera");
        m_rightCamera = CreateEyeCamera("RightEyePortalCamera");

        // Create render targets
        CreateRenderTarget(m_leftCamera);
        CreateRenderTarget(m_rightCamera);

        // Set textures on portal material
        if (m_portalRenderer != null && m_portalRenderer.material != null)
        {
            m_portalRenderer.material.SetTexture("_LeftEyeTexture", m_leftCamera.targetTexture);
            m_portalRenderer.material.SetTexture("_RightEyeTexture", m_rightCamera.targetTexture);

            // Set shader keywords for stereo rendering if needed
            m_portalRenderer.material.EnableKeyword("_STEREO_RENDERING_ON");
        }
        else
        {
            Debug.LogError("Portal renderer or material not properly set!");
        }

        m_renderTexturesCreated = true;
    }

    private void ValidateReferences()
    {
        if (m_portalRenderer == null)
        {
            Debug.LogError("Portal Renderer is not assigned!");
        }

        if (m_portalLocalObservationPoint == null)
        {
            Debug.LogError("Portal Local Observation Point is not assigned!");
        }

        if (m_portalRemoteObservationPoint == null)
        {
            Debug.LogError("Portal Remote Observation Point is not assigned!");
        }
    }

    private void Start()
    {
        InitializePortalCameras();
    }

    private void Awake()
    {
        ValidateReferences();
        m_centerCamera = GetComponent<Camera>();
        InitializeEyeAnchors();
    }

    private void OnDestroy()
    {
        // Clean up render textures
        if (m_leftCamera != null && m_leftCamera.targetTexture != null)
        {
            m_leftCamera.targetTexture.Release();
        }

        if (m_rightCamera != null && m_rightCamera.targetTexture != null)
        {
            m_rightCamera.targetTexture.Release();
        }
    }
}