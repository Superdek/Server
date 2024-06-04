
using Containers;
using System;
using System.Dynamic;
using System.Reflection;
using System.Xml;

namespace Protocol
{
    internal sealed class Window : System.IDisposable
    {
        private bool _disposed = false;

        private bool _ambiguous = false;

        /**
         *  -1: Window is closed.
         *   0: Window is opened with only self inventory.
         * > 0: Window is opened with self and public inventory.
         */
        private int _windowId = -1;
        private int _id = -1;

        private PublicInventory? _publicInventory = null;
        // TODO: 
        private SelfInventory _selfInventory;

        //private Item? _itemCursor = null;
        private readonly ItemCursor _cursor = new ItemCursor();

        public Window(
            Queue<ClientboundPlayingPacket> outPackets,
            SelfInventory selfInventory)
        {
            _windowId = 0;

            {
                int i = 0, n = selfInventory.TotalSlotCount;
                var arr = new SlotData[n];

                foreach (Item? item in selfInventory.Items)
                {
                    if (item == null)
                    {
                        arr[i++] = new();
                        continue;
                    }

                    arr[i++] = item.ConvertToPacketFormat();
                }

                System.Diagnostics.Debug.Assert(_windowId >= byte.MinValue);
                System.Diagnostics.Debug.Assert(_windowId <= byte.MaxValue);
                outPackets.Enqueue(new SetWindowItemsPacket((byte)_windowId, arr));

                System.Diagnostics.Debug.Assert(_cursor.GetItem() == null);
                outPackets.Enqueue(new SetSlotPacket(-1, 0, new()));

                System.Diagnostics.Debug.Assert(i == n);
            }

            _selfInventory = selfInventory;
        }

        ~Window() => System.Diagnostics.Debug.Assert(false);

        /*public int GetWindowId()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(_windowId >= 0);
            System.Diagnostics.Debug.Assert(_windowId > 0 ?
                _publicInventory != null : _publicInventory == null);

            return _windowId;
        }*/

        public bool IsOpenedWithPublicInventory()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            return _windowId > 0;
        }

        public void OpenWindowWithPublicInventory(
            Queue<ClientboundPlayingPacket> outPackets,
            SelfInventory selfInventory,
            PublicInventory publicInventory)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            _ambiguous = true;

            System.Diagnostics.Debug.Assert(_windowId == 0);
            System.Diagnostics.Debug.Assert(_id == -1);
            System.Diagnostics.Debug.Assert(_publicInventory == null);

            _windowId = (new System.Random().Next() % 100) + 1;

            _id = publicInventory.Open(_windowId, outPackets, selfInventory);

            Item? cursorItem = _cursor.GetItem();
            if (cursorItem != null)
            {
                outPackets.Enqueue(new SetSlotPacket(-1, 0, cursorItem.ConvertToPacketFormat()));
            }

            _publicInventory = publicInventory;
        }

        public void ResetWindow(
            int windowId, Queue<ClientboundPlayingPacket> outPackets)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(_windowId >= 0);

            if (windowId != _windowId)
            {
                if (_ambiguous)
                {
                    _ambiguous = false;
                    return;
                }
                else
                {
                    throw new UnexpectedValueException("ClickWindowPacket.WindowId");
                }
            }

            _ambiguous = false;

            if (_windowId == 0)
            {
                System.Diagnostics.Debug.Assert(_id == -1);
                System.Diagnostics.Debug.Assert(_publicInventory == null);

            }
            else
            {
                System.Diagnostics.Debug.Assert(_windowId > 0);
                System.Diagnostics.Debug.Assert(_id >= 0);
                System.Diagnostics.Debug.Assert(_publicInventory != null);

                _publicInventory.Close(_id, _windowId);

                _windowId = 0;
                _id = -1;
                _publicInventory = null;
            }

            /*if (_itemCursor != null)
            {
                // TODO: Drop item if _iremCursor is not null.
                _itemCursor = null;

                outPackets.Enqueue(new SetSlotPacket(-1, 0, new()));
            }*/

            System.Diagnostics.Debug.Assert(_cursor.GetItem() == null);
        }

        internal void ResetWindowForcibly(
            SelfInventory selfInventory, Queue<ClientboundPlayingPacket> outPackets, bool f)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            if (f)
            {
                System.Diagnostics.Debug.Assert(_windowId >= byte.MinValue);
                System.Diagnostics.Debug.Assert(_windowId <= byte.MaxValue);
                outPackets.Enqueue(new ClientboundCloseWindowPacket((byte)_windowId));
            }

            _ambiguous = true;

            _windowId = 0;
            _id = -1;
            _publicInventory = null;

            {
                int count = _selfInventory.TotalSlotCount;
                int i = 0;
                var arr = new SlotData[count];

                foreach (Item? item in _selfInventory.Items)
                {
                    if (item == null)
                    {
                        arr[i++] = new();
                        continue;
                    }

                    arr[i++] = item.ConvertToPacketFormat();
                }

                System.Diagnostics.Debug.Assert(_windowId == 0);
                System.Diagnostics.Debug.Assert(_windowId >= byte.MinValue);
                System.Diagnostics.Debug.Assert(_windowId <= byte.MaxValue);
                outPackets.Enqueue(new SetWindowItemsPacket((byte)_windowId, arr));

                System.Diagnostics.Debug.Assert(i == count);
            }

            /*if (_itemCursor != null)
            {
                // TODO: Drop item if _iremCursor is not null.
                _itemCursor = null;

                outPackets.Enqueue(new SetSlotPacket(-1, 0, new()));
            }*/

            System.Diagnostics.Debug.Assert(_cursor.GetItem() == null);
        }

        private void ClickLeftMouseButton(
            SelfInventory selfInventory, int index, SlotData slotData)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            if (index >= _selfInventory.TotalSlotCount)
            {
                throw new UnexpectedValueException("ClickWindowPacket.SlotNumber");
            }

            bool f;

            if (_cursor.GetItem() == null)
            {
                Item item;
                (f, item) = _selfInventory.TakeAll(index, slotData);
                _cursor.SetItem(item);
            }
            else
            {
                Item item;
                (f, item) = _selfInventory.PutAll(index, _cursor.GetItem(), slotData);
                _cursor.SetItem(item);
            }

            if (!f)
            {
                /*SlotData slotDataInCursor = _itemCursor.ConventToPacketFormat();

                outPackets.Enqueue(new SetSlotPacket(-1, 0, slotDataInCursor));*/

                throw new UnexpectedValueException("ClickWindowPacket.SLOT_DATA");
            }
        }

        private void ClickLeftMouseButtonWithPublicInventory(
            SelfInventory selfInventory, int index, SlotData slotData,
            Queue<ClientboundPlayingPacket> outPackets)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(_windowId > 0);
            System.Diagnostics.Debug.Assert(_publicInventory != null);

            bool f;

            if (_cursor.GetItem() == null)
            {
                if (index >= 0 && index < _publicInventory.TotalSlotCount)
                {
                    Item item;
                    (f, item) = _publicInventory.TakeAll(index, slotData);
                    _cursor.SetItem(item);
                }
                else if (
                    index >= _publicInventory.TotalSlotCount &&
                    index < _publicInventory.TotalSlotCount + _selfInventory.PrimarySlotCount)
                {
                    int j = index + 9 - _publicInventory.TotalSlotCount;
                    Item item;
                    (f, item) = _selfInventory.TakeAll(j, slotData);
                    _cursor.SetItem(item);
                }
                else
                {
                    throw new UnexpectedValueException("ClickWindowPacket.SlotNumber");
                }
            }
            else
            {
                if (index >= 0 && index < _publicInventory.TotalSlotCount)
                {
                    Item item;
                    (f, item) = _publicInventory.PutAll(index, _cursor.GetItem(), slotData);
                    _cursor.SetItem(item);

                }
                else if (
                    index >= _publicInventory.TotalSlotCount &&
                    index < _publicInventory.TotalSlotCount + _selfInventory.PrimarySlotCount)
                {
                    int j = index + 9 - _publicInventory.TotalSlotCount;
                    Item item;
                    (f, item) = _selfInventory.PutAll(index, _cursor.GetItem(), slotData);
                    _cursor.SetItem(item);
                }
                else
                {
                    throw new UnexpectedValueException("ClickWindowPacket.SlotNumber");
                }
            }

            if (!f)
            {
                if (_cursor.GetItem() == null)
                {
                    if (index >= 0 && index < _publicInventory.TotalSlotCount)
                    {
                        outPackets.Enqueue(new SetSlotPacket(-1, 0, new()));
                    }
                    else if (
                        index >= _publicInventory.TotalSlotCount &&
                        index < _publicInventory.TotalSlotCount + _selfInventory.PrimarySlotCount)
                    {
                        throw new UnexpectedValueException("ClickWindowPacket.SLOT_DATA");
                    }
                    else
                    {
                        throw new System.NotImplementedException();
                    }
                }
                else
                {
                    if (index >= 0 && index < _publicInventory.TotalSlotCount)
                    {
                        outPackets.Enqueue(new SetSlotPacket(-1, 0, _cursor.GetItem().ConvertToPacketFormat()));
                    }
                    else if (
                        index >= _publicInventory.TotalSlotCount &&
                        index < _publicInventory.TotalSlotCount + _selfInventory.PrimarySlotCount)
                    {
                        throw new UnexpectedValueException("ClickWindowPacket.SLOT_DATA");
                    }
                    else
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }

        }

        private void ClickRightMouseButton(
            SelfInventory selfInventory, int index, SlotData slotData)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            if (index >= _selfInventory.TotalSlotCount)
            {
                throw new UnexpectedValueException("ClickWindowPacket.SlotNumber");
            }

            bool f;

            if (_cursor.GetItem() == null)
            {
                Item item;
                (f, item) = _selfInventory.TakeHalf(index, slotData);
                _cursor.SetItem(item);
            }
            else
            {
                Item item;
                (f, item) = _selfInventory.PutOne(index, _cursor.GetItem(), slotData);
                _cursor.SetItem(item);
            }

            if (!f)
            {
                /*SlotData slotDataInCursor = _itemCursor.ConventToPacketFormat();

                outPackets.Enqueue(new SetSlotPacket(-1, 0, slotDataInCursor));*/

                throw new UnexpectedValueException("ClickWindowPacket.SLOT_DATA");
            }
        }

        private void ClickRightMouseButtonWithPublicInventory(
            SelfInventory selfInventory, int index, SlotData slotData,
            Queue<ClientboundPlayingPacket> outPackets)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(_windowId > 0);
            System.Diagnostics.Debug.Assert(_publicInventory != null);

            bool f;

            if (_cursor.GetItem() == null)
            {
                if (index >= 0 && index < _publicInventory.TotalSlotCount)
                {
                    Item item;
                    (f, item) = _selfInventory.TakeHalf(index, slotData);
                    _cursor.SetItem(item);
                }
                else if (
                    index >= _publicInventory.TotalSlotCount &&
                    index < _publicInventory.TotalSlotCount + _selfInventory.PrimarySlotCount)
                {
                    int j = index + 9 - _publicInventory.TotalSlotCount;
                    Item item;
                    (f, item) = _selfInventory.TakeHalf(j, slotData);
                    _cursor.SetItem(item);
                }
                else
                {
                    throw new UnexpectedValueException("ClickWindowPacket.SlotNumber");
                }
            }
            else
            {
                if (index >= 0 && index < _publicInventory.TotalSlotCount)
                {
                    Item item;
                    (f, item) = _publicInventory.PutOne(index, _cursor.GetItem(), slotData);
                    _cursor.SetItem(item);
                }
                else if (
                    index >= _publicInventory.TotalSlotCount &&
                    index < _publicInventory.TotalSlotCount + _selfInventory.PrimarySlotCount)
                {
                    int j = index + 9 - _publicInventory.TotalSlotCount;
                    Item item;
                    (f, item) = _publicInventory.PutOne(j, _cursor.GetItem(), slotData);
                    _cursor.SetItem(item);
                }
                else
                {
                    throw new UnexpectedValueException("ClickWindowPacket.SlotNumber");
                }
            }

            if (!f)
            {
                if (_cursor.GetItem() == null)
                {
                    if (index >= 0 && index < _publicInventory.TotalSlotCount)
                    {
                        outPackets.Enqueue(new SetSlotPacket(-1, 0, new()));
                    }
                    else if (
                        index >= _publicInventory.TotalSlotCount &&
                        index < _publicInventory.TotalSlotCount + _selfInventory.PrimarySlotCount)
                    {
                        throw new UnexpectedValueException("ClickWindowPacket.SLOT_DATA");
                    }
                    else
                    {
                        throw new System.NotImplementedException();
                    }
                }
                else
                {
                    if (index >= 0 && index < _publicInventory.TotalSlotCount)
                    {
                        outPackets.Enqueue(new SetSlotPacket(-1, 0, _cursor.GetItem().ConvertToPacketFormat()));
                    }
                    else if (
                        index >= _publicInventory.TotalSlotCount &&
                        index < _publicInventory.TotalSlotCount + _selfInventory.PrimarySlotCount)
                    {
                        throw new UnexpectedValueException("ClickWindowPacket.SLOT_DATA");
                    }
                    else
                    {
                        throw new System.NotImplementedException();
                    }
                }
            }

        }

        internal void HandleMiddleDrag(Queue<ClickWindowPacket> packets, Queue<ClientboundPlayingPacket> outPackets)
        {
            System.Diagnostics.Debug.Assert(!_disposed);
            System.Diagnostics.Debug.Assert(_windowId >= 0);

            Item? cursorItem = _cursor.GetItem();
            if (cursorItem == null)
            {
                return;
            }

            while (packets.Empty == false)
            {
                ClickWindowPacket packet = packets.Dequeue();
                if (_windowId != packet.WINDOW_ID)
                {
                    if (packet.WINDOW_ID == 0)
                    {
                        return;
                    }
                    throw new UnexpectedValueException("ClickWindowPacket.WindowId");
                }

                int index = packet.SLOT;
                Item? slotItem = _selfInventory.GetItem(index);
                SlotData slotData = _selfInventory.GetSlotData(index);
                outPackets.Enqueue(new SetSlotPacket((sbyte)_windowId, (short)index, slotData));
            }

            cursorItem = _cursor.GetItem();
            if (cursorItem == null)
            {
                outPackets.Enqueue(new SetSlotPacket(-1, 0, new()));
            }
            else
            {
                outPackets.Enqueue(new SetSlotPacket(-1, 0, cursorItem.ConvertToPacketFormat()));
            }

            // console log
            {
                if (_windowId == 0)
                {
                    _selfInventory.Print();
                }
                else
                {
                    System.Diagnostics.Debug.Assert(_publicInventory != null);
                    _publicInventory.Print();
                    _selfInventory.Print();
                }

                cursorItem = _cursor.GetItem();
                if (cursorItem != null)
                    System.Console.WriteLine($"itemCursor: {cursorItem.Type} {cursorItem.Count}");
            }
        }

        internal void HandleRightDrag(Queue<ClickWindowPacket> packets, Queue<ClientboundPlayingPacket> outPackets)
        {
            System.Diagnostics.Debug.Assert(!_disposed);
            System.Diagnostics.Debug.Assert(_windowId >= 0);

            Item? cursorItem = _cursor.GetItem();
            if (cursorItem == null)
            {
                return;
            }

            SlotFiltering(packets, cursorItem);

            int remain = cursorItem.Count - packets.Count;
            while (packets.Empty == false)
            {
                ClickWindowPacket packet = packets.Dequeue();

                int index = packet.SLOT;
                Item? slotItem = _selfInventory.GetItem(index);
                if (slotItem == null)
                {
                    _selfInventory.PutItem(new Item(cursorItem.Type, 1), index);
                    continue;
                }

                slotItem.SetCount(slotItem.Count + 1);
            }

            if (remain == 0)
            {
                _cursor.SetItem(null);
            }
            else
            {
                cursorItem.SetCount(remain);
            }

            cursorItem = _cursor.GetItem();
            if (cursorItem == null)
            {
                outPackets.Enqueue(new SetSlotPacket(-1, 0, new()));
            }
            else
            {
                outPackets.Enqueue(new SetSlotPacket(-1, 0, cursorItem.ConvertToPacketFormat()));
            }

            // console log
            {
                if (_windowId == 0)
                {
                    _selfInventory.Print();
                }
                else
                {
                    System.Diagnostics.Debug.Assert(_publicInventory != null);
                    _publicInventory.Print();
                    _selfInventory.Print();
                }

                cursorItem = _cursor.GetItem();
                if (cursorItem != null)
                    System.Console.WriteLine($"itemCursor: {cursorItem.Type} {cursorItem.Count}");
            }
        }

        internal void HandleLeftDrag(Queue<ClickWindowPacket> packets, Queue<ClientboundPlayingPacket> outPackets)
        {
            System.Diagnostics.Debug.Assert(!_disposed);
            System.Diagnostics.Debug.Assert(_windowId >= 0);

            Item? cursorItem = _cursor.GetItem();
            if (cursorItem == null)
            {
                return;
            }

            SlotFiltering(packets, cursorItem);

            int count = cursorItem.Count / packets.Count;
            int remain = cursorItem.Count % packets.Count;
            while (packets.Empty == false)
            {
                ClickWindowPacket packet = packets.Dequeue();

                int index = packet.SLOT;
                Item? slotItem = _selfInventory.GetItem(index);
                if (slotItem == null)
                {
                    _selfInventory.PutItem(new Item(cursorItem.Type, count), index);
                    continue;
                }

                int blank = slotItem.MaxCount - slotItem.Count;
                if (count > blank)
                {
                    remain += count - blank;
                    slotItem.SetCount(slotItem.MaxCount);
                    continue;
                }

                int sum = slotItem.Count + count;
                slotItem.SetCount(sum);
            }

            if (remain == 0)
            {
                _cursor.SetItem(null);
            }
            else
            {
                cursorItem.SetCount(remain);
            }

            cursorItem = _cursor.GetItem();
            if (cursorItem == null)
            {
                outPackets.Enqueue(new SetSlotPacket(-1, 0, new()));
            }
            else
            {
                outPackets.Enqueue(new SetSlotPacket(-1, 0, cursorItem.ConvertToPacketFormat()));
            }

            // console log
            {
                if (_windowId == 0)
                {
                    _selfInventory.Print();
                }
                else
                {
                    System.Diagnostics.Debug.Assert(_publicInventory != null);
                    _publicInventory.Print();
                    _selfInventory.Print();
                }

                cursorItem = _cursor.GetItem();
                if (cursorItem != null)
                    System.Console.WriteLine($"itemCursor: {cursorItem.Type} {cursorItem.Count}");
            }
        }

        private void SlotFiltering(Queue<ClickWindowPacket> packets, Item? cursorItem)
        {
            System.Diagnostics.Debug.Assert(cursorItem != null);
            for (int i = 0; i < packets.Count; ++i)
            {
                ClickWindowPacket packet = packets.Dequeue();
                if (_windowId != packet.WINDOW_ID)
                {
                    if (packet.WINDOW_ID == 0)
                    {
                        return;
                    }
                    throw new UnexpectedValueException("ClickWindowPacket.WindowId");
                }

                int index = packet.SLOT;

                Item? slotItem = _selfInventory.GetItem(index);
                if (slotItem == null)
                {
                    packets.Enqueue(packet);
                    continue;
                }

                if (cursorItem.Type != slotItem.Type)
                {
                    continue;
                }

                if (slotItem.Count == slotItem.MaxCount)
                {
                    continue;
                }

                packets.Enqueue(packet);
            }
        }

        public void Handle(int windowId, int mode, int button, int index,
            Queue<ClientboundPlayingPacket> outPackets)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            System.Diagnostics.Debug.Assert(_windowId >= 0);

            if (_windowId == 0)
            {
                if (windowId > 0)
                {
                    if (_ambiguous)
                    {
                        return;
                    }
                    else
                    {
                        throw new UnexpectedValueException("ClickWindowPacket.WindowId");
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(_windowId > 0);
                if (windowId == 0)
                {
                    if (_ambiguous)
                    {
                        // Ignored...
                        return;
                    }
                    else
                    {
                        throw new UnexpectedValueException("ClickWindowPacket.WindowId");
                    }
                }
                else if (_windowId != windowId)
                {
                    throw new UnexpectedValueException("ClickWindowPacket.WindowId");
                }
            }

            _ambiguous = false;

            if (index < 0)
            {
                throw new UnexpectedValueException("ClickWindowPacket.SlotNumber");
            }



            // TODO: 
            /*if (_windowId == 0)
            {
                // self
                if (mode == 0 && button == 0)
                {
                    selfInventory.LeftClick(_cursor, index);
                }
                else if (mode == 0 && button == 1)
                {

                }
                else if (mode == 1 && button == 0)
                {
                    selfInventory.ShiftClick(index);
                }
                else if (mode == 1 && button == 1)
                {
                    selfInventory.ShiftClick(index);
                }
                else if (mode == 2 && button == 0)
                {

                }
            }
            else
            {
                // public
                if (mode == 0 && button == 0)
                {
                    LeftClickWithPublicInventory(index);
                }
                else if (mode == 0 && button == 1)
                {

                }
                else if (mode == 1 && button == 0)
                {

                }
                else if (mode == 1 && button == 1)
                {

                }
                else if (mode == 2 && button == 0)
                {

                }
            }
            if (_cursor.GetItem() == null)
            {
                outPackets.Enqueue(new SetSlotPacket(-1, 0, new()));
            }
            else
            {
                outPackets.Enqueue(new SetSlotPacket(-1, 0, _cursor.GetItem().ConvertToPacketFormat()));
            }*/



            switch (mode)
            {
                default:
                    throw new UnexpectedValueException("ClickWindowPacket.ModeNumber");
                case 0:
                    switch (button)
                    {
                        default:
                            throw new UnexpectedValueException("ClickWindowPacket.ButtonNumber");
                        case 0:
                            if (_windowId == 0)
                            {
                                _selfInventory.LeftClick(_cursor, index);
                                break;
                            }
                            LeftClickWithPublicInventory(index);

                            /*ClickLeftMouseButtonWithPublicInventory(
                                    selfInventory, index, slotData,
                                    outPackets);*/
                            break;
                        case 1:
                            if (_windowId == 0)
                            {
                                _selfInventory.RightClick(_cursor, index);
                                break;
                            }
                            RightClickWithPublicInventory(index);

                            /*ClickRightMouseButtonWithPublicInventory(
                                    selfInventory, index, slotData,
                                    outPackets);*/
                            break;
                    }
                    break;
                case 1:
                    switch (button)
                    {
                        default:
                            throw new UnexpectedValueException("ClickWindowPacket.ButtonNumber");

                        case 0:
                        case 1:
                            if (_windowId == 0)
                            {
                                _selfInventory.ShiftClick(index);
                                break;
                            }
                            ShiftClickWithPublicInventory(index);
                            break;
                    }
                    break;
                case 2:
                    if (_windowId == 0)
                    {
                        _selfInventory.NumberKey(button, index);
                        break;
                    }
                    NumberKeyWithPublicInventory(button, index);
                    break;
                case 4:
                    // TODO: left click item drop
                    /*if (button == 0 && index == -999)
                    {
                        Item? cursorItem = _cursor.GetItem();
                        if (cursorItem == null)
                        {
                            return;
                        }

                        // TODO: make ItemEntity

                        // TODO: spawn ItemEntity in player position
                        Entity.Vector pos = player.Position;
                        SpawnObjectPacket spawnObjectPacket = new(1, Guid.NewGuid(), 1, pos.X, pos.Y, pos.Z, 1, 1, 1, 1, 1, 1);
                        outPackets.Enqueue(spawnObjectPacket);
                    }

                    if (button == 1 && index == -999)
                    {

                    }*/
                    break;
                case 5:
                    System.Diagnostics.Debug.Assert(false);
                    break;
                case 6:
                    // TODO: fill cursor with double clicked item
                    if (_windowId == 0)
                    {
                        Item? cursorItem = _cursor.GetItem();
                        if (cursorItem == null)
                        {
                            return;
                        }

                        if (cursorItem.Count == cursorItem.MaxCount)
                        {
                            return;
                        }
                        _selfInventory.DoubleClick(cursorItem);
                    }
                    break;
            }
            if (_cursor.GetItem() == null)
            {
                outPackets.Enqueue(new SetSlotPacket(-1, 0, new()));
            }
            else
            {
                outPackets.Enqueue(new SetSlotPacket(-1, 0, _cursor.GetItem().ConvertToPacketFormat()));
            }

            // console log
            {
                if (_windowId == 0)
                {
                    _selfInventory.Print();
                }
                else
                {
                    System.Diagnostics.Debug.Assert(_publicInventory != null);
                    _publicInventory.Print();
                    _selfInventory.Print();
                }
                Item? cursorItem = _cursor.GetItem();
                if (cursorItem != null)
                    System.Console.WriteLine($"itemCursor: {cursorItem.Type} {cursorItem.Count}");
            }
        }

        private void NumberKeyWithPublicInventory(int button, int index)
        {
            if (_publicInventory == null)
            {
                return;
            }

            if (index >= 0 && index < _publicInventory.TotalSlotCount)
            {
                button = button + 36;
                Item? cursorItem = _publicInventory.TakeItem(index);
                Item? buttonItem = _selfInventory.TakeItem(button);

                _publicInventory.PutItem(buttonItem, index);
                _selfInventory.PutItem(cursorItem, button);
                return;
            }

            if (index >= _publicInventory.TotalSlotCount && index < _publicInventory.TotalSlotCount + (_selfInventory.TotalSlotCount - 10))
            {
                int convertIndex = index - _publicInventory.TotalSlotCount + 9;

                _selfInventory.NumberKey(button, convertIndex);
                return;
            }
        }

        private void ShiftClickWithPublicInventory(int index)
        {
            if (_publicInventory == null)
            {
                return;
            }

            if (index >= 0 && index < _publicInventory.TotalSlotCount)
            {
                Item? clickItem = _publicInventory.TakeItem(index);
                if (clickItem == null)
                {
                    return;
                }

                _selfInventory.MoveToEndOfInventory(clickItem);
                return;
            }

            if (index >= _publicInventory.TotalSlotCount && index < _publicInventory.TotalSlotCount + (_selfInventory.TotalSlotCount - 10))
            {
                int convertIndex = index - _publicInventory.TotalSlotCount + 9;

                Item? clickItem = _selfInventory.TakeItem(convertIndex);
                if (clickItem == null)
                {
                    return;
                }

                _publicInventory.MoveToInventory(clickItem);
                return;
            }
        }

        private void RightClickWithPublicInventory(int index)
        {
            if (_publicInventory == null)
            {
                return;
            }

            if (index >= 0 && index < _publicInventory.TotalSlotCount)
            {
                _publicInventory.RightClick(_cursor, index);
                return;
            }

            if (index >= _publicInventory.TotalSlotCount && index < _publicInventory.TotalSlotCount + (_selfInventory.TotalSlotCount - 10))
            {
                int convertIndex = index - _publicInventory.TotalSlotCount + 9;
                _selfInventory.RightClick(_cursor, convertIndex);
                return;
            }
        }

        private void LeftClickWithPublicInventory(int index)
        {
            if (_publicInventory == null)
            {
                return;
            }

            if (index >= 0 && index < _publicInventory.TotalSlotCount)
            {
                _publicInventory.LeftClick(_cursor, index);
                return;
            }

            if (index >= _publicInventory.TotalSlotCount && index < _publicInventory.TotalSlotCount + (_selfInventory.TotalSlotCount - 10))
            {
                int convertIndex = index - _publicInventory.TotalSlotCount + 9;
                _selfInventory.LeftClick(_cursor, convertIndex);
                return;
            }
        }

        public void Flush(World world)
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            if (_cursor.GetItem() != null)
            {
                // TODO: Drop Item.
                throw new System.NotImplementedException();

                _cursor.SetItem(null);
            }

            if (_publicInventory != null)
            {
                System.Diagnostics.Debug.Assert(_windowId > 0);
                System.Diagnostics.Debug.Assert(_id >= 0);

                _publicInventory.Close(_id, _windowId);

                _publicInventory = null;
            }

            _windowId = 0;
            _id = -1;

        }

        public void Dispose()
        {
            System.Diagnostics.Debug.Assert(!_disposed);

            // Assertion.
            System.Diagnostics.Debug.Assert(_windowId == 0);
            System.Diagnostics.Debug.Assert(_id == -1);
            System.Diagnostics.Debug.Assert(_publicInventory == null);
            System.Diagnostics.Debug.Assert(_cursor.GetItem() == null);

            // Release Resources.

            System.GC.SuppressFinalize(this);
            _disposed = true;
        }
    }

    public class ItemCursor
    {
        private Item? item = null;

        public void SetItem(Item? item) { this.item = item; }
        public Item? GetItem() { return this.item; }
    }
}
