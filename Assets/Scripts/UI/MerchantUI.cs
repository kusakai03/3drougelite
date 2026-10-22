using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MerchantUI : MonoBehaviour
{
    [Header("Items")]
    [SerializeField] private Transform itemContent;
    [SerializeField] private MerchantItemSlotUI itemSlotPrefab;

    [Header("Info")]
    [SerializeField] private TMP_Text tpText;

    [Header("Refresh")]
    [SerializeField] private Button refreshButton;
    [SerializeField] private TMP_Text refreshCostText;

    private readonly List<GameObject> spawnedSlots = new();
    private MerchantItem pendingArtifactPurchase;

    private string playerID;
    private StageRoomData room;
    [SerializeField] private ArtifactUpgradeUI artifactUpgradeUI;
    [SerializeField] private MerchantSellUI sellUI;
    private void OnEnable()
    {
        StageManager.Instance.OnPlayerPurchase += OnPlayerPurchase;
        StageManager.Instance.OnArtifactReplaced += OnArtifactReplaced;
    }
    private void OnPlayerPurchase(string playerID, MerchantItem merchantItem)
    {
        if (playerID != this.playerID)
            return;

        RefreshUI();
    }
    private void OnArtifactReplaced(string replacedPlayerID, ArtifactInstance artifact)
    {
        if (pendingArtifactPurchase == null)
            return;

        if (replacedPlayerID != playerID)
            return;

        if (pendingArtifactPurchase.artifact != artifact)
            return;

        StageManager.Instance.CompleteMerchantPurchase(playerID, pendingArtifactPurchase);

        pendingArtifactPurchase = null;

        RefreshUI();
    }
    public void Open(string playerID, StageRoomData room)
    {
        this.playerID = playerID;
        this.room = room;

        artifactUpgradeUI.gameObject.SetActive(false);
        sellUI.gameObject.SetActive(false);
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (room == null || room.merchant == null)
            return;

        CharacterData character = StageManager.Instance.GetCharacter(playerID);

        if (character == null)
            return;

        ClearItems();

        tpText.text = "TP " +character.tp.ToString();

        foreach (MerchantItem merchantItem in room.merchant.items)
        {
            MerchantItemSlotUI slot = Instantiate(itemSlotPrefab, itemContent);

            slot.gameObject.SetActive(true);
            slot.Setup(playerID, merchantItem, this);

            spawnedSlots.Add(slot.gameObject);
        }

        RefreshRefreshButton(character);
    }

    public void BuyItem(MerchantItem merchantItem)
    {
        MerchantPurchaseResult result = StageManager.Instance.BuyMerchantItem(playerID, merchantItem);

        switch (result)
        {
            case MerchantPurchaseResult.Failed:
                return;

            case MerchantPurchaseResult.Success:
                RefreshUI();
                break;

            case MerchantPurchaseResult.RequireArtifactReplace:
            {
                LootDrop loot = new LootDrop
                {
                    item = merchantItem.item,
                    quantity = 1,
                    artifact = merchantItem.artifact
                };

                pendingArtifactPurchase = merchantItem;
                UIManager.Instance.OpenArtifactReplaceUI(playerID, loot);
                break;
            }
        }
    }

    public void RefreshMerchant()
    {
        if (StageManager.Instance.RefreshMerchant(playerID, room))
            RefreshUI();
    }

    private void RefreshRefreshButton(CharacterData character)
    {
        Merchant merchant = room.merchant;

        refreshButton.gameObject.SetActive(merchant.allowRefresh);

        if (!merchant.allowRefresh)
            return;

        int cost = merchant.GetRefreshCost(StageManager.Instance.currentRoomIndex);

        refreshCostText.text = $"Refresh ({cost} TP)";
        refreshButton.interactable = character.tp >= cost;
    }

    private void ClearItems()
    {
        foreach (GameObject slot in spawnedSlots)
        {
            if (slot != null)
                Destroy(slot);
        }

        spawnedSlots.Clear();
    }
    public void OpenArtifactUpgrade()
    {
        artifactUpgradeUI.Open(playerID, room);
        artifactUpgradeUI.gameObject.SetActive(true);
    }
    public void CloseArtifactUpgrade()
    {
        artifactUpgradeUI.gameObject.SetActive(false);
        UIManager.Instance.HideItemInfo(playerID);
    }
    public void OpenSell()
    {
        sellUI.Open(playerID, this);
        sellUI.gameObject.SetActive(true);
    }
    public void CloseSell()
    {
        sellUI.gameObject.SetActive(false);
        UIManager.Instance.HideItemInfo(playerID);
    }
    private void OnDisable()
    {
        CloseArtifactUpgrade();
        CloseSell();

        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnPlayerPurchase -= OnPlayerPurchase;
            StageManager.Instance.OnArtifactReplaced -= OnArtifactReplaced;
        }

        pendingArtifactPurchase = null;
    }
}