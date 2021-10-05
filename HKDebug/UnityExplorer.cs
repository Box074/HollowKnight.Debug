using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace HKDebug
{
    public static class UnityExplorer
    {
        public static dynamic Instance { get; private set; } = null; 
        public static void Init()
        {
            try
            {
                Instance = global::UnityExplorer.ExplorerStandalone.CreateInstance();
            }catch(Exception e)
            {
                Modding.Logger.Log(e);
            }
        }
    }
}
