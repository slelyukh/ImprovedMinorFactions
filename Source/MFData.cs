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
using ImprovedMinorFactions.Patches;

namespace ImprovedMinorFactions
{
    /// <summary>
    /// A minor faction's camp settings and its list of camps. The numbers come from IMFModels defaults with mf_data.xml on top.
    /// </summary>
    public class MFData : MBObjectBase
    {
        public MFData()
        {
        }

        public MFData(Clan c)
        {
            InitData(c);
        }

        /// <summary>
        /// Fills in the defaults for the clan and applies mf_data.xml. Clans that are destroyed or have no faction get no active camps.
        /// </summary>
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
            ApplyConfig();

            // make sure clans that shouldn't have hideouts don't
            if (mfClan.IsEliminated || mfClan.MapFaction == null)
                NumActiveHideouts = 0;
        }

        /// <summary>
        /// Overrides the defaults with the clan's settings from mf_data.xml, if there are any.
        /// </summary>
        private void ApplyConfig()
        {
            FactionConfig? f = ConfigLoader.For(mfClan?.StringId);
            if (f == null)
                return;
            if (f.NumActiveHideouts.HasValue) NumActiveHideouts = f.NumActiveHideouts.Value;
            if (f.NumMilitiaFirstTime.HasValue) NumMilitiaFirstTime = f.NumMilitiaFirstTime.Value;
            if (f.NumMilitiaPostRaid.HasValue) NumMilitiaPostRaid = f.NumMilitiaPostRaid.Value;
            if (f.NumLvl2Militia.HasValue) NumLvl2Militia = f.NumLvl2Militia.Value;
            if (f.NumLvl3Militia.HasValue) NumLvl3Militia = f.NumLvl3Militia.Value;
            if (f.MaxMilitia.HasValue) MaxMilitia = f.MaxMilitia.Value;
            if (f.ClanGender != null)
                ClanGender = f.ClanGender == "Male" ? IMFModels.Gender.Male
                           : f.ClanGender == "Female" ? IMFModels.Gender.Female
                           : IMFModels.Gender.Any;
            if (f.Enabled == false)
                NumActiveHideouts = 0;
        }

        // The game's XML loader has to skip mf_data.xml, a row for a missing clan crashes some maps.
        public override void Deserialize(MBObjectManager objectManager, XmlNode node)
        {
            base.Deserialize(objectManager, node);
        }

        internal void AddMFHideout(MinorFactionHideout mfh)
        {
            Hideouts.Add(mfh);
        }

        /// <summary>
        /// Adds a camp settlement to the clan's list. Throws if the settlement isn't a camp.
        /// </summary>
        internal void AddMFHideout(Settlement s)
        {
            if (!Helpers.IsMFHideout(s))
                throw new Exception("Error 3242: trying to add non mfh settlement to MF hideouts list.");
            Hideouts.Add(Helpers.GetMFHideout(s)!);
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
