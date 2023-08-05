using System.Linq;
using ChessChallenge.API;

public partial class MyBot3_Base
{
  protected Move[] InternalIterativeDeepening(Move[] bestMoves, Board board, int color)
  {
    return IterativeDepth > 0
        ? NegaMaxRoot(bestMoves.AsQueryable().Take(5).ToArray(), board, IterativeDepth, color, UseMTD)
        : bestMoves;
  }
}