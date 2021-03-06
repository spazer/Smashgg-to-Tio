﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Smashgg_to_Tio
{
    class smashgg
    {
        static int PLAYER_BYE = -1;

        Dictionary<string, string> flagList = new Dictionary<string, string>();

        /// <summary>
        /// Constructor
        /// </summary>
        public smashgg()
        { 
        }

        #region Public Methods
        /// <summary>
        /// Appends entrants from the json input into entrantList
        /// </summary>
        /// <param name="input">json of the entrants token</param>
        /// <param name="entrantList">List of entrants to be outputted to</param>
        /// <returns>Returns true if successful, false otherwise</returns>
        public bool GetEntrants(JToken input, ref Dictionary<int, Entrant> entrantList)
        {
            if (input == null) return false;

            // Add bye info
            entrantList.Add(-1, new Entrant(new Player("Bye", string.Empty)));

            // Divide input into manageable chunks
            foreach (JToken entrant in input.Children())
            {
                // Get player ID
                if (entrant[SmashggStrings.ID].IsNullOrEmpty()) { continue; }
                int id = GetIntParameter(entrant, SmashggStrings.ID);

                // Get participant IDs
                SortedList<int, Player> pIds = new SortedList<int, Smashgg_to_Tio.Player>();
                foreach (int participant in entrant[SmashggStrings.ParticipantIds])
                {
                    Player newPlayer = new Player();
                    pIds.Add(participant, newPlayer);
                }

                foreach (KeyValuePair<int, Player> participant in pIds)
                {
                    // Get player ID based off participant ID
                    int playerId = entrant.SelectToken(SmashggStrings.PlayerIds + "." + participant.Key).Value<int>();

                    // Select player token based off player ID
                    JToken playerInfo = entrant.SelectToken("mutations.players" + "." + playerId);

                    // Get player tag
                    pIds[participant.Key].name = playerInfo[SmashggStrings.Gamertag].Value<string>();

                    // Make player country. Leave it empty.
                    pIds[participant.Key].country = string.Empty;
                }

                Entrant newEntrant = new Entrant(pIds.Values.ToList<Player>());
                entrantList.Add(id, newEntrant);
            }

            return true;
        }

        /// <summary>
        /// Appends sets from the json input into setList
        /// </summary>
        /// <param name="input">json of the sets token</param>
        /// <param name="entrantList">List of sets to be outputted to</param>
        /// <returns>Returns true if successful, false otherwise</returns>
        public bool GetSets(JToken input, ref List<Set> setList)
        {
            if (input == null) return false;

            // Get set data
            List<int> matchCountWinners = new List<int>();
            List<int> matchCountLosers = new List<int>();
            foreach (JToken set in input.Children())
            {
                Set newSet = new Set();

                // Get the entrant IDs. Set the entrant as a bye if it is null.
                newSet.entrantID1 = GetIntParameter(set, SmashggStrings.Entrant1Id);
                if (newSet.entrantID1 == -99)
                {
                    newSet.entrantID1 = PLAYER_BYE;
                }

                newSet.entrantID2 = GetIntParameter(set, SmashggStrings.Entrant2Id);
                if (newSet.entrantID2 == -99)
                {
                    newSet.entrantID2 = PLAYER_BYE;
                }

                // Get match data
                newSet.entrant1wins = GetIntParameter(set, SmashggStrings.Entrant1Score);
                newSet.entrant2wins = GetIntParameter(set, SmashggStrings.Entrant2Score);
                newSet.winner = GetIntParameter(set, SmashggStrings.Winner);
                newSet.state = GetIntParameter(set, SmashggStrings.State);

                if (!set[SmashggStrings.IsGF].IsNullOrEmpty())
                {
                    newSet.isGF = set[SmashggStrings.IsGF].Value<bool>();
                }
                else
                {
                    newSet.isGF = false;
                }

                // Bracket rank
                newSet.wPlacement = GetIntParameter(set, SmashggStrings.wPlace);
                newSet.lPlacement = GetIntParameter(set, SmashggStrings.lPlace);

                // Round and match identifiers
                newSet.id = GetIntParameter(set, SmashggStrings.ID);
                newSet.originalRound = GetIntParameter(set, SmashggStrings.OriginalRound);
                newSet.displayRound = GetIntParameter(set, SmashggStrings.DisplayRound);
                newSet.entrant1PrereqId = GetIntParameter(set, SmashggStrings.Entrant1PrereqId);
                newSet.entrant2PrereqId = GetIntParameter(set, SmashggStrings.Entrant2PrereqId);
                int round = Math.Abs(newSet.originalRound);

                if (newSet.originalRound == -99)
                {
                    continue;
                }
                else if (newSet.originalRound > 0)
                {
                    while (round > matchCountWinners.Count)
                    {
                        matchCountWinners.Add(0);
                    }

                    matchCountWinners[round - 1]++;
                    newSet.match = matchCountWinners[round - 1];
                }
                else if (newSet.originalRound < 0)
                {
                    while (round > matchCountLosers.Count)
                    {
                        matchCountLosers.Add(0);
                    }

                    matchCountLosers[round - 1]++;
                    newSet.match = matchCountLosers[round - 1];
                }

                setList.Add(newSet);
            }

            return true;
        }

        /// <summary>
        /// Returns an integer from the specified parameter, or -99 on a null entry
        /// </summary>
        /// <param name="token">json input</param>
        /// <param name="param">Requested parameter</param>
        /// <returns>Integer value of param, or -99 for null</returns>
        public int GetIntParameter(JToken token, string param)
        {
            if (!token[param].IsNullOrEmpty())
            {
                if (token[param].Type == JTokenType.Integer)
                {
                    return token[param].Value<int>();
                }
            }

            return -99;
        }
        #endregion
    }
}
