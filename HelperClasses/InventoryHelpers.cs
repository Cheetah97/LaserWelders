using Sandbox.Game;
using Sandbox.Game.Entities;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;

namespace Cheetah.LaserTools
{
    public static class InventoryHelpers
    {
        public static VRage.MyFixedPoint ComputeAmountThatFits(this VRage.Game.ModAPI.Ingame.IMyInventory Inventory, VRage.Game.ModAPI.Ingame.IMyInventoryItem Item, VRage.MyFixedPoint? Amount = null)
        {
            var Id = Item.Content.GetId();
            var ActualAmount = Amount.HasValue ? Amount.Value : Item.Amount;
            var FittingAmount = (Inventory as MyInventory).ComputeAmountThatFits(Id);
            return FittingAmount > ActualAmount ? ActualAmount : FittingAmount;
        }

        public static void PickupItem(this IMyInventory Inventory, IMyFloatingObject FloatingObject)
        {
            (Inventory as MyInventory).TakeFloatingObject(FloatingObject as MyFloatingObject);
        }

        public static VRage.MyFixedPoint GetAmount(this IMyFloatingObject FloatingObject)
        {
            return (FloatingObject as MyFloatingObject).Amount;
        }

        /*public static bool PullAny(this IMyInventory Inventory, List<IMyCubeBlock> sourceInventories, string component, int count)
        {
            return PullAny(Inventory, sourceInventories, new Dictionary<string, int> { { component, count } });
        }

        // (c) Rexxar
        public static bool PullAny(this IMyInventory Inventory, List<IMyCubeBlock> sourceInventories, Dictionary<string, int> toPull)
        {
            bool result = false;
            MyInventory inventory = Inventory as MyInventory;
            foreach (KeyValuePair<string, int> entry in toPull)
            {
                int remainingAmount = entry.Value;
                if (remainingAmount <= 0) continue;
                //Logging.Instance.WriteDebug(entry.Key + entry.Value);
                foreach (IMyCubeBlock block in sourceInventories)
                {
                    if (block == null || block.Closed)
                        continue;

                    MyInventory sourceInventory;
                    //get the output inventory for production blocks
                    if (((MyEntity)block).InventoryCount > 1)
                        sourceInventory = ((MyEntity)block).GetInventory(1);
                    else
                        sourceInventory = ((MyEntity)block).GetInventory();

                    List<MyPhysicalInventoryItem> sourceItems = sourceInventory.GetItems();
                    if (sourceItems.Count == 0)
                        continue;

                    var toMove = new List<KeyValuePair<MyPhysicalInventoryItem, int>>();
                    foreach (MyPhysicalInventoryItem item in sourceItems)
                    {
                        if (item.Content.SubtypeName == entry.Key)
                        {
                            if (item.Amount <= 0) //KEEEN
                                continue;

                            if (item.Amount >= remainingAmount)
                            {
                                toMove.Add(new KeyValuePair<MyPhysicalInventoryItem, int>(item, remainingAmount));
                                remainingAmount = 0;
                                result = true;
                            }
                            else
                            {
                                remainingAmount -= (int)item.Amount;
                                toMove.Add(new KeyValuePair<MyPhysicalInventoryItem, int>(item, (int)item.Amount));
                                result = true;
                            }
                        }
                    }

                    foreach (KeyValuePair<MyPhysicalInventoryItem, int> itemEntry in toMove)
                    {
                        if (inventory.ComputeAmountThatFits(itemEntry.Key.Content.GetId()) < itemEntry.Value)
                            return false;

                        sourceInventory.Remove(itemEntry.Key, itemEntry.Value);
                        inventory.Add(itemEntry.Key, itemEntry.Value);
                    }

                    if (remainingAmount == 0)
                        break;
                }
            }

            return result;
        }

        public static bool PullAny(this IMyInventory Inventory, List<IMyCubeBlock> sourceInventories, MyDefinitionId item, float amount)
        {
            return PullAny(Inventory, sourceInventories, new Dictionary<MyDefinitionId, float> { { item, amount } });
        }

        public static bool PullAny(this IMyInventory Inventory, List<IMyCubeBlock> sourceInventories, Dictionary<MyDefinitionId, float> toPull)
        {
            bool result = false;
            MyInventory inventory = Inventory as MyInventory;
            foreach (KeyValuePair<MyDefinitionId, float> entry in toPull)
            {
                VRage.MyFixedPoint remainingAmount = (VRage.MyFixedPoint)entry.Value;
                //Logging.Instance.WriteDebug(entry.Key + entry.Value);
                foreach (IMyCubeBlock block in sourceInventories)
                {
                    if (block == null || block.Closed)
                        continue;

                    MyInventory sourceInventory;
                    //get the output inventory for production blocks
                    if (((MyEntity)block).InventoryCount > 1)
                        sourceInventory = ((MyEntity)block).GetInventory(1);
                    else
                        sourceInventory = ((MyEntity)block).GetInventory();

                    List<MyPhysicalInventoryItem> sourceItems = sourceInventory.GetItems();
                    if (sourceItems.Count == 0)
                        continue;

                    var toMove = new List<KeyValuePair<MyPhysicalInventoryItem, VRage.MyFixedPoint>>();
                    foreach (MyPhysicalInventoryItem item in sourceItems)
                    {
                        if (item.Content.GetId() == entry.Key)
                        {
                            if (item.Amount <= 0) //KEEEN
                                continue;

                            if (item.Amount >= remainingAmount)
                            {
                                toMove.Add(new KeyValuePair<MyPhysicalInventoryItem, VRage.MyFixedPoint>(item, remainingAmount));
                                remainingAmount = 0;
                                result = true;
                            }
                            else
                            {
                                remainingAmount -= (int)item.Amount;
                                toMove.Add(new KeyValuePair<MyPhysicalInventoryItem, VRage.MyFixedPoint>(item, item.Amount));
                                result = true;
                            }
                        }
                    }

                    foreach (KeyValuePair<MyPhysicalInventoryItem, VRage.MyFixedPoint> itemEntry in toMove)
                    {
                        if (inventory.ComputeAmountThatFits(itemEntry.Key.Content.GetId()) < itemEntry.Value)
                            return false;

                        sourceInventory.Remove(itemEntry.Key, itemEntry.Value);
                        inventory.Add(itemEntry.Key, itemEntry.Value);
                    }

                    if (remainingAmount == 0)
                        break;
                }
            }

            return result;
        }
        */
    }
}
