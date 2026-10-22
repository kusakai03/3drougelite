using UnityEngine;
using System;
public class CameraController : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] Transform pivot;
    [SerializeField] Transform holder;
    [SerializeField] Camera cam;

    [SerializeField] LayerMask collisionMask;

    [SerializeField] float sensitivity = 0.15f;

    [SerializeField] float minPitch = -70;
    [SerializeField] float maxPitch = 70;

    [SerializeField] float minDistance = 2;
    [SerializeField] float maxDistance = 8;
    [SerializeField] float zoomSpeed = 3;

    [SerializeField] float sphereRadius = 0.3f;
    [SerializeField] float collisionOffset = 0.2f;
    [SerializeField] float smooth = 12;

    float yaw;
    float pitch;

    float targetDistance = 5;
    float currentDistance = 5;
    [Header("Aim Mode")]
    [SerializeField] private float aimDistance = 4f;
    [SerializeField] private float aimSideOffset = 1f;
    [SerializeField] private float aimTransitionSpeed = 10f;

    private float currentSideOffset;
    private void Start()
    {
        if (PlayerManager.Instance == null)
            return;

        PlayerManager.Instance.OnLocalPlayerSpawned += HandlePlayerSpawned;

        if (PlayerManager.Instance.LocalPlayer != null)
        {
            HandlePlayerSpawned(PlayerManager.Instance.LocalPlayer);
        }
    }

    private void OnDisable()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnLocalPlayerSpawned -= HandlePlayerSpawned;
        }
    }

    private void HandlePlayerSpawned(GameObject obj)
    {
        if (obj == null)
            return;

        player = obj.transform;
        SetupCamera();
    }
    private void SetupCamera()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        yaw = player.eulerAngles.y;
        
    }
    private void LateUpdate()
    {
        if (player == null) return;

        bool alter = InputManager.Instance.AlterHeld;

        Cursor.lockState = alter ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = alter;

        transform.position = player.position;

        if (!alter)
        {
            Vector2 look = InputManager.Instance.LookInput;
            yaw += look.x * sensitivity;
            pitch -= look.y * sensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        transform.localRotation = Quaternion.Euler(0, yaw, 0);
        pivot.localRotation = Quaternion.Euler(pitch, 0, 0);
        
        bool aimMode = InputManager.Instance.AimMode;

        if (!aimMode)
        {
            targetDistance -= InputManager.Instance.ZoomInput * zoomSpeed;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }

        float desired = aimMode ? aimDistance : targetDistance;
        float desiredSideOffset = aimMode ? aimSideOffset : 0f;

        Vector3 origin = pivot.position;
        Vector3 dir = -pivot.forward;

        if (Physics.SphereCast(origin, sphereRadius, dir, out RaycastHit hit, desired, collisionMask))
        {
            desired = hit.distance - collisionOffset;
        }

        currentDistance = Mathf.Lerp(currentDistance, desired, Time.deltaTime * smooth);
        currentSideOffset = Mathf.Lerp(currentSideOffset, desiredSideOffset, Time.deltaTime * aimTransitionSpeed);

        holder.localPosition = new Vector3(currentSideOffset, 0f, -currentDistance);
    }
}