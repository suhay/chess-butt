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
  protected Move[] SortMoves(Move[] moves, Board board, bool isRoot = false)
  {
    if (MoveSort == "module")
      return SortMovesModule(moves, board, isRoot);

    return moves;
  }

  // 1. PV move
  // 2. Captures in MVV/LVA
  // 3. 1st killer move
  // 4. 2nd killer move
  // 5. History moves
  // 6. Unsorted moves
  Move[] SortMovesModule(Move[] moves, Board board, bool isRoot = false)
  {
    List<MoveEvaluation> orderedMoves = new();
    foreach (Move move in moves)
    {
      int score = EvaluateMove(board, move, board.PlyCount, isRoot);
      orderedMoves.Add(new MoveEvaluation(score, move));
    }

    return orderedMoves.OrderByDescending(o => o.Score).Select(o => o.Move).ToArray();
  }
}
