﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smashgg_to_Tio
{
    class Entrant
    {
        private List<Player> players = new List<Player>();

        public Entrant() { }

        public Entrant(Player player)
        {
            AddPlayer(player);
        }

        public Entrant(List<Player> playerList)
        {
            foreach (Player player in playerList)
            {
                AddPlayer(player);
            }
        }

        public void AddPlayer(Player newPlayer)
        {
            players.Add(newPlayer);
        }

        public List<Player> Players
        {
            get
            {
                return players;
            }
        }
    }
}
