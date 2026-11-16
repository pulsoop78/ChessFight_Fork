using System;
using System.Globalization;
namespace ChessFight.Network
{
    public sealed class ChessReplayStep
    {
        public readonly int From, To, RookFrom, RookTo, Captured;
        public readonly char Promotion;
        public readonly ulong Attack;
        public ChessReplayStep(string[] row)
        {
            From=int.Parse(row[0]);To=int.Parse(row[1]);RookFrom=int.Parse(row[2]);RookTo=int.Parse(row[3]);
            Captured=int.Parse(row[6]);Promotion=row[7][0];Attack=ulong.Parse(row[4],NumberStyles.HexNumber,CultureInfo.InvariantCulture);
        }
        // Resolve simultaneous visuals in score order, including captures of the first moving piece.
        public void Apply<T>(T[] board, Func<char,int,T> promote, Action<T> remove) where T:class
        {
            T moving=board[From];
            if(board[To]!=null) remove(board[To]);
            if(Captured>=0 && Captured!=To && board[Captured]!=null) {remove(board[Captured]);board[Captured]=null;}
            board[To]=moving;board[From]=null;
            if(Promotion!='.') {remove(moving);board[To]=promote(Promotion,To);}
            if(RookFrom>=0) {board[RookTo]=board[RookFrom];board[RookFrom]=null;}
        }
    }
}
