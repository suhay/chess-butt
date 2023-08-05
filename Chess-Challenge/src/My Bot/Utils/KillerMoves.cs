using System.Collections.Generic;
using ChessChallenge.API;

public struct KillerMoves
{
  public KillerMoves()
  {
    K1 = new Dictionary<int, Move>();
    K2 = new Dictionary<int, Move>();
  }

  public Dictionary<int, Move> K1 { get; private set; }
  public Dictionary<int, Move> K2 { get; private set; }

  public void Store(Move move, int ply)
  {
    if (K1.ContainsKey(ply))
    {
      Move firstKillerMove = K1[ply];
      K2[ply] = firstKillerMove;
    }
    K1[ply] = move;
  }

  public void Clear()
  {
    K1.Clear();
    K2.Clear();
  }
}