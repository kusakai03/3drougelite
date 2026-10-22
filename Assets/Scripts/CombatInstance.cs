using UnityEngine;
using System;
public class CombatInstance : MonoBehaviour
{
    public CombatState state = new();
    public event Action<string> OnCombatStateChanged;
    public void Initialize(string instanceID, string instanceRace, CombatStats stats)
    {
        state.instanceID = instanceID;
        state.instanceRace = instanceRace;
        state.stats = stats.Clone();
    }
    private void OnEnable()
    {
        if (CombatManager.Instance != null)
            CombatManager.Instance.Register(this);

        if (StageManager.Instance != null)
            StageManager.Instance.OnCombatChanged += ApplyCombatState;
    }

    private void OnDisable()
    {
        if (CombatManager.Instance != null)
            CombatManager.Instance.Unregister(this);

        if (StageManager.Instance != null)
            StageManager.Instance.OnCombatChanged -= ApplyCombatState;
    }

    private void ApplyCombatState(CombatState newState)
    {
        if (newState.instanceID != state.instanceID)
            return;

        state.stats = newState.stats;
        OnCombatStateChanged?.Invoke(state.instanceID);
    }
}
[Serializable]
public class CombatState
{
    public string instanceID;
    public string instanceRace;
    public CombatStats stats;

    public CombatState Clone()
    {
        return new CombatState
        {
            instanceID = instanceID,
            instanceRace = instanceRace,
            stats = stats.Clone()
        };
    }
}