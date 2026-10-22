using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    private Dictionary<string, ItemData> itemDictionary = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadItems();
    }

    private void LoadItems()
    {
        itemDictionary.Clear();

        ItemData[] items = Resources.LoadAll<ItemData>("Item");

        foreach (ItemData item in items)
        {
            if (string.IsNullOrEmpty(item.itemID))
            {
                Debug.LogWarning($"Item '{item.name}' has empty itemID.");
                continue;
            }

            if (itemDictionary.ContainsKey(item.itemID))
            {
                Debug.LogWarning($"Duplicate Item ID: {item.itemID}");
                continue;
            }

            itemDictionary.Add(item.itemID, item);
        }

        Debug.Log($"Loaded {itemDictionary.Count} items.");
    }

    public ItemData GetItem(string itemID)
    {
        if (itemDictionary.TryGetValue(itemID, out ItemData item))
            return item;
    
        Debug.LogWarning($"Item '{itemID}' not found.");
        return null;
    }

    public T GetItem<T>(string itemID) where T : ItemData
    {
        ItemData item = GetItem(itemID);

        if (item == null)
            return null;

        return item as T;
    }

    public bool HasItem(string itemID)
    {
        return itemDictionary.ContainsKey(itemID);
    }

    public IReadOnlyDictionary<string, ItemData> GetAllItems()
    {
        return itemDictionary;
    }
    public List<T> GetAllItems<T>() where T : ItemData
    {
        List<T> result = new();

        foreach (ItemData item in itemDictionary.Values)
        {
            if (item is T typedItem)
                result.Add(typedItem);
        }

        return result;
    }
}