using ChessChallenge.API;

public class TestBot : IChessBot
{
  IChessBot Bot;
  public TestBot()
  {
    Bot = new MyBot5_2.MyBot();
  }

  public Move Think(Board board, Timer timer)
  {
    return Bot.Think(board, timer);
  }
}