using System;
using System.Collections.Generic;
using TaleWorlds.Localization;
using System.IO;
using System.Xml.Serialization;
using ImprovedMinorFactions.Patches;
using TaleWorlds.CampaignSystem;


namespace ImprovedMinorFactions.Source
{
    /// <summary>
    /// Faction-specific texts from imf_faction_texts.xml, e.g. a camp description written for one clan.
    /// Loaded once per game start.
    /// </summary>
    // class to manage faction-specific versions of texts
    internal class IMFTexts
    {
        IMFTexts()
        {
            mfTexts = new Dictionary<string, Dictionary<string, TextObject>>();
            DeserializeTexts();
        }

        public static void InitIMFTexts()
        {
            IMFTexts.Current = new IMFTexts();
        }

        private TextObject? GetFactionTextInternal(string TextId, string FactionId)
        {
            if (!mfTexts.ContainsKey(TextId))
                return null;
            if (!mfTexts[TextId].ContainsKey(FactionId))
                return null;
            return mfTexts[TextId][FactionId];
        }

        /// <summary>
        /// Gets the faction's version of a text.
        /// </summary>
        /// <returns>the text, or null if that faction has no version of it</returns>
        public static TextObject? GetFactionText(string TextId, Clan mFaction)
        {
            return Current!.GetFactionTextInternal(TextId, mFaction.StringId);
        }

        /// <summary>
        /// Reads imf_faction_texts.xml from the module folder. A missing or broken file just means no faction texts.
        /// </summary>
        private void DeserializeTexts()
        {
            string binDir = Path.GetDirectoryName(typeof(IMFTexts).Assembly.Location) ?? ".";
            string filePath = Path.GetFullPath(Path.Combine(binDir, "..", "..", "ModuleData", "imf_faction_texts.xml"));
            if (!File.Exists(filePath))
                return;

            try
            {
                // Deserialize the XML content from the file
                XmlSerializer serializer = new XmlSerializer(typeof(IMFTextsXML));
                using (StreamReader reader = new StreamReader(filePath))
                {
                    IMFTextsXML imfTexts = (IMFTextsXML) serializer.Deserialize(reader)!;
                    foreach (var imfText in imfTexts.IMFTextList)
                    {
                        if (!mfTexts.ContainsKey(imfText.TextId))
                        {
                            mfTexts[imfText.TextId] = new Dictionary<string, TextObject>();
                        }
                        mfTexts[imfText.TextId][imfText.FactionId] = new TextObject(imfText.Text);
                    }
                }
            }
            catch (Exception ex)
            {
                IMFDiagnostics.Log("IMFTexts could not read imf_faction_texts.xml:", ex);
            }
            
        }

        public static IMFTexts? Current;

        private Dictionary<string, Dictionary<string, TextObject>> mfTexts;
    }

    // Define the classes that match the XML structure
    [XmlRoot("IMFTexts")]
    public class IMFTextsXML
    {
        [XmlElement("IMFText")]
        public List<IMFTextXML> IMFTextList { get; set; }
    }

    public class IMFTextXML
    {

        [XmlAttribute("faction_id")]
        public string FactionId { get; set; }

        [XmlAttribute("text_id")]
        public string TextId { get; set; }

        [XmlAttribute("text")]
        public string Text { get; set; }
    }
}
