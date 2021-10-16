using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using Modding;
using UnityEngine;

namespace HotReloadTest
{
    public class Test : Mod
    {
        public Test()
        {
            Log("Test");
        }
        void OnAfterHotReload(Dictionary<string,object> data)
        {
            Log("OnAfterHotReload");
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
        
        public TestMod3 t = new TestMod3();
    }
    class A : MonoBehaviour
    {
        static int c = 0;
        void Awake()
        {
            Modding.Logger.Log(Assembly.GetExecutingAssembly().FullName);
            Modding.Logger.Log("TestA"+c);
            c++;
        }
        public int a = 0;
        void Update()
        {
            a--;
        }
    }
    public class TestMod3
    {
        public static object a = null;
        public static int i = 0;
        public int g = 0;
        static TestMod3()
        {
            //a = new GameObject().AddComponent<A>();
        }
        void OnAfterHotReload(Dictionary<string, object> data)
        {
            Modding.Logger.Log("TestMod3:OnAfterHotReload");
        }
        public void SayHello()
        {
            Modding.Logger.Log(((A)a).a);
            i--;
            g = i;
        }
    }
}
