using System;

namespace ChessFight.Network
{
    // A stable, seed-derived refuge corridor prevents a new wave trapping a slower player.
    // Waves randomize the remaining cells; a new run changes the corridor itself.
    public sealed class ArmorySpearPattern
    {
        public const int Columns = 4, Rows = 6, CellCount = Columns * Rows;
        readonly int[] safeStarts;
        readonly bool[] raised;
        ArmorySpearPattern(int[] starts, bool[] cells) { safeStarts = starts; raised = cells; }
        public int SafeStart(int row) => safeStarts[row];
        public bool Protected(int row, int column) => row >= 0 && row < Rows && column >= safeStarts[row] && column <= safeStarts[row] + 1;
        public bool Raised(int index) => index >= 0 && index < CellCount && raised[index];
        public static ArmorySpearPattern Generate(int seed, int wave)
        {
            var corridor = new Random(seed);
            var starts = new int[Rows];
            starts[0] = corridor.Next(Columns - 1);
            for (int row = 1; row < Rows; row++)
                starts[row] = row < 2 ? starts[0] : corridor.Next(Math.Max(0, starts[row - 1] - 1), Math.Min(Columns - 2, starts[row - 1] + 1) + 1);
            var random = new Random(unchecked(seed * 397 ^ wave * 7919 ^ 0x5e71));
            var cells = new bool[CellCount];
            for (int row = 0; row < Rows; row++)
            {
                int first = -1;
                for (int col = 0; col < Columns; col++)
                    if (col < starts[row] || col > starts[row] + 1)
                    {
                        first = row * Columns + col;
                        cells[first] = random.Next(3) != 0;
                    }
                // Every row has a visible threat, while both protected cells remain safe.
                bool any = false;
                for (int col = 0; col < Columns; col++) any |= cells[row * Columns + col];
                if (!any) cells[first] = true;
            }
            return new ArmorySpearPattern(starts, cells);
        }
    }
}
