using System;
using System.Collections.Generic;
using UnityEngine;

public class Container : MonoBehaviour
{
    [Tooltip("Player Only")]
    public EquipmentManager equipmentManager;
    public List<ContainerItem> items;
    public event Action<ContainerItem> OnItemAdded;
    public event Action<ContainerItem> OnItemRemoved;
    public event Action<ContainerItem> OnItemUpdated;

    public void AddItem(ContainerItem item)
    {
        if (item.item.stacks)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].item == item.item)
                {
                    items[i].amount += item.amount;
                    OnItemUpdated?.Invoke(item);
                    return;
                }
            }
        }

        items.Add(item);
        OnItemAdded?.Invoke(item);
    }

    public void RemoveItem(ContainerItem item)
    {
        OnItemRemoved?.Invoke(item);
        items.Remove(items.Find(x => x.item == item.item));
    }

    public void UseItem(ContainerItem item)
    {
        if (item.item is EquipmentItem)
        {
            equipmentManager.EquipItem(item.item as EquipmentItem);
        }
    }

    public void UnequipItem(ContainerItem item)
    {
        if (item.item is EquipmentItem)
        {
            equipmentManager.UnequipItem(item.item as EquipmentItem);
        }
    }
}
