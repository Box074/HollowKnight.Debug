using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace HKDebug.HotReload
{
    public static class HRHook
    {
        public static Dictionary<string, Type> TypeCaches = new Dictionary<string, Type>();
        public static Type GetType(string fullname)
        {
            if (TypeCaches.TryGetValue(fullname, out var v))
            {
                return v;
            }
            Type t = AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetType(fullname)).FirstOrDefault(x => x != null);
            TypeCaches.Add(fullname, t);
            return t;
        }
        public static FieldInfo GetFieldInfo(Type t, string name)
        {
            FieldInfo f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (f == null)
            {
                throw new MissingFieldException(t.FullName, name);
            }
            return f;
        }
        public static object GetField(object @this, string name, Type t)
        {
            if (t == null) throw new NullReferenceException();
            return GetFieldInfo(t, name).GetValue(@this);
        }
        public static void SetField(object @this, object value, string name, Type t)
        {
            if (t == null) throw new NullReferenceException();
            GetFieldInfo(t, name).SetValue(@this, value);
        }
        public static MethodInfo GetMethodInfo(Type t, string name, params Type[] p)
        {
            MethodInfo m = t.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                Type.DefaultBinder, p, null);
            if (m == null)
            {
                throw new MissingMethodException(t.FullName, name);
            }
            return m;
        }
        public static object InvokeMethod(object[] args, Type t, string name, Type[] p)
        {
            var m = GetMethodInfo(t, name, p);
            object @this = m.IsStatic ? null : args[0];
            args = args.Skip(1).ToArray();
            return m.Invoke(@this, args);
        }

        public static List<object> HStartPushArg() => new List<object>();
        public static List<object> HPushArgs(object obj, List<object> a)
        {
            a.Add(obj);
            return a;
        }
        public static object[] HEndPushArg(List<object> l) => l.ToArray();
        
    }
}
