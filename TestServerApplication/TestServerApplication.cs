﻿using Common;
using MinecraftServerEngine;

namespace TestServerApplication
{
    public static class TestServerApplication
    {
        public static void Main()
        {
            Console.Printl("Hello, World!");

            using World world = new SuperWorld();

            using ServerFramework framework = new(world);
            framework.Run();


        }

    }
}
