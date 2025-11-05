using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum InteractableType { Enemy, Item }

public class Interactable : MonoBehaviour
{
    public Actor myActor { get; private set; }

    public InteractableType interactionType;

    void Awake() 
    {
        if(interactionType == InteractableType.Enemy)
        { myActor = GetComponent<Actor>(); }
    }

    public void InteractWithItem(PlayerController player)
    {
        // Check if this item has a pickup effect
        ItemPickup itemPickup = GetComponent<ItemPickup>();
        if (itemPickup != null)
        {
            itemPickup.ApplyEffect(player);
        }

        // Pickup Item
        Destroy(gameObject);
    }
}
