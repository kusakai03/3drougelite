using UnityEngine;
using UnityEngine.AI;

public class LeapAttack : EnemyAttack
{
    private readonly float leapHeight;
    private readonly float leapDuration;

    private Vector3 startPosition;
    private Vector3 targetPosition;

    private float timer;
    private bool finished;

    public LeapAttack(EntityAgent owner, float leapHeight, float leapDuration) : base(owner)
    {
        this.leapHeight = leapHeight;
        this.leapDuration = leapDuration;
    }

    public override void StartAttack(CombatInstance target)
    {
        this.target = target;

        if (target == null)
        {
            finished = true;
            return;
        }

        startPosition = owner.transform.position;

        if (NavMesh.SamplePosition(
            target.transform.position,
            out NavMeshHit hit,
            2f,
            NavMesh.AllAreas))
        {
            targetPosition = hit.position;
            targetPosition.y = startPosition.y;
        }
        else
        {
            finished = true;
            return;
        }

        timer = 0f;
        finished = false;
    }

    public override void UpdateAttack()
    {
        if (finished)
            return;

        timer += Time.deltaTime;

        float t = Mathf.Clamp01(timer / leapDuration);

        Vector3 position = Vector3.Lerp(
            startPosition,
            targetPosition,
            t
        );

        position.y += Mathf.Sin(t * Mathf.PI) * leapHeight;

        owner.transform.position = position;

        if (t >= 1f)
        {
            owner.transform.position = targetPosition;
            finished = true;
        }
    }

    public override bool IsFinished()
    {
        return finished;
    }
}