using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using HKDebug.Menu;
using MonoMod.RuntimeDetour.HookGen;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using Mono.Cecil;

namespace HKDebug.HotReload
{
    public sealed class ObjectHandler
    {
        public ObjectHandler(object o)
        {
            if (o == null) throw new ArgumentNullException("o");
            Object = new WeakReference(o);
            t = o.GetType();
        }
        public override int GetHashCode()
        {
            return t.GetHashCode();
        }
        public bool IsNull()
        {
            return Object.IsAlive || Object.Target == null;
        }
        public override bool Equals(object obj)
        {
            if (Object == null || Object.IsAlive) return false;
            if (obj == null) return false;
            if(obj is ObjectHandler handler)
            {
                if (handler.Object.IsAlive) return false;
                if (ReferenceEquals(handler.Object.Target, Object.Target))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (ReferenceEquals(obj, Object.Target))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        private readonly Type t = null;
        private readonly WeakReference Object = null;
    }
    public class HRObjectCache
    {
        public Dictionary<Type, LinkedList<(ObjectHandler, object)>> caches = new Dictionary<Type, LinkedList<(ObjectHandler, object)>>();
        public object TryGetCache(object src)
        {
            if (src == null) return null;
            Type t = src.GetType();
            LinkedList<(ObjectHandler, object)> table;
            if (!caches.TryGetValue(t,out table))
            {
                table = new LinkedList<(ObjectHandler, object)>();
                caches.Add(t, table);
            }
            return table.FirstOrDefault(x => x.Item1.Equals(src));
        }
        public void AddCache(object src,object dst)
        {
            if (src == null || dst == null) return;
            if(caches.TryGetValue(src.GetType(),out var v))
            {
                v.AddFirst((new ObjectHandler(src), dst));
            }
        }
        public void Clean()
        {
            foreach(var v in caches)
            {
                var l = v.Value;
                var f = l.First;
                while (f != null)
                {
                    if (f.Value.Item1.IsNull())
                    {
                        var old = f;
                        f = f.Next;
                        l.Remove(old);
                    }
                    else
                    {
                        f = f.Next;
                    }
                }
            }
        }
    }
    public static class HRLCore
    {
        public readonly static Dictionary<Type, Type> TypeCaches = new Dictionary<Type, Type>();
        public readonly static HRObjectCache ObjectCaches = new HRObjectCache();
        public static void CleanObjectCache()
        {
            ObjectCaches.Clean();
        }
        public static object ConvertObject(object src)
        {
            if (src == null) return null;
            Type st = src.GetType();
            var o = ObjectCaches.TryGetCache(src);
            if (o != null) return o;

            if (TypeCaches.TryGetValue(src.GetType(), out var type))
            {
                //logger.Log("Create Instance: " + st.FullName);
                o = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);

                Dictionary<string, object> data = new Dictionary<string, object>();
                foreach (var f in st.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    data.Add(f.Name, f.GetValue(src));
                }
                foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (data.TryGetValue(f.Name, out var val))
                    {
                        if (f.FieldType.IsValueType && val == null) continue;
                        if (val == null)
                        {
                            f.SetValue(o, null);
                            continue;
                        }
                        if (f.FieldType.IsAssignableFrom(val.GetType()))
                        {
                            f.SetValue(o, val);
                            continue;
                        }
                    }
                }
                //logger.Log("Save Cache");
                ObjectCaches.AddCache(src, o);
                if (TypeCaches.ContainsKey(type))
                {
                    return ConvertObject(o);
                }
                else
                {
                    MethodInfo afterHR = type.GetMethod("OnAfterHotReload", BindingFlags.Instance | BindingFlags.Public |
                        BindingFlags.NonPublic);
                    if (afterHR != null)
                    {
                        try
                        {
                            afterHR.Invoke(o, new object[]
                            {
                            data
                            });
                        }
                        catch (Exception e)
                        {
                            Modding.Logger.Log(e);
                        }
                    }
                    ObjectCaches.AddCache(src, o);
                    return o;
                }
            }
            else
            {
                return src;
            }

        }
        public readonly static MethodInfo MConvertObject = typeof(HRLCore).GetMethod("ConvertObject");
        public static void ToMethod(ILContext iL, MethodBase target)
        {
            ILCursor cur = new ILCursor(iL);
            if (!target.IsStatic)
            {
                cur.Emit(OpCodes.Ldarg_0);
                cur.Emit(OpCodes.Call, MConvertObject);
            }
            ParameterInfo[] ps = target.GetParameters();
            for (int i = 0; i < ps.Length; i++)
            {
                cur.Emit(OpCodes.Ldarg, i + (!target.IsStatic ? 1 : 0));
            }
            if (target.IsStatic)
            {
                cur.Emit(OpCodes.Call, target);
            }
            else
            {
                cur.Emit(OpCodes.Callvirt, target);
            }
            cur.Emit(OpCodes.Ret);
        }
        [Obsolete]
        public static void HBadMethod() => throw new HRBadMethod();
        public readonly static MethodInfo MBadMethod = typeof(HRLCore).GetMethod("HBadMethod");
        public static void CType(Type st, Type tt)
        {
            if (st.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() != null)
            {
                logger.Log("CompilerGeneratedAttribute: " + st.FullName);
                return;
            }
            logger.Log("Load Type: " + st.FullName);
            TypeCaches.Add(st, tt);
            foreach (var v in st.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.DeclaredOnly
                ))
            {
                HookEndpointManager.Modify(v, new Action<ILContext>(
                    (il) =>
                    {
                        logger.Log("Modify Method IL");
                        MethodInfo m = tt.GetMethod(v.Name,
                            BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Instance | BindingFlags.Static, Type.DefaultBinder,
                            v.GetParameters().Select(x => x.ParameterType).ToArray(),
                            null);
                        if (m == null)
                        {
                            ILCursor cur = new ILCursor(il);
                            cur.Emit(OpCodes.Call, MBadMethod);
                            return;
                        }
                        else
                        {
                            ToMethod(il, m);
                        }
                    }));
            }
        }
        public static void CAssembly(Assembly src, Assembly dst)
        {
            Type[] dts = dst.GetTypes();
            foreach (var v in src.GetTypes()
                .Where(
                x => dts
                .Any(
                    x2 => dts
                    .Any(
                        x3 => x2.FullName == x3.FullName
                        )))
                .Select(
                x => (
                x,
                dts.FirstOrDefault(
                    x2 => x2.FullName == x.FullName
                    )
                )
                )
                )
            {
                try
                {
                    CType(v.x, v.Item2);
                } catch (Exception e)
                {
                    Modding.Logger.LogError(e.ToString());
                }
            }


        }
        public static void LoadAssembly(string path)
        {
            string pdb = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + ".pdb");
            Assembly ass;
            if (File.Exists(pdb))
            {
                ass = Assembly.Load(File.ReadAllBytes(path), File.ReadAllBytes(pdb));
            }
            else
            {
                ass = Assembly.Load(File.ReadAllBytes(path));
            }
            if (hrcaches.TryGetValue(path, out var old))
            {
                hrcaches[path] = ass;
                CAssembly(old, ass);
            }
            else
            {
                hrcaches[path] = ass;
                HotLoadMod(ass, true);
            }
        }
        public static string PatchPath
        {
            get
            {
                string p = Path.Combine(HKDebugMod.HKDebugPath, "HotReloadMods");
                Directory.CreateDirectory(p);
                return p;
            }
        }
        internal static Dictionary<string, DateTime> cacheTimes = new Dictionary<string, DateTime>();
        public static Dictionary<string, Assembly> hrcaches = new Dictionary<string, Assembly>();
        public static void HotLoadMod(Assembly ass,bool init = false)
        {
            foreach (var vt in ass.GetTypes().Where(x => x.IsSubclassOf(typeof(Modding.Mod)) && !x.IsAbstract))
            {
                try
                {
                    Modding.Mod m = (Modding.Mod)Activator.CreateInstance(vt);
                    MHotReload.mods.Add(m);
                    if (init)
                    {
                        m.Initialize(null);
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e.ToString());
                }
            }
        }
        public static void RefreshAssembly()
        {
            LoadConfig();
            List<string> s = new List<string>();
            s.AddRange(Directory.GetFiles(PatchPath, "*.dll"));
            s.AddRange(Config.modsPath);
            foreach(var v in s)
            {
                logger.Log("Try load path: " + v);
                if (!File.Exists(v)) continue;
                DateTime wt = File.GetLastWriteTimeUtc(v);
                if(cacheTimes.TryGetValue(v,out var v2))
                {
                    if (v2 >= wt)
                    {
                        continue;
                    }
                }
                cacheTimes[v] = wt;
                try
                {
                    logger.Log("Try load mod: " + v);
                    LoadAssembly(v);
                }catch(Exception e)
                {
                    logger.LogError(e.ToString());
                }
            }
        }
        public static void Init()
        {
            LoadConfig();
            MenuManager.AddButton(new ButtonInfo()
            {
                label = "HotReload",
                submit = (_) => MenuManager.EnterGroup(group)
            });
            group.AddButton(new ButtonInfo()
            {
                label = "刷新",
                submit = (_) => RefreshAssembly()
            });
        }
        public static void LoadConfig()
        {
            string cp = Path.Combine(HKDebugMod.ConfigPath, "HotReload.json");
            if (!File.Exists(cp))
            {
                Config = new HotReloadConfig();
                File.WriteAllText(cp, Newtonsoft.Json.JsonConvert.SerializeObject(Config, Newtonsoft.Json.Formatting.Indented));
                return;
            }
            Config = Newtonsoft.Json.JsonConvert.DeserializeObject<HotReloadConfig>(File.ReadAllText(cp));
        }
        public static readonly ButtonGroup group = new ButtonGroup();
        public static HotReloadConfig Config = new HotReloadConfig();
        public static Modding.SimpleLogger logger = new Modding.SimpleLogger("HKDebug.HotReload");
    }
}
