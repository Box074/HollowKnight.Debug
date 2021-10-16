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
using Modding;

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
            if (Object == null || !Object.IsAlive) return false;
            if (obj == null) return false;
            if(obj is ObjectHandler handler)
            {
                if (!handler.Object.IsAlive) return false;
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
            if (!caches.TryGetValue(t,out var table))
            {
                table = new LinkedList<(ObjectHandler, object)>();
                caches.Add(t, table);
            }
            return table.FirstOrDefault(x => x.Item1.Equals(src)).Item2;
        }
        public void AddCache(object src,object dst)
        {
            if (src == null || dst == null) return;

            if(!caches.TryGetValue(src.GetType(),out var table))
            {
                table = new LinkedList<(ObjectHandler, object)>();
                caches.Add(src.GetType(), table);
            }
            table.AddFirst((new ObjectHandler(src), dst));
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
        private static readonly Type ModLoaderType = typeof(IMod).Assembly.GetType("Modding.ModLoader");

        private static readonly MethodInfo ModLoaderAddModInstance =
            ModLoaderType.GetMethod("AddModInstance", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo ModLoaderUpdateModText =
            ModLoaderType.GetMethod("UpdateModText", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly Type ModInstance = ModLoaderType.GetNestedType("ModInstance");


        public readonly static Dictionary<Type, Type> TypeCaches = new Dictionary<Type, Type>();
        public readonly static LinkedList<Type> NeedInitStatic = new LinkedList<Type>();
        public readonly static HRObjectCache ObjectCaches = new HRObjectCache();
        public static void CleanObjectCache()
        {
            ObjectCaches.Clean();
        }
        public static object ConvertObject(object src)
        {
            //return null;//TODO
            //logger.Log("Convert Object: " + src?.ToString());
            if (src == null) return null;
            Type st = src.GetType();

            var o = ObjectCaches.TryGetCache(src);

            //return null;
            if (o != null)
            {
                //logger.Log("Use Cache: " + o?.ToString());
                return o;
            }


            if (TypeCaches.TryGetValue(st, out var type))
            {
                //logger.Log("T/F:" + (st == type).ToString());
                //logger.Log("Create Instance: " + st.FullName);
                //return null;
                if (st.IsSubclassOf(typeof(UnityEngine.Component)))
                {
                    return ComponentHelper.ConvertComponent((UnityEngine.Component)src, type);
                }
                o = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);

                Dictionary<string, object> data = new Dictionary<string, object>();
                foreach (var f in st.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    data[f.Name] = ConvertObject(f.GetValue(src));
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
                //logger.Log("Not Catch");
                return src;
            }

        }
        public readonly static MethodInfo MConvertObject = typeof(HRLCore).GetMethod("ConvertObject");
        public static void ToMethod(ILContext iL, MethodBase target)
        {
            ILCursor cur = new ILCursor(iL);
            logger.Log("Method: " + target.Name);
            if (!target.IsStatic)
            {
                cur.Emit(OpCodes.Ldarg_0);
                cur.Emit(OpCodes.Call, MConvertObject);
            }
            ParameterInfo[] ps = target.GetParameters();
            for (int i = 0; i < ps.Length; i++)
            {
                
                //logger.Log("Push arg[" + i + "]: " + ps[i].Name);
                cur.Emit(OpCodes.Ldarg, i + (target.IsStatic ? 0 : 1));
            }
            //cur.Emit(OpCodes.Call, MBadMethod);
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
        public static Type TryGetType(Type o)
        {
            if(TypeCaches.TryGetValue(o,out var v))
            {
                return v;
            }
            return o;
        }
        [Obsolete]
        public static void HBadMethod() => throw new HRBadMethod();
        public readonly static MethodInfo MBadMethod = typeof(HRLCore).GetMethod("HBadMethod");
        public static void CType(Type st, Type tt)
        {
            if (st == tt)
            {
                logger.LogError("Bad Type");
                return;
            }
            if(st.IsGenericType || tt.IsGenericType)
            {
                logger.LogError("无法处理泛型类型: " + st.FullName);
            }
            /*if (st.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() != null)
            {
                //logger.Log("CompilerGeneratedAttribute: " + st.FullName);
                return;
            }*/
            logger.Log("Load Type: " + st.FullName);
            TypeCaches[st] = tt;
            
            foreach (var v in st.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.DeclaredOnly
                ))
            {
                HookEndpointManager.Modify(v, new Action<ILContext>(
                    (il) =>
                    {
                        MethodInfo m = tt.GetMethod(v.Name,
                            BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Instance | BindingFlags.Static, Type.DefaultBinder,
                            v.GetParameters().Select(x => TryGetType(x.ParameterType)).ToArray(),
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
        public static Type[] GetTypesForAssembly(Assembly ass)
        {
            List<Type> types = new List<Type>();
            void FindInType(Type p,List<Type> list)
            {
                foreach(var v in p.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic
                    | BindingFlags.Static | BindingFlags.Instance))
                {
                    list.Add(v);
                    FindInType(v, list);
                }
            }
            foreach(var v in ass.GetTypes())
            {
                types.Add(v);
                //FindInType(v, types);
            }
            return types.ToArray();
        }
        public static void InitStatic(Type s,Type d)
        {
            if (s == null || d == null) return;
            if (s.IsGenericType || d.IsGenericType)
            {
                return;
            }
            Dictionary<string, object> data = new Dictionary<string, object>();
            foreach (var f in s.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                data[f.Name] = ConvertObject(f.GetValue(null));
            }
            foreach (var f in d.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (data.TryGetValue(f.Name, out var val))
                {
                    if (f.FieldType.IsValueType && val == null) continue;
                    if (val == null)
                    {
                        f.SetValue(null, null);
                        continue;
                    }
                    if (f.FieldType.IsAssignableFrom(val.GetType()))
                    {
                        f.SetValue(null, val);
                        continue;
                    }
                }
            }
        }
        public static void CAssembly(Assembly src, Assembly dst)
        {
            List<(Type,Type)> ns = new List<(Type, Type)>();
            Type[] dts = GetTypesForAssembly(dst);
            foreach (var v in GetTypesForAssembly(src)
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
                    if (v.Item2.IsSubclassOf(typeof(Delegate))) continue;
                    ns.Add(v);
                    CType(v.x, v.Item2);
                } catch (Exception e)
                {
                    Modding.Logger.LogError(e.ToString());
                }
                foreach(var v2 in ns)
                {
                    try
                    {
                        InitStatic(v2.Item1, v2.Item2);
                    }catch(Exception e)
                    {
                        logger.LogError(e);
                    }
                }
            }
        }
        static int hrcount = 0;
        public static void LoadAssembly(string path)
        {
            if (!File.Exists(path)) return;
            byte[] b = File.ReadAllBytes(path);
            MemoryStream stream = new MemoryStream(b, true);
            using (AssemblyDefinition assd = AssemblyDefinition.ReadAssembly(stream))
            {
                
                assd.Name.Name = assd.Name.Name + ".HotReload" + hrcount;
                hrcount++;
                assd.Write(stream);
            }
            b = stream.ToArray();

            string pdb = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + ".pdb");
            Assembly ass;
            if (File.Exists(pdb))
            {
                ass = Assembly.Load(b, File.ReadAllBytes(pdb));
            }
            else
            {
                ass = Assembly.Load(b);
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
            foreach (var vt in ass.GetTypes().Where(x => x.IsSubclassOf(typeof(Mod)) && !x.IsAbstract))
            {
                try
                {
                    Mod m = (Mod)Activator.CreateInstance(vt);
                    MHotReload.mods.Add(m);
                    if (init)
                    {
                        m.Initialize(null);
                    }
                    object mi = Activator.CreateInstance(ModInstance);
                    Modding.ReflectionHelper.SetField(mi, "Mod", m);
                    Modding.ReflectionHelper.SetField(mi, "Enable", true);
                    Modding.ReflectionHelper.SetField(mi, "Name", m.GetName());
                    ModLoaderAddModInstance.Invoke(null, new object[]
                    {
                        vt, mi
                    });
                    ModLoaderUpdateModText.Invoke(null, null);
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
                    if (v2 >= wt && !Config.ingoreLastWriteTime)
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
        public static void LoadConfig() => Config = HKDebug.Config.LoadConfig("HotReload", () => new HotReloadConfig());
        public static readonly ButtonGroup group = new ButtonGroup();
        public static HotReloadConfig Config = new HotReloadConfig();
        public static Modding.SimpleLogger logger = new Modding.SimpleLogger("HKDebug.HotReload");
    }
}
