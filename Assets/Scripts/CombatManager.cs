using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    private readonly List<CombatInstance> combatInstances = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Register(CombatInstance instance)
    {
        if (instance == null)
            return;

        if (!combatInstances.Contains(instance))
            combatInstances.Add(instance);
    }

    public void Unregister(CombatInstance instance)
    {
        if (instance == null)
            return;

        combatInstances.Remove(instance);
    }

    public CombatInstance FindNearestEnemy(CombatInstance requester, float range)
    {
        if (requester == null)
            return null;

        CombatInstance nearest = null;
        float nearestDistanceSqr = range * range;

        foreach (CombatInstance instance in combatInstances)
        {
            if (instance == null)
                continue;

            if (instance == requester)
                continue;

            if (instance.state.instanceRace == requester.state.instanceRace)
                continue;

            float distanceSqr = (instance.transform.position - requester.transform.position).sqrMagnitude;

            if (distanceSqr > nearestDistanceSqr)
                continue;

            nearestDistanceSqr = distanceSqr;
            nearest = instance;
        }

        return nearest;
    }
}