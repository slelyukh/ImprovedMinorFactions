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

        /// <summary>
        /// Adds the "who are you" question for camp notables and their answer.
        /// </summary>
        private void SetDialogs(CampaignGameStarter starter)
        {
            starter.AddPlayerLine("mf_intro_start", "hero_main_options", "mf_intro_continue",
                    "{=!}Can you remind me who the {IMF_INTRO_CLAN} are?",
                    this.player_intro_on_condition,
                    this.player_intro_consequence);
            starter.AddDialogLine("mf_intro_continue", "mf_intro_continue", "hero_main_options",
                    "{=!}The {IMF_INTRO_CLAN} are {IMF_INTRO_CLAN_DESC}{OUTLAW_KINGDOMS_TEXT} You can hire us as mercenaries," +
                    " or you can let your enemies hire us first.",
                    () => true,
                    null);

        }

        /// <summary>
        /// Shows the question when talking to a notable in a camp of a clan the player hasn't asked about yet.
        /// </summary>
        private bool player_intro_on_condition()
        {
            var conversationHero = Hero.OneToOneConversationHero;
            var ownerClan = conversationHero?.CurrentSettlement?.OwnerClan;
            return conversationHero != null
                && conversationHero.IsNotable
                && (conversationHero.CurrentSettlement?.SettlementComponent is MinorFactionHideout)
                && ownerClan != null
                && !this._playerMetFactions.Contains(ownerClan);
        }

        /// <summary>
        /// Remembers that the player asked about this clan.
        /// </summary>
        private void player_intro_consequence()
        {
            var ownerClan = Hero.OneToOneConversationHero?.CurrentSettlement?.OwnerClan;
            if (ownerClan != null)
                this._playerMetFactions.Add(ownerClan);
            MBTextManager.SetTextVariable("IMF_INTRO_CLAN", ownerClan?.Name ?? new TextObject("{=!}this clan"));
        }
        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            SetDialogs(starter);
        }

        /// <summary>
        /// Fills in the text for the answer: the clan's own description from imf_faction_texts.xml or one for its type,
        /// and which kingdoms see it as outlaws.
        /// </summary>
        private void OnConversationStart(IAgent agent)
        {
            var conversationHero = ((CharacterObject) agent?.Character)?.HeroObject;
            var ownerClan = conversationHero?.CurrentSettlement?.OwnerClan;
            if (conversationHero != null && conversationHero.IsNotable
                && (conversationHero.CurrentSettlement?.SettlementComponent is MinorFactionHideout)
                && ownerClan != null
                && !this._playerMetFactions.Contains(ownerClan))
            {
                var mfClan = ownerClan;
                MBTextManager.SetTextVariable("IMF_INTRO_CLAN", mfClan.Name);

                var factionDesc = IMFTexts.GetFactionText("MFDescription_ClanDescription", mfClan);
                if (factionDesc != null)
                {
                    MBTextManager.SetTextVariable("IMF_INTRO_CLAN_DESC", factionDesc);
                }
                else if (mfClan.IsNomad)
                {
                    MBTextManager.SetTextVariable("IMF_INTRO_CLAN_DESC", "a traditional nomadic tribe.");
                }
                else if (mfClan.IsSect)
                {
                    MBTextManager.SetTextVariable("IMF_INTRO_CLAN_DESC", "a zealous religious movement.");
                }
                else if (mfClan.IsMafia)
                {
                    MBTextManager.SetTextVariable("IMF_INTRO_CLAN_DESC", "an influential mafia clan.");
                }
                else if (mfClan.IsClanTypeMercenary)
                {
                    MBTextManager.SetTextVariable("IMF_INTRO_CLAN_DESC", "a powerful mercenary company.");
                }
                else
                {
                    MBTextManager.SetTextVariable("IMF_INTRO_CLAN_DESC", "a minor faction.");
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
                }
                else
                {
                    MBTextManager.SetTextVariable("OUTLAW_KINGDOMS_TEXT", "");
                }
            }
        }
        public override void SyncData(IDataStore dataStore)
        {
        }

        /// <summary>
        /// Joins kingdom names into "A, B and C".
        /// </summary>
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

    }
}
