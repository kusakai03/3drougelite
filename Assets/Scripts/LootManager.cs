using System.Collections.Generic;
using UnityEngine;

public class LootManager : MonoBehaviour
{
    public static LootManager Instance { get; private set; }

    [SerializeField] private GlobalDropPool globalDropPool;
    [SerializeField] private LootPickup lootPickupPrefab;
    [SerializeField] private float dropScatterRadius = 1f;
    [SerializeField] private float dropUpForce = 2f;
    [SerializeField] private float dropSideForce = 1.5f;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public List<LootDrop> RollLoot(EntityData entityData)
    {
        List<LootDrop> result = new();

        RollEntityDrops(entityData, result);
        RollGlobalDrops(result);

        return result;
    }

    private void RollEntityDrops(EntityData entityData, List<LootDrop> result)
    {
        if (entityData == null)
            return;

        foreach (EntityItemDrop drop in entityData.itemDrops)
        {
            if (drop.item == null)
                continue;

            if (Random.value > drop.dropChance)
                continue;

            int quantity = Random.Range(drop.minQuantity, drop.maxQuantity + 1);

            AddDrop(result, drop.item, quantity);
        }
    }

    private void RollGlobalDrops(List<LootDrop> result)
    {
        if (globalDropPool == null || globalDropPool.drops.Count == 0)
            return;

        int dropCount = Random.Range(globalDropPool.minDrops, globalDropPool.maxDrops + 1);

        List<PoolItemDrop> availableDrops = new(globalDropPool.drops);

        for (int i = 0; i < dropCount; i++)
        {
            PoolItemDrop selected = RollWeightedDrop(availableDrops);

            if (selected == null)
                break;

            int quantity = Random.Range(selected.minQuantity, selected.maxQuantity + 1);

            AddDrop(result, selected.item, quantity);

            availableDrops.Remove(selected);
        }
    }

    private PoolItemDrop RollWeightedDrop(List<PoolItemDrop> drops)
    {
        float totalWeight = 0f;

        foreach (PoolItemDrop drop in drops)
        {
            if (drop.item == null || drop.weight <= 0f)
                continue;

            totalWeight += drop.weight;
        }

        if (totalWeight <= 0f)
            return null;

        float roll = Random.Range(0f, totalWeight);

        foreach (PoolItemDrop drop in drops)
        {
            if (drop.item == null || drop.weight <= 0f)
                continue;

            roll -= drop.weight;

            if (roll <= 0f)
                return drop;
        }

        return null;
    }

    private void AddDrop(List<LootDrop> result, ItemData item, int quantity)
    {
        if (item == null || quantity <= 0)
            return;

        if (item.itemType == ItemType.Artifact)
        {
            ArtifactData artifactData = item as ArtifactData;

            if (artifactData == null)
                return;

            for (int i = 0; i < quantity; i++)
            {
                ArtifactInstance artifact = ArtifactManager.Instance.CreateArtifact(artifactData);

                result.Add(new LootDrop
                {
                    item = item,
                    quantity = 1,
                    artifact = artifact
                });
            }

            return;
        }

        result.Add(new LootDrop
        {
            item = item,
            quantity = quantity,
            artifact = null
        });
    }
    public void SpawnDrops(List<LootDrop> drops, Vector3 position)
    {
        foreach (LootDrop drop in drops)
        {
            if (drop == null || drop.item == null)
                continue;

            Vector2 randomCircle = Random.insideUnitCircle * dropScatterRadius;
            Vector3 spawnPosition = position + new Vector3(randomCircle.x, 0.5f,randomCircle.y);
            LootPickup pickup = Instantiate(lootPickupPrefab, spawnPosition, Quaternion.identity);
            pickup.Initialize(drop);

            Rigidbody rb = pickup.GetComponent<Rigidbody>();

            if (rb != null)
            {
                Vector3 force = new Vector3(randomCircle.x * dropSideForce, dropUpForce, randomCircle.y * dropSideForce);

                rb.AddForce(force, ForceMode.Impulse);
            }
        }
    }
    public void DropLoot(EntityData entityData, Vector3 position)
    {
        List<LootDrop> drops = RollLoot(entityData);
        SpawnDrops(drops, position);
    }

    public List<LootDrop> RollPool(List<PoolItemDrop> pool, int minDrops, int maxDrops)
    {
        List<LootDrop> result = new();

        if (pool == null || pool.Count == 0)
            return result;

        int dropCount = Random.Range(minDrops, maxDrops + 1);
        List<PoolItemDrop> availableDrops = new(pool);

        for (int i = 0; i < dropCount && availableDrops.Count > 0; i++)
        {
            PoolItemDrop selected = RollWeightedDrop(availableDrops);

            if (selected == null)
                break;

            int quantity = Random.Range(selected.minQuantity, selected.maxQuantity + 1);

            AddDrop(result, selected.item, quantity);
            availableDrops.Remove(selected);
        }

        return result;
    }
}