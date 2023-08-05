using System;
using ChessChallenge.API;

public partial class MyBot3_Base
{
  public int MTD(int depth, Board board, int guess, int color)
  {
    int upperBound = Inf;
    int lowerBound = -Inf;

    while (lowerBound < upperBound)
    {
      int beta = Math.Max(guess, lowerBound + 1);
      guess = NegaMax(depth, board, beta - 1, beta, color);

      if (guess < beta)
        upperBound = guess;
      else
        lowerBound = guess;
    }

    return guess;
  }
}