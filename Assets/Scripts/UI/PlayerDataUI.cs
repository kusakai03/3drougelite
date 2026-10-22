using System;
using UnityEngine;

public class PlayerDataUI : MonoBehaviour
{
    [Header("Tabs")]
    [SerializeField] private GameObject inventoryTab;
    [SerializeField] private GameObject blessingTab;
    [SerializeField] private GameObject settingTab;

    [Header("Tab UI")]
    [SerializeField] private InventoryTabUI inventoryTabUI;
    [SerializeField] private BlessingTabUI blessingTabUI;

    private GameObject currentTab;
    private string playerID;
    private GameObject playerObject;
    private CombatInstance combatInstance;
    public void Open(string playerID, GameObject playerObject)
    {
        this.playerID = playerID;
        this.playerObject = playerObject;

        combatInstance = playerObject.GetComponent<CombatInstance>();

        if (combatInstance != null)
            combatInstance.OnCombatStateChanged += Refresh;

        OpenInventoryTab();
    }

    public void Refresh(string instanceID)
    {
        if (playerID == instanceID)
        {
            if (currentTab == inventoryTab)
                inventoryTabUI.Refresh();
            else if (currentTab == blessingTab)
                blessingTabUI.Refresh();
        }
    }

    private void OnDisable()
    {
        if (playerObject)
            playerObject.GetComponent<CombatInstance>().OnCombatStateChanged -= Refresh;
    }

    public void OpenInventoryTab()
    {
        OpenTab(inventoryTab);
        inventoryTabUI.Show(playerID, combatInstance);
    }

    public void OpenBlessingTab()
    {
        OpenTab(blessingTab);
        blessingTabUI.Show(playerID);
    }

    public void OpenSettingTab()
    {
        OpenTab(settingTab);
    }

    private void OpenTab(GameObject tab)
    {
        if (currentTab == tab)
            return;

        if (currentTab != null)
            currentTab.SetActive(false);

        currentTab = tab;
        currentTab.SetActive(true);
    }
}