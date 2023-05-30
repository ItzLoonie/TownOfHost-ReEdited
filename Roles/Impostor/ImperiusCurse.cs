using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TOHE.Options;
using static TOHE.Translator;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.Impostor
{
    public static class ImperiusCurse
    {
        public enum SwitchTrigger
        {
            Switch,
            Drag,
            Jump,
        };
        public static readonly string[] SwitchTriggerText =
        {
            "TriggerSwitch", "TriggerDrag","TriggerJump"
        };

        private static readonly int Id = 902436;

        public static OptionItem ModeTp;
        public static void SetupCustomOption()
        {
            ModeTp = StringOptionItem.Create(Id, "ImperiusCurseModeTp", SwitchTriggerText, 0, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.ImperiusCurse]);
        }

        public static void OnShapeshift(PlayerControl pc, PlayerControl target, bool shapeshifting)
        {
            if (shapeshifting)
            {
                new LateTask(() =>
                {
                    if (!(!GameStates.IsInTask || !pc.IsAlive() || !target.IsAlive() || pc.inVent || target.inVent))
                    {
                        var originPs = target.GetTruePosition();

                        switch ((SwitchTrigger)ModeTp.GetValue())
                        {
                            case SwitchTrigger.Switch:
                                Utils.TP(target.NetTransform, pc.GetTruePosition());
                                Utils.TP(pc.NetTransform, originPs);
                                break;
                            case SwitchTrigger.Drag:
                                Utils.TP(pc.NetTransform, originPs);
                                break; 
                            case SwitchTrigger.Jump:
                                Utils.TP(target.NetTransform, pc.GetTruePosition());
                                break;
                        }
                    }
                }, 1.5f, "ImperiusCurse TP");
            }
        }
    }
}
