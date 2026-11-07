using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;

public class ContainerUI : MonoBehaviour
{
    [BoxGroup("References")]
    public Container container;
    public PlayerInputs inputs;

    [BoxGroup("UI")]
    public GameObject containerUI;

    [BoxGroup("UI")]
    public GameObject itemSlotHolder;

    [BoxGroup("UI")]
    public GameObject itemSlot;

    [BoxGroup("UI")]
    public List<GameObject> itemSlots;

    [BoxGroup("UI")]
    public List<Item> itemToSlot;

    void OnEnable()
    {
        container.OnItemAdded += HandleItemAdded;
        container.OnItemUpdated += HandleItemUpdated;
        container.OnItemRemoved += HandleItemRemoved;
    }

    void Start()
    {
        inputs = new();
        inputs.Enable();
    }

    void Update()
    {
        if (inputs.UI.InventoryOpen.WasPressedThisFrame())
            containerUI.SetActive(!containerUI.activeInHierarchy);
    }

    void HandleItemRemoved(ContainerItem item)
    {
        for (int i = 0; i < itemToSlot.Count; i++)
        {
            if (itemToSlot[i] == item.item)
            {
                Destroy(itemSlots[i]);
                itemToSlot.RemoveAt(i);
                itemSlots.RemoveAt(i);

                break;
            }
        }
    }

    void HandleItemUpdated(ContainerItem item)
    {
        for (int i = 0; i < itemToSlot.Count; i++)
        {
            if (itemToSlot[i] == item.item)
            {
                var slot = itemSlots[i].GetComponent<ItemSlot>();
                slot.item = container.items.Find(x => x.item == item.item);

                break;
            }
        }
    }

    void HandleItemAdded(ContainerItem item)
    {
        GameObject slot = Instantiate(itemSlot);
        slot.GetComponent<ItemSlot>().item = item;
        slot.GetComponent<ItemSlot>().container = container;
        // Set parent with false to avoid scaling issues when the parent is scaled
        slot.transform.SetParent(itemSlotHolder.transform, false);
        itemSlots.Add(slot);
        itemToSlot.Add(item.item);
    }
}
