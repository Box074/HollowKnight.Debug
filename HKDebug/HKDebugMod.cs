using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Modding;
using Modding.Menu;
using UnityEngine;
using HKDebug.Menu;

namespace HKDebug
{
    class Script : MonoBehaviour
    {
        void Update()
        {
            var camera = Camera.main;
            if (camera != null)
            {
                if (camera.GetComponent<MenuShow>() == null) camera.gameObject.AddComponent<MenuShow>();
            }
            if (HKDebugMod.ConfigUpdate)
            {
                HKDebugMod.ConfigUpdate = false;
                HKDebugMod.TConfigUpdate();
            }

        }
        void FixedUpdate()
        {
            
            if (HitBox.HitBoxCore.enableHitBox)
            {
                HitBox.HitBoxCore.RefreshHitBox();
            }
        }
    }
    public class HKDebugMod : Mod
    {
        public override string GetVersion() => "1.0.0";
        public static bool ConfigUpdate = false;
        public static string ConfigPath
        {
            get
            {
                string p = Path.Combine(Application.dataPath, "HKDebug");
                if (!Directory.Exists(p)) Directory.CreateDirectory(p);
                return p;
            }
        }
        public static event Action OnConfigUpdate;
        internal static void TConfigUpdate()
        {
            foreach(var v in OnConfigUpdate.GetInvocationList())
            {
                try
                {
                    v.DynamicInvoke();
                }catch(Exception e)
                {
                    Modding.Logger.LogError(e);
                }
            }
        }
        FileSystemWatcher configWatcher = new FileSystemWatcher(ConfigPath, "*.json");
        public override void Initialize()
        {
            MenuManager.AddButton(new ButtonInfo()
            {
                label = "GitHub",
                submit = (_) => Application.OpenURL("https://github.com/HKLab/HollowKnight.Debug")
            });
            MenuManager.AddButton(new ButtonInfo()
            {
                label = "打开配置文件夹",
                submit = (_) => System.Diagnostics.Process.Start(ConfigPath)
            });
            UnityExplorer.Init();
            Tool.Init();
            HitBox.HitBoxCore.Init();
            FakeDebug.Init();
            HotReload.HRLCore.Init();

            var g = new GameObject("Debug");
            UnityEngine.Object.DontDestroyOnLoad(g);
            g.AddComponent<Script>();

            configWatcher.Changed += HKDebugMod_Changed;
            
        }

        private void HKDebugMod_Changed(object sender, FileSystemEventArgs e)
        {
            ConfigUpdate = true;
        }
    }
}
