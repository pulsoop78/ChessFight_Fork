"""Run the production paired-landing rules against all stored score positions."""
import json,shutil,subprocess,tempfile,unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
HARNESS=r'''
using System;using System.IO;using ChessFight.Network;
class Test {
 static void Main(string[] args) {
  var lines=File.ReadAllLines(args[0]);int pos=0,total=0,pairs=0;
  while(pos<lines.Length) {
   int n=int.Parse(lines[pos++]);var rows=new string[n][];var expected=new string[n];
   for(int i=0;i<n;i++){var parts=lines[pos++].Split('\t');rows[i]=parts[0].Split(',');expected[i]=parts[1];}
   var board=new string[64];string setup=rows[0][8];for(int s=0;s<64;s++)board[s]=setup[s]=='.'?null:setup[s].ToString();
   for(int i=0;i<n;i+=2) {
    int count=Math.Min(2,n-i);var rules=new ChessReplayStep[count];var originals=new string[count];
    for(int j=0;j<count;j++){rules[j]=new ChessReplayStep(rows[i+j]);originals[j]=board[rules[j].From];if(originals[j]==null)throw new Exception("Missing simultaneous source");}
    for(int j=0;j<count;j++) {
     rules[j].Apply(board,(c,s)=>c.ToString(),p=>{});
     string actual="";for(int s=0;s<64;s++)actual+=board[s]??".";
     if(actual!=expected[i+j])throw new Exception("Score position mismatch");total++;
    }
    pairs++;
   }
  }
  Console.WriteLine("PASS: "+pairs+" paired batches / "+total+" half-moves, captures, castling, en passant, promotion and odd final batch");
 }
}
'''
class ChessReplayPairs(unittest.TestCase):
    @unittest.skipUnless(shutil.which('mcs') and shutil.which('mono'),'Mono unavailable')
    def test_all_stored_games_pair_sources_and_final_positions(self):
        games=json.loads((ROOT/'Assets/Resources/GiantChessGames.json').read_text())['games']
        lines=[]
        for game in games:
            lines.append(str(len(game['rows'])))
            for row in game['rows']:
                fields=row.split(',');board=list(fields[8]);a,b,rf,rt=map(int,fields[:4]);captured=int(fields[6])
                if captured>=0:board[captured]='.'
                board[b]=board[a];board[a]='.'
                if fields[7]!='.':board[b]=fields[7]
                if rf>=0:board[rt]=board[rf];board[rf]='.'
                lines.append(row+'\t'+''.join(board))
        with tempfile.TemporaryDirectory() as temp:
            folder=Path(temp);(folder/'Scores.txt').write_text('\n'.join(lines));(folder/'Test.cs').write_text(HARNESS)
            exe=str(folder/'Pairs.exe')
            subprocess.run(['mcs','-out:'+exe,str(ROOT/'Assets/Scripts/Core/ChessReplayStep.cs'),str(folder/'Test.cs')],check=True)
            subprocess.run(['mono',exe,str(folder/'Scores.txt')],check=True)
