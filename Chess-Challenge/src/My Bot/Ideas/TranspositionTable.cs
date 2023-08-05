using System.Collections.Generic;

struct Transposition
{
  public Transposition(int score, int depth, int flag)
  {
    Depth = depth;
    Score = score;
    Flag = flag;
  }

  public int Depth { get; }
  public int Score { get; }
  /// <summary>
  /// 0 = Exact, 1 = Alpha, 2 = Beta
  /// </summary>
  public int Flag { get; }
}

public class TranspositionTable
{
  Dictionary<ulong, Transposition> Table = new();
  Dictionary<ulong, Transposition> MaxTable = new();

  internal void Clear()
  {
    Table.Clear();
    MaxTable.Clear();
  }

  internal int? Get(ulong key, int depth, int alpha, int beta, int ply)
  {
    Transposition entry = Table.ContainsKey(key) ? Table[key] : new(0, -1, 0);

    int i = 0;
    do
    {
      int score = entry.Score;
      if (score < -MyBot3_Base.CheckMateSoon) score += ply;
      if (score > MyBot3_Base.CheckMateSoon) score -= ply;

      if (entry.Depth >= depth)
      {
        if (entry.Flag == 0)
          return score;
        else if (entry.Flag == 1 && score <= alpha)
          return alpha;
        else if (entry.Flag == 2 && score >= beta)
          return beta;
      }
      i++;
      if (!MaxTable.ContainsKey(key)) break;
      entry = MaxTable[key];
    } while (i < 2);

    return null;
  }

  /// <summary>
  /// Flag: 0 = Exact, 1 = Alpha, 2 = Beta
  /// </summary>
  internal void Store(ulong key, int score, int depth, int flag, int ply)
  {
    if (score < -MyBot3_Base.CheckMateSoon) score -= ply;
    if (score > MyBot3_Base.CheckMateSoon) score += ply;

    Transposition newTransposition = new(score, depth, flag);
    if (MaxTable.ContainsKey(key))
    {
      if (MaxTable[key].Depth > depth)
        MaxTable[key] = newTransposition;
    }
    else
      MaxTable[key] = newTransposition;

    Table[key] = newTransposition;
  }
}