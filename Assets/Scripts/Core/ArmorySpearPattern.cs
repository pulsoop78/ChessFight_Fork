using System;

namespace ChessFight.Network
{
    // Each telegraphed wave has a different connected refuge, with bridge cells at turns.
    public sealed class ArmorySpearPattern
    {
        public const int Columns = 4, Rows = 6, CellCount = Columns * Rows;
        readonly int[] safeStarts;
        readonly bool[] raised;
        ArmorySpearPattern(int[] starts, bool[] cells) { safeStarts = starts; raised = cells; }
        public int SafeStart(int row) => safeStarts[row];
        public bool Protected(int row, int column) => row >= 0 && row < Rows && (column == safeStarts[row] || (row > 0 && column == safeStarts[row - 1]));
        public bool Raised(int index) => index >= 0 && index < CellCount && raised[index];
        public static ArmorySpearPattern Generate(int seed, int wave)
        {
            var random = new Random(unchecked(seed * 397 ^ wave * 7919 ^ 0x5e71));
            var starts = new int[Rows];
            // The entrance refuge moves by one column per wave, bouncing at the edges.
            int offset = new Random(seed).Next(6);
            int step = (int)(((long)wave + offset) % 6 + 6) % 6;
            starts[0] = step < Columns ? step : 6 - step;
            for (int row = 1; row < Rows; row++)
            {
                int lo = Math.Max(0, starts[row - 1] - 1), hi = Math.Min(Columns - 1, starts[row - 1] + 1);
                starts[row] = random.Next(lo, hi + 1);
            }
            var cells = new bool[CellCount];
            for (int row = 0; row < Rows; row++)
            {
                int[] candidates=new int[3];int count=0;
                for(int col=0;col<Columns;col++)if(col!=starts[row] && (row==0 || col!=starts[row-1]))candidates[count++]=col;
                for(int i=count-1;i>0;i--){int j=random.Next(i+1);int c=candidates[i];candidates[i]=candidates[j];candidates[j]=c;}
                int raisedCount=row==0?3:Math.Min(count,2+random.Next(2));
                for(int i=0;i<raisedCount;i++)cells[row*Columns+candidates[i]]=true;
            }
            return new ArmorySpearPattern(starts, cells);
        }
    }
}
