using UnityEngine;

[CreateAssetMenu(fileName = "New Currency", menuName = "Items/Currency")]
public class CurrencyData : ItemData
{
    [Min(0)]
    public int tpValue = 1;

    private void OnValidate()
    {
        itemType = ItemType.Currency;
    }
}