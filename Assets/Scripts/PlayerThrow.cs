using System;
using UnityEngine;

public class PlayerThrow : MonoBehaviour
{
    public event Action<string> OnCurrentThrowChanged;
    [Header("Reference")]
    private const string DefaultThrowObjectID = "throw_rock";
    [SerializeField] private Transform throwPoint;
    [SerializeField] private LayerMask enemyLayer;

    private CombatInstance character;
    private CharacterData characterData;
    private PlayerMovement playerMovement;
    private PlayerStateMachine stateMachine;
    private float nextThrowTime;
    [SerializeField] private float maxArcMissDistance = 3f;
    [SerializeField] private float directAttackRange;
    [SerializeField] private float arcAttackRange;
    [SerializeField] private float maxArcAttackRange = 100f;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        stateMachine = GetComponent<PlayerStateMachine>();
    }
    private void Update()
    {
        if (InputManager.Instance.AttackHeld)
        {
            Throw();
        }
    }
    private void Start()
    {
        
        character = GetComponent<CombatInstance>();
        PlayerInstance pi = GetComponent<PlayerInstance>();
        characterData = StageManager.Instance.GetCharacter(pi.PlayerID);

        InputManager.Instance.OnScroll += OnScrollThrow;
        InputManager.Instance.OnDrop += DropThrow;

        ValidateCurrentThrowObject();
        UpdateThrowRanges();
    }
    private void OnDestroy()
    {
        if (InputManager.Instance != null){
            InputManager.Instance.OnScroll -= OnScrollThrow;
            InputManager.Instance.OnDrop -= DropThrow;
        }
    }
    private void ValidateCurrentThrowObject()
    {
        if (string.IsNullOrEmpty(characterData.currentThrowObjectID))
            characterData.currentThrowObjectID = DefaultThrowObjectID;

        ThrowObj current = ItemManager.Instance.GetItem<ThrowObj>(characterData.currentThrowObjectID);

        if (current == null)
            characterData.currentThrowObjectID = DefaultThrowObjectID;
    }
    private void OnScrollThrow(float scroll)
    {
        if (characterData == null || characterData.inventory == null)
            return;

        int direction = scroll > 0f ? 1 : -1;

        SelectNextThrowObject(direction);
    }
    private void SelectNextThrowObject(int direction)
    {
        System.Collections.Generic.List<InventoryItem> inventory = characterData.inventory;

        if (inventory.Count == 0)
        {
            SetCurrentThrowObject(DefaultThrowObjectID);
            return;
        }

        int currentIndex = -1;

        for (int i = 0; i < inventory.Count; i++)
        {
            if (inventory[i].itemID == characterData.currentThrowObjectID)
            {
                currentIndex = i;
                break;
            }
        }

        for (int step = 1; step <= inventory.Count; step++)
        {
            int index;

            if (currentIndex == -1)
            {
                index = direction > 0 ? step - 1 : inventory.Count - step;
            }
            else
            {
                index = (currentIndex + direction * step + inventory.Count) % inventory.Count;
            }

            InventoryItem inventoryItem = inventory[index];
            ThrowObj throwObj = ItemManager.Instance.GetItem<ThrowObj>(inventoryItem.itemID);

            if (throwObj == null)
                continue;

            SetCurrentThrowObject(inventoryItem.itemID);
            return;
        }

        SetCurrentThrowObject(DefaultThrowObjectID);
    }
    private void SetCurrentThrowObject(string itemID)
    {
        characterData.currentThrowObjectID = itemID;
        UpdateThrowRanges();
        OnCurrentThrowChanged?.Invoke(character.state.instanceID);
    }
    private void UpdateThrowRanges()
    {
        ThrowObj throwObj = GetCurrentThrowObject();

        if (throwObj == null)
            return;

        directAttackRange = CalculateDirectAttackRange(throwObj);
        arcAttackRange = Mathf.Min(CalculateArcAttackRange(throwObj), maxArcAttackRange);
    }
    private ThrowObj GetCurrentThrowObject()
    {
        ThrowObj throwObj = ItemManager.Instance.GetItem<ThrowObj>(characterData.currentThrowObjectID);

        if (throwObj != null)
            return throwObj;

        characterData.currentThrowObjectID = DefaultThrowObjectID;

        return ItemManager.Instance.GetItem<ThrowObj>(DefaultThrowObjectID);
    }
    private void Throw()
    {
        if (UIManager.Instance != null && UIManager.Instance.HasOpenUI)
            return;
        if (!stateMachine.CanAttack)
            return;

        if (Time.time < nextThrowTime)
            return;

        ThrowObj throwObj = GetCurrentThrowObject();

        if (throwObj == null)
            return;

        if (!CanThrow(throwObj))
            return;

        Transform target;

        if (InputManager.Instance.AimMode)
            target = FindAimTarget();
        else
            target = FindTarget(arcAttackRange);

        if (target == null)
        {
            playerMovement.FaceCamera();
            DirectThrow(throwObj, null);
            return;
        }

        Vector3 directionToTarget = target.position - transform.position;
        directionToTarget.y = 0f;

        playerMovement.FaceDirection(directionToTarget);

        float distance = directionToTarget.magnitude;

        if (distance <= directAttackRange)
            DirectThrow(throwObj, target);
        else
            ArcThrow(throwObj, target);
    }

    private bool CanThrow(ThrowObj throwObj)
    {
        if (throwObj == null)
            return false;

        if (throwObj.itemID == DefaultThrowObjectID)
            return true;

        InventoryItem inventoryItem = characterData.inventory.Find(x => x.itemID == throwObj.itemID);

        if (inventoryItem == null)
            return false;

        return inventoryItem.quantity >= throwObj.ammoCost;
    }

    private void ConsumeAmmo(ThrowObj throwObj)
    {
        if (throwObj == null)
            return;

        if (throwObj.itemID == DefaultThrowObjectID)
            return;

        if (throwObj.ammoCost <= 0)
            return;

        InventoryItem inventoryItem = characterData.inventory.Find(x => x.itemID == throwObj.itemID);

        if (inventoryItem == null)
            return;

        inventoryItem.quantity -= throwObj.ammoCost;

        if (inventoryItem.quantity <= 0)
            characterData.inventory.Remove(inventoryItem);

        if (!CanThrow(throwObj))
            SelectNextThrowObject(1);
    }

    private float GetThrowSpeed(ThrowObj throwObj)
    {
        float speed = characterData.ThrowSpeed;

        if (characterData.ThrowPower < throwObj.weight)
            speed *= Mathf.Min(characterData.ThrowPower / throwObj.weight, 1f);

        return speed;
    }
    private void DirectThrow(ThrowObj throwObj, Transform target)
    {
        if (Time.time < nextThrowTime)
            return;

        Vector3 direction;

        if (target != null)
        {
            Collider targetCollider = target.GetComponent<Collider>();

            if (targetCollider != null)
                direction = (targetCollider.bounds.center - throwPoint.position).normalized;
            else
                direction = (target.position - throwPoint.position).normalized;
        }
        else if (InputManager.Instance.AimMode && Camera.main != null)
        {
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 aimPoint = ray.origin + ray.direction * 100f;
            direction = (aimPoint - throwPoint.position).normalized;
        }
        else
        {
            direction = transform.forward;
        }

        float maxSpread = 30f / characterData.baseAccuracy;
        float yaw = UnityEngine.Random.Range(-maxSpread, maxSpread);
        float pitch = UnityEngine.Random.Range(-maxSpread, maxSpread);

        direction = Quaternion.Euler(pitch, yaw, 0f) * direction;
        direction.Normalize();

        GameObject obj = Instantiate(throwObj.projectilePrefab, throwPoint.position, Quaternion.identity);

        ThrowProjectile projectile = obj.GetComponent<ThrowProjectile>();

        if (projectile == null)
        {
            Destroy(obj);
            return;
        }

        projectile.Initialize(throwObj, character);
        projectile.Launch(CalculateThrowVelocity(direction, throwObj));

        ConsumeAmmo(throwObj);

        nextThrowTime = Time.time + (1f + throwObj.throwDelay) / characterData.AttackSpeed;
        stateMachine.EnterAttackUntil(nextThrowTime + 0.1f);
    }

    private float CalculateDirectAttackRange(ThrowObj throwObj)
    {
        float speed = GetThrowSpeed(throwObj);
        float gravity = Mathf.Abs(Physics.gravity.y);

        float groundY = transform.position.y - 1f;
        float height = Mathf.Max(0f, throwPoint.position.y - groundY);

        float fallTime = Mathf.Sqrt(2f * height / gravity);

        return speed * fallTime;
    }
    private Vector3 CalculateThrowVelocity(Vector3 direction, ThrowObj throwObj)
    {
        return direction * GetThrowSpeed(throwObj);
    }
    private Vector3 ApplyArcAccuracy(Vector3 targetPosition)
    {
        float accuracy = Mathf.Max(characterData.baseAccuracy, 0.01f);
        float missRadius = maxArcMissDistance / accuracy;

        Vector2 offset = UnityEngine.Random.insideUnitCircle * missRadius;

        targetPosition.x += offset.x;
        targetPosition.z += offset.y;

        return targetPosition;
    }
    private void ArcThrow(ThrowObj throwObj, Transform target)
    {
        if (Time.time < nextThrowTime)
            return;

        if (target == null)
            return;

        Collider targetCollider = target.GetComponent<Collider>();

        if (targetCollider == null)
            return;

        Vector3 targetPosition = targetCollider.bounds.center;
        targetPosition = ApplyArcAccuracy(targetPosition);

        GameObject obj = Instantiate(throwObj.projectilePrefab, throwPoint.position, Quaternion.identity);

        ThrowProjectile projectile = obj.GetComponent<ThrowProjectile>();

        if (projectile == null)
        {
            Destroy(obj);
            return;
        }

        projectile.Initialize(throwObj, GetComponent<CombatInstance>());
        projectile.Launch(CalculateArcVelocity(throwPoint.position, targetPosition, throwObj));

        ConsumeAmmo(throwObj);

        nextThrowTime = Time.time + (1f + throwObj.throwDelay) / characterData.AttackSpeed;
        stateMachine.EnterAttackUntil(nextThrowTime + 0.1f);
    }
    private float CalculateArcAttackRange(ThrowObj throwObj)
    {
        float speed = GetThrowSpeed(throwObj);
        float gravity = Mathf.Abs(Physics.gravity.y);

        float groundY = transform.position.y - 1f;
        float height = Mathf.Max(0f, throwPoint.position.y - groundY);

        return speed / gravity * Mathf.Sqrt(speed * speed + 2f * gravity * height);
    }
    private Vector3 CalculateArcVelocity(Vector3 start, Vector3 target, ThrowObj throwObj)
    {
        float speed = GetThrowSpeed(throwObj);
        float gravity = Mathf.Abs(Physics.gravity.y);

        Vector3 displacement = target - start;

        Vector3 horizontal = displacement;
        horizontal.y = 0f;

        float distance = horizontal.magnitude;
        float height = displacement.y;

        if (distance < 0.01f)
            return Vector3.up * speed;

        float speedSqr = speed * speed;

        float discriminant = speedSqr * speedSqr - gravity * (gravity * distance * distance + 2f * height * speedSqr);

        if (discriminant < 0f)
            return Vector3.zero;

        float angle = Mathf.Atan((speedSqr - Mathf.Sqrt(discriminant)) / (gravity * distance));

        Vector3 horizontalDirection = horizontal.normalized;

        float horizontalSpeed = speed * Mathf.Cos(angle);
        float verticalSpeed = speed * Mathf.Sin(angle);

        return horizontalDirection * horizontalSpeed + Vector3.up * verticalSpeed;
    }
    private void DropThrow()
    {
        ThrowObj throwObj = GetCurrentThrowObject();

        if (throwObj == null)
            return;

        Vector3 spawnPosition = throwPoint.position + throwPoint.forward * 0.5f;

        GameObject obj = Instantiate(throwObj.projectilePrefab, spawnPosition, Quaternion.identity);

        ThrowProjectile projectile = obj.GetComponent<ThrowProjectile>();

        if (projectile == null)
        {
            Destroy(obj);
            return;
        }

        projectile.Initialize(throwObj, GetComponent<CombatInstance>());
        projectile.Launch(Vector3.zero);

        ConsumeAmmo(throwObj);

        nextThrowTime = Time.time + (1f + throwObj.throwDelay) / characterData.AttackSpeed;
        stateMachine.EnterAttackUntil(nextThrowTime + 0.1f);
    }

    private Transform FindTarget(float range)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, range, enemyLayer);

        if (hits.Length == 0)
            return null;

        Camera cam = Camera.main;

        Transform bestTarget = null;
        float bestScore = float.MaxValue;

        foreach (Collider hit in hits)
        {
            CombatInstance targetCombat = hit.GetComponentInParent<CombatInstance>();

            if (targetCombat == null || targetCombat == character)
                continue;

            if (targetCombat.state.stats.currentHp <= 0f)
                continue;

            Vector3 viewport = cam.WorldToViewportPoint(hit.bounds.center);

            if (viewport.z <= 0)
                continue;

            float score = Vector2.SqrMagnitude(new Vector2(viewport.x - 0.5f, viewport.y - 0.5f));

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = hit.transform;
            }
        }

        return bestTarget;
    }
    private Transform FindAimTarget()
    {
        Camera cam = Camera.main;

        if (cam == null)
            return null;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, enemyLayer))
            return null;

        CombatInstance target = hit.collider.GetComponentInParent<CombatInstance>();

        if (target == null || target == character)
            return null;

        if (target.state.stats.currentHp <= 0f)
            return null;

        Vector3 offset = target.transform.position - transform.position;
        offset.y = 0f;

        if (offset.sqrMagnitude > arcAttackRange * arcAttackRange)
            return null;

        return target.transform;
    }
}