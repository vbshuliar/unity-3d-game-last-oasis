using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType { GreenPotion }

public class ItemPickup : MonoBehaviour
{
    [Header("Item Settings")]
    public ItemType itemType;

    [Header("Green Potion Settings")]
    [SerializeField] float sizeMultiplier = 2f;
    [SerializeField] float speedMultiplier = 2f;
    [SerializeField] float duration = 5f;

    public void ApplyEffect(PlayerController player)
    {
        switch (itemType)
        {
            case ItemType.GreenPotion:
                player.ApplyGreenPotionEffect(sizeMultiplier, speedMultiplier, duration);
                break;
        }
    }

    public float GetSizeMultiplier() => sizeMultiplier;
    public float GetSpeedMultiplier() => speedMultiplier;
    public float GetDuration() => duration;
}
