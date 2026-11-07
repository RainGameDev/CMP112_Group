using UnityEngine;

// The chest at the start of the level that gives the player the spear,
// which is the only weapon in the game.
// It builds on our normal ChestInteractable (so it plays the same open
// animation and uses the same interact button), and on top of that it puts
// the spear in the player's hand.
public class WeaponChest : ChestInteractable
{
    // the weapon inside the chest (the Iron Spear item asset)
    public WeaponItem weapon;

    // so the chest can only be opened once
    bool opened;

    public override void Interact()
    {
        if (opened) return;
        opened = true;

        // this runs the normal chest code: plays the open animation
        // and removes the "Interact with chest" prompt
        base.Interact();

        var player = GameObject.FindWithTag("Player");
        if (player && weapon)
        {
            // if the player has an inventory, put the spear in it
            // so it shows up when you open the inventory
            var container = player.GetComponentInChildren<Container>();
            if (container) container.AddItem(new ContainerItem { item = weapon, amount = 1 });

            // equip it, this spawns the spear model in the player's hand
            var equipment = player.GetComponentInChildren<EquipmentManager>();
            if (equipment) equipment.EquipItem(weapon);
        }

        // tell the level manager the player is armed now
        // (this opens the WeaponGate and shows the "acquired" message)
        if (LevelManager.Instance) LevelManager.Instance.GiveWeapon(weapon);
    }
}
