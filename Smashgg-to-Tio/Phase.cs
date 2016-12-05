using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smashgg_to_Tio
{
    class Phase
    {
        public List<PhaseGroup> id;
        public int phaseId;
        private int waveId;

        public Phase()
        {
            id = new List<PhaseGroup>();
        }

        public int WaveId
        {
            get
            {
                return waveId;
            }
            set
            {
                waveId = value;
            }
        }
    }
}
