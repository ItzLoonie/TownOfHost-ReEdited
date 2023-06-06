using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AmongUs.GameOptions;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.Crewmate
{
    public static class Farseer
    {
        private static readonly int Id = 7052269;

        public static OptionItem FarseerCooldown;
        public static OptionItem FarseerRevealTime;
        public static OptionItem Vision; 

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Farseer);
            FarseerCooldown = FloatOptionItem.Create(Id + 10, "FarseerRevealCooldown", new(0f, 990f, 2.5f), 25f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Farseer])
                .SetValueFormat(OptionFormat.Seconds);
            FarseerRevealTime = FloatOptionItem.Create(Id + 11, "FarseerRevealTime", new(0f, 60f, 1f), 10f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Farseer])
                .SetValueFormat(OptionFormat.Seconds);
            Vision = FloatOptionItem.Create(Id + 12, "FarseerVision", new(0f, 5f, 0.05f), 0.25f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Farseer])
                .SetValueFormat(OptionFormat.Multiplier);

        }

        public static void SetCooldown(byte id) => Main.AllPlayerKillCooldown[id] = FarseerCooldown.GetFloat();

        public static void OnPostFix(PlayerControl player)
        {
            if (GameStates.IsInTask && Main.FarseerTimer.ContainsKey(player.PlayerId))//アーソニストが誰かを塗っているとき
            {
                if (!player.IsAlive() || Pelican.IsEaten(player.PlayerId))
                {
                    Main.FarseerTimer.Remove(player.PlayerId);
                    Utils.NotifyRoles(player);
                    RPC.ResetCurrentRevealTarget(player.PlayerId);
                }
                else
                {
                    var ar_target = Main.FarseerTimer[player.PlayerId].Item1;//塗られる人
                    var ar_time = Main.FarseerTimer[player.PlayerId].Item2;//塗った時間
                    if (!ar_target.IsAlive())
                    {
                        Main.FarseerTimer.Remove(player.PlayerId);
                    }
                    else if (ar_time >= Farseer.FarseerRevealTime.GetFloat())//時間以上一緒にいて塗れた時
                    {
                        player.SetKillCooldown();
                        Main.FarseerTimer.Remove(player.PlayerId);//塗が完了したのでDictionaryから削除
                        Main.isRevealed[(player.PlayerId, ar_target.PlayerId)] = true;//塗り完了
                        player.RpcSetRevealtPlayer(ar_target, true);
                        Utils.NotifyRoles(player);//名前変更
                        RPC.ResetCurrentRevealTarget(player.PlayerId);
                    }
                    else
                    {

                        float range = NormalGameOptionsV07.KillDistances[Mathf.Clamp(player.Is(CustomRoles.Reach) ? 2 : Main.NormalOptions.KillDistance, 0, 2)] + 0.5f;
                        float dis = Vector2.Distance(player.transform.position, ar_target.transform.position);//距離を出す
                        if (dis <= range)//一定の距離にターゲットがいるならば時間をカウント
                        {
                            Main.FarseerTimer[player.PlayerId] = (ar_target, ar_time + Time.fixedDeltaTime);
                        }
                        else//それ以外は削除
                        {
                            Main.FarseerTimer.Remove(player.PlayerId);
                            Utils.NotifyRoles(player);
                            RPC.ResetCurrentRevealTarget(player.PlayerId);

                            Logger.Info($"Canceled: {player.GetNameWithRole()}", "Arsonist");
                        }
                    }
                }
            }
        }
    }
}
