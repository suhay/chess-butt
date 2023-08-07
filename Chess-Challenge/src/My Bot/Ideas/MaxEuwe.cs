using System.Collections.Generic;
using ChessChallenge.API;

// This will use Max Euew's piece weights instead of the standard.
// Queen: 9.5 points
// Rook: 4.5 points
// Bishop: 3 points
// Knights: 3 points
// Pawn: 1 points
public static class MaxEuwe
{
  public static Dictionary<PieceType, int> pieceVal = new Dictionary<PieceType, int>
  {
    {PieceType.None, 0},
    {PieceType.Pawn, 100},
    {PieceType.Rook, 450},
    {PieceType.Knight, 300},
    {PieceType.Bishop, 300},
    {PieceType.Queen, 950},
    {PieceType.King, 0},
  };

  public static int Evaluate(Piece piece)
  {
    return pieceVal[piece.PieceType];
  }
}