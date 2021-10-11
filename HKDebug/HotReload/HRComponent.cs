using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;

namespace HKDebug.HotReload
{
    public static class HRComponent
    {
        internal static Dictionary<string, Type> ComponentCache = new Dictionary<string, Type>();
        public static void ReplaceAllComponent(Type old, Type @new)
        {
            ComponentCache[old.FullName] = @new;
            foreach (var v in UnityEngine.Object.FindObjectsOfType(old))
            {
                Component c = v as Component;
                
                if (c == null) throw new InvalidOperationException();
                Component n = c.gameObject.AddComponent(@new);
                foreach(var f in old.GetFields(BindingFlags.Public | BindingFlags.NonPublic
                    | BindingFlags.Instance))
                {
                    FieldInfo nf = @new.GetField(f.Name, BindingFlags.Public | BindingFlags.NonPublic
                    | BindingFlags.Instance);
                    if (nf != null)
                    {
                        if (nf.FieldType.IsAssignableFrom(f.FieldType))
                        {
                            nf.SetValue(n, f.GetValue(c));
                        }
                    }
                }
            }
        }
    }
}
