using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using Utility;
using PlagueButtonAPI;
using PlagueButtonAPI.Controls;
using PlagueButtonAPI.Controls.Grouping;
using PlagueButtonAPI.External_Libraries;
using PlagueButtonAPI.Main;
using PlagueButtonAPI.Pages;
using UnityEngine;
using VRC;
using VRC.Core;
using VRC.UI.Core;

[assembly: MelonInfo(typeof(MainClass), "Utility", "1.0", "Candl3 & Plague")]
[assembly: MelonGame("VRChat", "VRChat")]
[assembly: MelonColor(ConsoleColor.Magenta)]

namespace Utility
{
    internal class MainClass : MelonMod
    {
        public static HarmonyLib.Harmony harmony;

        private static IEnumerable<BaseModule> BaseModules = null;

        private static IEnumerable<BaseModule> BaseModulesOnUpdate = null;

        internal static int FPS = 0;

        private float Offset = 0f;

        internal static CollapsibleButtonGroup MainMenu = null;

        internal static CollapsibleButtonGroup UserSelectedPage = null;

        public class Configuration
        {
            public bool ExampleSavedVariable { get; set; } = true; // True On User's First Ever Time Using This Mod; Saves Their Preference Automatically On Editing.
            public bool Test1 { get; set; } = true;

            public bool HideOverDistance { get; set; } = false;
        }

        public static ConfigLib<Configuration> Config = new(Environment.CurrentDirectory + "\\Utility.json");

        public override void OnApplicationStart()
        {
            harmony = HarmonyInstance;

            var AllModules = Assembly.GetTypes().Where(o => o.IsSubclassOf(typeof(BaseModule))).OrderBy(o => (o.GetCustomAttributes(false).FirstOrDefault(q => q is LoadOrder) as LoadOrder)?.Priority).Select(a => (BaseModule)Activator.CreateInstance(a));

            var ModulesWithUpdate = AllModules.Where(o =>
                o.GetType().GetMethod("OnFixedUpdate", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly) != null || o.GetType().GetMethod("OnUpdate", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly) != null ||
                o.GetType().GetMethod("OnLateUpdate", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly) != null ||
                o.GetType().GetMethod("OnSecondPassed", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly) != null).ToList(); // I hate reflection

            var ModulesWithoutUpdate = AllModules.Where(o => !ModulesWithUpdate.Contains(o)).ToList();

            BaseModules = ModulesWithoutUpdate;
            BaseModulesOnUpdate = ModulesWithUpdate;

            MelonLogger.Msg($"Loaded {ModulesWithoutUpdate.Count} Modules, {ModulesWithUpdate.Count} With Updates Per Frame.");

            MelonCoroutines.Start(WaitForUiManager());

            Hooks.OnAvatarChanged_Post += NetworkEvents_OnAvatarChanged;
            Hooks.OnAvatarInstantiated += NetworkEvents_OnAvatarInstantiated;
            Hooks.OnPlayerJoin += NetworkEvents_OnPlayerJoined;
            Hooks.OnPlayerLeave += NetworkEvents_OnPlayerLeft;
            Hooks.OnRoomJoin += NetworkEvents_OnRoomJoined;
            Hooks.OnRoomLeave += NetworkEvents_OnRoomLeft;

            foreach (var module in BaseModules)
            {
                module?.OnApplicationStart();
            }

            MelonLogger.Msg("Finished Loading Utility!");
        }


        private void NetworkEvents_OnRoomLeft()
        {
            foreach (var module in BaseModules)
            {
                module?.OnRoomLeft();
            }
        }

        private void NetworkEvents_OnRoomJoined()
        {
            foreach (var module in BaseModules)
            {
                module?.OnRoomJoined();
            }
        }

        private void NetworkEvents_OnPlayerLeft(Player obj)
        {
            MelonLogger.Msg($"Player Left: {obj?.field_Private_APIUser_0?.displayName}");

            foreach (var module in BaseModules)
            {
                module?.OnPlayerLeft(obj);
            }
        }

        private void NetworkEvents_OnPlayerJoined(Player obj)
        {
            MelonLogger.Msg($"Player Joined: {obj?.field_Private_APIUser_0?.displayName}");

            foreach (var module in BaseModules)
            {
                module?.OnPlayerJoined(obj);
            }
        }

        private void NetworkEvents_OnAvatarChanged(VRCAvatarManager arg1, GameObject arg2, string arg3, float arg4, ApiAvatar arg5)
        {
            foreach (var module in BaseModules)
            {
                module?.OnAvatarChanged(arg1, arg2, arg3, arg4, arg5);
            }

            if (arg1.field_Private_VRCPlayer_0 == VRCPlayer.field_Internal_Static_VRCPlayer_0)
            {
                MelonLogger.Msg("Avatar Changed!");
            }
        }

        private void NetworkEvents_OnAvatarInstantiated(VRCAvatarManager arg1, ApiAvatar arg2, GameObject arg3)
        {
            foreach (var module in BaseModules)
            {
                module?.OnAvatarInstantiated(arg1, arg2, arg3);
            }

            if (arg1.field_Private_VRCPlayer_0 == VRCPlayer.field_Internal_Static_VRCPlayer_0)
            {
                MelonLogger.Msg("Avatar Instantiated!");
            }
        }

        public override void OnApplicationLateStart()
        {
            foreach (var module in BaseModules)
            {
                module?.OnApplicationLateStart();
            }
        }

        public override void OnApplicationQuit()
        {
            foreach (var module in BaseModules)
            {
                module?.OnApplicationQuit();
            }
        }

        public override void OnGUI()
        {
            foreach (var module in BaseModules)
            {
                module?.OnGUI();
            }
        }

        private IEnumerator WaitForUiManager()
        {
            while (VRCUiManager.field_Private_Static_VRCUiManager_0 == null)
            {
                yield return null;
            }

            foreach (var module in BaseModules)
            {
                module?.OnUIManagerInit();
            }

            while (UIManager.field_Private_Static_UIManager_0 == null)
            {
                yield return null;
            }

            while (GameObject.Find("UserInterface").GetComponentInChildren<VRC.UI.Elements.QuickMenu>(true) == null)
            {
                yield return null;
            }

            OnQuickMenuInit();
        }

        public static void OnQuickMenuInit()
        {
            ButtonAPI.OnInit += () =>
            {
                var Page = MenuPage.CreatePage(WingSingleButton.Wing.Both, null, "Utility", "<color=#ff00ff>Utility</color>", true, false, null, "", null, true).Item1;

                MainMenu = Page.AddCollapsibleButtonGroup("Main Options", true);

                UserSelectedPage = new CollapsibleButtonGroup(TransformHelper.SelectedUser_Local, "<color=#ff00ff>Utility</color>");

                foreach (var module in BaseModules)
                {
                    try
                    {
                        module?.OnQuickMenuInit();
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Error("A BaseModule Failed To Execute OnQuickMenuInit!\n\n" + ex);
                    }
                }
            };
        }

        public override void OnFixedUpdate()
        {
            foreach (var module in BaseModulesOnUpdate)
            {
                module?.OnFixedUpdate();
            }
        }

        public override void OnUpdate()
        {
            FPS = (int)(1f / Time.smoothDeltaTime);

            foreach (var module in BaseModulesOnUpdate)
            {
                module?.OnUpdate();
            }

            if (Time.time > Offset)
            {
                Offset = Time.time + 1f;

                foreach (var module in BaseModulesOnUpdate)
                {
                    module?.OnSecondPassed();
                }
            }
        }

        public override void OnLateUpdate()
        {
            foreach (var module in BaseModulesOnUpdate)
            {
                module?.OnLateUpdate();
            }
        }

        public override void OnLevelWasInitialized(int level)
        {
            foreach (var module in BaseModules)
            {
                module?.OnLevelWasInitialized(level);
            }
        }

        public override void OnLevelWasLoaded(int level)
        {
            foreach (var module in BaseModules)
            {
                module?.OnLevelWasLoaded(level);
            }
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            foreach (var module in BaseModules)
            {
                module?.OnSceneWasInitialized(buildIndex, sceneName);
            }
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            foreach (var module in BaseModules)
            {
                module?.OnSceneWasLoaded(buildIndex, sceneName);
            }
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            foreach (var module in BaseModules)
            {
                module?.OnSceneWasUnloaded(buildIndex, sceneName);
            }
        }

        public override void OnModSettingsApplied()
        {
            foreach (var module in BaseModules)
            {
                module?.OnModSettingsApplied();
            }
        }

        public override void OnPreferencesLoaded()
        {
            foreach (var module in BaseModules)
            {
                module?.OnPreferencesLoaded();
            }
        }

        public override void OnPreferencesSaved()
        {
            foreach (var module in BaseModules)
            {
                module?.OnPreferencesSaved();
            }
        }
    }
}
