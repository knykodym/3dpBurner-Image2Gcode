using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace _3dpBurnerImage2Gcode
{
    public class GenerateGCodeResults
    {
        public List<string> FileLines { get; set; }
        public int PixBurned { get; set; }
        public int PixTotal { get; set; }
    }
}
