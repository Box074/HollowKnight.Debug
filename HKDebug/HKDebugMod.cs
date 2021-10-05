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
            if (HitBox.HitBoxCore.autoFind)
            {
                HitBox.HitBoxCore.RefreshHitBox();
            }
        }
    }
    public class HKDebugMod : Mod
    {
        public static string ConfigPath
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
            MenuManager.AddButton(new ButtonInfo()
            {
                label = "Open Config",
                submit = (_) => System.Diagnostics.Process.Start(ConfigPath)
            });
            UnityExplorer.Init();

            HitBox.HitBoxCore.Init();

            var g = new GameObject("Debug");
            UnityEngine.Object.DontDestroyOnLoad(g);
            g.AddComponent<Script>();

            MenuManager.AddButton(new ButtonInfo()
            {
                label = "Respawn",
                submit = (_) => HeroController.instance.StartCoroutine(HeroController.instance.Respawn())
            });
            MenuManager.AddButton(new ButtonInfo()
            {
                label = "Accept Pause",
                submit = (_) => PlayerData.instance.disablePause = false
            });
            MenuManager.AddButton(new ButtonInfo()
            {
                label = "End Boss Scene",
                submit = (_) => BossSceneController.Instance.EndBossScene()
            });
            MenuManager.AddButton(new ButtonInfo()
            {
                label = "Accept Move",
                submit = (_) =>
                {
                    HeroController.instance.hero_state = GlobalEnums.ActorStates.idle;
                    HeroController.instance.AcceptInput();
                }
            });
            MenuManager.AddButton(new ButtonInfo()
            {
                label = "Enable No Damage",
                submit = (but) =>
                {
                    HeroController.instance.takeNoDamage = !HeroController.instance.takeNoDamage;
                    but.label = (HeroController.instance.takeNoDamage ? "Disable" : "Enable") + " No Damage";
                }
            });
        }
    }
}
