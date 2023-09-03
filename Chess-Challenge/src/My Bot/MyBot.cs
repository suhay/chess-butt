using ChessChallenge.API;

public class MyBot : IChessBot
{
  IChessBot Bot;

  public MyBot()
  {
    int DeltaCutoff = 300; // #DEBUG
    int MobilityWeight = 8; // #DEBUG

    int FullDepthMoves = 3; // #DEBUG
    int ReductionLimit = 3; // #DEBUG

    int R = 2; // #DEBUG

    int Panic = 10000; // #DEBUG
    int PanicD = 2; // #DEBUG  2 prevented all over tines, but was slightly worse

    int LateGamePly = 70; // #DEBUG

    int TMax = 3000000;  // #DEBUG

    Bot = new ChessButt.MyBot(DeltaCutoff, MobilityWeight, FullDepthMoves, ReductionLimit, R, Panic, PanicD, LateGamePly, TMax);
  }

  public MyBot(Chromosomes x)
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

    Bot = new ChessButt.MyBot(DeltaCutoff, MobilityWeight, FullDepthMoves, ReductionLimit, R, Panic, PanicD, LateGamePly, TMax);
  }

  public Move Think(Board board, Timer timer)
  {
    return Bot.Think(board, timer);
  }
}