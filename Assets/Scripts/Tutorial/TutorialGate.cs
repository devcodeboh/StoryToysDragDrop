using System.Linq;
using UnityEngine;

namespace StoryToys.DragDrop
{
    public static class TutorialGate
    {
        public static bool Active;
        public static ItemController AllowedItem; // if null and Active, no pick allowed
        public static bool AllowAnyPick;
        public static EquipSlot AllowedDropSlot; // if null and Active, drop anywhere allowed

        public static void Reset()
        {
            Active = false;
            AllowedItem = null;
            AllowAnyPick = false;
            AllowedDropSlot = null;
        }

        public static bool AllowPick(ItemController item)
        {
            if (!Active) return true;
            if (AllowAnyPick) return true;
            // If no specific item is gated, allow any pick
            if (AllowedItem == null) return true;
            return item != null && item == AllowedItem;
        }

        public static bool AllowDrop(Vector3 worldPos)
        {
            if (!Active) return true;
            if (AllowedDropSlot == null) return true;
            var hits = Physics2D.OverlapPointAll(worldPos);
            for (int i = 0; i < hits.Length; i++)
            {
                var slot = hits[i].GetComponentInParent<EquipSlot>();
                if (slot != null && slot == AllowedDropSlot) return true;
            }
            return false;
        }
    }
}
