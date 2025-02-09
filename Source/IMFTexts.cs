using System;
using System.Collections.Generic;
using TaleWorlds.Localization;
using System.IO;
using System.Xml.Serialization;
using TaleWorlds.ModuleManager;
using TaleWorlds.Engine;
using System.Text.RegularExpressions;
using TaleWorlds.CampaignSystem;


namespace ImprovedMinorFactions.Source
{
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

        public static TextObject? GetFactionText(string TextId, Clan mFaction)
        {
            return Current!.GetFactionTextInternal(TextId, mFaction.StringId);
        }

        private void DeserializeTexts()
        {
            // TODO: get path to current dir without ImprovedMinorFactions
            string filePath = "../../Modules/ImprovedMinorFactions/ModuleData/imf_faction_texts.xml";
            var moduleNames = Utilities.GetModulesNames();
            foreach (string module in moduleNames)
            {
                if (Regex.IsMatch(module, @"Improved\s?Minor\s?Factions"))
                {
                    filePath = ModuleHelper.GetXmlPath(module, "imf_faction_texts");
                    Console.WriteLine(filePath);
                }
            }

            // TODO: make this shit not throw no error
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
                Console.WriteLine($"An error occurred while deserializing the XML file: {ex.Message}");
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
