using System;
using System.Collections.Generic;
using ChessChallenge.API;

public struct Chromosomes
{
  int dcMin = 1569;
  public int DeltaCutoff;
  int dcDefault = 300;
  int dcMax = 5000;

  int mwMin = 3;
  public int MobilityWeight; // #DEBUG
  int mwDefault = 8;
  int mwMax = 15;

  int fdmMin = 2; // #DEBUG
  public int FullDepthMoves; // #DEBUG
  int fdmDefault = 3; // #DEBUG
  int fdmMax = 10; // #DEBUG

  int rlMin = 1; // #DEBUG
  public int ReductionLimit; // #DEBUG
  int rlMax = 4; // #DEBUG
  int rlDefault = 3; // #DEBUG

  int rMin = 1; // #DEBUG
  public int R; // #DEBUG
  int rDefault = 2; // #DEBUG
  int rMax = 2; // #DEBUG

  int pMin = 6791; // #DEBUG
  public int Panic; // #DEBUG
  int pMax = 20000; // #DEBUG
  int pDefault = 10000; // #DEBUG

  int pdMin = 0; // #DEBUG  2 prevented all over tines, but was slightly worse
  public int PanicD; // #DEBUG  2 prevented all over tines, but was slightly worse
  int pdMax = 4; // #DEBUG  2 prevented all over tines, but was slightly worse
  int pdDefault = 2; // #DEBUG  2 prevented all over tines, but was slightly worse

  int lgpMin = 44; // #DEBUG
  public int LateGamePly; // #DEBUG
  int lgpMax = 90; // #DEBUG
  int lgpDefault = 70; // #DEBUG

  int tmMin = 3047476;  // #DEBUG
  public int TMax = 3000000;  // #DEBUG
  int tmMax = 10000000;  // #DEBUG
  int tmDefault = 3000000;  // #DEBUG

  public int Fitness = 0;

  public Chromosomes(bool random = false)
  {
    DeltaCutoff = dcDefault;
    MobilityWeight = mwDefault;
    FullDepthMoves = fdmDefault;
    ReductionLimit = rlDefault;
    R = rDefault;
    Panic = pDefault;
    PanicD = pdDefault;
    LateGamePly = lgpDefault;
    TMax = tmDefault;

    if (random)
    {
      ReRoll();
    }
  }

  public Chromosomes(int dc, int mw, int fdm, int rl, int r, int p, int pd, int lgp, int tm)
  {
    DeltaCutoff = dc;
    MobilityWeight = mw;
    FullDepthMoves = fdm;
    ReductionLimit = rl;
    R = r;
    Panic = p;
    PanicD = pd;
    LateGamePly = lgp;
    TMax = tm;
  }

  public void ReRoll()
  {
    Random rand = new();

    DeltaCutoff = rand.Next(dcMin, dcMax);
    MobilityWeight = rand.Next(mwMin, mwMax);
    FullDepthMoves = rand.Next(fdmMin, fdmMax);
    ReductionLimit = rand.Next(rlMin, rlMax);
    R = rand.Next(rMin, rMax);
    Panic = rand.Next(pMin, pMax);
    PanicD = rand.Next(pdMin, pdMax);
    LateGamePly = rand.Next(lgpMin, lgpMax);
    TMax = rand.Next(tmMin, tmMax);
  }

  public List<Chromosomes> CrossWith(Chromosomes x)
  {
    Random rand = new();

    int crossover = rand.Next(9);

    if (crossover > 0)
    {
      (DeltaCutoff, x.DeltaCutoff) = (x.DeltaCutoff, DeltaCutoff);
      crossover--;
    }

    if (crossover > 0)
    {
      (MobilityWeight, x.MobilityWeight) = (x.MobilityWeight, MobilityWeight);
      crossover--;
    }

    if (crossover > 0)
    {
      (FullDepthMoves, x.FullDepthMoves) = (x.FullDepthMoves, FullDepthMoves);
      crossover--;
    }

    if (crossover > 0)
    {
      (ReductionLimit, x.ReductionLimit) = (x.ReductionLimit, ReductionLimit);
      crossover--;
    }

    if (crossover > 0)
    {
      (R, x.R) = (x.R, R);
      crossover--;
    }

    if (crossover > 0)
    {
      (Panic, x.Panic) = (x.Panic, Panic);
      crossover--;
    }

    if (crossover > 0)
    {
      (PanicD, x.PanicD) = (x.PanicD, PanicD);
      crossover--;
    }

    if (crossover > 0)
    {
      (LateGamePly, x.LateGamePly) = (x.LateGamePly, LateGamePly);
    }

    if (rand.Next() % 7 < 5)
    {
      int mutation = rand.Next(1, 10);

      if (mutation == 1)
      {
        int max = Math.Max(DeltaCutoff, x.DeltaCutoff);
        int min = Math.Min(DeltaCutoff, x.DeltaCutoff);

        DeltaCutoff = rand.Next(min, max);
        x.DeltaCutoff = rand.Next(min, max);
      }

      if (mutation == 2)
      {
        int max = Math.Max(MobilityWeight, x.MobilityWeight);
        int min = Math.Min(MobilityWeight, x.MobilityWeight);

        MobilityWeight = rand.Next(min, max);
        x.MobilityWeight = rand.Next(min, max);
      }

      if (mutation == 3)
      {
        int max = Math.Max(FullDepthMoves, x.FullDepthMoves);
        int min = Math.Min(FullDepthMoves, x.FullDepthMoves);

        FullDepthMoves = rand.Next(min, max);
        x.FullDepthMoves = rand.Next(min, max);
      }

      if (mutation == 4)
      {
        int max = Math.Max(ReductionLimit, x.ReductionLimit);
        int min = Math.Min(ReductionLimit, x.ReductionLimit);

        ReductionLimit = rand.Next(min, max);
        x.ReductionLimit = rand.Next(min, max);
      }

      if (mutation == 5)
      {
        int max = Math.Max(R, x.R);
        int min = Math.Min(R, x.R);

        R = rand.Next(min, max);
        x.R = rand.Next(min, max);
      }

      if (mutation == 6)
      {
        int max = Math.Max(Panic, x.Panic);
        int min = Math.Min(Panic, x.Panic);

        Panic = rand.Next(min, max);
        x.Panic = rand.Next(min, max);
      }

      if (mutation == 7)
      {
        int max = Math.Max(PanicD, x.PanicD);
        int min = Math.Min(PanicD, x.PanicD);

        PanicD = rand.Next(min, max);
        x.PanicD = rand.Next(min, max);
      }

      if (mutation == 8)
      {
        int max = Math.Max(LateGamePly, x.LateGamePly);
        int min = Math.Min(LateGamePly, x.LateGamePly);

        LateGamePly = rand.Next(min, max);
        x.LateGamePly = rand.Next(min, max);
      }

      if (mutation == 9)
      {
        int max = Math.Max(TMax, x.TMax);
        int min = Math.Min(TMax, x.TMax);

        TMax = rand.Next(min, max);
        x.TMax = rand.Next(min, max);
      }
    }

    return new() { x, this };
  }

  public override readonly string ToString()
  {
    return string.Format("dc: {0}, mw: {1}, fdm: {2}, rl: {3}, r: {4}, p: {5}, pd: {6}, lgp: {7}, tm: {8}, Fitness = {9}",
      DeltaCutoff, MobilityWeight, FullDepthMoves, ReductionLimit, R, Panic, PanicD, LateGamePly, TMax, Fitness);
  }
}

public class TestBot : IChessBot
{
  IChessBot Bot;

  public TestBot()
  {
    int DeltaCutoff = 300; // #DEBUG
    int MobilityWeight = 8; // #DEBUG

    int FullDepthMoves = 3; // #DEBUG
    int ReductionLimit = 3; // #DEBUG

    int R = 2; // #DEBUG

    int Panic = 10000; // #DEBUG
    int PanicD = 2; // #DEBUG  2 prevented all over tines, but was slightly worse

    int LateGamePly = 70; // #DEBUG

    int TMax = 3000000;  // #DEBUG

    Bot = new ChessButt_Copy.MyBot(DeltaCutoff, MobilityWeight, FullDepthMoves, ReductionLimit, R, Panic, PanicD, LateGamePly, TMax);
  }

  public TestBot(Chromosomes x)
  {
    int DeltaCutoff = x.DeltaCutoff; // #DEBUG
    int MobilityWeight = x.MobilityWeight; // #DEBUG

    int FullDepthMoves = x.FullDepthMoves; // #DEBUG
    int ReductionLimit = x.ReductionLimit; // #DEBUG

    int R = x.R; // #DEBUG

    int Panic = x.Panic; // #DEBUG
    int PanicD = x.PanicD; // #DEBUG  2 prevented all over tines, but was slightly worse

    int LateGamePly = x.LateGamePly; // #DEBUG

    int TMax = x.TMax;  // #DEBUG

    Bot = new ChessButt_Copy.MyBot(DeltaCutoff, MobilityWeight, FullDepthMoves, ReductionLimit, R, Panic, PanicD, LateGamePly, TMax);
  }

  public Move Think(Board board, Timer timer)
  {
    return Bot.Think(board, timer);
  }
}