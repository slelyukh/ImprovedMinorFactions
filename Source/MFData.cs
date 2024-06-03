using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace ImprovedMinorFactions
{
    public class MFData : MBObjectBase
    {
        public MFData()
        {
        }

        public MFData(Clan c)
        {
            InitData(c);
        }

        private void InitData(Clan c)
        {
            // Only want to set the current manager here because if we try to call init our new hideouts are not loaded in yet.
            if (IMFManager.Current == null)
                IMFManager.Current = new IMFManager();
            Hideouts = new List<MinorFactionHideout>();
            mfClan = c;
            NumActiveHideouts = IMFModels.NumActiveHideouts(c);
            NumMilitiaFirstTime = IMFModels.NumMilitiaFirstTime(c);
            NumMilitiaPostRaid = IMFModels.NumMilitiaPostRaid(c);
            NumLvl3Militia = IMFModels.NumLvl3Militia(c);
            NumLvl2Militia = IMFModels.NumLvl2Militia(c);
            MaxMilitia = IMFModels.MaxMilitia(c);
            ClanGender = IMFModels.ClanGender(c);
        }

        public override void Deserialize(MBObjectManager objectManager, XmlNode node)
        {
            base.Deserialize(objectManager, node);
            string? mfClanId = node.Attributes?.GetNamedItem("minor_faction")?.Value.Replace("Faction.", "");
            Clan? mfClan = Clan.All?.Find((x) => x.StringId == mfClanId);
            if (mfClan == null)
                mfClan = objectManager.ReadObjectReferenceFromXml<Clan>("minor_faction", node);
            if (mfClan == null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"IMF: {mfClanId} not found. mf_data.xml data not loaded", Colors.Red));
                return;
            }

            InitData(mfClan);

            // parse data for me
            if (node.Attributes?.GetNamedItem("num_active_hideouts") != null)
                this.NumActiveHideouts = Int32.Parse(node.Attributes.GetNamedItem("num_active_hideouts").Value);
            if (node.Attributes?.GetNamedItem("num_militia_first_time") != null)
                this.NumMilitiaFirstTime = Int32.Parse(node.Attributes.GetNamedItem("num_militia_first_time").Value);
            if (node.Attributes?.GetNamedItem("num_militia_post_raid") != null)
                this.NumMilitiaPostRaid = Int32.Parse(node.Attributes.GetNamedItem("num_militia_post_raid").Value);
            if (node.Attributes?.GetNamedItem("num_lvl2_militia") != null)
                this.NumLvl2Militia = Int32.Parse(node.Attributes.GetNamedItem("num_lvl2_militia").Value);
            if (node.Attributes?.GetNamedItem("num_lvl3_militia") != null)
                this.NumLvl3Militia = Int32.Parse(node.Attributes.GetNamedItem("num_lvl3_militia").Value);
            if (node.Attributes?.GetNamedItem("max_militia") != null)
                this.MaxMilitia = Int32.Parse(node.Attributes.GetNamedItem("max_militia").Value);
            if (node.Attributes?.GetNamedItem("clan_gender") != null)
            {
                string genderString = node.Attributes.GetNamedItem("clan_gender").Value;
                if (genderString == "Male")
                {
                    this.ClanGender = IMFModels.Gender.Male;
                }
                else if (genderString == "Female")
                {
                    this.ClanGender = IMFModels.Gender.Female;
                }
                else if ((genderString == "Any"))
                {
                    this.ClanGender = IMFModels.Gender.Any;
                }
                else
                {
                    throw new Exception($"{genderString} is not a valid gender type for {mfClanId}. " +
                        $"The only valid options are 'Male', 'Female', or 'Any'");
                }
            }

            if (IMFManager.Current?.GetClanMFData(mfClan) != null)
            {
                // set values for current data
                MFData existingData = IMFManager.Current.GetClanMFData(mfClan);
                existingData.NumActiveHideouts = this.NumActiveHideouts;
                existingData.NumMilitiaFirstTime = this.NumMilitiaFirstTime;
                existingData.NumMilitiaPostRaid = this.NumMilitiaPostRaid;
                existingData.NumLvl2Militia = this.NumLvl2Militia;
                existingData.NumLvl3Militia = this.NumLvl3Militia;
                existingData.MaxMilitia = this.MaxMilitia;
                existingData.ClanGender = this.ClanGender;
            }
            else
            {
                IMFManager.Current?.AddMFData(mfClan, this);
            }
            return;
        }

        internal void AddMFHideout(MinorFactionHideout mfh)
        {
            Hideouts.Add(mfh);
        }

        internal void AddMFHideout(Settlement s)
        {
            if (!Helpers.IsMFHideout(s))
                throw new Exception("Error 3242: trying to add non mfh settlement to MF hideouts list.");
            Hideouts.Add(Helpers.GetMFHideout(s));
        }


        public int NumTotalHideouts
        {
            get => Hideouts.Count;
        }

        public Clan mfClan;
        public int NumActiveHideouts;
        public int NumMilitiaFirstTime;
        public int NumMilitiaPostRaid;
        public int NumLvl3Militia;
        public int NumLvl2Militia;
        public int MaxMilitia;
        internal IMFModels.Gender ClanGender;
        public List<MinorFactionHideout> Hideouts;
        public bool IsWaitingForWarWithPlayer;
    }
}
