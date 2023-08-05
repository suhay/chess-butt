using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

public struct MoveEvaluation
{
  public int Score;
  public Move Move;
  public MoveEvaluation(int score, Move move)
  {
    Score = score;
    Move = move;
  }
}

public partial class MyBot3_Base
{
  protected Move[] SortMoves(Move[] moves, Board board, int color)
  {
    if (MoveSort == "module")
      return SortMovesModule(moves, board);

    return moves;
  }

  Move[] SortMovesModule(Move[] moves, Board board)
  {
    List<MoveEvaluation> orderedMoves = new List<MoveEvaluation>();
    foreach (Move move in moves)
    {
      int score = EvaluateMove(move, Ply);
      orderedMoves.Add(new MoveEvaluation(score, move));
    }

    return orderedMoves.OrderByDescending(o => o.Score).Select(o => o.Move).ToArray();
  }
}
