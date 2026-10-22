using UnityEngine;

public class HitProjectile : MonoBehaviour
{
    [SerializeField] private float lifeTime = 5f;

    private ReactionReceiver reactionReceiver;
    private ReactionContact reactionContact;

    private void Awake()
    {
        reactionReceiver = GetComponent<ReactionReceiver>();
        reactionContact = GetComponent<ReactionContact>();
    }

    private void Update()
    {
        if (reactionReceiver != null && reactionReceiver.CurrentEffects.Count == 0)
            Destroy(gameObject);
    }

    public void Initialize(CombatInstance owner)
    {
        if (reactionContact != null)
            reactionContact.Initialize(owner);

        if (lifeTime > 0f)
            Destroy(gameObject, lifeTime);
    }
}