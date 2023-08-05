using System.Collections.Generic;
using ChessChallenge.API;

public partial class MyBot : IChessBot
{
  IChessBot bot;

  public static Dictionary<PieceType, int> PieceVal = new Dictionary<PieceType, int>
  {
    {PieceType.None, 0},
    {PieceType.Pawn, 100},
    {PieceType.Rook, 500},
    {PieceType.Knight, 300},
    {PieceType.Bishop, 300},
    {PieceType.Queen, 900},
    {PieceType.King, 10000}
  };

  public MyBot()
  {
    bot = new MyBot3();
  }

  public Move Think(Board board, Timer timer)
  {
    return bot.Think(board, timer);
  }
}