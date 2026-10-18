using System;

namespace SchulteFlow.Core
{
    public enum GameMode { Classic, Sprint }
    public enum RoundState { Ready, Running, Paused, Won, TimedOut }
    public enum Hit { Ignored, Wrong, Correct, Complete, Expired }

    [Serializable]
    public sealed class RoundSnapshot
    {
        public int version = 1;
        public int size;
        public int mode;
        public int[] board;
        public int next;
        public int mistakes;
        public int maxCombo;
        public long elapsedMs;
    }

    // No Unity dependency: monotonic time is supplied by the client.
    public sealed class Round
    {
        private readonly int[] board;
        private long accrued, started, lastHitElapsed = -1;
        public int Size { get; private set; }
        public int Count { get { return Size * Size; } }
        public GameMode Mode { get; private set; }
        public RoundState State { get; private set; }
        public int Next { get; private set; }
        public int Mistakes { get; private set; }
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }
        public int Completed { get { return Math.Min(Count, Next - 1); } }
        public long LimitMs { get { return Mode == GameMode.Classic ? 0 : Size == 3 ? 20000 : Size == 4 ? 35000 : 60000; } }
        public double Accuracy { get { return Completed + Mistakes == 0 ? 1 : (double)Completed / (Completed + Mistakes); } }
        public int[] Board { get { return (int[])board.Clone(); } }

        public Round(int size, GameMode mode, int seed)
        {
            if (size < 3 || size > 5) throw new ArgumentOutOfRangeException("size");
            if (!Enum.IsDefined(typeof(GameMode), mode)) throw new ArgumentOutOfRangeException("mode");
            Size = size; Mode = mode; Next = 1; State = RoundState.Ready;
            board = new int[Count];
            for (int i = 0; i < Count; i++) board[i] = i + 1;
            var rng = new Random(seed);
            for (int i = Count - 1; i > 0; i--) { int j = rng.Next(i + 1); int n = board[i]; board[i] = board[j]; board[j] = n; }
        }

        public long Elapsed(long now)
        {
            long value = accrued + (State == RoundState.Running ? Math.Max(0, now - started) : 0);
            return LimitMs > 0 ? Math.Min(value, LimitMs) : value;
        }
        public long Remaining(long now) { return Math.Max(0, LimitMs - Elapsed(now)); }
        public void Start(long now) { if (State == RoundState.Ready) { started = now; State = RoundState.Running; } }
        public bool Tick(long now)
        {
            if (State != RoundState.Running || LimitMs == 0 || Elapsed(now) < LimitMs) return false;
            accrued = LimitMs; State = RoundState.TimedOut; Combo = 0; return true;
        }
        public void Pause(long now)
        {
            if (State != RoundState.Running || Tick(now)) return;
            accrued = Elapsed(now); State = RoundState.Paused; ResetCombo();
        }
        public void Resume(long now) { if (State == RoundState.Paused) { started = now; State = RoundState.Running; ResetCombo(); } }
        private void ResetCombo() { Combo = 0; lastHitElapsed = -1; }

        public Hit Tap(int value, long now)
        {
            if (Tick(now)) return Hit.Expired;
            if (State != RoundState.Running || value < Next || value > Count) return Hit.Ignored;
            if (value != Next) { Mistakes++; ResetCombo(); return Hit.Wrong; }
            long elapsed = Elapsed(now);
            Combo = lastHitElapsed >= 0 && elapsed - lastHitElapsed <= 1600 ? Combo + 1 : 1;
            lastHitElapsed = elapsed; MaxCombo = Math.Max(MaxCombo, Combo); Next++;
            if (Next <= Count) return Hit.Correct;
            accrued = elapsed; State = RoundState.Won; return Hit.Complete;
        }

        public RoundSnapshot Snapshot(long now)
        {
            Tick(now);
            if (State != RoundState.Running && State != RoundState.Paused) return null;
            return new RoundSnapshot { size = Size, mode = (int)Mode, board = Board, next = Next,
                mistakes = Mistakes, maxCombo = MaxCombo, elapsedMs = Elapsed(now) };
        }

        public static Round Restore(RoundSnapshot s)
        {
            if (s == null || s.version != 1) throw new ArgumentException("Unsupported snapshot");
            var g = new Round(s.size, (GameMode)s.mode, 0);
            if (s.board == null || s.board.Length != g.Count || s.next < 1 || s.next > g.Count ||
                s.mistakes < 0 || s.mistakes > 1000000 || s.maxCombo < 0 || s.maxCombo > s.next - 1 ||
                s.elapsedMs < 0 || s.elapsedMs > 604800000 || (g.LimitMs > 0 && s.elapsedMs >= g.LimitMs))
                throw new ArgumentException("Invalid snapshot");
            var seen = new bool[g.Count + 1];
            foreach (int n in s.board)
            {
                if (n < 1 || n > g.Count || seen[n]) throw new ArgumentException("Invalid board");
                seen[n] = true;
            }
            Array.Copy(s.board, g.board, g.Count);
            g.Next = s.next; g.Mistakes = s.mistakes; g.MaxCombo = s.maxCombo;
            g.accrued = s.elapsedMs; g.State = RoundState.Paused;
            return g;
        }
    }
}
