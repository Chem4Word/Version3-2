using System.Windows;

namespace Chem4Word.ACME.Behaviors
{
    public class CandidatePlacement
    {
        public int Separation { get; set; }
        public Vector Orientation { get; set; }
        public int NeighbourWeights { get; set; }
        public Point PossiblePlacement { get; set; }
        public bool Crowding { get; set; }
    }
}