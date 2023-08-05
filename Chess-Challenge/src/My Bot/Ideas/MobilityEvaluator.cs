using System.Collections.Generic;
using ChessChallenge.API;

public static class MobilityEvaluator
{
  public static int Evaluate(Board board)
  {
    Move[] nextMoves = board.GetLegalMoves();
    int value = 0;

    Dictionary<string, int> movesAvailable = new Dictionary<string, int>();
    foreach (Move move in nextMoves)
    {
      Piece movingPiece = board.GetPiece(move.StartSquare);

      if (movingPiece.IsWhite)
      {
        value += 1;
      }
      else
      {
        value -= 1;
      }
    }

    return value;
  }
}