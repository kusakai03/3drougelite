using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ThrowProjectile : MonoBehaviour
{
    private Rigidbody rb;

    private ThrowObj throwObj;
    private int currentBounce;
    private ReactionSource reactionSource;
    private CombatInstance owner;
    [SerializeField] private float lifetime = 10f;
    private ReactionContact reactionContact;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        reactionSource = GetComponent<ReactionSource>();
        reactionContact = GetComponent<ReactionContact>();
    }

    private Vector3 lastVelocity;
    private void Start()
    {
        Destroy(gameObject, lifetime);
    }
    private void FixedUpdate()
    {
        lastVelocity = rb.linearVelocity;
    }
    public void Initialize(ThrowObj throwObj, CombatInstance owner)
    {
        this.throwObj = throwObj;
        this.owner = owner;

        if (reactionContact != null)
            reactionContact.Initialize(owner);

        currentBounce = throwObj.bounceCount;
    }

    public void Launch(Vector3 velocity)
    {
        rb.linearVelocity = velocity;
    }

    private void OnCollisionEnter(Collision collision)
    {
        float impactSpeed = collision.relativeVelocity.magnitude;

        float damageMulti = (1 + impactSpeed / 100) * (1 + 0.1f*throwObj.weight);
        CombatInstance defender = collision.collider.GetComponent<CombatInstance>();
        if (defender != null && defender != owner)
            StageManager.Instance.Hit(owner, defender, damageMulti);


        SpawnHitProjectile(collision.GetContact(0).point);

        // Bounce
        switch (throwObj.hitBehavior)
        {
            case ThrowObj.HitBehavior.Bounce:
                if (currentBounce <= 0)
                {
                    Destroy(gameObject);
                    break;
                }

                Vector3 velocity = lastVelocity;

                velocity.y = Mathf.Abs(velocity.y) * throwObj.bouncePower;

                rb.linearVelocity = velocity;

                currentBounce--;
                break;

            case ThrowObj.HitBehavior.Destroy:
                Destroy(gameObject);
                break;

            case ThrowObj.HitBehavior.StayStill:
                break;

            case ThrowObj.HitBehavior.StickToTarget:
                break;
        }
    }
    private void SpawnHitProjectile(Vector3 position)
    {
        if (throwObj.hitProjectilePrefab == null)
            return;

        GameObject obj = Instantiate(throwObj.hitProjectilePrefab, position, Quaternion.identity);

        HitProjectile hitProjectile = obj.GetComponent<HitProjectile>();

        if (hitProjectile != null)
            hitProjectile.Initialize(owner);
    }
}