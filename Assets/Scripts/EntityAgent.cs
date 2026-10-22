using UnityEngine;

public class EntityAgent : MonoBehaviour
{
    [SerializeField] private UnityEngine.AI.NavMeshAgent navMeshAgent;
    private CombatManager combatManager;
    private CombatInstance combatInstance;
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float spareRange = 100f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private Vector3 hitBoxHalfExtents = new Vector3(1f, 1f, 1f);
    private float nextAttackTime;
    private CombatInstance currentTarget;
    public CombatInstance CurrentTarget => currentTarget;
    private EntityStateMachine stateMachine;
    Rigidbody rb;
    Collider mainCollider;
    
    private bool isDead;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        mainCollider = GetComponent<Collider>();

        rb.isKinematic = true;
        rb.useGravity = false;
    }
    private void Start()
    {
        combatManager = CombatManager.Instance;
        combatInstance = GetComponent<CombatInstance>();
        stateMachine = GetComponent<EntityStateMachine>();
        InvokeRepeating(nameof(Think), 0f, 0.1f);
    }

    public void SetDetectionRange(float range)
    {
        detectionRange = range;
    }

    private void Think()
    {
        if (stateMachine.IsDead)
            return;

        if (currentTarget == null)
        {
            currentTarget = combatManager.FindNearestEnemy(combatInstance, detectionRange);

            if (currentTarget == null)
            {
                stateMachine.SetState(EntityState.Idle);
                navMeshAgent.isStopped = true;
                return;
            }
        }

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

        if (distance <= attackRange)
        {
            stateMachine.SetState(EntityState.Attack);
            navMeshAgent.isStopped = true;
            FaceTarget();
            Attack();
        }
        else
        {
            stateMachine.SetState(EntityState.Chase);
            navMeshAgent.isStopped = false;
            navMeshAgent.SetDestination(currentTarget.transform.position);
        }
        if (distance > spareRange)
        {
            LoseTarget();
        }
    }
    private void FaceTarget()
    {
        Vector3 direction = currentTarget.transform.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction.normalized);
    }
    public void LoseTarget()
    {
        currentTarget = null;
        navMeshAgent.isStopped = true;
        stateMachine.SetState(EntityState.Idle);
    }
    private void Attack()
    {
        if (Time.time < nextAttackTime)
            return;

        if (currentTarget == null)
            return;

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

        if (distance > attackRange)
            return;

        StageManager.Instance.Hit(combatInstance, currentTarget, 1f);

        nextAttackTime = Time.time + attackCooldown;
        currentTarget = null;
    }

    public void Die()
    {
        if (isDead)
            return;

        isDead = true;

        if (navMeshAgent != null)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.enabled = false;
            CancelInvoke(nameof(Think));
        }

        if (combatInstance != null && CombatManager.Instance != null)
            CombatManager.Instance.Unregister(combatInstance);
        StageManager.Instance.EntityDefeated(gameObject);

        enabled = false;
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddTorque(Random.onUnitSphere * 4f, ForceMode.Impulse);

        }
        gameObject.layer = 2;
        EntityInstance entityInstance = GetComponent<EntityInstance>();

        if (entityInstance != null)
            LootManager.Instance.DropLoot(entityInstance.Data, transform.position);
    }
}
