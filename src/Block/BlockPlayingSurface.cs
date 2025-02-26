using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TabletopGames
{
    public class BlockPlayingSurface : Block
    {
        public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos) => false;
        // public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos) => null;

        public override Vec4f GetSelectionColor(ICoreClientAPI capi, BlockPos pos) => new(1, 1, 0, 1); // Yellow

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not BEPlayingSurface blockEntity) return false;

            var i = blockSel.SelectionBoxIndex;
            var targetSlot = blockEntity.inventory[i];
            var playerSlot = byPlayer.InventoryManager.ActiveHotbarSlot;

            if (playerSlot.Empty) return blockEntity.TryTake(byPlayer, i);

            if (targetSlot.Empty) return blockEntity.TryPut(byPlayer, i, true);

            if (targetSlot.Itemstack.Collectible is ItemPlayingCard && playerSlot.Itemstack.Collectible is ItemPlayingCard)
                return blockEntity.TryCreate(byPlayer, i);

            if (targetSlot.Itemstack.Collectible is ItemPlayingCards && playerSlot.Itemstack.Collectible is ItemPlayingCard)
                return blockEntity.TryMerge(byPlayer, i);

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            var original = base.OnPickBlock(world, pos);
            if (world.BlockAccessor.GetBlockEntity(pos) is not BEPlayingSurface blockEntity) return original;

            var blockStack = new ItemStack(this);
            blockStack.Attributes.SetInt("quantitySlots", blockEntity.quantitySlots);
            return blockStack;
        }

        // public bool CreateSurface(IWorldAccessor world, BlockSelection blockSel, IPlayer player)
        // {
        //     if (!world.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
        //     {
        //         player.InventoryManager.ActiveHotbarSlot.MarkDirty();
        //         return false;
        //     }

        //     BlockPos pos;
        //     if (blockSel.Face == null) pos = blockSel.Position;
        //     else pos = blockSel.Position.AddCopy(blockSel.Face);

        //     var belowBlock = world.BlockAccessor.GetBlock(pos.DownCopy());
        //     if (!belowBlock.CanAttachBlockAt(world.BlockAccessor, this, pos.DownCopy(), BlockFacing.UP) && belowBlock != this) return false;

        //     if (!player.Entity.Controls.CtrlKey) return false;

        //     world.BlockAccessor.SetBlock(BlockId, pos);
        //     return true;
        // }
    }
}