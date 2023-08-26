using ChessChallenge.API;

public class TestBot : IChessBot
{
  IChessBot Bot;
  public TestBot()
  {
    Bot = new MyBot6_Copy.MyBot(200, 4, 8, 4, 3);
  }

  public Move Think(Board board, Timer timer)
  {
    return Bot.Think(board, timer);
  }
}