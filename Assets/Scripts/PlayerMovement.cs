using UnityEngine;
using System;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] float rotateSpeed = 12f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float deceleration = 30f;

    private Vector3 currentVelocity;
    private CharacterController controller;
    private float verticalVelocity;
    private PlayerStateMachine stateMachine;
    private RoomManager roomManager;
    [SerializeField] private float scanRadius = 3f;
    [SerializeField] private float scanInterval = 0.1f;
    [SerializeField] private LayerMask dropItemLayer;
    private LootPickup nearestPickup;

    private float nextScanTime;
    public event Action<string, LootPickup> OnNearestPickupChanged;

    private LootPickup currentPickup;
    private string playerID;
    private void Start()
    {
        roomManager = FindAnyObjectByType<RoomManager>();
        moveSpeed = GetComponent<CombatInstance>().state.stats.moveSpeed;
        playerID = GetComponent<CombatInstance>().state.instanceID;
        InvokeRepeating(nameof(FindNearestPickup), 0f, scanInterval);
        InputManager.Instance.OnInteract += PickupNearest;
    }
    private void OnDestroy()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnInteract -= PickupNearest;
        }
    }
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        stateMachine = GetComponent<PlayerStateMachine>();
    }

    private void Update()
    {

        bool canMove = stateMachine.CanMove || InputManager.Instance.AimMode;
        Vector2 input = canMove ? InputManager.Instance.MoveInput : Vector2.zero;

        Transform cam = Camera.main.transform;

        Vector3 forward = cam.forward;
        Vector3 right = cam.right;

        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        Vector3 move = forward * input.y + right * input.x;

        float currentMoveSpeed = InputManager.Instance.AimMode ? 1f : moveSpeed;
        Vector3 targetVelocity = move * currentMoveSpeed;

        float changeSpeed = move.sqrMagnitude > 0.01f
            ? acceleration
            : deceleration;

        currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, changeSpeed * Time.deltaTime);

        Vector3 horizontalMove = currentVelocity;

        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }
        Vector3 horizontalDelta = new Vector3(horizontalMove.x, 0f, horizontalMove.z) * Time.deltaTime;
        Vector3 currentPosition = transform.position;

        if (!roomManager.IsWalkable(currentPosition + horizontalDelta))
        {
            bool canMoveX = roomManager.IsWalkable(currentPosition + new Vector3(horizontalDelta.x, 0f, 0f));
            bool canMoveZ = roomManager.IsWalkable(currentPosition + new Vector3(0f, 0f, horizontalDelta.z));

            if (canMoveX && !canMoveZ)
            {
                horizontalMove.z = 0f;
                currentVelocity.z = 0f;
            }
            else if (!canMoveX && canMoveZ)
            {
                horizontalMove.x = 0f;
                currentVelocity.x = 0f;
            }
            else if (canMoveX && canMoveZ)
            {
                if (Mathf.Abs(horizontalDelta.x) > Mathf.Abs(horizontalDelta.z))
                {
                    horizontalMove.z = 0f;
                    currentVelocity.z = 0f;
                }
                else
                {
                    horizontalMove.x = 0f;
                    currentVelocity.x = 0f;
                }
            }
            else
            {
                horizontalMove.x = 0f;
                horizontalMove.z = 0f;

                currentVelocity.x = 0f;
                currentVelocity.z = 0f;
            }
        }

        verticalVelocity += gravity * Time.deltaTime;
        horizontalMove.y = verticalVelocity;
        controller.Move(horizontalMove * Time.deltaTime);
        if (InputManager.Instance.AimMode)
        {
            Vector3 direction = Camera.main.transform.forward;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
            }
        }
        else if (move.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
        }
    }
    public void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction.normalized);
    }

    public void FaceCamera()
    {
        Vector3 direction = Camera.main.transform.forward;
        direction.y = 0f;

        FaceDirection(direction);
    }
    private void FindNearestPickup()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, scanRadius, dropItemLayer);

        nearestPickup = null;
        float nearestDistanceSqr = float.MaxValue;

        foreach (Collider hit in hits)
        {
            LootPickup pickup = hit.GetComponentInParent<LootPickup>();

            if (pickup == null || pickup.Loot == null)
                continue;

            float distanceSqr = (pickup.transform.position - transform.position).sqrMagnitude;

            if (distanceSqr >= nearestDistanceSqr)
                continue;

            nearestDistanceSqr = distanceSqr;
            nearestPickup = pickup;
        }
        if (currentPickup != nearestPickup)
        {
            currentPickup = nearestPickup;
            OnNearestPickupChanged?.Invoke(playerID, currentPickup);
        }
    }

    private void PickupNearest()
    {
        if (nearestPickup == null || nearestPickup.Loot == null || nearestPickup.Loot.item == null)
            return;

        if (UIManager.Instance.IsUIOpen(playerID))
            return;

        LootDrop loot = nearestPickup.Loot;

        if (loot.item.itemType == ItemType.Artifact)
        {
            PickupArtifact(loot);
            return;
        }

        PickupItem(loot);
    }

    private void PickupItem(LootDrop loot)
    {
        bool added = StageManager.Instance.AddToInventory(playerID, loot.item.itemID, loot.quantity);

        if (!added)
            return;

        RemovePickup();
    }

    private void PickupArtifact(LootDrop loot)
    {
        if (loot.artifact == null)
            return;

        bool added = StageManager.Instance.AddArtifact(playerID, loot.artifact);

        if (added)
        {
            RemovePickup();
            return;
        }

        UIManager.Instance.OpenArtifactReplaceUI(playerID, nearestPickup);
    }

    private void RemovePickup()
    {
        if (nearestPickup == null)
            return;

        LootPickup pickup = nearestPickup;

        nearestPickup = null;
        currentPickup = null;

        OnNearestPickupChanged?.Invoke(playerID, null);

        Destroy(pickup.gameObject);
    }
}