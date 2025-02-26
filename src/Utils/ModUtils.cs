using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using System.Linq;

namespace TabletopGames.Utils
{
    public static class ModUtils
    {
        public static ItemStack GenItemstack(this CollectibleObject collobj, ICoreAPI api, string json)
        {
            var jstack = new JsonItemStack();

            switch (json)
            {
                case null or "":
                    jstack = new JsonItemStack()
                    {
                        Code = collobj.Code,
                        Type = EnumItemClass.Item
                    };
                    break;

                case not null:
                    jstack = new JsonItemStack()
                    {
                        Code = collobj.Code,
                        Type = EnumItemClass.Item,
                        Attributes = new JsonObject(JToken.Parse(json))
                    };
                    break;
            }

            jstack.Resolve(api.World, "some type");

            return jstack.ResolvedItemstack;
        }

        public static Vec3f GetPositionOnBoard(this int index, int width, int height, float distanceBetweenSlots, float fromBorderX, float fromBorderZ)
        {
            var dX = Math.Floor((double)(index / width));
            var dZ = index % height;
            return new Vec3f((float)(fromBorderX + (distanceBetweenSlots * dX)), 0f, fromBorderZ - (distanceBetweenSlots * dZ));
        }

        public static void AppendWoodText(this StringBuilder dsc, ItemStack stack)
        {
            var woodType = stack.Attributes.GetString("wood");

            if (woodType != null) dsc.Append(Lang.Get("Wood")).Append(": ").AppendLine(Lang.Get($"material-{woodType}"));
        }

        public static void AppendWoodText(this StringBuilder dsc, string wood)
        {
            dsc.Append(Lang.Get("Wood")).Append(": ").AppendLine(Lang.Get($"material-{wood}"));
        }

        public static void AppendInventorySlotsText(this StringBuilder dsc, ItemStack stack)
        {
            var slots = stack.Attributes.GetAsInt("quantitySlots");

            if (slots != 0) dsc.AppendFormat(Lang.Get("Quantity Slots: {0}", slots)).AppendLine();
        }

        public static void AppendInventorySlotsText(this StringBuilder dsc, int quantitySlots)
        {
            if (quantitySlots != 0) dsc.AppendFormat(Lang.Get("Quantity Slots: {0}", quantitySlots)).AppendLine();
        }

        public static void AppendSelectedSlotText(this StringBuilder dsc, CollectibleObject collobj, IPlayer forPlayer, InventoryBase inventory, bool withSlotId = true, bool withStackName = true)
        {
            if (inventory == null || inventory.Count == 0) return;

            var selBoxIndex = forPlayer.CurrentBlockSelection.SelectionBoxIndex;

            if (collobj.GetIgnoredSelectionBoxIndexes()?.Contains(selBoxIndex) == true) return;

            if (withSlotId) dsc.AppendFormat($"[{inventory.GetSlotId(inventory?[selBoxIndex])}] ");
            if (withStackName) dsc.Append(inventory?[selBoxIndex].GetStackName() ?? Lang.Get("Empty"));
        }

        public static int[] GetIgnoredSelectionBoxIndexes(this CollectibleObject collobj) => collobj.Attributes?["tabletopgames"]["ignoreSelectionBoxIndexes"].AsArray<int>();

        public static AssetLocation TryGetTexturePath(this ItemStack stack, KeyValuePair<string, CompositeTexture> key)
        {
            var textures = (stack.Collectible as Item)?.Textures ?? (stack.Collectible as Block)?.Textures;

            if (stack.HasKeyAsAttribute(key, "color")) return textures[stack.Attributes.GetString("color")].Base;
            if (stack.HasKeyAsAttribute(key, "color1")) return textures[stack.Attributes.GetString("color1")].Base;
            if (stack.HasKeyAsAttribute(key, "color2")) return textures[stack.Attributes.GetString("color2")].Base;

            if (stack.HasKeyAsAttribute(key, "back")) return new AssetLocation(stack.GetTexturePath(key.Key) + ".png");
            if (stack.HasKeyAsAttribute(key, "face")) return new AssetLocation(stack.GetTexturePath(key.Key) + ".png");
            if (stack.HasKeyAsAttribute(key, "rank")) return new AssetLocation(stack.GetTexturePath(key.Key) + ".png");
            if (stack.HasKeyAsAttribute(key, "suit")) return new AssetLocation(stack.GetTexturePath(key.Key) + ".png");

            if (stack.HasKeyAsAttribute(key, "wood")) return new AssetLocation(stack.GetTexturePath(key.Key, "oak") + ".png");
            if (stack.HasKeyAsAttribute(key, "dark")) return new AssetLocation(stack.GetTexturePath(key.Key, "black") + ".png");
            if (stack.HasKeyAsAttribute(key, "light")) return new AssetLocation(stack.GetTexturePath(key.Key, "white") + ".png");

            if (stack.Collectible?.Attributes?["tabletopgames"]?["playingcard"]?.AsObject<PlayingCardData>() is PlayingCardData cardData and not null)
            {
                if (cardData.Backs.Contains(key.Key) && stack.Attributes.HasAttribute("back")) return new AssetLocation(stack.GetTexturePath("back") + ".png");
                if (cardData.Faces.Contains(key.Key) && stack.Attributes.HasAttribute("face")) return new AssetLocation(stack.GetTexturePath("face") + ".png");
                if (cardData.Ranks.Contains(key.Key) && stack.Attributes.HasAttribute("rank")) return new AssetLocation(stack.GetTexturePath("rank") + ".png");
                if (cardData.Suits.Contains(key.Key) && stack.Attributes.HasAttribute("suit")) return new AssetLocation(stack.GetTexturePath("suit") + ".png");
            }

            return textures[key.Key].Base;
        }

        private static string GetTexturePath(this ItemStack stack, string key) => stack.Collectible.GetTextureLocationPrefix(key) + stack.Attributes.GetString(key);
        private static string GetTexturePath(this ItemStack stack, string key, string defaultKey) => stack.Collectible.GetTextureLocationPrefix(key) + stack.Attributes.GetString(key, defaultKey);

        public static string GetTextureLocationPrefix(this CollectibleObject collobj, string key) => collobj.Attributes["texturePrefixes"][key].AsString();

        private static bool HasKeyAsAttribute(this ItemStack stack, KeyValuePair<string, CompositeTexture> key, string compare)
        {
            return key.Key == compare && stack.Attributes.HasAttribute(compare);
        }

        public static AssetLocation GetShapePath(this CollectibleObject collobj)
        {
            var shapeBase = (collobj as Item)?.Shape.Base ?? (collobj as Block)?.Shape.Base;
            return new(shapeBase.Domain, "shapes/" + shapeBase.Path + ".json");
        }

        public static bool TryPickup(this Block block, BlockEntityContainer blockEntity, IWorldAccessor world, IPlayer byPlayer)
        {
            if (blockEntity.Inventory == null) return false;
            if (!byPlayer.Entity.Controls.ShiftKey) return false;
            if (!byPlayer.Entity.Controls.CtrlKey) return false;

            var blockStack = block.OnPickBlock(world, blockEntity.Pos);

            if (!byPlayer.InventoryManager.TryGiveItemstack(blockStack, true))
            {
                world.SpawnItemEntity(blockStack, blockEntity.Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }

            world.BlockAccessor.SetBlock(0, blockEntity.Pos);
            return true;
        }

        public static void ApplyStackRotation(this ITreeAttribute stackAttributes, IPlayer byPlayer, Block block)
        {
            var facing = BlockFacing.HorizontalFromAngle(GameMath.Mod(byPlayer.Entity.Pos.Yaw, (float)Math.PI * 2f));
            var side = block?.Variant?["side"];

            stackAttributes.RemoveAttribute("rotation");
            if (side == "east" && facing == BlockFacing.EAST) stackAttributes.SetInt("rotation", 180);
            if (side == "east" && facing == BlockFacing.NORTH) stackAttributes.SetInt("rotation", 270);
            if (side == "east" && facing == BlockFacing.WEST) stackAttributes.SetInt("rotation", 0);
            if (side == "east" && facing == BlockFacing.SOUTH) stackAttributes.SetInt("rotation", 90);

            if (side == "north" && facing == BlockFacing.EAST) stackAttributes.SetInt("rotation", 90);
            if (side == "north" && facing == BlockFacing.NORTH) stackAttributes.SetInt("rotation", 180);
            if (side == "north" && facing == BlockFacing.WEST) stackAttributes.SetInt("rotation", 270);
            if (side == "north" && facing == BlockFacing.SOUTH) stackAttributes.SetInt("rotation", 0);

            if (side == "west" && facing == BlockFacing.EAST) stackAttributes.SetInt("rotation", 0);
            if (side == "west" && facing == BlockFacing.NORTH) stackAttributes.SetInt("rotation", 90);
            if (side == "west" && facing == BlockFacing.WEST) stackAttributes.SetInt("rotation", 180);
            if (side == "west" && facing == BlockFacing.SOUTH) stackAttributes.SetInt("rotation", 270);

            if (side == "south" && facing == BlockFacing.EAST) stackAttributes.SetInt("rotation", 270);
            if (side == "south" && facing == BlockFacing.NORTH) stackAttributes.SetInt("rotation", 0);
            if (side == "south" && facing == BlockFacing.WEST) stackAttributes.SetInt("rotation", 90);
            if (side == "south" && facing == BlockFacing.SOUTH) stackAttributes.SetInt("rotation", 180);


            if (side == null && facing == BlockFacing.EAST) stackAttributes.SetInt("rotation", 270);
            if (side == null && facing == BlockFacing.NORTH) stackAttributes.SetInt("rotation", 0);
            if (side == null && facing == BlockFacing.WEST) stackAttributes.SetInt("rotation", 90);
            if (side == null && facing == BlockFacing.SOUTH) stackAttributes.SetInt("rotation", 180);

        }

        public static void RotateClockwise(this ItemStack stack)
        {
            var rotation = stack.Attributes.GetInt("rotation");
            rotation += 90;
            if (rotation == 360) rotation = 0;

            stack.Attributes.SetInt("rotation", rotation);
        }

        public static void RotateAntiClockwise(this ItemStack stack)
        {
            var rotation = stack.Attributes.GetInt("rotation");
            rotation -= 90;
            if (rotation < 0) rotation = 270;

            stack.Attributes.SetInt("rotation", rotation);
        }

        public static void TryChangeSizeVariant(this ItemStack stack, IPlayer byPlayer, int index, Dictionary<string, int> sizes)
        {
            var api = byPlayer.Entity.Api;
            if (stack.Collectible.Variant?["size"] == null) return;
            var sizeVariants = sizes.Keys.ToList();
            var sizeQuantitySlots = sizes.Values.ToList();

            stack.TryDropAllSlots(byPlayer, api);

            var clonedAttributes = stack.Attributes.Clone();

            var newStack = new ItemStack(api.World.GetBlock(stack.Collectible.CodeWithVariant("size", sizeVariants[index])))
            {
                Attributes = clonedAttributes
            };

            newStack.Attributes.SetInt("quantitySlots", sizeQuantitySlots[index]);

            stack.SetFrom(newStack);
        }
    }
}