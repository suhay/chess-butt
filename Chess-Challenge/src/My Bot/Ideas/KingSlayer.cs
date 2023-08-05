using ChessChallenge.API;

public static class KingSlayer
{
  public static int Evaluate(Board board, Move move)
  {
    if (board.IsInCheck())
    {
      // Console.WriteLine("Check detected");
      return 1000;
    }

    PieceList[] pieceList = board.GetAllPieceLists();

    int left = 0;

    foreach (PieceList pieces in pieceList)
    {
      foreach (Piece piece in pieces)
      {
        left++;
      }
    }

    if (left <= 10)
    {
      // Console.WriteLine("end game");
    }

    return 0;
  }
}