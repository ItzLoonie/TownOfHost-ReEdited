using System;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.AddOns.Common
{
    public static class Fategiver
    {
        private static readonly int Id = 114514; //pls change the id. I set this cause I'm lazy to do checks.
        public static OptionItem ImpCanbeFategiver;
        public static OptionItem CrewCanbeFategiver;
        public static OptionItem NeutralsCanbeFategiver;

        public static OptionItem DoubleVoteChance;
        public static OptionItem HalfVoteChance;
        public static OptionItem CancelVoteChance;
        public static OptionItem Add1VoteChance;
        public static OptionItem Add2VoteChance;
        public static OptionItem Minus1VoteChance;
        public static OptionItem NormalVoteChance;
        public static OptionItem SuperVoteChance;

        public static void SetupCustomOption()
        {
            SetupAdtRoleOptions(Id, CustomRoles.Fategiver, canSetNum: true, tab: TabGroup.OtherRoles);
            ImpCanbeFategiver = BooleanOptionItem.Create(Id + 10, "ImpCanbeFategiver", false, TabGroup.OtherRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Fategiver]);
            CrewCanbeFategiver = BooleanOptionItem.Create(Id + 11, "CrewCanbeFategiver", true, TabGroup.OtherRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Fategiver]);
            NeutralsCanbeFategiver = BooleanOptionItem.Create(Id + 12, "NeutralsCanbeFategiver", false, TabGroup.OtherRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Fategiver]); //Also contains coven team
            DoubleVoteChance = IntegerOptionItem.Create(Id + 13, "FgDoubleVoteChance", new(0, 100, 5), 10, TabGroup.OtherRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Fategiver]).SetValueFormat(OptionFormat.Percent);
            HalfVoteChance = IntegerOptionItem.Create(Id + 14, "FgHalfVoteChance", new(0, 100, 5), 10, TabGroup.OtherRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Fategiver]).SetValueFormat(OptionFormat.Percent);
            CancelVoteChance = IntegerOptionItem.Create(Id + 15, "FgCancelVoteChance", new(0, 100, 5), 10, TabGroup.OtherRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Fategiver]).SetValueFormat(OptionFormat.Percent);
            Add1VoteChance = IntegerOptionItem.Create(Id + 16, "FgAdd1VoteChance", new(0, 100, 5), 20, TabGroup.OtherRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Fategiver]).SetValueFormat(OptionFormat.Percent);
            Add2VoteChance = IntegerOptionItem.Create(Id + 17, "FgAdd2VoteChance", new(0, 100, 5), 20, TabGroup.OtherRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Fategiver]).SetValueFormat(OptionFormat.Percent);
            Minus1VoteChance = IntegerOptionItem.Create(Id + 18, "FgMinus1VoteChance", new(0, 100, 5), 20, TabGroup.OtherRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Fategiver]).SetValueFormat(OptionFormat.Percent);
            NormalVoteChance = IntegerOptionItem.Create(Id + 19, "FgNormalVoteChance", new(0, 100, 5), 10, TabGroup.OtherRoles, false)
                 .SetParent(CustomRoleSpawnChances[CustomRoles.Fategiver]).SetValueFormat(OptionFormat.Percent);
            SuperVoteChance = IntegerOptionItem.Create(Id + 20, "FgSuperVoteChance", new(0, 100, 1), 0, TabGroup.OtherRoles, false)
                 .SetParent(CustomRoleSpawnChances[CustomRoles.Fategiver]).SetValueFormat(OptionFormat.Percent);
        }

        public static void Init()
        {
            int doubleVoteChance = DoubleVoteChance.GetInt();
            int halfVoteChance = HalfVoteChance.GetInt();
            int cancelVoteChance = CancelVoteChance.GetInt();
            int add1VoteChance = Add1VoteChance.GetInt();
            int add2VoteChance = Add2VoteChance.GetInt();
            int minus1VoteChance = Minus1VoteChance.GetInt();
            int normalVoteChance = NormalVoteChance.GetInt();
            int superVoteChance = SuperVoteChance.GetInt();
            _ = new RandomSystem(doubleVoteChance, halfVoteChance, cancelVoteChance,
                                            add1VoteChance, add2VoteChance, minus1VoteChance, normalVoteChance, superVoteChance);
        }

        public static int CalculateFategiverVotes(byte PlayerId, int VoteNum)
        {
            PlayerControl player = Utils.GetPlayerById(PlayerId);
            if (!player.Is(CustomRoles.Fategiver)) return VoteNum;
            if (VoteNum < 1 || VoteNum > 9) return VoteNum; //Fategiver can't help on extreme votes

            int result = RandomSystem.GetRandomResult();
            switch (result)
            {
                case 0:
                    VoteNum *= 2;
                    Utils.SendMessage(GetString("Fategiver_case0"), PlayerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.Fategiver), GetString("FategiverNotify")));
                    Logger.Info(player.GetNameWithRole() + ":case 0", "Fategiver");
                    break;
                case 1:
                    VoteNum /= 2;
                    Utils.SendMessage(GetString("Fategiver_case1"), PlayerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.Fategiver), GetString("FategiverNotify")));
                    Logger.Info(player.GetNameWithRole() + ":case 1", "Fategiver");
                    break;
                case 2:
                    VoteNum = 0;
                    Utils.SendMessage(GetString("Fategiver_case2"), PlayerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.Fategiver), GetString("FategiverNotify")));
                    Logger.Info(player.GetNameWithRole() + ":case 2", "Fategiver");
                    break;
                case 3:
                    VoteNum += 1;
                    Utils.SendMessage(GetString("Fategiver_case3"), PlayerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.Fategiver), GetString("FategiverNotify")));
                    Logger.Info(player.GetNameWithRole() + ":case 3", "Fategiver");
                    break;
                case 4:
                    VoteNum += 2;
                    Utils.SendMessage(GetString("Fategiver_case4"), PlayerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.Fategiver), GetString("FategiverNotify")));
                    Logger.Info(player.GetNameWithRole() + ":case 4", "Fategiver");
                    break;
                case 5:
                    VoteNum -= 1;
                    Utils.SendMessage(GetString("Fategiver_case5"), PlayerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.Fategiver), GetString("FategiverNotify")));
                    Logger.Info(player.GetNameWithRole() + ":case 5", "Fategiver");
                    break;
                case 6:
                    Utils.SendMessage(GetString("Fategiver_case6"), PlayerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.Fategiver), GetString("FategiverNotify")));
                    Logger.Info(player.GetNameWithRole() + ":case 6", "Fategiver");
                    break;
                case 7:
                    VoteNum = 999;
                    Utils.SendMessage(GetString("Fategiver_case7"), PlayerId, title: Utils.ColorString(Utils.GetRoleColor(CustomRoles.Fategiver), GetString("FategiverNotify")));
                    Logger.Info(player.GetNameWithRole() + ":case 7", "Fategiver");
                    break;
            }

            Logger.Info(player.GetNameWithRole() + "New VoteNum: " + VoteNum.ToString(), "Fategiver");
            return VoteNum;
        }

        public class RandomSystem //Code by chatgpt LOL
        {
            private static int[] chances;
            private static Random random;
            private static int totalSum;

            public RandomSystem(params int[] chances)
            {
                if (chances.Length != 8)
                {
                    throw new ArgumentException("The chances must be an array of 8 integers.");
                } //Tigger AntiBlackout if wrong here

                RandomSystem.chances = chances;
                random = new Random();
                totalSum = CalculateTotalSum(chances);
            }
            private static int CalculateTotalSum(int[] chances)
            {
                int sum = 0;
                foreach (int chance in chances)
                {
                    sum += chance;
                }
                return sum;
            }

            public static int GetRandomResult()
            {
                int randomNumber = random.Next(1, totalSum + 1);
                int sum = 0;

                for (int i = 0; i < chances.Length; i++)
                {
                    sum += chances[i];
                    if (randomNumber <= sum)
                    {
                        switch (i)
                        {
                            case 0:
                                return 0;
                            case 1:
                                return 1;
                            case 2:
                                return 2;
                            case 3:
                                return 3;
                            case 4:
                                return 4;
                            case 5:
                                return 5;
                            case 6:
                                return 6;
                            case 7:
                                return 7;
                        }
                    } //Shxt codes but works fine
                }
                return 6;
            }
        }
    }
}
