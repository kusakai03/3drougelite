using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Stage Settings", menuName = "Stage/Stage Settings")]
public class StageSettings : ScriptableObject
{
    [Header("Stage")]
    public string stageID;

    [Header("Rooms")]
    public int numberOfRooms = 3;
    public BiomeData biome;
    public bool useTriggerGroup = true;

    [Header("Nodes")]
    public int nodeCount = 3;
    public List<StageTriggerConfig> globalTriggerNodes = new();

    [Header("Combat")]
    public bool takeDamage = true;

    [Header("Room")]
    public bool hasKeyRoom = true;
    public int roomLevel = 1;

    [Header("Merchant")]
    public bool merchantSandbox = false;
}