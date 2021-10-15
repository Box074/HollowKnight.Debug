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
            HRLCore.LoadConfig();
            List<string> s = new List<string>();
            s.AddRange(Directory.GetFiles(HRLCore.PatchPath, "*.dll"));
            s.AddRange(HRLCore.Config.modsPath);
            foreach (var v in s)
            {
                try
                {
                    if (!File.Exists(v)) continue;
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
                    HRLCore.hrcaches[v] = ass;
                    HRLCore.HotLoadMod(ass, false);

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
