using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using TabletopGames.Utils;
using Vintagestory.API.Util;
using System.Linq;
using Vintagestory.API.Client;

namespace TabletopGames
{
    public class BlockChessBoard : BlockWithAttributes
    {
        public Item boxItem;
        public BoardData BoardData => Attributes["tabletopgames"]["board"].AsObject<BoardData>();

        public override bool SaveInventory => true;
        public override bool HasWoodType => true;
        public override bool HasCheckerboardTypes => true;
        public override bool CanBePickedUp => true;
        public override string MeshRefName => "tableTopGames_ChessBoard_Meshrefs";

        public int CurrentMeshRefid => GetHashCode();

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            boxItem = api.World.GetItem(new AssetLocation(Attributes["tabletopgames"]?["packTo"].AsString()));
            skillItems = capi.GetBoxToolModes("pack")
                .Append(capi.GetDropAllSlotsToolModes())
                .Append(capi.GetSizeVariantsToolModes(this))
                .Append(capi.GetCheckerBoardToolModes(this));
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
        {
            var stack = slot.Itemstack;
            var sizeVariants = BoardData.Sizes.Keys.ToList();
            var sizeQuantitySlots = BoardData.Sizes.Values.ToList();

            var colors1 = BoardData.DarkVariants.Keys.ToList();
            var colors2 = BoardData.LightVariants.Keys.ToList();

            if (toolMode == 0)
            {
                var boxStack = boxItem?.GenItemstack(api, null);
                if (boxStack.ResolveBlockOrItem(api.World))
                {
                    slot.ConvertBlockToItemBox(boxStack, "containedStack");
                }
            }
            else if (toolMode == 1)
            {
                stack.TryDropAllSlots(byPlayer, api);
            }
            else if (toolMode <= sizeVariants.Count + 1)
            {
                stack.TryChangeSizeVariant(byPlayer, toolMode - 2, BoardData.Sizes);
            }
            else if (toolMode <= sizeVariants.Count + colors1.Count + 1)
            {
                stack.Attributes.SetString("dark", colors1[toolMode - sizeVariants.Count - 2]);
            }
            else if (toolMode <= sizeVariants.Count + colors1.Count + colors2.Count + 2)
            {
                stack.Attributes.SetString("light", colors2[toolMode - colors1.Count - sizeVariants.Count - 2]);
            }

            slot.MarkDirty();
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not BEChessBoard blockEntity) return false;

            var i = blockSel.SelectionBoxIndex;
            return (this.GetIgnoredSelectionBoxIndexes()?.Contains(i)) switch
            {
                true => this.TryPickup(blockEntity, world, byPlayer) || base.OnBlockInteractStart(world, byPlayer, blockSel),
                _ => this.TryPickup(blockEntity, world, byPlayer) || blockEntity.TryPut(byPlayer, i, true) || blockEntity.TryTake(byPlayer, i),
            };
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            var original = base.OnPickBlock(world, pos);
            if (world.BlockAccessor.GetBlockEntity(pos) is not BEChessBoard blockEntity) return original;
            return OnPickBlock(
                world,
                pos,
                blockEntity.inventory,
                blockEntity.woodType,
                blockEntity.quantitySlots,
                true,
                blockEntity.darkType,
                blockEntity.lightType);
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack stack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            var meshrefid = stack.Attributes.GetInt("meshRefId");
            if (meshrefid == CurrentMeshRefid || !Meshrefs.TryGetValue(meshrefid, out renderinfo.ModelRef))
            {
                var num = Meshrefs.Count + 1;
                var value = capi.Render.UploadMesh(GenMesh(stack, capi.BlockTextureAtlas, null));
                renderinfo.ModelRef = Meshrefs[num] = value;
                stack.Attributes.SetInt("meshRefId", num);
            }
        }

        public override string GetMeshCacheKey(ItemStack stack)
        {
            string wood = stack.Attributes.GetString("wood", defaultValue: "oak");
            string dark = stack.Attributes.GetString("dark", defaultValue: "black");
            string light = stack.Attributes.GetString("light", defaultValue: "white");

            string size = VariantStrict?["size"];
            string side = VariantStrict?["side"];

            return Code.ToShortString() + "-" + size + "-" + side + "-" + wood + "-" + dark + "-" + light;
        }
    }
}