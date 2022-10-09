using Vintagestory.API.Common;

namespace TabletopGames
{
    public class ItemSlotGoBoard : ItemSlot
    {
        public ItemSlotGoBoard(InventoryBase inventory) : base(inventory)
        {
        }

        public override int MaxSlotStackSize => 1;

        public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
        {
            return sourceSlot.Itemstack.Collectible is ItemGoStone;
        }

        public override bool CanHold(ItemSlot sourceSlot)
        {
            return sourceSlot.Itemstack.Collectible is ItemGoStone;
        }
    }
}