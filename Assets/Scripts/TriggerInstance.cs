using UnityEngine;

public class TriggerInstance : MonoBehaviour
{
    public string nodeID;
    public TriggerData triggerData;
    private GameObject spawnedObject;
    private bool activated;
    private Transform chunkParent;

    public void Initialize(string nodeID, TriggerData triggerData, Vector3 position, Quaternion rotation, Transform chunkParent)
    {
        this.nodeID = nodeID;
        this.triggerData = triggerData;
        this.chunkParent = chunkParent;
    
        CreateTriggerCollider();
        if (triggerData.prefab != null)
        {
            spawnedObject = Instantiate(triggerData.prefab, position, rotation, chunkParent);
        }
    }

    private void CreateTriggerCollider()
    {
        switch (triggerData.shape)
        {
            case TriggerShape.Box:
            {
                BoxCollider collider = gameObject.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.size = new Vector3(triggerData.range, 2f, triggerData.range);
                break;
            }

            case TriggerShape.Circle:
            {
                SphereCollider collider = gameObject.AddComponent<SphereCollider>();
                collider.isTrigger = true;
                collider.radius = triggerData.range;
                break;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerInstance player = other.gameObject.GetComponentInChildren<PlayerInstance>();

        if (player == null)
            return;

        if (triggerData.type == TriggerType.Merchant)
        {
            StageManager.Instance.EnterMerchantTrigger(this, player);
            return;
        }
        if (triggerData.type == TriggerType.Chest)
        {
            StageManager.Instance.OpenChest(this, player);
            return;
        }
        if (triggerData.type == TriggerType.Rest)
        {
            CombatInstance combat = player.GetComponent<CombatInstance>();

            if (StageManager.Instance.IsPlayerInCombat(combat))
                return;

            StageManager.Instance.RestPlayer(player);
            Activate(false);
            return;
        }
        if (triggerData.type == TriggerType.Portal)
        {
            StageManager.Instance.EnterPortal(this, player);
            return;
        }

        Activate();
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerInstance player = other.gameObject.GetComponentInChildren<PlayerInstance>();

        if (player == null)
            return;

        if (triggerData.type == TriggerType.Merchant)
            StageManager.Instance.ExitMerchantTrigger(this, player);
        if (triggerData.type == TriggerType.Portal)
        {
            StageManager.Instance.ExitPortal(this, player);
            return;
        }
    }

    public void Activate(bool oneTime = true)
    {
        if (oneTime && activated)
            return;

        if (oneTime)
            activated = true;

        StageManager.Instance.ActivateTrigger(this);

        if (triggerData.activePrefab != null)
            Instantiate(triggerData.activePrefab, transform.position, transform.rotation, chunkParent);

        if (spawnedObject != null && triggerData.type == TriggerType.Chest)
        {
            BreakableObject breakable = spawnedObject.GetComponent<BreakableObject>();

            if (breakable != null)
                breakable.Break();
            else
                Destroy(spawnedObject);

            spawnedObject = null;
        }
    }
}