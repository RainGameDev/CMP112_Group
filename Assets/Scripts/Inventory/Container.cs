using System;
using System.Collections.Generic;
using UnityEngine;

public class Container : MonoBehaviour
{
    public List<ContainerItem> items;
    public event Action<ContainerItem> OnItemAdded;

    public void AddItem(ContainerItem item)
    {
        items.Add(item);

        OnItemAdded?.Invoke(item);
    }
}
