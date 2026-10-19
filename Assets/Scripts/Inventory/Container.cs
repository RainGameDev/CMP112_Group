using System;
using System.Collections.Generic;
using UnityEngine;

public class Container : MonoBehaviour
{
    public List<ContainerItem> items;
    public event Action<ContainerItem> OnItemAdded;
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
}
