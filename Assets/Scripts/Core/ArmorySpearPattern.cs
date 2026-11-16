using System;

namespace ChessFight.Network
{
    // A stable, seed-derived refuge path prevents a new wave trapping a slower player.
    // Waves randomize the remaining cells; a new run changes the corridor itself.
    public sealed class ArmorySpearPattern
    {
        public const int Columns = 4, Rows = 6, CellCount = Columns * Rows;
        readonly int[] safeStarts;
        readonly bool[] raised;
        ArmorySpearPattern(int[] starts, bool[] cells) { safeStarts = starts; raised = cells; }
        public int SafeStart(int row) => safeStarts[row];
        public bool Protected(int row, int column) => row >= 0 && row < Rows && column >= safeStarts[row] && column == safeStarts[row];
        public bool Raised(int index) => index >= 0 && index < CellCount && raised[index];
        public static ArmorySpearPattern Generate(int seed, int wave)
        {
            var corridor = new Random(seed);
            var starts = new int[Rows];
            starts[0] = corridor.Next(Columns);
            for (int row = 1; row < Rows; row++)
                starts[row] = starts[0];
            var random = new Random(unchecked(seed * 397 ^ wave * 7919 ^ 0x5e71));
            var cells = new bool[CellCount];
            for (int row = 0; row < Rows; row++)
            {
                int[] candidates=new int[3];int count=0;
                for(int col=0;col<Columns;col++)if(col!=starts[row])candidates[count++]=col;
                for(int i=2;i>0;i--){int j=random.Next(i+1);int c=candidates[i];candidates[i]=candidates[j];candidates[j]=c;}
                int raisedCount=2+random.Next(2);
                for(int i=0;i<raisedCount;i++)cells[row*Columns+candidates[i]]=true;
            }
            return new ArmorySpearPattern(starts, cells);
        }
    }
}
