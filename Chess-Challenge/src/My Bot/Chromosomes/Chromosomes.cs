using System;
using System.Collections.Generic;

public class Chromosomes
{
  int dcMin = 3372;
  public int DeltaCutoff;
  int dcDefault = 300;
  int dcMax = 5000;

  int mwMin = 7;
  public int MobilityWeight; // #DEBUG
  int mwDefault = 8;
  int mwMax = 15;

  int fdmMin = 6; // #DEBUG
  public int FullDepthMoves; // #DEBUG
  int fdmDefault = 3; // #DEBUG
  int fdmMax = 10; // #DEBUG

  int rlMin = 2; // #DEBUG
  public int ReductionLimit; // #DEBUG
  int rlDefault = 3; // #DEBUG
  int rlMax = 4; // #DEBUG

  int rMin = 1; // #DEBUG
  public int R; // #DEBUG
  int rDefault = 2; // #DEBUG
  int rMax = 3; // #DEBUG

  int pMin = 11000; // #DEBUG
  public int Panic; // #DEBUG
  int pDefault = 10000; // #DEBUG
  int pMax = 20000; // #DEBUG

  int pdMin = 1; // #DEBUG  2 prevented all over tines, but was slightly worse
  public int PanicD; // #DEBUG  2 prevented all over tines, but was slightly worse
  int pdDefault = 2; // #DEBUG  2 prevented all over tines, but was slightly worse
  int pdMax = 3; // #DEBUG  2 prevented all over tines, but was slightly worse

  int lgpMin = 44; // #DEBUG
  public int LateGamePly; // #DEBUG
  int lgpDefault = 70; // #DEBUG
  int lgpMax = 90; // #DEBUG

  int tmMin = 3000000;  // #DEBUG
  public int TMax;  // #DEBUG
  int tmDefault = 3000000;  // #DEBUG
  int tmMax = 10000000;  // #DEBUG

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
        DeltaCutoff = rand.Next(dcMin, dcMax);
        x.DeltaCutoff = rand.Next(dcMin, dcMax);
      }

      if (mutation == 2)
      {
        MobilityWeight = rand.Next(mwMin, mwMax);
        x.MobilityWeight = rand.Next(mwMin, mwMax);
      }

      if (mutation == 3)
      {
        FullDepthMoves = rand.Next(fdmMin, fdmMax);
        x.FullDepthMoves = rand.Next(fdmMin, fdmMax);
      }

      if (mutation == 4)
      {
        ReductionLimit = rand.Next(rlMin, rlMax);
        x.ReductionLimit = rand.Next(rlMin, rlMax);
      }

      if (mutation == 5)
      {
        R = rand.Next(rMin, rMax);
        x.R = rand.Next(rMin, rMax);
      }

      if (mutation == 6)
      {
        Panic = rand.Next(pMin, pMax);
        x.Panic = rand.Next(pMin, pMax);
      }

      if (mutation == 7)
      {
        PanicD = rand.Next(pdMin, pdMax);
        x.PanicD = rand.Next(pdMin, pdMax);
      }

      if (mutation == 8)
      {
        LateGamePly = rand.Next(lgpMin, lgpMax);
        x.LateGamePly = rand.Next(lgpMin, lgpMax);
      }

      if (mutation == 9)
      {
        TMax = rand.Next(tmMin, tmMax);
        x.TMax = rand.Next(tmMin, tmMax);
      }
    }

    return new() { x, this };
  }

  public override string ToString()
  {
    return string.Format("dc: {0}, mw: {1}, fdm: {2}, rl: {3}, r: {4}, p: {5}, pd: {6}, lgp: {7}, tm: {8}, Fitness = {9}",
      DeltaCutoff, MobilityWeight, FullDepthMoves, ReductionLimit, R, Panic, PanicD, LateGamePly, TMax, Fitness);
  }

  public string NextGen()
  {
    return string.Format("new Chromosomes(dc: {0}, mw: {1}, fdm: {2}, rl: {3}, r: {4}, p: {5}, pd: {6}, lgp: {7}, tm: {8}),",
      DeltaCutoff, MobilityWeight, FullDepthMoves, ReductionLimit, R, Panic, PanicD, LateGamePly, TMax);
  }
}