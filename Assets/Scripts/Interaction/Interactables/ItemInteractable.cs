using UnityEngine;

public class ItemInteractable : Interactable
{
    public ContainerItem item;
    public GameObject player;

    void Start()
    {
        player = GameObject.FindWithTag("Player");
    }

    public override void Interact()
    {
        base.Interact();
        player.GetComponent<Container>().AddItem(item);
        Destroy(gameObject);
    }
}
