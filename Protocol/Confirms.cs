﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Protocol
{
    public abstract class Confirm
    {
    }

    public class TeleportConfirm : Confirm
    {
        public readonly int Payload;

        internal TeleportConfirm(int payload)
        {
            Payload = payload;
        }

    }

}