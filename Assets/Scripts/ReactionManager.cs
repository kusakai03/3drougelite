using System.Collections.Generic;
using UnityEngine;

public class ReactionManager : MonoBehaviour
{
    public static ReactionManager Instance { get; private set; }

    private readonly List<ReactionData> reactions = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        reactions.AddRange(Resources.LoadAll<ReactionData>("Game/Reaction"));
    }

    public ReactionData FindReaction(ReactionEffectType effectA, ReactionEffectType effectB)
    {
        return reactions.Find(x => x.effectA == effectA && x.effectB == effectB);
    }
}