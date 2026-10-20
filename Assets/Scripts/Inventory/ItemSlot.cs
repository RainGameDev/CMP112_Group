using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemSlot : MonoBehaviour
{
    public ContainerItem item;
    public Container container;

    public TMP_Text amountLabel;
    public Image itemIcon;

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
}
