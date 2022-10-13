using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace GridDemo
{
    public class Rule
    {
        public Vector3 Color { get; set; }
        public List<Vector3> Neighbours { get; set; }
    }
}
