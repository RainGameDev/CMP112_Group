using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemSlot : MonoBehaviour
{
    public ContainerItem item;
    public Container container;

    public TMP_Text amountLabel;
    public Image itemIcon;
    public GameObject equippedIcon;

    public bool equipped;

    // Update is called once per frame
    void Update()
    {
        amountLabel.text = item.amount.ToString();
        itemIcon.sprite = item.item.itemIcon;
    }

    public void RemoveItem()
    {
        container.RemoveItem(item);
    }

    public void UseItem()
    {
        container.UseItem(item);

        if (item.item is EquipmentItem)
        {
            equipped = !equipped;
            equippedIcon.SetActive(equipped);

            if (!equipped)
            {
                container.UnequipItem(item);
            }
        }
    }
}
