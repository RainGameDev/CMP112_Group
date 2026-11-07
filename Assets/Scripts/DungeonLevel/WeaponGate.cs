using UnityEngine;

// An invisible wall across the exit of the starting room (the Gatehouse).
// It stops the player leaving before they've opened the chest and picked up
// the spear, because they'd have nothing to fight the skeletons with.
// As soon as the weapon is picked up the wall switches itself off.
[RequireComponent(typeof(Collider))]
public class WeaponGate : MonoBehaviour
{
    Collider gate;   // the invisible box collider that blocks the way

    void Start()
    {
        gate = GetComponent<Collider>();

        // if the player somehow already has the weapon, open straight away
        if (LevelManager.Instance && LevelManager.Instance.HasWeapon) gate.enabled = false;

        // listen for the moment the weapon is picked up, so we can open then
        LevelManager.WeaponObtained += Open;
    }

    void OnDestroy()
    {
        // stop listening when this object is destroyed, otherwise the event
        // would try to call Open on something that doesn't exist any more
        LevelManager.WeaponObtained -= Open;
    }

    // turning the collider off lets the player walk through
    void Open() => gate.enabled = false;

    void OnCollisionEnter(Collision collision)
    {
        // if the player walks into the wall, give them a hint about why
        if (LevelManager.IsPlayerBody(collision.collider) && LevelManager.Instance)
            LevelManager.Instance.ShowMessage("You won't last down there unarmed. Check the chest.", 2.5f);
    }
}
