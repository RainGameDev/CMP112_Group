using UnityEngine;

public class WeaponChest : ChestInteractable
{
    public WeaponItem weapon;
    public float launchUp = 4f;
    public float launchForward = 2.5f;

    public override void Interact()
    {
        base.Interact();

        GameObject player = GameObject.FindWithTag("Player");
        Vector3 toPlayer = player.transform.position - transform.position;
        toPlayer.y = 0f;

        GameObject spear = Instantiate(weapon.droppedPrefab, transform.position + Vector3.up * 1.2f, transform.rotation);
        spear.GetComponent<Rigidbody>().linearVelocity = toPlayer.normalized * launchForward + Vector3.up * launchUp;

        WeaponItem chestWeapon = weapon;
        player.GetComponent<Container>().OnItemAdded += item =>
        {
            LevelManager lm = LevelManager.Instance;
            if (item.item != chestWeapon || !lm || lm.HasWeapon) return;
            lm.GiveWeapon(chestWeapon);
            lm.ShowMessage("Press Tab to open your inventory and equip the spear", 4f);
        };

        Destroy(this);
    }
}
