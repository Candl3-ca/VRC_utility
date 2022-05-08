using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using PlagueButtonAPI;
using PlagueButtonAPI.Misc;
using UnityEngine;
using Utility;
using VRC;

namespace ModBase
{
    internal class ExampleModule : BaseModule
    {
        public override void OnApplicationStart()
        {
            MelonLogger.Msg("ExampleModule Loaded!");
        }

        public override void OnQuickMenuInit()
        {
            MainClass.MainMenu.AddSimpleSingleButton("sus?", "Red do be sus tho", () =>
            {
                ButtonAPI.GetQuickMenuInstance().ShowAlert("among us");
            });

            MainClass.MainMenu.AddToggleButton("Hide over distance", "Hides avatars that are 10 meters away", b =>
            {
                MainClass.Config.InternalConfig.HideOverDistance = b;

                if (!b)
                {
                    var players = PlayerManager.prop_PlayerManager_0.GetPlayers().Where(o => o != Player.prop_Player_0);

                    foreach (var player in players)
                    {
                        var avatar = player.transform.Find("ForwardDirection/Avatar").gameObject;

                        avatar.SetActive(true);
                    }
                }
            }, MainClass.Config.InternalConfig.HideOverDistance);
        }

        public override void OnUpdate()
        {
            if (MainClass.Config.InternalConfig.HideOverDistance)
            {
                var players = PlayerManager.prop_PlayerManager_0.GetPlayers().Where(o => o != Player.prop_Player_0);

                foreach (var player in players)
                {
                    var avatar = player.transform.Find("ForwardDirection/Avatar").gameObject;

                    var distance = Vector3.Distance(player.transform.position, Player.prop_Player_0.transform.position);

                    if (avatar.active != (distance < 10f))
                    {
                       avatar.SetActive((distance < 10f));
                    }
                }
            }
        }
    }
}
