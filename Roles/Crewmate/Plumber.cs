using System.Collections.Generic;
using System.Linq;
using TOHE.Modules;
using UnityEngine;
using static TOHE.Options;

namespace TOHE.Roles.Crewmate
{
    public class Plumber
    {
        public static Dictionary<byte, VentState> PlumberVentCount = new Dictionary<byte, VentState>();

        public class VentState
        {
            public int RemainingVentCount { get; set; }
            public bool IsVentFinished { get; set; }
            public VentState(int remainingVentCount) 
            {
                RemainingVentCount = remainingVentCount;
                IsVentFinished = false;
            }
        }

        public static readonly int Id = 21550;
        public static readonly List<byte> playerIdList = new();
        public static Color RoleColor = Utils.GetRoleColor(CustomRoles.Plumber);

        public static OptionItem OptionPlumberVentNumWin;
        public static OptionItem OptionEnableTargetArrow;
        public static OptionItem OptionCanGetColoredArrow;
        public static OptionItem OptionCanFindNeutralKiller;
        public static OptionItem OptionCanFindMadmate;
        public static OptionItem OptionRemainingVents;

        public static int PlumberVentNumWin;
        public static bool EnableTargetArrow;
        public static bool CanGetColoredArrow;
        public static bool CanFindNeutralKiller;
        public static bool PlumberCanBeMadmate;
        public static bool CanFindMadmate;
        public static int RemainingVentsToBeFound;

        public static readonly Dictionary<byte, bool> IsExposed = new();
        public static readonly Dictionary<byte, bool> IsComplete = new();

        public static readonly HashSet<byte> TargetList = new();
        public static readonly Dictionary<byte, Color> TargetColorlist = new();

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Plumber);
            OptionEnableTargetArrow = BooleanOptionItem.Create(Id + 10, "PlumberEnableTargetArrow", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Plumber]);
            OptionCanGetColoredArrow = BooleanOptionItem.Create(Id + 11, "PlumberCanGetArrowColor", true, TabGroup.CrewmateRoles, false).SetParent(OptionEnableTargetArrow);
            OptionCanFindNeutralKiller = BooleanOptionItem.Create(Id + 12, "PlumberCanFindNeutralKiller", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Plumber]);
            OptionCanFindMadmate = BooleanOptionItem.Create(Id + 13, "PlumberCanFindMadmate", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Plumber]);
            OptionRemainingVents = IntegerOptionItem.Create(Id + 14, "PlumberRemainingVentsFound", new(5, 50, 5), 10, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Plumber]);
            OptionPlumberVentNumWin = IntegerOptionItem.Create(Id + 15, "PlumberVentNumWin", new(5, 900, 5), 55, TabGroup.CrewmateRoles, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Plumber])
            .SetValueFormat(OptionFormat.Times);
        }
        public static void Init()
        {
            playerIdList.Clear();
            IsEnable = false;

            PlumberVentNumWin = OptionPlumberVentNumWin.GetInt();
            EnableTargetArrow = OptionEnableTargetArrow.GetBool();
            CanGetColoredArrow = OptionCanGetColoredArrow.GetBool();
            CanFindNeutralKiller = OptionCanFindNeutralKiller.GetBool();
            CanFindMadmate = OptionCanFindMadmate.GetBool();
            RemainingVentsToBeFound = OptionRemainingVents.GetInt();

            IsExposed.Clear();
            IsComplete.Clear();

            TargetList.Clear();
            TargetColorlist.Clear();
        }

        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            IsEnable = true;

            IsExposed[playerId] = false;
            IsComplete[playerId] = false;

            PlumberVentCount[playerId] = new VentState(PlumberVentNumWin);
        }

        public static bool IsEnable;
        public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
        public static bool GetExpose(PlayerControl pc)
        {
            if (!IsThisRole(pc.PlayerId) || !pc.IsAlive() || pc.Is(CustomRoles.Madmate)) return false;

            var plumberId = pc.PlayerId;
            return IsExposed[plumberId];
        }

        public static bool IsPlumberTarget(PlayerControl target) => IsEnable && (target.Is(CustomRoleTypes.Impostor) && !target.Is(CustomRoles.Trickster) || (target.IsPlumberTarget() && CanFindNeutralKiller) || (target.Is(CustomRoles.Madmate) && CanFindMadmate));
        public static void CheckVent(PlayerControl plumber)
        {
            if (!plumber.IsAlive() || plumber.Is(CustomRoles.Madmate)) return;

            var plumberId = plumber.PlayerId;
            Main.PlumberVentCount[plumberId] = 0;

            if (!IsExposed[plumberId] && PlumberVentNumWin - PlumberVentCount[plumberId].RemainingVentCount <= RemainingVentsToBeFound)
            {
                foreach (var target in Main.AllAlivePlayerControls)
                {
                    if (!IsPlumberTarget(target)) continue;

                    TargetArrow.Add(target.PlayerId, plumberId);
                }
                IsExposed[plumberId] = true;  // Set IsExposed to true when the conditions are met
            }

            if (IsComplete[plumberId] || !PlumberVentCount[plumberId].IsVentFinished) return;


            foreach (var target in Main.AllAlivePlayerControls)
            {
                if (!IsPlumberTarget(target)) continue;

                var targetId = target.PlayerId;
                NameColorManager.Add(plumberId, targetId);

                if (!EnableTargetArrow) continue;

                TargetArrow.Add(plumberId, targetId);

                //ターゲットは共通なので2回登録する必要はない
                if (!TargetList.Contains(targetId))
                {
                    TargetList.Add(targetId);

                    if (CanGetColoredArrow)
                        TargetColorlist.Add(targetId, target.GetRoleColor());
                }
            }

            NameNotifyManager.Notify(plumber, Translator.GetString("PlumberDoneVents"));

            IsComplete[plumberId] = true;
        }

        /// <summary>
        /// タスクが進んだスニッチに警告マーク
        /// </summary>
        /// <param name="seer">キラーの場合有効</param>
        /// <param name="target">スニッチの場合有効</param>
        /// <returns></returns>
        public static string GetWarningMark(PlayerControl seer, PlayerControl target)
            => IsPlumberTarget(seer) && GetExpose(target) ? Utils.ColorString(RoleColor, "★") : "";

        /// <summary>
        /// キラーからスニッチに対する矢印
        /// </summary>
        /// <param name="seer">キラーの場合有効</param>
        /// <param name="target">キラーの場合有効</param>
        /// <returns></returns>
        public static string GetWarningArrow(PlayerControl seer, PlayerControl target = null)
        {
            if (GameStates.IsMeeting || !IsPlumberTarget(seer)) return "";
            if (target != null && seer.PlayerId != target.PlayerId) return "";

            var exposePlumber = playerIdList.Where(s => !Main.PlayerStates[s].IsDead && IsExposed[s]);
            if (exposePlumber.Count() == 0) return "";

            var warning = "↕";
            if (EnableTargetArrow)
                warning += TargetArrow.GetArrows(seer, exposePlumber.ToArray());

            return Utils.ColorString(RoleColor, warning);
        }
        /// <summary>
        /// スニッチからキラーへの矢印
        /// </summary>
        /// <param name="seer">スニッチの場合有効</param>
        /// <param name="target">スニッチの場合有効</param>
        /// <returns></returns>
        public static string GetPlumberArrow(PlayerControl seer, PlayerControl target = null)
        {
            if (!IsThisRole(seer.PlayerId) || seer.Is(CustomRoles.Madmate)) return "";
            if (!EnableTargetArrow || GameStates.IsMeeting) return "";
            if (target != null && seer.PlayerId != target.PlayerId) return "";
            var arrows = "";
            foreach (var targetId in TargetList)
            {
                var arrow = TargetArrow.GetArrows(seer, targetId);
                arrows += CanGetColoredArrow ? Utils.ColorString(TargetColorlist[targetId], arrow) : arrow;
            }
            return arrows;
        }
        public static void OnCompleteVents(PlayerControl player)
        {
            if (!IsThisRole(player.PlayerId) || player.Is(CustomRoles.Madmate)) return;
            CheckVent(player);
        }
        public static void OnEnterVent(PlayerControl pc)
        {
            if (pc.Is(CustomRoles.Plumber))
            {
                Main.PlumberVentCount.TryAdd(pc.PlayerId, 0);
                Main.PlumberVentCount[pc.PlayerId]++;
                Utils.NotifyRoles(pc);

                if (pc.AmOwner)
                {
                    CustomSoundsManager.Play("MarioCoin");
                }

                if (AmongUsClient.Instance.AmHost && Main.PlumberVentCount[pc.PlayerId] >= Plumber.PlumberVentNumWin)
                {
                    Plumber.OnCompleteVents(pc);
                    Plumber.GetExpose(pc);
                }
            }
        }
    }
}
