using System;
using ChessChallenge.API;

public partial class MyBot3_Base
{
  public int MTD(int depth, Board board, int guess, int color)
  {
    int upperbound = Max;
    int lowerbound = Min;

    while (lowerbound < upperbound)
    {
      int beta = Math.Max(guess, lowerbound + 1);
      guess = NegaMax(depth, board, beta - 1, beta, color);

      if (guess < beta)
        upperbound = guess;
      else
        lowerbound = guess;
    }

    return guess;
  }
}