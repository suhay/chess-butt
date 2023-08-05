using ChessChallenge.API;

public static class TournamentHeatMap
{
  public static int Evaluate(Piece piece)
  {
    bool evalWhite = piece.IsWhite;
    Square pos = piece.Square;
    int row = pos.Rank;
    int column = pos.File;

    switch (piece.PieceType)
    {
      case PieceType.Pawn:
        return evalWhite ? PawnWhite[row, column] : PawnBlack[row, column];
      case PieceType.Rook:
        return evalWhite ? WhiteRook[row, column] : BlackRook[row, column];
      case PieceType.Knight:
        return Knight[row, column];
      case PieceType.Bishop:
        return evalWhite ? WhiteBishop[row, column] : BlackBishop[row, column];
      case PieceType.Queen:
        return Queen[row, column];
      default:
        return 0;
    }
  }

  static int[,] PawnWhite = new int[,]
    {
      {0, 0, 0, 0, 0, 0, 0, 0},
      {50, 50, 50, 50, 50, 50, 50, 50},
      {10, 10, 20, 30, 30, 20, 10, 10},
      {5, 5, 10, 25, 25, 10, 5, 5},
      {0, 0, 0, 20, 20, 0, 0, 0},
      {5, -5, -10, 0, 0, -10, -5, 5},
      {5, 10, 10, -20, -20, 10, 10, 5},
      {0, 0, 0, 0, 0, 0, 0, 0}
    };

  static int[,] PawnBlack = ReverseArray(PawnWhite);

  static int[,] Knight = new int[,]
    {
      {-50, -40, -30, -30, -30, -30, -40, -50},
      {-40, -20, 0, 0, 0, 0, -20, -40},
      {-30, 0, 10, 15, 15, 10, 0, -30},
      {-30, 5, 15, 20, 20, 15, 5, -30},
      {-30, 0, 15, 20, 20, 15, 0, -30},
      {-30, 5, 10, 15, 15, 10, 5, -30},
      {-40, -20, 0, 5, 5, 0, -20, -40},
      {-50, -40, -30, -30, -30, -30, -40, -50}
    };

  static int[,] WhiteBishop = new int[,]
    {
      {-20, -10, -10, -10, -10, -10, -10, -20},
      {-10, 0, 0, 0, 0, 0, 0, -10},
      {-10, 0, 5, 10, 10, 5, 0, -10},
      {-10, 5, 5, 10, 10, 5, 5, -10},
      {-10, 0, 10, 10, 10, 10, 0, -10},
      {-10, 10, 10, 10, 10, 10, 10, -10},
      {-10, 5, 0, 0, 0, 0, 5, -10},
      {-20, -10, -10, -10, -10, -10, -10, -20}
    };
  static int[,] BlackBishop = ReverseArray(WhiteBishop);

  static int[,] WhiteRook = new int[,]
    {
      {0, 0, 0, 0, 0, 0, 0, 0},
      {5, 10, 10, 10, 10, 10, 10, 5},
      {-5, 0, 0, 0, 0, 0, 0, -5},
      {-5, 0, 0, 0, 0, 0, 0, -5},
      {-5, 0, 0, 0, 0, 0, 0, -5},
      {-5, 0, 0, 0, 0, 0, 0, -5},
      {-5, 0, 0, 0, 0, 0, 0, -5},
      {0, 0, 0, 5, 5, 0, 0, 0}
    };
  static int[,] BlackRook = ReverseArray(WhiteRook);

  static int[,] Queen = new int[,]
    {
      {-20, -10, -10, -5, -5, -10, -10, -20},
      {-10, 0, 0, 0, 0, 0, 0, -10},
      {-10, 0, 5, 5, 5, 5, 0, -10},
      {-5, 0, 5, 5, 5, 5, 0, -5},
      {0, 0, 5, 5, 5, 5, 0, -5},
      {-10, 5, 5, 5, 5, 5, 0, -10},
      {-10, 0, 5, 0, 0, 0, 0, -10},
      {-20, -10, -10, -5, -5, -10, -10, -20}
    };

  static int[,] WhiteKing = new int[,]
    {
      {-30, -40, -40, -50, -50, -40, -40, -30},
      {-30, -40, -40, -50, -50, -40, -40, -30},
      {-30, -40, -40, -50, -50, -40, -40, -30},
      {-30, -40, -40, -50, -50, -40, -40, -30},
      {-20, -30, -30, -40, -40, -30, -30, -20},
      {-10, -20, -20, -20, -20, -20, -20, -10},
      {20, 20, 0, 0, 0, 0, 20, 20},
      {20, 30, 10, 0, 0, 10, 30, 20}
    };
  static int[,] BlackKing = ReverseArray(WhiteKing);

  static int[,] ReverseArray(int[,] array)
  {
    int[,] revArray = new int[8, 8];
    for (int x = 7; x > 0; x--)
    {
      int xr = 0;
      for (int y = 0; y < 8; y++)
      {
        revArray[xr, y] = array[x, y];
        xr++;
      }
    }
    return revArray;
  }
}