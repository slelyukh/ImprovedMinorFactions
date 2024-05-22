using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation.Tags;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace ImprovedMinorFactions.Source.Patches
{
    [HarmonyPatch(typeof(CharacterHelper), "GetNonconversationFacialIdle")]
    public class NonConversationFacialIdlePatch
    {
        static void Postfix(CharacterObject character, ref string __result)
        {
            if (Helpers.IsMFGangLeader(character.HeroObject))
            {
                if (character.HeroObject.GetTraitLevel(DefaultTraits.Mercy) <= 0 && character.HeroObject.GetTraitLevel(DefaultTraits.Calculating) < 0)
                {
                    __result = "convo_predatory";
                    return;
                }
                __result = "convo_confused_annoyed";
            }
        }
    }

    [HarmonyPatch(typeof(CharacterHelper), "GetNonconversationPose")]
    public class NonConversationPosePatch
    {
        static void Postfix(CharacterObject character, ref string __result)
        {
            if (Helpers.IsMFGangLeader(character.HeroObject))
            {
                __result = "aggressive";
            }
        }
    }

    [HarmonyPatch(typeof(HeroHelper), "GetCharacterTypeName")]
    public class CharacterTypeNamePatch
    {
        static void Postfix(Hero hero, ref TextObject __result)
        {
            if (Helpers.IsMFGangLeader(hero))
            {
                __result = GameTexts.FindText("str_charactertype_gangleader");
            }
        }
    }

    [HarmonyPatch(typeof(ConversationTagHelper), "EducatedClass")]
    public class EducatedClassPatch
    {
        static void Postfix(CharacterObject character, ref bool __result)
        {
            if (Helpers.IsMFGangLeader(character.HeroObject))
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(ImpoliteTag), "IsApplicableTo")]
    public class ImpoliteTagPatch
    {
        static void Postfix(CharacterObject character, ref bool __result)
        {
            if (character.IsHero && Helpers.IsMFGangLeader(character.HeroObject))
            {
                int heroRelation = CharacterRelationManager.GetHeroRelation(character.HeroObject, Hero.MainHero);
                __result = Clan.PlayerClan.Renown < 100f
                    && heroRelation < 1
                    && character.GetTraitLevel(DefaultTraits.Mercy) + character.GetTraitLevel(DefaultTraits.Generosity) < 0;
            }
        }
    }

    [HarmonyPatch(typeof(LordConversationsCampaignBehavior), "conversation_calculating_gangleader_introduction_on_condition")]
    public class CalculatingGangLeaderIntroPatch
    {
        static void Postfix(ref bool __result)
        {
            if (Campaign.Current.ConversationManager.CurrentConversationIsFirst 
                && Helpers.IsMFGangLeader(Hero.OneToOneConversationHero) 
                && Hero.OneToOneConversationHero.CharacterObject.GetTraitLevel(DefaultTraits.Calculating) == 1)
            {
                StringHelpers.SetCharacterProperties("CONVERSATION_HERO", Hero.OneToOneConversationHero.CharacterObject);
                __result = true;
                return;
            }
        }
    }

    [HarmonyPatch(typeof(LordConversationsCampaignBehavior), "conversation_cruel_gangleader_introduction_on_condition")]
    public class CruelGangLeaderIntroPatch
    {
        static void Postfix(ref bool __result)
        {
            if (Campaign.Current.ConversationManager.CurrentConversationIsFirst 
                && Helpers.IsMFGangLeader(Hero.OneToOneConversationHero) 
                && Hero.OneToOneConversationHero.GetTraitLevel(DefaultTraits.Mercy) < 0)
            {
                StringHelpers.SetCharacterProperties("CONVERSATION_HERO", Hero.OneToOneConversationHero.CharacterObject);
                __result = true;
                return;
            }
        }
    }

    [HarmonyPatch(typeof(LordConversationsCampaignBehavior), "conversation_default_gangleader_introduction_on_condition")]
    public class DefaultGangLeaderIntroPatch
    {
        static void Postfix(ref bool __result)
        {
            if (Campaign.Current.ConversationManager.CurrentConversationIsFirst
                && Helpers.IsMFGangLeader(Hero.OneToOneConversationHero))
            {
                StringHelpers.SetCharacterProperties("CONVERSATION_HERO", Hero.OneToOneConversationHero.CharacterObject);
                __result = true;
                return;
            }
        }
    }

    [HarmonyPatch(typeof(LordConversationsCampaignBehavior), "conversation_ironic_gangleader_introduction_on_condition")]
    public class IronicGangLeaderIntroPatch
    {
        static void Postfix(ref bool __result)
        {
            if (Campaign.Current.ConversationManager.CurrentConversationIsFirst 
                && Helpers.IsMFGangLeader(Hero.OneToOneConversationHero)
                && Hero.OneToOneConversationHero.CharacterObject.GetPersona() == DefaultTraits.PersonaIronic)
            {
                StringHelpers.SetCharacterProperties("CONVERSATION_HERO", Hero.OneToOneConversationHero.CharacterObject);
                __result = true;
                return;
            }
        }
    }

    [HarmonyPatch(typeof(LordConversationsCampaignBehavior), "UsesLordConversations")]
    public class UsesLordConversationsPatch
    {
        static void Postfix(Hero hero, ref bool __result)
        {
            if (Helpers.IsMFGangLeader(hero))
            {
                __result = true;
            }
        }
    }

}
