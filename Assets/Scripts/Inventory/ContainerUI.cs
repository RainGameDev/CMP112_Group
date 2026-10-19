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

    void OnEnable() => container.OnItemAdded += HandleItemAdded;

    void OnDisable() => container.OnItemAdded -= HandleItemAdded;

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

    void HandleItemAdded(ContainerItem item)
    {
        GameObject slot = Instantiate(itemSlot);
        slot.GetComponent<ItemSlot>().item = item;
        slot.transform.parent = itemSlotHolder.transform;
        itemSlots.Add(slot);
    }
}
