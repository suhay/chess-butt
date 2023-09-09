using ChessChallenge.API;

public class TestBot : IChessBot
{
  IChessBot Bot;

  public TestBot()
  {
    Bot = new ChessButt2_Copy.MyBot();
  }

  public TestBot(Chromosomes x)
  {
    int DeltaCutoff = x.DeltaCutoff; // #DEBUG
    int MobilityWeight = x.MobilityWeight; // #DEBUG

    int FullDepthMoves = x.FullDepthMoves; // #DEBUG
    int ReductionLimit = x.ReductionLimit; // #DEBUG

    int R = x.R; // #DEBUG

    int Panic = x.Panic; // #DEBUG
    int PanicD = x.PanicD; // #DEBUG  2 prevented all over tines, but was slightly worse

    int LateGamePly = x.LateGamePly; // #DEBUG

    int TMax = x.TMax;  // #DEBUG

    Bot = new ChessButt_Copy.MyBot(DeltaCutoff, MobilityWeight, FullDepthMoves, ReductionLimit, R, Panic, PanicD, LateGamePly, TMax);
  }

  public Move Think(Board board, Timer timer)
  {
    return Bot.Think(board, timer);
  }
}