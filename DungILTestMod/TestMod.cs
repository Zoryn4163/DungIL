using System;
using DungILModLoader;

namespace DungILTestMod
{
    public class TestMod : ModBase
    {
        public override void Init()
        {
            Log.Out("We loaded a test mod!");
            test();
            test2();
            test3();
            test4();
        }

        public string test()
        {
            return "test";
        }

        public string test2()
        {
            return test();
        }

        public string test3()
        {
            return test() + test2();
        }

        public string test4()
        {
            return test() + test2() + "test";
        }
    }
}
