
using Containers;
using System;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace Protocol
{
    public abstract class Inventory : System.IDisposable
    {
        private bool _disposed = false;

        //private readonly InventoryRenderer _Renderer = new();  // Disposable
        public readonly int TotalSlotCount;

        private int _count = 0;
        public int Count => _count;
        protected readonly Item?[] _items;

        public System.Collections.Generic.IEnumerable<Item?> Items
        {
            get
            {
                System.Diagnostics.Debug.Assert(!_disposed);
                return _items;
            }
        }

        public Inventory(int totalCount)
        {
            TotalSlotCount = totalCount;

            _items = new Item?[totalCount];
            System.Array.Fill(_items, null);
        }

        ~Inventory() => System.Diagnostics.Debug.Assert(false);

        internal virtual (bool, Item?) TakeAll(int index, SlotData slotData)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(_count >= 0);
            System.Diagnostics.Debug.Assert(_count <= TotalSlotCount);
            System.Diagnostics.Debug.Assert(index >= 0);
            System.Diagnostics.Debug.Assert(index < TotalSlotCount);

            bool f;

            Item? itemTaked = _items[index];
            _items[index] = null;

            if (itemTaked != null)
            {
                _count--;

                f = itemTaked.CompareWithPacketFormat(slotData);
            }
            else
            {
                f = (slotData.Id == -1);
            }

            return (f, itemTaked);
        }

        internal virtual (bool, Item?) PutAll(int index, Item itemCursor, SlotData slotData)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(_count >= 0);
            System.Diagnostics.Debug.Assert(_count <= TotalSlotCount);
            System.Diagnostics.Debug.Assert(index >= 0);
            System.Diagnostics.Debug.Assert(index < TotalSlotCount);

            bool f;

            Item? itemTaked = _items[index];

            if (itemTaked != null)
            {
                System.Diagnostics.Debug.Assert(!ReferenceEquals(itemTaked, itemCursor));

                f = itemTaked.CompareWithPacketFormat(slotData);

                _items[index] = itemCursor;

                if (itemCursor.Type == itemTaked.Type)
                {
                    int spend = itemCursor.Stack(itemTaked.Count);

                    if (spend == itemTaked.Count)
                    {
                        itemTaked = null;
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(spend < itemCursor.Count);
                        itemTaked.Spend(spend);
                    }
                }
                else
                {
                    
                }
            }
            else
            {
                _items[index] = itemCursor;

                _count++;

                f = (slotData.Id == -1);
            }
            
            return (f, itemTaked);
        }

        internal virtual (bool, Item?) TakeHalf(int index, SlotData slotData)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(_count >= 0);
            System.Diagnostics.Debug.Assert(_count <= TotalSlotCount);
            System.Diagnostics.Debug.Assert(index >= 0);
            System.Diagnostics.Debug.Assert(index < TotalSlotCount);

            bool f;

            Item? itemTaked = _items[index];

            if (itemTaked == null)
            {
                f = (slotData.Id == -1);
            }
            else
            {
                f = itemTaked.CompareWithPacketFormat(slotData);

                if (itemTaked.Count == 1)
                {
                    _items[index] = null;
                    _count--;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(itemTaked.Count > 1);

                    itemTaked = itemTaked.DivideHalf();
                    System.Diagnostics.Debug.Assert(itemTaked != null);
                }
            }

            return (f, itemTaked);
        }

        internal virtual (bool, Item?) PutOne(int index, Item itemCursor, SlotData slotData)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(_count >= 0);
            System.Diagnostics.Debug.Assert(_count <= TotalSlotCount);
            System.Diagnostics.Debug.Assert(index >= 0);
            System.Diagnostics.Debug.Assert(index < TotalSlotCount);

            bool f;

            Item? itemTaked = _items[index];

            if (itemTaked != null)
            {
                System.Diagnostics.Debug.Assert(!ReferenceEquals(itemTaked, itemCursor));

                f = itemTaked.CompareWithPacketFormat(slotData);

                _items[index] = itemCursor;

                if (itemCursor.Type == itemTaked.Type)
                {
                    if (itemCursor.Count == 1)
                    {
                        int spend = itemTaked.Stack(1);
                        if (spend == 1)
                        {
                            itemCursor.SetCount(itemTaked.Count);
                            itemTaked = null;
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(spend == 0);

                            int temp = itemCursor.Count;
                            itemCursor.SetCount(itemTaked.Count);
                            itemTaked.SetCount(temp);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.Assert(itemCursor.Count > 1);

                        int spend = itemTaked.Stack(1);
                        if (spend == 1)
                        {
                            itemCursor.Spend(1);
                        }
                        else
                        {
                            System.Diagnostics.Debug.Assert(spend == 0);

                        }

                        int temp = itemCursor.Count;
                        itemCursor.SetCount(itemTaked.Count);
                        itemTaked.SetCount(temp);
                    }

                    /*int temp = itemCursor.Count;
                    itemCursor.SetCount(itemTaked.Count);
                    itemTaked.SetCount(temp);*/
                }
                else
                {

                }

                return (f, itemTaked);
            }
            else
            {
                _items[index] = itemCursor;

                if (itemCursor.Count > 1)
                {
                    itemTaked = itemCursor.DivideExceptOne();
                }
                else
                {
                    System.Diagnostics.Debug.Assert(itemCursor.Count == 1);
                    System.Diagnostics.Debug.Assert(itemTaked == null);
                }

                _count++;

                f = (slotData.Id == -1);

                
            }

            return (f, itemTaked);
        }

        public void Print()
        {
            System.Console.WriteLine($"Count: {_count}");
            for (int i = 0; i < TotalSlotCount; ++i)
            {
                if (i % 9 == 0)
                {
                    System.Console.WriteLine();
                }

                Item? item = _items[i];
                if (item == null)
                {
                    System.Console.Write($"[{-1}, {0}]");
                    continue;
                }

                System.Console.Write($"[{item.Type}, {item.Count}]");
            }
            System.Console.WriteLine();
        }

        public virtual void Dispose()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            // Assertion.
            System.Diagnostics.Debug.Assert(_count == 0);

            // Release resources.

            // Finish.
            System.GC.SuppressFinalize(this);
            _disposed = true;
        }

        internal virtual void TakeOne(ItemCursor cursor, int index)
        {
            Item? slotItem = _items[index];
            System.Diagnostics.Debug.Assert(slotItem != null);

            if (slotItem.Count == 1)
            {
                TakeAll(cursor, index);
                return;
            }

            Item? cursorItem = cursor.GetItem();
            if (cursorItem == null)
            {
                slotItem.SetCount(slotItem.Count - 1);
                cursor.SetItem(new Item(slotItem.Type, 1));
                return;
            }

            if (cursorItem.Type != slotItem.Type)
            {
                return;
            }

            if (cursorItem.MaxCount == cursorItem.Count)
            {
                return;
            }

            cursorItem.SetCount(cursorItem.Count + 1);
            slotItem.SetCount(slotItem.Count - 1);
        }

        internal virtual void TakeHalf(ItemCursor cursor, int index)
        {
            Item? cursorItem = cursor.GetItem();
            System.Diagnostics.Debug.Assert(cursorItem == null);

            Item? slotItem = _items[index];
            if (slotItem == null)
            {
                return;
            }

            if (slotItem.Count == 1)
            {
                TakeAll(cursor, index);
                return;
            }

            int remain = slotItem.Count / 2;
            int count = slotItem.Count - remain;
            slotItem.SetCount(remain);
            cursor.SetItem(new Item(slotItem.Type, count));
            return;
        }

        internal virtual void TakeAll(ItemCursor cursor, int index)
        {
            Item? slotItem = _items[index];
            System.Diagnostics.Debug.Assert(slotItem != null);

            Item? cursorItem = cursor.GetItem();
            if (cursorItem == null)
            {
                cursor.SetItem(slotItem);
                _items[index] = null;
                SubCount();
                return ;
            }

            if (cursorItem.Type != slotItem.Type)
            {
                return;
            }

            if (cursorItem.MaxCount == cursorItem.Count)
            {
                return;
            }

            int blank = cursorItem.MaxCount - cursorItem.Count;
            if (slotItem.Count > blank)
            {
                cursorItem.SetCount(cursorItem.Count + blank);
                slotItem.SetCount(slotItem.Count - blank);
                return;
            }

            cursorItem.SetCount(cursorItem.Count + slotItem.Count);
            _items[index] = null;
            SubCount();
        }

        internal virtual void PutOne(ItemCursor cursor, int index)
        {
            Item? cursorItem = cursor.GetItem();
            System.Diagnostics.Debug.Assert(cursorItem != null);

            if (cursorItem.Count == 1)
            {
                PutAll(cursor, index);
                return;
            }

            Item? slotItem = _items[index];
            if (slotItem == null)
            {
                _items[index] = new Item(cursorItem.Type, 1);
                cursorItem.SetCount(cursorItem.Count - 1);
                return;
            }

            if (cursorItem.Type != slotItem.Type)
            {
                return;
            }

            if (slotItem.MaxCount == slotItem.Count)
            {
                return;
            }

            cursorItem.SetCount(cursorItem.Count - 1);
            slotItem.SetCount(slotItem.Count + 1);
        }

        internal virtual void PutAll(ItemCursor cursor, int index)
        {
            Item? cursorItem = cursor.GetItem();
            System.Diagnostics.Debug.Assert(cursorItem != null);

            Item? slotItem = _items[index];
            if (slotItem == null)
            {
                _items[index] = cursorItem;
                cursor.SetItem(null);
                ++_count;
                return;
            }

            if (slotItem.Type != cursorItem.Type)
            {
                return;
            }

            if (slotItem.MaxCount == slotItem.Count)
            {
                return;
            }

            int n = slotItem.MaxCount - slotItem.Count;
            if (cursorItem.Count > n)
            {
                slotItem.SetCount(slotItem.Count + n);
                cursorItem.SetCount(cursorItem.Count - n);
                return;
            }

            slotItem.SetCount(slotItem.Count + cursorItem.Count);
            cursor.SetItem(null);
        }

        internal virtual void Swap(ItemCursor cursor, int index)
        {
            Item? cursorItem = cursor.GetItem();
            System.Diagnostics.Debug.Assert(cursorItem != null);

            Item? slotItem = _items[index];
            System.Diagnostics.Debug.Assert(slotItem != null);

            Item temp = cursorItem;
            cursor.SetItem(slotItem);
            _items[index] = temp;
            return;
        }

        internal virtual Item? GetItem(int index)
        {
            return _items[index];
        }

        internal virtual Item? TakeItem(int index)
        {
            Item? item = _items[index];
            if (item == null)
            {
                return null;
            }

            _items[index] = null;
            SubCount();
            return item;
        }

        internal virtual void PutItem(Item? item, int index)
        {
            System.Diagnostics.Debug.Assert(item != null);
            if (item == null)
            {
                return;
            }

            _items[index] = item;
            ++_count;
        }

        internal virtual bool Exists(int index)
        {
            return _items.GetValue(index) != null;
        }

        internal virtual SlotData GetSlotData(int index)
        {
            Item? slotItem = GetItem(index);
            SlotData slotData = (slotItem == null) ? new() : slotItem.ConvertToPacketFormat();
            return slotData;
        }


        internal void AddCount()
        {
            ++_count;
        }

        internal void SubCount()
        {
            --_count;
        }
    }

    internal sealed class SelfInventory : Inventory
    {
        private bool _disposed = false;

        public int PrimarySlotCount = 36;

        public System.Collections.Generic.IEnumerable<Item?> PrimaryItems
        {
            get
            {
                System.Diagnostics.Debug.Assert(!_disposed);
                for (int i = 9; i < 45; ++i)
                    yield return _items[i];
            }
        }

        public System.Collections.Generic.IEnumerable<Item?> NotPrimaryItems
        {
            get
            {
                System.Diagnostics.Debug.Assert(!_disposed);
                for (int i = 0; i < 9; ++i)
                    yield return _items[i];
            }
        }

        internal Item? OffhandItem
        {
            get
            {
                System.Diagnostics.Debug.Assert(!_disposed);
                return _items[45];
            }
        }

        public SelfInventory() : base(46) 
        {
            PutAll(9, new Item(Item.Types.Shield, 1), new(-1, 0));
            PutAll(10, new Item(Item.Types.DiamondHelmet, 1), new(-1, 0));
            PutAll(11, new Item(Item.Types.DiamondChestPlate, 1), new(-1, 0));
            PutAll(12, new Item(Item.Types.DiamondLeggins, 1), new(-1, 0));
            PutAll(13, new Item(Item.Types.DiamondBoots, 1), new(-1, 0));
            PutAll(14, new Item(Item.Types.planks, 64), new(-1, 0));
            PutAll(15, new Item(Item.Types.Grass, 64), new(-1, 0));
            PutAll(16, new Item(Item.Types.Stone, 64), new(-1, 0));
            PutAll(17, new Item(Item.Types.Stone, 64), new(-1, 0));
            PutAll(18, new Item(Item.Types.Stone, 32), new(-1, 0));
            PutAll(19, new Item(Item.Types.IronHelmet, 1), new(-1, 0));
            for (int index = 20; index <= 35; ++index)
            {
                PutAll(index, new Item(Item.Types.Stone, 1), new(-1, 0));
            }
            PutAll(36, new Item(Item.Types.DiamondHelmet, 1), new(-1, 0));
            PutAll(37, new Item(Item.Types.chest, 64), new(-1, 0));
        }

        ~SelfInventory() => System.Diagnostics.Debug.Assert(false);

        internal override (bool, Item?) TakeAll(int index, SlotData slotData)
        {
            if (index == 0)
            {
                return base.TakeAll(index, slotData);
            }
            else if (index > 0 && index <= 4)
            {
                return base.TakeAll(index, slotData);
            }
            else if (index > 4 && index <= 8)
            {
                return base.TakeAll(index, slotData);
            }
            else
            {
                return base.TakeAll(index, slotData);
            }
        }

        internal override (bool, Item?) PutAll(int index, Item itemCursor, SlotData slotData)
        {
            if (index == 0)
            {
                bool f;
                Item? itemTaked = _items[index];
                if (itemTaked != null)
                {
                    f = itemTaked.CompareWithPacketFormat(slotData);
                }
                else
                {
                    if (slotData.Id == -1)
                        f = true;
                    else
                        f = false;
                }

                return (f, itemCursor);
            }
            else if (index > 0 && index <= 4)
            {
                throw new System.NotImplementedException();
            }
            else if (index > 4 && index <= 8)
            {
                throw new System.NotImplementedException();
            }
            else
            {
                return base.PutAll(index, itemCursor, slotData);
            }
        }

        public void DistributeItem(int[] indexes, Item item)
        {
            throw new System.NotImplementedException();
        }

        public void CollectItems()
        {
            throw new System.NotImplementedException();
        }

        public override void Dispose()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            // Assertion.

            // Release resources.

            // Finish.
            base.Dispose();
            _disposed = true;
        }

        public bool IsCanMoveShieldOrArmorSlot(Item clickItem)
        {
            if (clickItem.IsHelmet)
            {
                return Exists(5) == false;
            }

            if (clickItem.IsChestPlate)
            {
                return Exists(6) == false;
            }

            if (clickItem.IsLeggins)
            {
                return Exists(7) == false;
            }

            if (clickItem.IsBoots)
            {
                return Exists(8) == false;
            }

            if (clickItem.IsShield)
            {
                return Exists(45) == false;
            }

            return false;
        }

        public int GetArmorIndex(Item item)
        {
            if (item == null)
            {
                return -1;
            }

            if (item.IsHelmet)
            {
                return 5;
            }
            
            if (item.IsChestPlate)
            {
                return 6;
            }

            if (item.IsLeggins)
            {
                return 7;
            }

            if (item.IsBoots)
            {
                return 8;
            }

            if (item.IsShield)
            {
                return 45;
            }

            return -1;
        }

        private void MoveToShieldOrArmor(Item clickItem)
        {
            int index = GetArmorIndex(clickItem);

            System.Diagnostics.Debug.Assert(index != -1);

            _items.SetValue(clickItem, index);
        }

        private void MoveToEmptyInventory(Item clickItem)
        {
            for (int index = 9; index <= 44; ++index)
            {
                if (!Exists(index))
                {
                    _items.SetValue(clickItem, index);
                    return;
                }
            }
        }

        internal void MoveToInventory(Item clickItem)
        {
            for (int index = 9; index <= 44; ++index)
            {
                if (clickItem.Count == 1)
                {
                    break;
                }

                Item? slotItem = (Item?)_items.GetValue(index);
                if (slotItem == null)
                {
                    continue;
                }

                if (slotItem.Type != clickItem.Type)
                {
                    continue;
                }

                if (slotItem.Count == slotItem.MaxCount)
                {
                    continue;
                }


                int blank = slotItem.MaxCount - slotItem.Count;

                if (clickItem.Count > blank)
                {
                    int remain = clickItem.Count - blank;
                    slotItem.SetCount(slotItem.MaxCount);
                    clickItem.SetCount(remain);
                    continue;
                }

                slotItem.SetCount(slotItem.Count + clickItem.Count);
                return;
            }

            MoveToEmptyInventory(clickItem);
        }

        private void MoveToEptyMainInventory(Item clickItem)
        {
            for (int index = 9; index <= 35; ++index)
            {
                Item? slotItem = (Item?)_items.GetValue(index);
                if (slotItem == null)
                {
                    _items.SetValue(clickItem, index);
                    return;
                }
            }
        }

        private void  MoveToMainInventory(Item clickItem)
        {
            for (int index = 9; index <= 35; ++index)
            {
                if (clickItem.MaxCount == 1)
                {
                    break;
                }

                Item? slotItem = (Item?)_items.GetValue(index);
                if (slotItem == null)
                {
                    continue;
                }

                if (slotItem.Type != clickItem.Type)
                {
                    continue;
                }

                if (slotItem.Count == slotItem.MaxCount)
                {
                    continue;
                }


                int blank = slotItem.MaxCount - slotItem.Count;

                if (clickItem.Count > blank)
                {
                    int remain = clickItem.Count - blank;
                    slotItem.SetCount(slotItem.MaxCount);
                    clickItem.SetCount(remain);
                    continue;
                }

                slotItem.SetCount(slotItem.Count + clickItem.Count);
                return;
            }

            MoveToEptyMainInventory(clickItem);
        }

        private void MoveToEmptyHotBar(Item clickItem)
        {
            for (int index = 36; index <= 44; ++index)
            {
                Item? slotItem = _items[index];
                if (slotItem == null)
                {
                    _items.SetValue(clickItem, index);
                    return;
                }
            }
        }

        private void MoveToHotBar(Item clickItem)
        {
            for (int index = 36; index <= 44; ++index)
            {
                if (clickItem.Count == 1)
                {
                    break;
                }

                Item? slotItem = _items[index];
                if (slotItem == null)
                {
                    continue;
                }

                if (slotItem.Type != clickItem.Type)
                {
                    continue;
                }

                if (slotItem.Count == slotItem.MaxCount)
                {
                    continue;
                }

                int blank = slotItem.MaxCount - slotItem.Count;

                if (clickItem.Count > blank)
                {
                    int remain = clickItem.Count - blank;
                    slotItem.SetCount(slotItem.MaxCount);
                    clickItem.SetCount(remain);
                    continue;
                }

                slotItem.SetCount(slotItem.Count + clickItem.Count);
                return;
            }

            MoveToEmptyHotBar(clickItem);
        }

        private void MoveToEndOfEmptyInventory(Item clickItem)
        {
            for (int index = 44; index >= 9; --index)
            {
                if (Exists(index))
                {
                    continue;
                }

                _items[index] = clickItem;
                AddCount();
                return;
            }
        }

        internal void MoveToEndOfInventory(Item clickItem)
        {
            for (int index = 44; index >= 9; --index)
            {
                if (clickItem.MaxCount == 1)
                {
                    break;
                }

                Item? slotItem = _items[index];
                if (slotItem == null)
                {
                    continue;
                }

                if (slotItem.Type != clickItem.Type)
                {
                    continue;
                }

                if (slotItem.Count == slotItem.MaxCount)
                {
                    continue;
                }

                int blank = slotItem.MaxCount - slotItem.Count;

                if (clickItem.Count > blank)
                {
                    int remain = clickItem.Count - blank;
                    slotItem.SetCount(slotItem.MaxCount);
                    clickItem.SetCount(remain);
                    continue;
                }

                slotItem.SetCount(slotItem.Count + clickItem.Count);
                return;
            }

            MoveToEndOfEmptyInventory(clickItem);
        }


        internal override void TakeAll(ItemCursor cursor, int index)
        {
            Item? slotItem = _items[index];
            if (slotItem == null)
            {
                return;
            }

            if (index >= 5 && index <= 8)
            {
                TakeArmor(cursor, index);
                return;
            }

            base.TakeAll(cursor, index);
        }

        internal override void PutAll(ItemCursor cursor, int index)
        {
            Item? cursorItem = cursor.GetItem();
            System.Diagnostics.Debug.Assert(cursorItem != null);

            if (index >= 5 && index <= 8)
            {
                PutArmor(cursor, index);
                return;
            }

            base.PutAll(cursor, index);
        }

        private void TakeArmor(ItemCursor cursor, int index)
        {
            Item? slotItem = _items[index];
            System.Diagnostics.Debug.Assert(slotItem != null);

            Item? cursorItem = cursor.GetItem();
            if (cursorItem == null)
            {
                base.TakeAll(cursor, index);
                return;
            }

            int armorIndex = GetArmorIndex(cursorItem);

            if (armorIndex == index)
            {
                Swap(cursor, index);
                return;
            }
        }
        private void PutArmor(ItemCursor cursor, int index)
        {
            Item? cursorItem = cursor.GetItem();
            System.Diagnostics.Debug.Assert(cursorItem != null);

            int armorIndex = GetArmorIndex(cursorItem);
            if (armorIndex != index)
            {
                return;
            }

            Item? slotItem = _items[index];
            if (slotItem != null)
            {
                Swap(cursor, index);
                return;
            }

            base.PutAll(cursor, index);
        }



        internal void LeftClick(ItemCursor cursor, int index)
        {
            if (index == 0)
            {
                TakeOne(cursor, index);
                return;
            }

            Item? cursorItem = cursor.GetItem();
            if (cursorItem == null)
            {
                TakeAll(cursor, index);
                return;
            }

            Item? slotItem = _items[index];
            if (slotItem == null)
            {
                PutAll(cursor, index);
                return;
            }

            if (cursorItem.Type == slotItem.Type)
            {
                PutAll(cursor, index);
                return;
            }

            Swap(cursor, index);
        }

        internal void RightClick(ItemCursor cursor, int index)
        {
            if (index == 0)
            {
                TakeOne(cursor, index);
                return;
            }

            Item? cursorItem = cursor.GetItem();
            if (cursorItem == null)
            {
                TakeHalf(cursor, index);
                return;
            }

            Item? slotItem = _items[index];
            if (slotItem == null)
            {
                PutOne(cursor, index);
                return;
            }

            if (cursorItem.Type == slotItem.Type)
            {
                PutOne(cursor, index);
                return;
            }

            Swap(cursor, index);
        }

        internal void ShiftClick(int clickIndex)
        {
            Item? clickItem = _items[clickIndex];
            _items[clickIndex] = null;
            if (clickItem == null)
            {
                return;
            }

            if (clickIndex == 0)
            {
                MoveToEndOfInventory(clickItem);
                return;
            }

            if (clickIndex >= 1 && clickIndex <= 8 || clickIndex == 45)
            {
                MoveToInventory(clickItem);
                return;
            }

            if (clickIndex >= 9 && clickIndex <= 35)
            {
                bool isCanMove = IsCanMoveShieldOrArmorSlot(clickItem);
                if (isCanMove)
                {
                    MoveToShieldOrArmor(clickItem);
                    return;
                }

                MoveToHotBar(clickItem);
                return;
            }

            if (clickIndex >= 36 && clickIndex <= 44)
            {
                bool isCanMove = IsCanMoveShieldOrArmorSlot(clickItem);
                if (isCanMove)
                {
                    MoveToShieldOrArmor(clickItem);
                    return;
                }

                MoveToMainInventory(clickItem);
                return;
            }
        }

        internal void NumberKey(int buttonIndex, int cursorIndex)
        {
            buttonIndex += 36;
            Item? buttonItem = _items[buttonIndex];
            Item? cursorItem = _items[cursorIndex];
            if (cursorItem == null && buttonItem == null)
            {
                return;
            }

            if (buttonItem == null)
            {
                _items[buttonIndex] = cursorItem;
                _items[cursorIndex] = buttonItem;
                return;
            }

            if (cursorIndex == 5 && !buttonItem.IsHelmet)
            {
                return;
            }

            if (cursorIndex == 6 && !buttonItem.IsChestPlate)
            {
                return;
            }

            if (cursorIndex == 7 && !buttonItem.IsLeggins)
            {
                return;
            }

            if (cursorIndex == 8 && !buttonItem.IsBoots)
            {
                return;
            }

            _items[buttonIndex] = cursorItem;
            _items[cursorIndex] = buttonItem;
        }

        internal void DoubleClick(ItemCursor cursor)
        {
            Item? cursorItem = cursor.GetItem();
            System.Diagnostics.Debug.Assert(cursorItem != null);

            for (int index = 1; index <= 45; ++index)
            {
                Item? slotItem = GetItem(index);
                if (slotItem == null)
                {
                    continue;
                }

                if (slotItem.Count == slotItem.MaxCount)
                {
                    continue;
                }

                if (slotItem.Type != cursorItem.Type)
                {
                    continue;
                }

                int blank = cursorItem.MaxCount - cursorItem.Count;
                if (slotItem.Count > blank)
                {
                    cursorItem.SetCount(cursorItem.MaxCount);
                    slotItem.SetCount(slotItem.Count - blank);
                    return;
                }

                cursorItem.SetCount(cursorItem.Count + slotItem.Count);
                _items[index] = null;
                SubCount();
            }

            for (int index = 1; index <= 45; ++index)
            {
                Item? slotItem = GetItem(index);
                if (slotItem == null)
                {
                    continue;
                }

                if (slotItem.Type != cursorItem.Type)
                {
                    continue;
                }

                int blank = cursorItem.MaxCount - cursorItem.Count;
                if (slotItem.Count > blank)
                {
                    cursorItem.SetCount(cursorItem.MaxCount);
                    slotItem.SetCount(slotItem.Count - blank);
                    return;
                }

                cursorItem.SetCount(cursorItem.Count + slotItem.Count);
                _items[index] = null;
                SubCount();
            }
        }
    }

    public abstract class PublicInventory : Inventory
    {
        private bool _disposed = false;

        internal abstract string WindowType { get; }
        /*public abstract string WindowTitle { get; }*/

        private readonly object _SharedObject = new();

        private readonly NumList _IdList = new();  // Disposable

        internal readonly InventoryRenderer _Renderer = new();  // Disposable

        public PublicInventory(int count) : base(count) { }

        ~PublicInventory() => System.Diagnostics.Debug.Assert(false);

        internal int Open(
            int windowId, Queue<ClientboundPlayingPacket> outPackets,
            SelfInventory selfInventory)
        {

            System.Diagnostics.Debug.Assert(!_disposed);

            {
                int n = 9;
                var arr = new SlotData[n];

                for (int i = 0; i < n; ++i)
                {
                    Item? item = _items[i];

                    if (item == null)
                    {
                        arr[i] = new();
                        continue;
                    }

                    arr[i] = item.ConvertToPacketFormat();
                }

                outPackets.Enqueue(new SetWindowItemsPacket(0, arr));

                Item? offHandItem = selfInventory.OffhandItem;
                if (offHandItem == null)
                {
                    outPackets.Enqueue(new SetSlotPacket(0, 45, new()));
                }
                else
                {
                    outPackets.Enqueue(new SetSlotPacket(0, 45, offHandItem.ConvertToPacketFormat()));
                }
            }

            lock (_SharedObject)
            {

                System.Diagnostics.Debug.Assert(windowId >= byte.MinValue);
                System.Diagnostics.Debug.Assert(windowId <= byte.MaxValue);
                System.Diagnostics.Debug.Assert(TotalSlotCount >= byte.MinValue);
                System.Diagnostics.Debug.Assert(TotalSlotCount <= byte.MaxValue);
                outPackets.Enqueue(new OpenWindowPacket(
                    (byte)windowId, WindowType, "", (byte)TotalSlotCount));

                int count = selfInventory.PrimarySlotCount + TotalSlotCount;

                int i = 0;
                var arr = new SlotData[count];

                foreach (Item? item in Items)
                {
                    if (item == null)
                    {
                        arr[i++] = new();
                        continue;
                    }

                    arr[i++] = item.ConvertToPacketFormat();
                }

                foreach (Item? item in selfInventory.PrimaryItems)
                {
                    if (item == null)
                    {
                        arr[i++] = new();
                        continue;
                    }

                    arr[i++] = item.ConvertToPacketFormat();
                }

                System.Diagnostics.Debug.Assert(windowId >= byte.MinValue);
                System.Diagnostics.Debug.Assert(windowId <= byte.MaxValue);
                outPackets.Enqueue(new SetWindowItemsPacket((byte)windowId, arr));

                System.Diagnostics.Debug.Assert(i == count);

                int id = _IdList.Alloc();
                _Renderer.Add(id, windowId, outPackets);

                return id;
            }
        }

        internal void Close(int id, int _windowId)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            lock (_SharedObject)
            {
                int windowId = _Renderer.Remove(id);
                System.Diagnostics.Debug.Assert(_windowId == windowId);
            }
        }

        internal override (bool, Item?) TakeAll(int index, SlotData slotData)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            lock (_SharedObject)
            {
                (bool f, Item? item) = base.TakeAll(index, slotData);

                System.Diagnostics.Debug.Assert(_items[index] is null);
                _Renderer.RenderToEmpty(index);

                return (f, item);
            }
        }

        internal override (bool, Item?) PutAll(int index, Item itemCursor, SlotData slotData)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            lock (_SharedObject)
            {
                (bool f, Item? result) = base.PutAll(index, itemCursor, slotData);

                {
                    Item? item = _items[index];
                    System.Diagnostics.Debug.Assert(item != null);
                    _Renderer.RenderToSet(index, item);
                }

                return (f, result);
            }
        }

        internal override (bool, Item?) TakeHalf(int index, SlotData slotData)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            lock (_SharedObject)
            {
                (bool f, Item? result) = base.TakeHalf(index, slotData);

                {
                    Item? item = _items[index];
                    if (item == null)
                    {
                        _Renderer.RenderToEmpty(index);
                    }
                    else
                    {
                        _Renderer.RenderToSet(index, item);
                    }
                }

                return (f, result);
            }
        }

        internal override (bool, Item?) PutOne(int index, Item itemCursor, SlotData slotData)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            lock (_SharedObject)
            {
                (bool f, Item? result) = base.PutOne(index, itemCursor, slotData);

                {
                    Item? item = _items[index];
                    System.Diagnostics.Debug.Assert(item != null);
                    _Renderer.RenderToSet(index, item);
                }

                return (f, result);
            }
        }

        internal virtual void DistributeItem(
            int[] indexes, Item item, SelfInventory privateInventory)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            throw new System.NotImplementedException();
        }

        public virtual void CollectItems()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            throw new System.NotImplementedException();
        }

        public void CloseForcibly()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            lock (_SharedObject)
            {
                int[] ids = _Renderer.CloseForciblyAndFlush();
                for (int i = 0; i < ids.Length; ++i)
                {
                    _IdList.Dealloc(ids[i]);
                }
            }

        }

        public override void Dispose()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            // Assertion.
            System.Diagnostics.Debug.Assert(_IdList.Empty);

            // Release resources.
            _IdList.Dispose();
            _Renderer.Dispose();

            // Finish.
            base.Dispose();
            _disposed = true;
        }


        internal override Item? TakeItem(int index)
        {
            lock (_SharedObject)
            {
                Item? item = base.TakeItem(index);

                Render(index);
                return item;
            }
        }

        internal override void TakeHalf(ItemCursor cursor, int index)
        {
            base.TakeHalf(cursor, index);
        }

        internal override void TakeAll(ItemCursor cursor, int index)
        {
            base.TakeAll(cursor, index);
        }

        internal override void PutItem(Item? item, int index)
        {
            lock (_SharedObject)
            {
                base.PutItem(item, index);
            }
        }

        internal override void PutAll(ItemCursor cursor, int index)
        {
            base.PutAll(cursor, index);
        }

        internal override void Swap(ItemCursor cursor, int index)
        {
            base.Swap(cursor, index);
        }

        internal virtual void LeftClick(ItemCursor cursor, int index)
        {
            lock (_SharedObject)
            {
                Item? cursorItem = cursor.GetItem();
                if (cursorItem == null)
                {
                    TakeAll(cursor, index);
                    return;
                }

                Item? slotItem = _items[index];
                if (slotItem == null)
                {
                    PutAll(cursor, index);
                    return;
                }

                if (cursorItem.Type == slotItem.Type)
                {
                    PutAll(cursor, index);
                }

                Swap(cursor, index);
            }
        }

        internal void RightClick(ItemCursor cursor, int index)
        {
            lock (_SharedObject)
            {
                Item? cursorItem = cursor.GetItem();
                if (cursorItem == null)
                {
                    TakeHalf(cursor, index);
                    return;
                }

                Item? slotItem = _items[index];
                if (slotItem == null)
                {
                    PutOne(cursor, index);
                    return;
                }

                if (cursorItem.Type == slotItem.Type)
                {
                    PutOne(cursor, index);
                    return;
                }

                Swap(cursor, index);
            }
        }

        private void MoveToEmptyInventory(Item clickItem)
        {
            for (int index = 0; index <= 26; ++index)
            {
                if (Exists(index))
                {
                    continue;
                }
                _items[index] = clickItem;
                AddCount();
                Render(index);
                return;
            }
        }

        internal void MoveToInventory(Item clickItem)
        {
            lock(_SharedObject)
            {
                for (int index = 0; index <= 26; ++index)
                {
                    if (clickItem.MaxCount == 1)
                    {
                        break;
                    }

                    Item? slotItem = _items[index];
                    if (slotItem == null)
                    {
                        continue;
                    }

                    if (slotItem.Type != clickItem.Type)
                    {
                        continue;
                    }

                    if (slotItem.Count == slotItem.MaxCount)
                    {
                        continue;
                    }

                    int blank = slotItem.MaxCount - slotItem.Count;

                    if (clickItem.Count > blank)
                    {
                        int remain = clickItem.Count - blank;
                        slotItem.SetCount(slotItem.MaxCount);
                        clickItem.SetCount(remain);
                        Render(index);
                        continue;
                    }

                    slotItem.SetCount(slotItem.Count + clickItem.Count);
                    Render(index);
                    return;
                }

                MoveToEmptyInventory(clickItem);
            }
        }

        internal virtual void Render(int index)
        {
            SlotData slotData = GetSlotData(index);
            _Renderer.Render(index, slotData);
        }
    }

    public sealed class ChestInventory : PublicInventory
    {
        private bool _disposed = false;

        internal override string WindowType => "minecraft:chest";

        public ChestInventory() : base(27) { }

        ~ChestInventory() => System.Diagnostics.Debug.Assert(false);

        public override void Dispose()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            // Assertion.

            // Release resources.

            // Finish.
            base.Dispose();
            _disposed = true;
        }

    }

    public sealed class CraftingInventory : PublicInventory
    {
        private bool _disposed = false;

        internal override string WindowType => "minecraft:crafting_table";

        public CraftingInventory() : base(10) { }

        ~CraftingInventory() => System.Diagnostics.Debug.Assert(false);

        public override void Dispose()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            // Assertion.

            // Release resources.

            // Finish.
            base.Dispose();
            _disposed = true;
        }

        internal override void LeftClick(ItemCursor cursor, int index)
        {
            base.LeftClick(cursor, index);
        }

    }

}
