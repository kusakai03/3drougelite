using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu(
    fileName = "TriggerGroupData",
    menuName = "Game/Trigger Group Data")]
public class TriggerGroupData : ScriptableObject
{
    public SpawnTag spawnTag;

    public List<StageTriggerConfig> randomTriggerNodes = new();
    public List<StageTriggerConfig> uniqueTriggerNodes = new();
}