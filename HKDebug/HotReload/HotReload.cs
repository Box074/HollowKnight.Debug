using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Modding;

namespace HKDebug.HotReload
{
    class MHotReload : Mod
    {
        public static List<Mod> mods = new List<Mod>();
        public MHotReload() : base("HotReload")
        {
            Log("Try to load mods");
            foreach (var v in Directory.GetFiles(HRLCore.PatchPath).Where(x=>Path.GetExtension(x) == ".dll"))
            {
                try
                {
                    Log("Load mod: " + v);
                    HRLCore.cacheTimes[v] = File.GetLastWriteTimeUtc(v);
                    string pdb = Path.Combine(Path.GetDirectoryName(v), Path.GetFileNameWithoutExtension(v) + ".pdb");
                    Assembly ass;
                    if (File.Exists(pdb))
                    {
                        ass = Assembly.Load(File.ReadAllBytes(v), File.ReadAllBytes(pdb));
                    }
                    else
                    {
                        ass = Assembly.Load(File.ReadAllBytes(v));
                    }
                    foreach (var vt in ass.GetTypes().Where(x => x.IsSubclassOf(typeof(Mod)) && !x.IsAbstract))
                    {
                        Log("Find type: " + vt.FullName);
                        try
                        {
                            Mod m = (Mod)Activator.CreateInstance(vt);
                            Log("mod: " + m.GetName());
                            mods.Add(m);
                        }
                        catch (Exception e)
                        {
                            LogError(e.ToString());
                        }
                    }

                }
                catch (Exception e)
                {
                   LogError(e.ToString());
                }
            }
        }
        public override void Initialize()
        {
            foreach(var v in mods)
            {
                Log("Initialize mod: " + v);
                try
                {
                    v.Initialize(null);
                }catch(Exception e)
                {
                    v.LogError(e.ToString());
                }
            }
        }
        public override string GetVersion() => "1.0.0";
        
    }
}
