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
        public override string GetVersion() => "1.0.2";
        public static string ConfigPath
        {
            get
            {
                string p = Path.Combine(HKDebugPath, "Config");
                if (!Directory.Exists(p)) Directory.CreateDirectory(p);
                return p;
            }
        }
        public static string HKDebugPath
        {
            get
            {
                string p = Path.Combine(Application.dataPath, "HKDebug");
                if (!Directory.Exists(p)) Directory.CreateDirectory(p);
                return p;
            }
        }
        public override void Initialize()
        {
            Logo.Init();
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
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

            
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            File.WriteAllText(Path.Combine(HKDebugPath, "error.log"), e.ExceptionObject.ToString());
        }
    }
}
