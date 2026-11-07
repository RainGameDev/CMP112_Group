using UnityEngine;

public class ChestInteractable : Interactable
{
    public Animator anim;

    public override void Interact()
    {
        base.Interact();
        anim.SetBool("open", true);
        Destroy(interactionUI);
        Debug.Log("Interacted with chest");
    }
}
