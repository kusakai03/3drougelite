using System.Collections.Generic;
using UnityEngine;

public class ReactionContact : MonoBehaviour
{
    [SerializeField] private bool useCollision = true;
    [SerializeField] private bool useTrigger = true;
    [SerializeField] private bool hitOncePerTarget = false;

    private ReactionSource reactionSource;
    private ReactionReceiver selfReceiver;
    private CombatInstance owner;

    private readonly HashSet<ReactionReceiver> hitReceivers = new();

    private void Awake()
    {
        reactionSource = GetComponent<ReactionSource>();
        selfReceiver = GetComponent<ReactionReceiver>();

        CombatInstance selfCombat = GetComponent<CombatInstance>();

        if (selfCombat != null)
            owner = selfCombat;
    }

    public void Initialize(CombatInstance owner)
    {
        this.owner = owner;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!useCollision)
            return;

        TryApply(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!useTrigger)
            return;

        TryApply(other);
    }

    private void TryApply(Collider other)
    {
        if (reactionSource == null)
            return;

        ReactionReceiver receiver = other.GetComponentInParent<ReactionReceiver>();

        if (receiver == null)
            return;

        if (receiver == selfReceiver)
            return;

        if (hitOncePerTarget && hitReceivers.Contains(receiver))
            return;

        if (hitOncePerTarget)
            hitReceivers.Add(receiver);

        reactionSource.Apply(receiver, owner, 1f, gameObject);
    }
}