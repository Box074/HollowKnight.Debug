using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Modding;

namespace HotReloadTest
{
    public class Test : Mod
    {
        public Test()
        {
            Log("Test");
        }
        public override void Initialize()
        {
            Log("Hello,World!");
            ModHooks.AttackHook += ModHooks_AttackHook;
        }

        private void ModHooks_AttackHook(GlobalEnums.AttackDirection obj)
        {
            t.SayHello();
        }

        public TestMod2 t = new TestMod2();
    }
    public class TestMod2
    {
        public void SayHello()
        {
            Logger.Log("Hello,World!(Change)5");
        }
    }
}
