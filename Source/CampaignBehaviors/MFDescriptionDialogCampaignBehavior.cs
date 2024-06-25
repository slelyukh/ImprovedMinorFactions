using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace ImprovedMinorFactions.Source.CampaignBehaviors
{
    // CampaignBehavior to give a dialog for the player to be reminded of a minor faction's Sect/Nomad/Mercenary/Mafia status and who considers them outlaws.
    internal class MFDescriptionDialogCampaignBehavior : CampaignBehaviorBase
    {
        public MFDescriptionDialogCampaignBehavior() 
        {
            _playerMetFactions = new List<Clan>();
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
            CampaignEvents.OnAgentJoinedConversationEvent.AddNonSerializedListener(this, new Action<IAgent>(OnConversationStart));
        }

        private void SetDialogs(CampaignGameStarter starter)
        {
            starter.AddPlayerLine("mf_intro_start", "hero_main_options", "mf_intro_continue",
                    "{=!}Can you remind me who the {IMF_INTRO_CLAN} are?",
                    this.player_intro_on_condition,
                    this.player_intro_consequence);
            starter.AddDialogLine("mf_intro_continue", "mf_intro_continue", "hero_main_options",
                    "{=!}The {IMF_INTRO_CLAN} are a {IMF_INTRO_CLAN_DESC}.{OUTLAW_KINGDOMS_TEXT} You can hire us as mercenaries," +
                    " or you can let your enemies hire us first.",
                    () => true,
                    null);

        }

        private bool player_intro_on_condition()
        {
            var conversationHero = Hero.OneToOneConversationHero;
            return conversationHero != null
                && conversationHero.IsLord
                && conversationHero.IsMinorFactionHero
                && !this._playerMetFactions.Contains(conversationHero.Clan);
        }

        private void player_intro_consequence()
        {
            this._playerMetFactions.Add(Hero.OneToOneConversationHero.Clan);
            MBTextManager.SetTextVariable("IMF_INTRO_CLAN", Hero.OneToOneConversationHero.Clan.Name);
        }
        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            SetDialogs(starter);
        }

        private void OnConversationStart(IAgent agent)
        {
            var agentCharacter = (CharacterObject)agent.Character;
            var conversationHero = ((CharacterObject) agent?.Character)?.HeroObject;
            if (conversationHero != null && conversationHero.IsLord && conversationHero.IsMinorFactionHero
                && !this._playerMetFactions.Contains(conversationHero.Clan))
            {
                var mfClan = conversationHero.Clan;
                MBTextManager.SetTextVariable("IMF_INTRO_CLAN", mfClan.Name);
                if (mfClan.IsNomad) {
                    MBTextManager.SetTextVariable("IMF_INTRO_CLAN_DESC", "traditional nomadic tribe");
                } else if (mfClan.IsSect) {
                    MBTextManager.SetTextVariable("IMF_INTRO_CLAN_DESC", "zealous religious movement");
                } else if (mfClan.IsMafia) {
                    MBTextManager.SetTextVariable("IMF_INTRO_CLAN_DESC", "influential mafia clan");
                } else if (mfClan.IsClanTypeMercenary) {
                    MBTextManager.SetTextVariable("IMF_INTRO_CLAN_DESC", "powerful mercenary company");
                }
                List<Kingdom> rivalKingdoms = ( from kingdom in Kingdom.All
                                                where Helpers.ConsidersMFOutlaw(kingdom, mfClan)
                                                select kingdom
                                                ).ToList();
                if (rivalKingdoms.Count > 0)
                {
                    MBTextManager.SetTextVariable("OUTLAW_KINGDOMS_TEXT", 
                        new TextObject("{=!} We are currently considered to be outlaws by the ").ToString() + 
                        KingdomListString(rivalKingdoms) + ".");
                } else
                {
                    MBTextManager.SetTextVariable("OUTLAW_KINGDOMS_TEXT", "");
                }
            }
        }
        public override void SyncData(IDataStore dataStore)
        {
        }

        private string KingdomListString(List<Kingdom> kingdoms)
        {
            string result = "";
            for (int i = 0; i < kingdoms.Count; i++)
            {
                var text = new TextObject("{village}").SetTextVariable("village", kingdoms[i].EncyclopediaLinkWithName);

                if (i == 0)
                {
                    result += text.ToString();
                }
                else if (i == kingdoms.Count - 1)
                {
                    // ternary operator for comma before and
                    result += (i > 1 ? "," : "") + new TextObject("{=!} and ").ToString() + text.ToString();
                }
                else
                {
                    result += ", " + text.ToString();
                }
            }
            return result;
        }

        List<Clan> _playerMetFactions;

        private DialogFlow? dialogflow;
    }
}
