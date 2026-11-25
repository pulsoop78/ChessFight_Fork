using System;

namespace ChessFight.Network
{
    // Every row independently draws its density and uniformly shuffles all four positions.
    public sealed class ArmorySpearPattern
    {
        public const int Columns = 4, Rows = 6, CellCount = Columns * Rows;
        readonly int[] safeStarts;
        readonly bool[] raised;
        ArmorySpearPattern(int[] starts, bool[] cells) { safeStarts = starts; raised = cells; }
        public int SafeStart(int row) => safeStarts[row];
        public bool Protected(int row, int column) => row >= 0 && row < Rows && column >= 0 && column < Columns && !raised[row * Columns + column];
        public bool Raised(int index) => index >= 0 && index < CellCount && raised[index];
        public static ArmorySpearPattern Generate(int seed, int wave)
        {
            var random = new Random(unchecked(seed * 397 ^ wave * 7919 ^ 0x5e71));
            var starts = new int[Rows];
            var cells = new bool[CellCount];
            for (int row = 0; row < Rows; row++)
            {
                int raisedCount = random.Next(100) < 80 ? 3 : 2;
                int[] candidates = { 0, 1, 2, 3 };
                for (int i = Columns - 1; i > 0; i--)
                { int j = random.Next(i + 1); int c = candidates[i]; candidates[i] = candidates[j]; candidates[j] = c; }
                for (int i = 0; i < raisedCount; i++) cells[row * Columns + candidates[i]] = true;
                // Compatibility accessor: one safe tile, with no cross-row route constraint.
                for (int col = 0; col < Columns; col++)
                    if (!cells[row * Columns + col]) { starts[row] = col; break; }
            }
            return new ArmorySpearPattern(starts, cells);
        }
    }
}
