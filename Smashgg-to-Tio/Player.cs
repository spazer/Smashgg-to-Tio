using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smashgg_to_Tio
{
    class Player
    {
        public string name;
        public string country;

        // Blank constructor
        public Player() { }

        // Overloaded constructor
        public Player(string inputName, string inputCountry)
        {
            name = inputName;
            country = inputCountry;
        }
    }
}
