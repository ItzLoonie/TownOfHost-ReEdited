﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace TOHE;

public class DevUser
{
    public string Code { get; set; }
    public string Color { get; set; }
    public string Tag { get; set; }
    public bool IsUp { get; set; }
    public bool IsDev { get; set; }
    public bool DeBug { get; set; }
    public bool ColorCmd { get; set; }
    public string UpName { get; set; }
    public DevUser(string code = "", string color = "null", string tag = "null", bool isUp = false, bool isDev = false, bool deBug = false, bool colorCmd = false, string upName = "未认证用户")
    {
        Code = code;
        Color = color;
        Tag = tag;
        IsUp = isUp;
        IsDev = isDev;
        DeBug = deBug;
        ColorCmd = colorCmd;
        UpName = upName;
    }

    public bool HasTag() => Tag != "null";
    //public string GetTag() => Color == "null" ? $"<size=1.2>{Tag}</size>\r\n" : $"<color={Color}><size=1.2>{(Tag == "#Dev" ? Translator.GetString("Developer") : Tag)}</size></color>\r\n";
    public string GetTag()
    {
        string tagColorFilePath = @$"./TOHE-DATA/Tags/SPONSOR_TAGS/{Code}.txt";

        if (Color == "null" || Color == string.Empty) return $"<size=1.2>{Tag}</size>\r\n";
        var startColor = Color.TrimStart('#');

        if (File.Exists(tagColorFilePath))
        {
            var ColorCode = File.ReadAllText(tagColorFilePath);
            if (Utils.CheckColorHex(ColorCode)) startColor = ColorCode;
        }
        string t1;
        t1 = Tag == "#Dev" ? Translator.GetString("Developer") : Tag;
        return $"<size=1.2><color=#{startColor}>{t1}</color></size>\r\n";
    }
    //public string GetTag() 
    //{
    //    string tagColorFilePath = @$"./TOHE-DATA/Tags/SPONSOR_TAGS/{Code}.txt";

    //    if (Color == "null" || Color == string.Empty) return $"<size=1.2>{Tag}</size>\r\n";
    //    var startColor = "FFFF00";
    //    var endColor = "FFFF00";
    //    var startColor1 = startColor;
    //    var endColor1 = endColor;
    //    if (Color.Split(",").Length == 1)
    //    {
    //        startColor1 = Color.Split(",")[0].TrimStart('#');
    //        endColor1 = startColor1;
    //    }
    //    else if (Color.Split(",").Length == 2)
    //    {
    //         startColor1 = Color.Split(",")[0].TrimStart('#');
    //         endColor1 = Color.Split(",")[1].TrimStart('#');
    //    }
    //    if (File.Exists(tagColorFilePath))
    //    {
    //        var ColorCode = File.ReadAllText(tagColorFilePath);
    //        if (ColorCode.Split(" ").Length == 2)
    //        {
    //            startColor = ColorCode.Split(" ")[0];
    //            endColor = ColorCode.Split(" ")[1];
    //        }
    //        else
    //        {
    //            startColor = startColor1;
    //            endColor = endColor1;
    //        }
    //    }
    //    else
    //    {
    //        startColor = startColor1;
    //        endColor = endColor1;
    //    }
    //    if (!Utils.CheckGradientCode($"{startColor} {endColor}"))
    //    {
    //        startColor = "FFFF00";
    //        endColor = "FFFF00";
    //    }
    //    var t1 = "";
    //    t1 = Tag == "#Dev" ? Translator.GetString("Developer") : Tag;
    //    return $"<size=1.2>{Utils.GradientColorText(startColor,endColor, t1)}</size>\r\n";
    //}
}

public static class DevManager
{
    public static DevUser DefaultDevUser = new();
    public static List<DevUser> DevUserList = new();
    public static void Init()
    {
        // Dev
        DevUserList.Add(new(code: "actorour#0029", color: "#ffc0cb", tag: "Original Developer", isUp: true, isDev: true, deBug: true, colorCmd: true, upName: "KARPED1EM"));
        DevUserList.Add(new(code: "pinklaze#1776", color: "#30548e", tag: "#Dev", isUp: true, isDev: true, deBug: true, colorCmd: false, upName: "NCSIMON"));
        DevUserList.Add(new(code: "keepchirpy#6354", color: "#1FF3C6", tag: "Переводчик", isUp: true, isDev: true, deBug: true, colorCmd: true, upName: "TommyXL")); //Tommy-XL
        DevUserList.Add(new(code: "taskunsold#2701", color: "null", tag: "<color=#426798>Tem</color><color=#f6e509>mie</color>", isUp: false, isDev: true, deBug: false, colorCmd: false, upName: null)); //Tem
        DevUserList.Add(new(code: "timedapper#9496", color: "#48FFFF", tag: "#Dev", isUp: false, isDev: true, deBug: false, colorCmd: false, upName: null)); //阿龍
        DevUserList.Add(new(code: "sofaagile#3120", color: "null", tag: "null", isUp: false, isDev: true, deBug: true, colorCmd: false, upName: null)); //天寸
        DevUserList.Add(new(code: "keyscreech#2151", color: "null", tag: "<color=#D3A4FF>美術</color><color=#5A5AAD>NotKomi</color>", isUp: false, isDev: true, deBug: false, upName: null)); //Endrmen40409

        // Up
        DevUserList.Add(new(code: "truantwarm#9165", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "萧暮不姓萧"));
        DevUserList.Add(new(code: "drilldinky#1386", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "爱玩AU的河豚"));
        DevUserList.Add(new(code: "farardour#6818", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "-提米SaMa-"));
        DevUserList.Add(new(code: "vealused#8192", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "lag丶xy"));
        DevUserList.Add(new(code: "storyeager#0815", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "航娜丽莎"));
        DevUserList.Add(new(code: "versegame#3885", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "柴唔cw"));
        DevUserList.Add(new(code: "closegrub#6217", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "警长不会玩"));
        DevUserList.Add(new(code: "frownnatty#7935", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "鬼灵official"));
        DevUserList.Add(new(code: "veryscarf#5368", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "小武同学102"));
        DevUserList.Add(new(code: "sparklybee#0275", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "--红包SaMa--"));
        DevUserList.Add(new(code: "endingyon#3175", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "游侠开摆"));
        DevUserList.Add(new(code: "firmine#0232", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "YH永恒_"));
        DevUserList.Add(new(code: "storkfey#3570", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "Calypso"));
        DevUserList.Add(new(code: "fellowsand#1003", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "C-Faust"));
        DevUserList.Add(new(code: "jetsafe#8512", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "Hoream是好人"));
        DevUserList.Add(new(code: "primether#5348", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "AnonWorks"));
        DevUserList.Add(new(code: "spoonkey#0792", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "没好康的"));
        DevUserList.Add(new(code: "busethical#4134", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "茄-au"));
        DevUserList.Add(new(code: "doggedsize#7892", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "TronAndRey"));
        DevUserList.Add(new(code: "openlanded#9533", color: "#9e2424", tag: "God Of Death Love Apples", isUp: true, isDev: true, deBug: true, colorCmd: true, upName: "ryuk"));
        DevUserList.Add(new(code: "icingposh#6469", color: "#9e2424", tag: "God Of Death Love Apples", isUp: true, isDev: true, deBug: true, colorCmd: true, upName: "ryuk2"));
        DevUserList.Add(new(code: "unlikecity#4086", color: "#eD2F91", tag: "Ward", isUp: true, isDev: false, deBug: false, colorCmd: true, upName: "Ward"));
        DevUserList.Add(new(code: "iconicdrop#2727", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: true, upName: "jackler"));

        DevUserList.Add(new(code: "neatnet#5851", color: "#FFFF00", tag: "The 200IQ guy", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "The 200IQ guy"));
        DevUserList.Add(new(code: "contenthue#0404", color: "#FFFF0", tag: "The 200IQ guy", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "The 200IQ guy"));
        DevUserList.Add(new(code: "heavyclod#2286", color: "#FFFF00", tag: "小叨.exe已停止运行", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "小叨院长"));
        DevUserList.Add(new(code: "storeroan#0331", color: "#FF0066", tag: "Night_瓜", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "Night_瓜"));
        DevUserList.Add(new(code: "teamelder#5856", color: "#1379bf", tag: "屑Slok（没信誉的鸽子）", isUp: true, isDev: false, colorCmd: false, deBug: false, upName: "Slok7565"));

        DevUserList.Add(new(code: "radarright#2509", color: "null", tag: "null", isUp: false, isDev: false, deBug: true, colorCmd: false, upName: null));

        // Sponsor
        DevUserList.Add(new(code: "recentduct#6068", color: "#FF00FF", tag: "高冷男模法师", isUp: false, isDev: false, colorCmd: false, deBug: true, upName: null));
        DevUserList.Add(new(code: "canneddrum#2370", color: "#fffcbe", tag: "我是喜唉awa", isUp: false, isDev: false, colorCmd: false, deBug: false, upName: null));
        DevUserList.Add(new(code: "dovefitted#5329", color: "#1379bf", tag: "不要首刀我", isUp: false, isDev: false, colorCmd: false, deBug: false, upName: null));
        DevUserList.Add(new(code: "luckylogo#7352", color: "#f30000", tag: "林@林", isUp: false, isDev: false, colorCmd: false, deBug: false, upName: null));
        DevUserList.Add(new(code: "axefitful#8788", color: "#8e8171", tag: "寄才是真理", isUp: false, isDev: false, colorCmd: false, deBug: false, upName: null));
        DevUserList.Add(new(code: "raftzonal#8893", color: "#8e8171", tag: "寄才是真理", isUp: false, isDev: false, colorCmd: false, deBug: false, upName: null));
        DevUserList.Add(new(code: "twainrobin#8089", color: "#0000FF", tag: "啊哈修maker", isUp: false, isDev: false, colorCmd: false, deBug: false, upName: null));
        DevUserList.Add(new(code: "mallcasual#6075", color: "#f89ccb", tag: "波奇酱", isUp: false, isDev: false, colorCmd: false, deBug: false, upName: null));
        DevUserList.Add(new(code: "beamelfin#9478", color: "#6495ED", tag: "Amaster-1111", isUp: false, isDev: false, colorCmd: false, deBug: false, upName: null));
        DevUserList.Add(new(code: "lordcosy#8966", color: "#FFD6EC", tag: "HostTOHE", isUp: false, isDev: false, colorCmd: false, deBug: false, upName: null)); //K
//        DevUserList.Add(new(code: "honestsofa#2870", color: "#D381D9", tag: "Discord: SolarFlare#0700", isUp: true, isDev: false, colorCmd: false, deBug: false, upName: "SolarFlare")); //SolarFlare
        DevUserList.Add(new(code: "caseeast#7194", color: "#1c2451", tag: "disc.gg/maul", isUp: false, isDev: false, colorCmd: false, deBug: false, upName: null)); //laikrai
        // lol hi go away, im the main dev smfh
        DevUserList.Add(new(code: "gnuedaphic#7196", color: "#f34c50", tag: "Main Developer", isUp: true, isDev: true, deBug: false, colorCmd: true, upName: "Loonie")); //Loonie
        DevUserList.Add(new(code: "loonietoons", color: "#f34c50", tag: "Main Developer", isUp: true, isDev: true, deBug: false, colorCmd: true, upName: "Loonie")); //Loonie
        // Lauryn and Moe
        DevUserList.Add(new(code: "straymovie#6453", color: "#F6B05E", tag: "Website Developer", isUp: true, isDev: false, deBug: false, colorCmd: true, upName: "Moe")); //Moe
        DevUserList.Add(new(code: "singlesign#1823", color: "#ffb6cd", tag: "Princess", isUp: true, isDev: false, deBug: false, colorCmd: true, upName: "Lauryn")); //Lauryn
        // Other
        DevUserList.Add(new(code: "peakcrown#8292", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: true, upName: null)); //Hakaka
        DevUserList.Add(new(code: "croaktense#0572", color: "#C6C6C6", tag: "Shiny Eevee", isUp: true, isDev: false, deBug: false, colorCmd: true, upName: null)); //Eevee
        DevUserList.Add(new(code: "dovebliss#9271", color: "#c67c6f", tag: "Cherry", isUp: true, isDev: false, deBug: false, colorCmd: true, upName: null)); //Cake
        // Chinese translation
        DevUserList.Add(new(code: "cloakhazy#9133", color: "#87CEFA", tag: "这里是崽子吖awa", isUp: true, isDev: true, deBug: false, colorCmd: false, upName: null)); //乐崽吖
        DevUserList.Add(new(code: "drawncod#3642", color: "#00FFFF", tag: "简中翻译人员", isUp: false, isDev: true, deBug: false, colorCmd: false, upName: null)); //船员小青
        DevUserList.Add(new(code: "grubmotive#0072", color: "#4169E1", tag: "跟班诅咒中", isUp: false, isDev: true, deBug: false, colorCmd: false, upName: null));//您有一个好
        // Patreons
        DevUserList.Add(new(code: "firmshame#7569", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "Yankee"));
        DevUserList.Add(new(code: "ghostapt#7243", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "MasterKy"));
        DevUserList.Add(new(code: "moonmodest#5153", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "Allie"));
        DevUserList.Add(new(code: "woolrusty#4204", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "jo"));
        DevUserList.Add(new(code: "funnyshe#2647", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "Stabby"));
        DevUserList.Add(new(code: "fluffycord#2605", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "sarhadactyl"));
        DevUserList.Add(new(code: "cannylink#0564", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "SpicyPoops"));
        DevUserList.Add(new(code: "examfishy#9080", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "killer5362"));
        DevUserList.Add(new(code: "dusksole#6956", color: "null", tag: "null", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "Bandz"));
        DevUserList.Add(new(code: "rollingegg#7687", color: "#fe7d6e", tag: "Ruler of Jiggly Peach Cakes", isUp: true, isDev: false, deBug: false, colorCmd: false, upName: "DarlingXX"));
    }
    public static bool IsDevUser(this string code) => DevUserList.Any(x => x.Code == code);
    public static DevUser GetDevUser(this string code) => code.IsDevUser() ? DevUserList.Find(x => x.Code == code) : DefaultDevUser;
}
