using ChessChallenge.API;

public struct KillerMoves
{
  public KillerMoves()
  {
    K1 = new Move[64];
    K2 = new Move[64];
  }

  public Move[] K1 { get; private set; }
  public Move[] K2 { get; private set; }

  public void Store(Move move, Board board, int ply)
  {
    Move firstKillerMove = K1[ply];

    if (firstKillerMove != move)
    {
      K2[ply] = firstKillerMove;
      K1[ply] = move;
    }
    else
      K1[ply] = move;
  }

  public void Clear()
  {
    K1 = new Move[64];
    K2 = new Move[64];
  }
}