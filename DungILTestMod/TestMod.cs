using System;
using DungILModLoader;

namespace DungILTestMod
{
    public class TestMod : ModBase
    {
        public override void Init()
        {
            Log.Out("We loaded a test mod!");
        }
    }
}
