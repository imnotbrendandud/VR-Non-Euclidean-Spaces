using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using Unity.XR.CoreUtils;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class PortalTeleporter : MonoBehaviour
{
    public static event Action WentToScaledWorld;
    public static event Action LeftScaledWorld;
    public static event Action MakeBlockadeDisappear;

    [Tooltip("The corresponding portal on the other side.")]
    [SerializeField]
    private Transform remotePortal;

    [Tooltip("The point used for matching the XR Rig's camera position.")]
    [SerializeField]
    private Transform localObservationPoint;

    [Tooltip("The matching point on the remote portal (exact counterpart of the local observation point).")]
    [SerializeField]
    private Transform remoteObservationPoint;

    [SerializeField]
    private Transform remoteEnvironmentTransform;
    [SerializeField]
    private Transform remoteEnvironmentScaledUpTransform;
    [SerializeField]
    private Transform remoteEnvironmentScaledDownTransform;
    [SerializeField]
    private Vector3 portalForwardDirection;

    [SerializeField]
    private Transform vrPosition;

    [SerializeField]
    private bool isScalingPortal = false;
    [SerializeField]
    private bool isScalingUp = false;

    [SerializeField]
    private float scalingFactor = 2f;

    [Tooltip("The player object to teleport (usually the XR Origin root).")]
    public Transform playerTransform;

    private Transform cameraTransform;

    private bool hasTeleported = false;

    private HashSet<GameObject> recentlyTeleported = new HashSet<GameObject>();

    private void Start()
    {
        XROrigin xrOrigin = FindObjectOfType<XROrigin>();
        if (xrOrigin == null)
        {
            Debug.LogError("No XR Origin found in scene!");
            enabled = false;
            return;
        }

        cameraTransform = xrOrigin.Camera.transform;

        if (playerTransform == null)
        {
            playerTransform = xrOrigin.transform;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object is moving through the front of the portal
        Vector3 portalToObject = Camera.main.transform.position - transform.position;
        Vector3 localPosition = transform.InverseTransformPoint(other.transform.position);
        if (localPosition.z > 0f)
            return; // Player is entering from the back

        // Prevent double-triggering
       // if (recentlyTeleported.Contains(other.gameObject))
        //    return;

        Quaternion rotate180 = Quaternion.AngleAxis(180, Vector3.up);
        Quaternion rotationDifference = remoteObservationPoint.rotation * Quaternion.Inverse(transform.rotation);

        // Position
        Vector3 localPos = rotate180 * transform.InverseTransformPoint(other.transform.position);
        Vector3 newWorldPos = remoteObservationPoint.TransformPoint(localPos);

        // Rotation
        Quaternion newWorldRot = rotate180 * rotationDifference * other.transform.rotation;

        if (isScalingUp)
        {
            newWorldPos += -remoteObservationPoint.forward * 8f;
            WentToScaledWorld?.Invoke();
        }
        else
        {
            Debug.Log("hello");
            newWorldPos += -remoteObservationPoint.forward * 1f;
            LeftScaledWorld?.Invoke();
            MakeBlockadeDisappear?.Invoke();
        }

        // Apply
        other.transform.SetPositionAndRotation(newWorldPos, newWorldRot);

        // Prevent immediate re-teleportation
        //StartCoroutine(PreventImmediateRetrigger(other.gameObject));
    }

    private IEnumerator PreventImmediateRetrigger(GameObject obj)
    {
        recentlyTeleported.Add(obj);
        yield return new WaitForSeconds(0.2f); // Adjust based on how close portals are
        recentlyTeleported.Remove(obj);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            hasTeleported = false;
        }
    }

    private void Update()
    {
        if (hasTeleported || cameraTransform == null || playerTransform == null)
            return;

        Vector3 localCamPos = localObservationPoint.InverseTransformPoint(cameraTransform.position);

        // Check if camera crossed portal plane
        if (localCamPos.z < 0f)
        {
            TeleportPlayer();
            hasTeleported = true;
        }
    }

    private void TeleportPlayer()
    {
        //// Get relative transform of player to the local portal
        //Vector3 relativePos = localObservationPoint.InverseTransformPoint(playerTransform.position);
        //Quaternion relativeRot = Quaternion.Inverse(localObservationPoint.rotation) * playerTransform.rotation;

        //// Transform that relative data to the remote portal space
        //Vector3 newWorldPos = remoteObservationPoint.TransformPoint(relativePos);
        //Quaternion newWorldRot = remoteObservationPoint.rotation * relativeRot;

        //// Push a little forward to avoid double-triggering
        //Vector3 forwardOffset = newWorldRot * Vector3.forward * 0.2f;
        //Vector3 finalPos = newWorldPos + forwardOffset;

        //// Apply to player transform
        //playerTransform.SetPositionAndRotation(finalPos, newWorldRot);
    }
}
