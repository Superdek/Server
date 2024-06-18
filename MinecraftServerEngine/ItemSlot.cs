﻿

using Containers;

namespace MinecraftServerEngine
{
    

    internal sealed class ItemSlot
    {

        // TODO: Replace as IReadOnlyTable
        // TODO: 프로그램이 종료되었을 때 자원 해제하기. static destructor?
        private readonly static Table<Items, int> _ITEM_ENUM_TO_ID_MAP = new();
        private readonly static Table<int, Items> _ITEM_ID_TO_ENUM_MAP = new();

        static ItemSlot()
        {
            /*_ITEM_ENUM_TO_ID_MAP.Insert(Items.Stone, 1);
            _ITEM_ENUM_TO_ID_MAP.Insert(Items.Grass, 2);
            _ITEM_ENUM_TO_ID_MAP.Insert(Items.Dirt, 3);
            _ITEM_ENUM_TO_ID_MAP.Insert(Items.Cobbestone, 4);*/

            _ITEM_ENUM_TO_ID_MAP.Insert(Items.IronSword, 267);
            _ITEM_ENUM_TO_ID_MAP.Insert(Items.WoodenSword, 268);

            _ITEM_ENUM_TO_ID_MAP.Insert(Items.StoneSword, 272);

            _ITEM_ENUM_TO_ID_MAP.Insert(Items.DiamondSword, 276);

            _ITEM_ENUM_TO_ID_MAP.Insert(Items.GoldenSword, 283);

            _ITEM_ENUM_TO_ID_MAP.Insert(Items.LeatherHelmet, 298);

            _ITEM_ENUM_TO_ID_MAP.Insert(Items.ChainmailHelmet, 302);

            _ITEM_ENUM_TO_ID_MAP.Insert(Items.IronHelmet, 306);

            _ITEM_ENUM_TO_ID_MAP.Insert(Items.DiamondHelmet, 310);

            _ITEM_ENUM_TO_ID_MAP.Insert(Items.GoldenHelmet, 314);
            
            foreach ((Items item, int id) in _ITEM_ENUM_TO_ID_MAP.GetElements())
            {
                _ITEM_ID_TO_ENUM_MAP.Insert(id, item);
            }

            System.Diagnostics.Debug.Assert(_ITEM_ENUM_TO_ID_MAP.Count == _ITEM_ID_TO_ENUM_MAP.Count);
        }

        private static bool IsArmorItem(Items item)
        {
            switch (item)
            {
                default:
                    throw new System.NotImplementedException();
                case Items.Stone:
                    return false;
                case Items.Grass:
                    return false;
                case Items.Dirt:
                    return false;
                case Items.Cobbestone:
                    return false;

                case Items.IronSword:
                    return false;
                case Items.WoodenSword:
                    return false;

                case Items.StoneSword:
                    return false;

                case Items.DiamondSword:
                    return false;

                case Items.GoldenSword:
                    return false;

                case Items.LeatherHelmet:
                    return true;

                case Items.ChainmailHelmet:
                    return true;

                case Items.IronHelmet:
                    return true;

                case Items.DiamondHelmet:
                    return true;

                case Items.GoldenHelmet:
                    return true;
            }
        }

        private static int GetMaxItemCount(Items item)
        {
            switch (item)
            {
                default:
                    throw new System.NotImplementedException();
                case Items.Stone:
                    return 64;
                case Items.Grass:
                    return 64;
                case Items.Dirt:
                    return 64;
                case Items.Cobbestone:
                    return 64;

                case Items.IronSword:
                    return 1;
                case Items.WoodenSword:
                    return 1;

                case Items.StoneSword:
                    return 1;

                case Items.DiamondSword:
                    return 1;

                case Items.GoldenSword:
                    return 1;

                case Items.LeatherHelmet:
                    return 1;

                case Items.ChainmailHelmet:
                    return 1;

                case Items.IronHelmet:
                    return 1;

                case Items.DiamondHelmet:
                    return 1;

                case Items.GoldenHelmet:
                    return 1;
            }
        }


        public readonly Items Item;


        private int _count;
        public int Count => _count;


        public readonly int MaxCount;
        public int MinCount => 1;


        public ItemSlot(Items item, int count)
        {
            Item = item;
            _count = count;

            MaxCount = GetMaxItemCount(item);
            System.Diagnostics.Debug.Assert(MaxCount >= MinCount);
        }

        public int Stack(int count)
        {
            System.Diagnostics.Debug.Assert(_count >= MinCount);
            System.Diagnostics.Debug.Assert(_count <= MaxCount);
            System.Diagnostics.Debug.Assert(count >= MinCount);
            System.Diagnostics.Debug.Assert(count <= MaxCount);

            int rest;
            _count += count;

            if (_count > MaxCount)
            {
                rest = _count - MaxCount;
                _count = MaxCount;
            }
            else
            {
                rest = 0;
            }

            return count - rest;  // spend
        }

        public void Spend(int count)
        {
            System.Diagnostics.Debug.Assert(count >= 0);

            System.Diagnostics.Debug.Assert(_count >= MinCount);
            System.Diagnostics.Debug.Assert(_count <= MaxCount);

            if (count == 0)
            {
                return;
            }

            System.Diagnostics.Debug.Assert(count < _count);
            _count -= count;
            System.Diagnostics.Debug.Assert(_count > 0);
        }

        public ItemSlot DivideHalf()
        {
            System.Diagnostics.Debug.Assert(_count >= MinCount);
            System.Diagnostics.Debug.Assert(_count <= MaxCount);

            int count = (_count / 2) + (_count % 2);
            _count /= 2;

            System.Diagnostics.Debug.Assert(count >= MinCount);
            System.Diagnostics.Debug.Assert(count <= MaxCount);
            System.Diagnostics.Debug.Assert(_count >= MinCount);
            System.Diagnostics.Debug.Assert(_count <= MaxCount);

            return new ItemSlot(Item, count);
        }

        public ItemSlot DivideOne()
        {
            System.Diagnostics.Debug.Assert(_count >= MinCount);
            System.Diagnostics.Debug.Assert(_count <= MaxCount);

            --_count;

            System.Diagnostics.Debug.Assert(_count >= MinCount);

            return new ItemSlot(Item, 1);
        }

        public SlotData ConventToProtocolFormat()
        {
            System.Diagnostics.Debug.Assert(_count >= MinCount);
            System.Diagnostics.Debug.Assert(_count <= MaxCount);

            int id = _ITEM_ENUM_TO_ID_MAP.Lookup(Item);

            System.Diagnostics.Debug.Assert(id >= short.MinValue);
            System.Diagnostics.Debug.Assert(id <= short.MaxValue);
            System.Diagnostics.Debug.Assert(_count >= byte.MinValue);
            System.Diagnostics.Debug.Assert(_count <= byte.MaxValue);

            return new((short)id, (byte)_count);
        }

        public bool CompareWithProtocolFormat(SlotData slotData)
        {
            System.Diagnostics.Debug.Assert(_count >= MinCount);
            System.Diagnostics.Debug.Assert(_count <= MaxCount);

            if (slotData.Id == -1)
            {
                return false;
            }

            int id = _ITEM_ENUM_TO_ID_MAP.Lookup(Item);
            if (slotData.Id != id)
            {
                return false;
            }

            if (slotData.Count != _count)
            {
                return false;
            }

            return true;
        }

    }
}