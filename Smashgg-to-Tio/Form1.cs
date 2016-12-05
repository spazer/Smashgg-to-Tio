using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace Smashgg_to_Tio
{
    public partial class Form1 : Form
    {
        enum UrlNumberType { Phase, Phase_Group, None }

        Dictionary<int, Entrant> entrantList = new Dictionary<int, Entrant>();
        List<Set> setList = new List<Set>();
        List<Phase> phaseList = new List<Phase>();
        string tournament = string.Empty;
        JObject tournamentStructure;

        public Form1()
        {
            InitializeComponent();

            // Set format for the DateTimePicker
            dateTimePicker1.Format = DateTimePickerFormat.Custom;
            dateTimePicker1.CustomFormat = "MM/dd/yyyy hh:mm:ss";
        }

        private void buttonConvert_Click(object sender, EventArgs e)
        {
            // Input validation
            if (textBoxURL.Text.Trim() == string.Empty)
            {
                MessageBox.Show("You need to specify a URL");
                return;
            }
            else if (textBoxTourneyName.Text.Trim() == string.Empty)
            {
                MessageBox.Show("You need to specify a tournament name");
                return;
            }
            else if (textBoxBracketName.Text.Trim() == string.Empty)
            {
                MessageBox.Show("You need to specify a bracket name");
                return;
            }

            smashgg api = new smashgg();
            string json = string.Empty;
            UpdateTournamentStructure();

            // Get the phase group number and use it to request data
            int inputNumber;
            UrlNumberType parseResult;

            parseResult = parseURL(textBoxURL.Text, UrlNumberType.Phase_Group, out inputNumber);

            if (parseResult == UrlNumberType.Phase_Group)
            {
                if (!retrievePhaseGroup(inputNumber, out json)) return;
            }
            else if (parseResult == UrlNumberType.Phase)
            {
                foreach (Phase entry in phaseList)
                {
                    if (inputNumber == entry.phaseId)
                    {
                        // Cheat here and assume there's only one phase group
                        if (!retrievePhaseGroup(entry.id[0].id, out json))
                        {
                            MessageBox.Show("Error retrieving bracket");
                            return;
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("This URL is not valid.");
                return;
            }

            // Deserialize json
            JObject bracketJson = JsonConvert.DeserializeObject<JObject>(json);

            // Parse entrants and sets from the json
            api.GetEntrants(bracketJson.SelectToken(SmashggStrings.Entities + "." + SmashggStrings.Entrants), ref entrantList);
            api.GetSets(bracketJson.SelectToken(SmashggStrings.Entities + "." + SmashggStrings.Sets), ref setList);
            
            // Create the XML
            CreateXML();

            // Clear global vars to avoid problems from residual data
            entrantList.Clear();
            setList.Clear();
            phaseList.Clear();
        }

        private void CreateXML()
        {
            // Create new XML document
            XmlDocument doc = new XmlDocument();
            XmlElement AppData = (XmlElement)doc.AppendChild(doc.CreateElement("AppData"));
            XmlNode EventList = AppData.AppendChild(doc.CreateElement("EventList"));
            XmlNode PlayerList = AppData.AppendChild(doc.CreateElement("PlayerList"));

            // Create event
            XmlNode Event = EventList.AppendChild(doc.CreateElement("Event"));
            Event.AppendChild(doc.CreateElement("Name")).InnerText = textBoxTourneyName.Text;
            Event.AppendChild(doc.CreateElement("StartDate")).InnerText = dateTimePicker1.Text;

            // Create a game for the event
            XmlNode Games = Event.AppendChild(doc.CreateElement("Games"));
            XmlNode Game = Games.AppendChild(doc.CreateElement("Game"));
            Game.AppendChild(doc.CreateElement("Name")).InnerText = textBoxBracketName.Text;
            Game.AppendChild(doc.CreateElement("GameType")).InnerText = "Singles";

            // Add entrant list of player IDs to the Game node
            XmlNode Entrants = Game.AppendChild(doc.CreateElement("Entrants"));
            foreach (KeyValuePair<int, Entrant> entry in entrantList)
            {
                // Ignore byes
                if (entry.Key == -1) continue;

                Entrants.AppendChild(doc.CreateElement("PlayerID")).InnerText = entry.Key.ToString();
            }

            // Create the bracket and add all matches
            XmlNode Bracket = Game.AppendChild(doc.CreateElement("Bracket"));
            XmlNode Matches = Bracket.AppendChild(doc.CreateElement("Matches"));
            foreach (Set nextSet in setList)
            {
                // Ignore byes
                if (nextSet.entrantID1 == -1 || nextSet.entrantID2 == -1) continue;

                XmlNode Match = Matches.AppendChild(doc.CreateElement("Match"));
                Match.AppendChild(doc.CreateElement("Player1")).InnerText = nextSet.entrantID1.ToString();
                Match.AppendChild(doc.CreateElement("Player2")).InnerText = nextSet.entrantID2.ToString();
                Match.AppendChild(doc.CreateElement("Winner")).InnerText = nextSet.winner.ToString();
                if (nextSet.isGF)
                {
                    if (nextSet.match == 1)
                    {
                        Match.AppendChild(doc.CreateElement("IsChampionship")).InnerText = "True";
                        Match.AppendChild(doc.CreateElement("IsSecondChampionship")).InnerText = "False";
                    }
                    else if (nextSet.match == 2)
                    {
                        Match.AppendChild(doc.CreateElement("IsChampionship")).InnerText = "False";
                        Match.AppendChild(doc.CreateElement("IsSecondChampionship")).InnerText = "True";
                    }
                }
                else
                {
                    Match.AppendChild(doc.CreateElement("IsChampionship")).InnerText = "False";
                    Match.AppendChild(doc.CreateElement("IsSecondChampionship")).InnerText = "False";
                }
            }

            // Create the Player List
            XmlNode Players = PlayerList.AppendChild(doc.CreateElement("Players"));
            foreach (KeyValuePair<int,Entrant> entry in entrantList)
            {
                // Ignore byes
                if (entry.Key == -1) continue;

                XmlNode nextPlayer = Players.AppendChild(doc.CreateElement("Player"));
                nextPlayer.AppendChild(doc.CreateElement("Nickname")).InnerText = entry.Value.Players[0].name;
                nextPlayer.AppendChild(doc.CreateElement("ID")).InnerText = entry.Key.ToString();
            }

            // Create an XML declaration. 
            XmlDeclaration xmldecl;
            xmldecl = doc.CreateXmlDeclaration("1.0", null, null);
            xmldecl.Encoding = "UTF-8";
            xmldecl.Standalone = "yes";

            // Add the new node to the document.
            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(xmldecl, root);

            // Output file
            System.IO.Directory.CreateDirectory("Output Files");
            doc.Save("Output Files/" + MakeValidFileName(textBoxTourneyName.Text + " - " + textBoxBracketName.Text) + ".tio");

            MessageBox.Show("File ouptut to Output Files/" + MakeValidFileName(textBoxTourneyName.Text + " - " + textBoxBracketName.Text) + ".tio");
        }

        /// <summary>
        /// Returns a phase or phase_group number
        /// </summary>
        /// <param name="url">URL to parse</param>
        /// <param name="type">The requested type of number</param>
        /// <param name="output">The phase or phase_group number</param>
        /// <returns>The type of output</returns>
        private UrlNumberType parseURL(string url, UrlNumberType type, out int output)
        {
            string[] splitURL = url.Split(new string[1] { "/" }, StringSplitOptions.RemoveEmptyEntries);

            // Try getting the phase_group if it is requested
            if (type == UrlNumberType.Phase_Group)
            {
                // https://smash.gg/tournament/the-big-house-6/events/melee-singles/brackets/76014?per_page=20&filter=%7B%22phaseId%22%3A76014%2C%22id%22%3A241487%7D&sort_by=-startAt&order=-1&page=1
                // Look for filter, phaseId, id
                int index = url.IndexOf("filter=%7B");
                if (index != -1)
                {
                    if (url.IndexOf("phaseId%22") != -1)
                    {
                        int startPos = url.IndexOf("id%22%3A", index);
                        if (startPos != -1)
                        {
                            startPos += "id%22%3A".Length;
                            int endPos = url.IndexOf("%7D", startPos);

                            if (endPos != -1)
                            {
                                // Take the number
                                if (int.TryParse(url.Substring(startPos, endPos - startPos), out output))
                                {
                                    return UrlNumberType.Phase_Group;
                                }
                            }
                        }
                    }
                }


                for (int i = 0; i < splitURL.Count(); i++)
                {
                    // Phase_group number comes is the 2nd after "bracket"
                    if (splitURL[i] == "brackets" && i + 2 < splitURL.Count())
                    {
                        // Take the number
                        if (int.TryParse(splitURL[i + 2], out output))
                        {
                            return UrlNumberType.Phase_Group;
                        }
                    }
                }
            }

            // Get the phase as a fallback
            for (int i = 0; i < splitURL.Count(); i++)
            {
                // Phase number comes after "bracket"
                if (splitURL[i] == "brackets" && i + 1 < splitURL.Count())
                {
                    // Take the number
                    if (int.TryParse(splitURL[i + 1], out output))
                    {
                        return UrlNumberType.Phase;
                    }
                }
            }

            // Both methods have failed
            output = -1;
            return UrlNumberType.None;
        }

        /// <summary>
        /// Gets the requested phase group using the smash.gg api. 
        /// </summary>
        /// <param name="phaseGroup">phase group number</param>
        /// <param name="json">json file with bracket info</param>
        /// <returns>True on success, false otherwise</returns>
        private bool retrievePhaseGroup(int phaseGroup, out string json)
        {

            json = string.Empty;
            try
            {
                WebRequest r = WebRequest.Create(SmashggStrings.UrlPrefixPhaseGroup + phaseGroup + SmashggStrings.UrlSuffixPhaseGroup);
                WebResponse resp = r.GetResponse();
                using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                {
                    json = sr.ReadToEnd();
                }

                return true;
            }
            catch (Exception)
            {
                MessageBox.Show("Error occurred during webpage retrieval.");
                return false;
            }
        }

        /// <summary>
        /// Replaces invalid filename characters with underscores
        /// </summary>
        /// <param name="name">input filename</param>
        /// <returns>output filename</returns>
        private static string MakeValidFileName(string name)
        {
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
        }

        /// <summary>
        /// Gets tournament info from the base page. Mostly irrlevant for this program except for getting the phase/phase group list.
        /// </summary>
        private void UpdateTournamentStructure()
        {
            try
            {
                bool validUrl = false;

                string[] splitURL = textBoxURL.Text.Split(new string[1] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < splitURL.Length; i++)
                {
                    // A valid url will have smash.gg followed by tournament
                    if (splitURL[i].ToLower() == "smash.gg" && splitURL[i + 1].ToLower() == "tournament")
                    {
                        tournament = splitURL[i + 2];

                        // Retrieve tournament page and get the json into a JObject
                        WebRequest r = WebRequest.Create(SmashggStrings.UrlPrefixTourney + tournament + SmashggStrings.UrlSuffixPhase);
                        WebResponse resp = r.GetResponse();
                        using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                        {
                            tournamentStructure = JsonConvert.DeserializeObject<JObject>(sr.ReadToEnd());
                            validUrl = true;
                        }

                        break;
                    }
                }

                // Build phaseList using data from tournamentStructure json
                if (validUrl)
                {
                    phaseList.Clear();
                    foreach (JToken token in tournamentStructure.SelectToken(SmashggStrings.Entities + "." + SmashggStrings.Groups))
                    {
                        // If the phase already exists, append the phase group id into the id list
                        bool phaseExists = false;
                        foreach (Phase phase in phaseList)
                        {
                            if (phase.phaseId == token[SmashggStrings.PhaseId].Value<int>())
                            {
                                PhaseGroup newPhaseGroup = new PhaseGroup();
                                newPhaseGroup.id = token[SmashggStrings.ID].Value<int>();
                                newPhaseGroup.DisplayIdentifier = token[SmashggStrings.DisplayIdent].Value<string>();

                                phase.id.Add(newPhaseGroup);
                                phaseExists = true;
                                break;
                            }
                        }

                        // If the phase does not exist, create it
                        if (!phaseExists)
                        {
                            PhaseGroup newPhaseGroup = new PhaseGroup();
                            newPhaseGroup.id = token[SmashggStrings.ID].Value<int>();
                            newPhaseGroup.DisplayIdentifier = token[SmashggStrings.DisplayIdent].Value<string>();

                            Phase newPhase = new Phase();
                            newPhase.id.Add(newPhaseGroup);
                            newPhase.phaseId = token[SmashggStrings.PhaseId].Value<int>();

                            if (!token[SmashggStrings.WaveId].IsNullOrEmpty())
                            {
                                newPhase.WaveId = token[SmashggStrings.WaveId].Value<int>();
                            }

                            phaseList.Add(newPhase);
                        }
                    }
                }
            }
            catch
            {
                MessageBox.Show("Couldn't update tournament structure");
            }
        }
    }
}
