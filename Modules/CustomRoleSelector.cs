﻿using AmongUs.GameOptions;
using System;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Neutral;

namespace TOHE.Modules;

internal class CustomRoleSelector
{
    public static Dictionary<PlayerControl, CustomRoles> RoleResult;
    public static IReadOnlyList<CustomRoles> AllRoles => RoleResult.Values.ToList();

    public static void SelectCustomRoles()
    {
        // 开始职业抽取
        RoleResult = new();
        var rd = IRandom.Instance;
        int playerCount = Main.AllAlivePlayerControls.Count();
        int optImpNum = Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors);
        int optNonNeutralKillingNum = 0;
        int optNeutralKillingNum = 0;
        int optCovenNum = 0;

        if (Options.NonNeutralKillingRolesMaxPlayer.GetInt() > 0 && Options.NonNeutralKillingRolesMaxPlayer.GetInt() >= Options.NonNeutralKillingRolesMinPlayer.GetInt())
        {
            optNonNeutralKillingNum = rd.Next(Options.NonNeutralKillingRolesMinPlayer.GetInt(), Options.NonNeutralKillingRolesMaxPlayer.GetInt() + 1);
        }
        if (Options.NeutralKillingRolesMaxPlayer.GetInt() > 0 && Options.NeutralKillingRolesMaxPlayer.GetInt() >= Options.NeutralKillingRolesMinPlayer.GetInt())
        {
            optNeutralKillingNum = rd.Next(Options.NeutralKillingRolesMinPlayer.GetInt(), Options.NeutralKillingRolesMaxPlayer.GetInt() + 1);
        }
        if (Options.CovenRolesMaxPlayer.GetInt() > 0 && Options.CovenRolesMaxPlayer.GetInt() >= Options.CovenRolesMinPlayer.GetInt())
        {
            optCovenNum = rd.Next(Options.CovenRolesMinPlayer.GetInt(), Options.CovenRolesMaxPlayer.GetInt() + 1);
        }

        int readyRoleNum = 0;
        int readyNonNeutralKillingNum = 0;
        int readyNeutralKillingNum = 0;
        int readyCovenNum = 0;

        List<CustomRoles> rolesToAssign = new();
        List<CustomRoles> roleList = new();
        List<CustomRoles> roleOnList = new();

        List<CustomRoles> ImpOnList = new();
        List<CustomRoles> ImpRateList = new();

        List<CustomRoles> NonNeutralKillingOnList = new();
        List<CustomRoles> NonNeutralKillingRateList = new();

        List<CustomRoles> NeutralKillingOnList = new();
        List<CustomRoles> NeutralKillingRateList = new();

        List<CustomRoles> CovenOnList = new();
        List<CustomRoles> CovenRateList = new();

        List<CustomRoles> roleRateList = new();

        foreach (var cr in CustomRolesHelper.AllRoles)
        {
            var role = (CustomRoles)Enum.Parse(typeof(CustomRoles), cr.ToString());
            if (role.IsVanilla() || role.IsAdditionRole()) continue;
            if (role is CustomRoles.GM or CustomRoles.NotAssigned) continue;
            for (int i = 0; i < role.GetCount(); i++)
                roleList.Add(role);
        }

        // 职业设置为：优先
        foreach (var role in roleList) if (role.GetMode() == 2)
        {
            if (role.IsImpostor()) ImpOnList.Add(role);
            else if (role.IsNonNK()) NonNeutralKillingOnList.Add(role);
            else if (role.IsNK()) NeutralKillingOnList.Add(role);
            else if (role.IsCoven()) CovenOnList.Add(role);
            else roleOnList.Add(role);
        }
        // 职业设置为：启用
        foreach (var role in roleList) if (role.GetMode() == 1)
        {
            if (role.IsImpostor()) ImpRateList.Add(role);
            else if (role.IsNonNK()) NonNeutralKillingRateList.Add(role);
            else if (role.IsNK()) NeutralKillingRateList.Add(role);
            else if (role.IsCoven()) CovenRateList.Add(role);
            else roleRateList.Add(role);
        }

        // 抽取优先职业（内鬼）
        while (ImpOnList.Any())
        {
            var select = ImpOnList[rd.Next(0, ImpOnList.Count)];
            ImpOnList.Remove(select);
            rolesToAssign.Add(select);
            readyRoleNum++;
            Logger.Info(select.ToString() + " 加入内鬼职业待选列表（优先）", "CustomRoleSelector");
            if (readyRoleNum >= playerCount) goto EndOfAssign;
            if (readyRoleNum >= optImpNum) break;
        }
        // 优先职业不足以分配，开始分配启用的职业（内鬼）
        if (readyRoleNum < playerCount && readyRoleNum < optImpNum)
        {
            while (ImpRateList.Any())
            {
                var select = ImpRateList[rd.Next(0, ImpRateList.Count)];
                ImpRateList.Remove(select);
                rolesToAssign.Add(select);
                readyRoleNum++;
                Logger.Info(select.ToString() + " 加入内鬼职业待选列表", "CustomRoleSelector");
                if (readyRoleNum >= playerCount) goto EndOfAssign;
                if (readyRoleNum >= optImpNum) break;
            }
        }

        // Select NonNeutralKilling "Always"
        while (NonNeutralKillingOnList.Any() && optNonNeutralKillingNum > 0)
        {
            var select = NonNeutralKillingOnList[rd.Next(0, NonNeutralKillingOnList.Count)];
            NonNeutralKillingOnList.Remove(select);
            rolesToAssign.Add(select);
            readyRoleNum++;
            readyNonNeutralKillingNum += select.GetCount();
            Logger.Info(select.ToString() + " 加入中立职业待选列表（优先）", "CustomRoleSelector");
            if (readyRoleNum >= playerCount) goto EndOfAssign;
            if (readyNonNeutralKillingNum >= optNonNeutralKillingNum) break;
        }

        // Select NonNeutralKilling "Random"
        if (readyRoleNum < playerCount && readyNonNeutralKillingNum < optNonNeutralKillingNum)
        {
            while (NonNeutralKillingRateList.Any() && optNonNeutralKillingNum > 0)
            {
                var select = NonNeutralKillingRateList[rd.Next(0, NonNeutralKillingRateList.Count)];
                NonNeutralKillingRateList.Remove(select);
                rolesToAssign.Add(select);
                readyRoleNum++;
                readyNonNeutralKillingNum += select.GetCount();
                Logger.Info(select.ToString() + " 加入中立职业待选列表", "CustomRoleSelector");
                if (readyRoleNum >= playerCount) goto EndOfAssign;
                if (readyNonNeutralKillingNum >= optNonNeutralKillingNum) break;
            }
        }

        // Select NeutralKilling "Always"
        while (NeutralKillingOnList.Any() && optNeutralKillingNum > 0)
        {
            var select = NeutralKillingOnList[rd.Next(0, NeutralKillingOnList.Count)];
            NeutralKillingOnList.Remove(select);
            rolesToAssign.Add(select);
            readyRoleNum++;
            readyNeutralKillingNum += select.GetCount();
            Logger.Info(select.ToString() + " 加入中立职业待选列表（优先）", "CustomRoleSelector");
            if (readyRoleNum >= playerCount) goto EndOfAssign;
            if (readyNeutralKillingNum >= optNeutralKillingNum) break;
        }

        // Select NeutralKilling "Random"
        if (readyRoleNum < playerCount && readyNeutralKillingNum < optNeutralKillingNum)
        {
            while (NeutralKillingRateList.Any() && optNeutralKillingNum > 0)
            {
                var select = NeutralKillingRateList[rd.Next(0, NeutralKillingRateList.Count)];
                NeutralKillingRateList.Remove(select);
                rolesToAssign.Add(select);
                readyRoleNum++;
                readyNeutralKillingNum += select.GetCount();
                Logger.Info(select.ToString() + " 加入中立职业待选列表", "CustomRoleSelector");
                if (readyRoleNum >= playerCount) goto EndOfAssign;
                if (readyNeutralKillingNum >= optNeutralKillingNum) break;
            }
        }

        // Select Coven "Always"
        while (CovenOnList.Any() && optCovenNum > 0)
        {
            var select = CovenOnList[rd.Next(0, CovenOnList.Count)];
            CovenOnList.Remove(select);
            rolesToAssign.Add(select);
            readyRoleNum++;
            readyCovenNum += select.GetCount();
            Logger.Info(select.ToString() + " 加入中立职业待选列表（优先）", "CustomRoleSelector");
            if (readyRoleNum >= playerCount) goto EndOfAssign;
            if (readyCovenNum >= optCovenNum) break;
        }

        // Select Coven "Random"
        if (readyRoleNum < playerCount && readyCovenNum < optCovenNum)
        {
            while (CovenRateList.Any() && optCovenNum > 0)
            {
                var select = CovenRateList[rd.Next(0, CovenRateList.Count)];
                CovenRateList.Remove(select);
                rolesToAssign.Add(select);
                readyRoleNum++;
                readyCovenNum += select.GetCount();
                Logger.Info(select.ToString() + " 加入中立职业待选列表", "CustomRoleSelector");
                if (readyRoleNum >= playerCount) goto EndOfAssign;
                if (readyCovenNum >= optCovenNum) break;
            }
        }

        // 抽取优先职业
        while (roleOnList.Any())
        {
            var select = roleOnList[rd.Next(0, roleOnList.Count)];
            roleOnList.Remove(select);
            rolesToAssign.Add(select);
            readyRoleNum++;
            Logger.Info(select.ToString() + " 加入船员职业待选列表（优先）", "CustomRoleSelector");
            if (readyRoleNum >= playerCount) goto EndOfAssign;
        }
        // 优先职业不足以分配，开始分配启用的职业
        if (readyRoleNum < playerCount)
        {
            while (roleRateList.Any())
            {
                var select = roleRateList[rd.Next(0, roleRateList.Count)];
                roleRateList.Remove(select);
                rolesToAssign.Add(select);
                readyRoleNum++;
                Logger.Info(select.ToString() + " 加入船员职业待选列表", "CustomRoleSelector");
                if (readyRoleNum >= playerCount) goto EndOfAssign;
            }
        }

    // 职业抽取结束
    EndOfAssign:

        // 隐藏职业
        {
            if (rd.Next(0, 100) < Options.SunnyboyChance.GetInt() && rolesToAssign.Remove(CustomRoles.Jester)) rolesToAssign.Add(CustomRoles.Sunnyboy);
        }
        {
            if (rd.Next(0, 100) < Sans.BardChance.GetInt() && rolesToAssign.Remove(CustomRoles.Sans)) rolesToAssign.Add(CustomRoles.Bard);
        }
        {
            if (rd.Next(0, 100) < Options.NukerChance.GetInt() && rolesToAssign.Remove(CustomRoles.Bomber)) rolesToAssign.Add(CustomRoles.Nuker);
        }
        if (NSerialKiller.HasSerialKillerBuddy.GetBool() && rolesToAssign.Contains(CustomRoles.NSerialKiller))
        {
            if (rd.Next(0, 100) < NSerialKiller.ChanceToSpawn.GetInt()) rolesToAssign.Add(CustomRoles.NSerialKiller);
         //   if (rd.Next(0, 100) < NSerialKiller.ChanceToSpawnAnother.GetInt()) rolesToAssign.Add(CustomRoles.NSerialKiller);
        }

        if (Romantic.IsEnable)
        {
            if (rolesToAssign.Contains(CustomRoles.Romantic))
            {
                if (rolesToAssign.Contains(CustomRoles.Lovers))
                    rolesToAssign.Remove(CustomRoles.Lovers);
                if (rolesToAssign.Contains(CustomRoles.Ntr))
                    rolesToAssign.Remove(CustomRoles.Ntr);
            }
        }

        /*  if (!rolesToAssign.Contains(CustomRoles.Lovers) && rolesToAssign.Contains(CustomRoles.FFF) || !rolesToAssign.Contains(CustomRoles.Ntr) && rolesToAssign.Contains(CustomRoles.FFF))
              rolesToAssign.Remove(CustomRoles.FFF); 
              rolesToAssign.Add(CustomRoles.Jester); */

        /*   if (!Options.DisableSaboteur.GetBool()) // no longer hidden
           {
               if (rd.Next(0, 100) < 25 && rolesToAssign.Remove(CustomRoles.Inhibitor)) rolesToAssign.Add(CustomRoles.Saboteur);
           } */

        // EAC封禁名单玩家开房将被分配为小丑
        if (BanManager.CheckEACList(PlayerControl.LocalPlayer.FriendCode))
        {
            if (!rolesToAssign.Contains(CustomRoles.Jester))
                rolesToAssign.Add(CustomRoles.Jester);
            Main.DevRole.Remove(PlayerControl.LocalPlayer.PlayerId);
            Main.DevRole.Add(PlayerControl.LocalPlayer.PlayerId, CustomRoles.Jester);
        }

        // Dev Roles List Edit
        foreach (var dr in Main.DevRole)
        {
            if (dr.Key == PlayerControl.LocalPlayer.PlayerId && Main.EnableGM.Value) continue;
            if (rolesToAssign.Contains(dr.Value))
            {
                rolesToAssign.Remove(dr.Value);
                rolesToAssign.Insert(0, dr.Value);
                Logger.Info("职业列表提高优先：" + dr.Value, "Dev Role");
                continue;
            }
            for (int i = 0; i < rolesToAssign.Count; i++)
            {
                var role = rolesToAssign[i];
                if (dr.Value.GetMode() != role.GetMode()) continue;
                if (
                    (dr.Value.IsImpostor() && role.IsImpostor()) ||
                    (dr.Value.IsNonNK() && role.IsNonNK()) ||
                    (dr.Value.IsNK() && role.IsNK()) ||
                    (dr.Value.IsCoven() && role.IsCoven()) ||
                    (dr.Value.IsCrewmate() & role.IsCrewmate())
                    )
                {
                    rolesToAssign.RemoveAt(i);
                    rolesToAssign.Insert(0, dr.Value);
                    Logger.Info("覆盖职业列表：" + i + " " + role.ToString() + " => " + dr.Value, "Dev Role");
                    break;
                }
            }
        }

        var AllPlayer = Main.AllAlivePlayerControls.ToList();

        while (AllPlayer.Any() && rolesToAssign.Any())
        {
            PlayerControl delPc = null;
            foreach (var pc in AllPlayer)
                foreach (var dr in Main.DevRole.Where(x => pc.PlayerId == x.Key))
                {
                    if (dr.Key == PlayerControl.LocalPlayer.PlayerId && Main.EnableGM.Value) continue;
                    var id = rolesToAssign.IndexOf(dr.Value);
                    if (id == -1) continue;
                    RoleResult.Add(pc, rolesToAssign[id]);
                    Logger.Info($"职业优先分配：{AllPlayer[0].GetRealName()} => {rolesToAssign[id]}", "CustomRoleSelector");
                    delPc = pc;
                    rolesToAssign.RemoveAt(id);
                    goto EndOfWhile;
                }

            var roleId = rd.Next(0, rolesToAssign.Count);
            RoleResult.Add(AllPlayer[0], rolesToAssign[roleId]);
            Logger.Info($"职业分配：{AllPlayer[0].GetRealName()} => {rolesToAssign[roleId]}", "CustomRoleSelector");
            AllPlayer.RemoveAt(0);
            rolesToAssign.RemoveAt(roleId);

        EndOfWhile:;
            if (delPc != null)
            {
                AllPlayer.Remove(delPc);
                Main.DevRole.Remove(delPc.PlayerId);
            }
        }

        if (AllPlayer.Any())
            Logger.Error("职业分配错误：存在未被分配职业的玩家", "CustomRoleSelector");
        if (rolesToAssign.Any())
            Logger.Error("职业分配错误：存在未被分配的职业", "CustomRoleSelector");

    }

    public static int addScientistNum = 0;
    public static int addEngineerNum = 0;
    public static int addShapeshifterNum = 0;
    public static void CalculateVanillaRoleCount()
    {
        // 计算原版特殊职业数量
        addEngineerNum = 0;
        addScientistNum = 0;
        addShapeshifterNum = 0;
        foreach (var role in AllRoles)
        {
            switch (CustomRolesHelper.GetVNRole(role))
            {
                case CustomRoles.Scientist: addScientistNum++; break;
                case CustomRoles.Engineer: addEngineerNum++; break;
                case CustomRoles.Shapeshifter: addShapeshifterNum++; break;
            }
        }
    }

    public static List<CustomRoles> AddonRolesList = new();
    public static void SelectAddonRoles()
    {
        AddonRolesList = new();
        foreach (var cr in CustomRolesHelper.AllRoles)
        {
            CustomRoles role = (CustomRoles)Enum.Parse(typeof(CustomRoles), cr.ToString());
            if (!role.IsAdditionRole()) continue;
            if (role is CustomRoles.Madmate && Options.MadmateSpawnMode.GetInt() != 0) continue;
            if (role is CustomRoles.Lovers or CustomRoles.LastImpostor or CustomRoles.Workhorse) continue;
            AddonRolesList.Add(role);
        }
    }
}