using System.Collections.Generic;
using UnityEngine;

public class ArtifactManager : MonoBehaviour
{
    public static ArtifactManager Instance { get; private set; }
    [SerializeField] private ArtifactSubStatPool subStatPool;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    public ArtifactInstance CreateArtifact(ArtifactData data)
    {
        ArtifactInstance artifact = new ArtifactInstance
        {
            artifactID = data.itemID,
            level = Mathf.Clamp(data.startingLevel, 0, 3),
            subStats = new List<ArtifactStat>()
        };

        UpdateArtifact(artifact);

        return artifact;
    }

    public ArtifactInstance UpdateArtifact(ArtifactInstance artifact)
    {
        artifact.level = Mathf.Clamp(artifact.level, 0, 3);

        int targetSubStatCount = artifact.level;

        while (artifact.subStats.Count < targetSubStatCount)
        {
            ArtifactStat newStat = RollNewSubStat(artifact);

            if (newStat == null)
                break;

            artifact.subStats.Add(newStat);
        }

        while (artifact.subStats.Count > targetSubStatCount)
            artifact.subStats.RemoveAt(artifact.subStats.Count - 1);

        return artifact;
    }

    private ArtifactStat RollNewSubStat(ArtifactInstance artifact)
    {
        List<ArtifactStat> available = new();

        foreach (ArtifactStat candidate in subStatPool.subStats)
        {
            bool alreadySelected = false;

            foreach (ArtifactStat existing in artifact.subStats)
            {
                if (IsSameStat(existing, candidate))
                {
                    alreadySelected = true;
                    break;
                }
            }

            if (!alreadySelected)
                available.Add(candidate);
        }

        if (available.Count == 0)
            return null;

        ArtifactStat selected = available[Random.Range(0, available.Count)];

        return selected.Clone();
    }

    private bool IsSameStat(ArtifactStat a, ArtifactStat b)
    {
        return a.statType == b.statType &&
               a.modifierType == b.modifierType &&
               Mathf.Approximately(a.value, b.value);
    }

    public float GetEffectivenessMultiplier(ArtifactInstance artifact)
    {
        return 1f + artifact.level * 0.3f;
    }
    public BonusStats CalculateTotalBonus(CharacterData character)
    {
        BonusStats totalBonus = new();

        foreach (ArtifactInstance artifact in character.artifacts)
        {
            if (artifact == null)
                continue;

            ArtifactData data = ItemManager.Instance.GetItem<ArtifactData>(artifact.artifactID);

            if (data == null)
                continue;

            float effectiveness = 1f + artifact.level * 0.3f;

            data.mainStat.ApplyToBonus(totalBonus, effectiveness);

            foreach (ArtifactStat subStat in artifact.subStats)
                subStat.ApplyToBonus(totalBonus, effectiveness);
        }

        return totalBonus;
    }

    public bool UpgradeArtifact(ArtifactInstance artifact)
    {
        if (artifact == null || artifact.level >= 3)
            return false;

        artifact.level++;

        UpdateArtifact(artifact);

        return true;
    }

}