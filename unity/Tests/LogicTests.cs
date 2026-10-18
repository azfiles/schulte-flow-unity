using System;
using System.Linq;
using SchulteFlow.Core;

internal static class LogicTests
{
    private static int checks;
    private static void Check(bool condition,string context)
    {
        checks++;if(!condition)throw new Exception("FAILED: "+context);
    }
    private static void Reject(Action action,string context)
    {
        try{action();}catch(ArgumentException){checks++;return;}
        throw new Exception("FAILED: expected invalid input rejection: "+context);
    }
    public static void Main()
    {
        for(int size=3;size<=5;size++)
        {
            for(int seed=0;seed<50;seed++)
            {
                var shuffled=new Round(size,GameMode.Classic,seed);
                Check(shuffled.Board.OrderBy(n=>n).SequenceEqual(Enumerable.Range(1,size*size)),"each shuffle is a full permutation");
                var copy=shuffled.Board;copy[0]=999;Check(shuffled.Board[0]!=999,"board cannot be mutated by caller");
            }
            foreach(GameMode mode in Enum.GetValues(typeof(GameMode)))
            {
                var game=new Round(size,mode,123);
                Check(game.Tap(1,0)==Hit.Ignored,"ready board cannot be played");
                game.Start(100);game.Start(200);Check(game.Elapsed(300)==200,"start is idempotent");
                Check(game.Tap(2,310)==Hit.Wrong && game.Next==1 && game.Mistakes==1,"wrong tap does not advance");
                Check(game.Tap(0,315)==Hit.Ignored && game.Tap(game.Count+1,315)==Hit.Ignored,"out of range ignored");
                Check(game.Tap(1,320)==Hit.Correct,"correct tap advances");
                Check(game.Tap(1,330)==Hit.Ignored && game.Mistakes==1,"completed tile ignored");
                Check(game.Tap(2,340)==Hit.Correct && game.Combo==2,"fast hits build combo");
                game.Pause(400);game.Pause(500);
                Check(game.Elapsed(100000)==300,"pause freezes time");
                Check(game.Tap(3,100000)==Hit.Ignored,"pause locks input");
                var saved=game.Snapshot(100000);game=Round.Restore(saved);
                Check(game.State==RoundState.Paused && game.Next==3 && game.Mistakes==1,"unfinished round restored paused");
                Check(game.MaxCombo==2 && game.Combo==0,"restore retains peak but resets active combo");
                game.Resume(200000);game.Resume(300000);
                for(int n=3;n<=game.Count;n++)Check(game.Tap(n,200100)==(n==game.Count?Hit.Complete:Hit.Correct),"full completion");
                Check(game.Elapsed(999999)==400,"result time remains frozen");
                Check(game.State==RoundState.Won && game.Next==game.Count+1,"won terminal state");
                Check(game.Snapshot(999999)==null,"finished round not resumable");
                Check(Math.Abs(game.Accuracy-(double)game.Count/(game.Count+1))<.000001,"accuracy includes wrong taps");
                Check(game.Tap(1,999999)==Hit.Ignored,"terminal input ignored");
            }
        }
        var sprint=new Round(3,GameMode.Sprint,1);sprint.Start(100);
        Check(sprint.Remaining(100)==20000,"sprint budget");
        Check(sprint.Tap(1,20099)==Hit.Correct,"tap one millisecond before deadline accepted");
        Check(sprint.Tap(2,20100)==Hit.Expired,"deadline tap expires before accepting input");
        Check(sprint.Completed==1 && sprint.State==RoundState.TimedOut,"timeout preserves progress");
        Check(sprint.Remaining(999999)==0 && sprint.Elapsed(999999)==20000,"timeout clamps time");
        Check(sprint.Snapshot(999999)==null,"expired round cannot be resumed");
        sprint=new Round(4,GameMode.Sprint,2);sprint.Start(0);sprint.Pause(5000);sprint.Resume(999999);
        Check(sprint.Remaining(1000999)==29000,"background time excluded from sprint");
        sprint.Pause(1029999);Check(sprint.State==RoundState.TimedOut,"pausing at deadline cannot bypass timeout");
        var combo=new Round(3,GameMode.Classic,0);combo.Start(0);
        combo.Tap(1,0);combo.Tap(2,1600);Check(combo.Combo==2,"zero timestamp and exact combo window");
        combo.Tap(3,3201);Check(combo.Combo==1,"slow hit resets combo");
        combo.Tap(7,3210);combo.Tap(4,3220);Check(combo.Combo==1 && combo.MaxCombo==2,"mistake breaks combo, retains peak");
        combo.Pause(3300);combo.Resume(10000);combo.Tap(5,10010);Check(combo.Combo==1,"pause breaks combo");
        var valid=combo.Snapshot(10010);
        var snapshot=Round.Restore(valid);valid.board[0]=999;Check(snapshot.Board[0]!=999,"restore copies snapshot board");
        Reject(()=>Round.Restore(valid),"bad board number");
        valid=combo.Snapshot(10010);valid.board[0]=valid.board[1];Reject(()=>Round.Restore(valid),"duplicate board value");
        valid=combo.Snapshot(10010);valid.next=0;Reject(()=>Round.Restore(valid),"zero next");
        valid=combo.Snapshot(10010);valid.maxCombo=99;Reject(()=>Round.Restore(valid),"impossible combo");
        valid=combo.Snapshot(10010);valid.elapsedMs=-1;Reject(()=>Round.Restore(valid),"negative time");
        valid=combo.Snapshot(10010);valid.version=2;Reject(()=>Round.Restore(valid),"unknown schema");
        valid=combo.Snapshot(10010);valid.mode=7;Reject(()=>Round.Restore(valid),"unknown game mode");
        valid=combo.Snapshot(10010);valid.mode=1;valid.elapsedMs=20000;Reject(()=>Round.Restore(valid),"expired stored challenge");
        Reject(()=>Round.Restore(null),"null save");Reject(()=>new Round(2,GameMode.Classic,0),"unsupported size");
        var classic=new Round(5,GameMode.Classic,1);classic.Start(20);
        Check(classic.Elapsed(10)==0,"monotonic input guard");
        Check(!classic.Tick(99999999) && classic.State==RoundState.Running,"classic has no deadline");
        Console.WriteLine("PASS: "+checks+" Unity core checks (all sizes, both modes, pause/restore, deadlines, combo and corrupt saves).");
    }
}
