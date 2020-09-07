using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegexSolver
{
    public class PuzzleJSON
    {
        public string id { get; set; }
        public string playerNo { get; set; }
        public string name { get; set; }
        public string[][] patternsX { get; set; }
        public string[][] patternsY { get; set; }
        public string[][] patternsZ { get; set; }
        public string[] solutionMap { get; set; }
        public string characters { get; set; }
        public string size { get; set; }
        public bool hexagonal { get; set; }
        public bool mobile { get; set; }
        public bool published { get; set; }
        public string dateCreated { get; set; }
        public string dateUpdated { get; set; }
        public float ratingAvg { get; set; }
        public string votes { get; set; }
        public object solved { get; set; }
        public bool ambiguous { get; set; }
    }
}
