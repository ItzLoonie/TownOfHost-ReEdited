using AmongUs.GameOptions;
using System;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Impostor;

namespace TOHE.Modules;

public class MeetingTimeManager
{
    private static int DiscussionTime;
    private static int VotingTime;
    private static int DefaultDiscussionTime;
    private static int DefaultVotingTime;

    public static void Init()
    {
        DefaultDiscussionTime = Main.RealOptionsData.GetInt(Int32OptionNames.DiscussionTime);
        DefaultVotingTime = Main.RealOptionsData.GetInt(Int32OptionNames.VotingTime);
        Logger.Info($"DefaultDiscussionTime:{DefaultDiscussionTime}, DefaultVotingTime{DefaultVotingTime}", "MeetingTimeManager.Init");
        ResetMeetingTime();
    }
    public static void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetInt(Int32OptionNames.DiscussionTime, DiscussionTime);
        opt.SetInt(Int32OptionNames.VotingTime, VotingTime);
    }
    private static void ResetMeetingTime()
    {
        DiscussionTime = DefaultDiscussionTime;
        VotingTime = DefaultVotingTime;
    }
    public static void OnReportDeadBody()
    {
        if (Options.AllAliveMeeting.GetBool() && Utils.IsAllAlive)
        {
            DiscussionTime = 0;
            VotingTime = Options.AllAliveMeetingTime.GetInt();
            Logger.Info($"DiscussionTime:{DiscussionTime}, VotingTime{VotingTime}", "MeetingTimeManager.OnReportDeadBody");
            return;
        }

        ResetMeetingTime();
        int BonusMeetingTime = 0;
        int MeetingTimeMinTimeThief = 0;
        int MeetingTimeMinTimeManager = 0;
        int MeetingTimeMax = 300;

        if (TimeThief.IsEnable)
        {
            MeetingTimeMinTimeThief = TimeThief.LowerLimitVotingTime.GetInt();
            BonusMeetingTime += TimeThief.TotalDecreasedMeetingTime();
        }
        if (TimeManager.IsEnable)
        {
            MeetingTimeMinTimeManager = TimeManager.MadMinMeetingTimeLimit.GetInt();
            MeetingTimeMax = TimeManager.MeetingTimeLimit.GetInt();
            BonusMeetingTime += TimeManager.TotalIncreasedMeetingTime();
        }

        int TotalMeetingTime = DiscussionTime + VotingTime;
        //時間の下限、上限で刈り込み
        if (TimeManager.IsEnable) BonusMeetingTime = Math.Clamp(TotalMeetingTime + BonusMeetingTime, MeetingTimeMinTimeManager, MeetingTimeMax) - TotalMeetingTime;
        if (TimeThief.IsEnable) BonusMeetingTime = Math.Clamp(TotalMeetingTime + BonusMeetingTime, MeetingTimeMinTimeThief, MeetingTimeMax) - TotalMeetingTime;
        if (!TimeManager.IsEnable && !TimeThief.IsEnable) BonusMeetingTime = Math.Clamp(TotalMeetingTime + BonusMeetingTime, MeetingTimeMinTimeThief, MeetingTimeMax) - TotalMeetingTime;

        if (BonusMeetingTime >= 0)
            VotingTime += BonusMeetingTime; //投票時間を延長
        else
        {
            DiscussionTime += BonusMeetingTime; //会議時間を優先的に短縮
            if (DiscussionTime < 0) //会議時間だけでは賄えない場合
            {
                VotingTime += DiscussionTime; //足りない分投票時間を短縮
                DiscussionTime = 0;
            }
        }
        Logger.Info($"DiscussionTime:{DiscussionTime}, VotingTime{VotingTime}", "MeetingTimeManager.OnReportDeadBody");
    }
}