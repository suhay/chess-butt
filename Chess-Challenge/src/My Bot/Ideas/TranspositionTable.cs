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
  /// 0 = Exact, 1 = Upper, -1 = Lower
  /// </summary>
  public int Flag { get; }
}

public class TranspositionTable
{
  Dictionary<ulong, Transposition> Table = new Dictionary<ulong, Transposition>();
  Dictionary<ulong, Transposition> MaxTable = new Dictionary<ulong, Transposition>();

  internal void Clear()
  {
    Table.Clear();
    MaxTable.Clear();
  }

  internal int? Get(ulong key, int depth, int alpha, int beta)
  {
    Transposition entry = Table.ContainsKey(key) ? Table[key] : new Transposition(0, -1, 0);

    int i = 0;
    do
    {
      if (entry.Depth >= depth)
      {
        if (entry.Flag == 0)
          return entry.Score;
        else if (entry.Flag == -1 && entry.Score >= alpha)
          return entry.Score;
        else if (entry.Flag == 1 && entry.Score <= beta)
          return entry.Score;
      }
      i++;
      entry = MaxTable.ContainsKey(key) ? MaxTable[key] : new Transposition(0, -1, 0);
    } while (i < 2);

    return null;
  }

  internal void Store(ulong key, int score, int oldAlpha, int beta, int depth)
  {
    int flag = 0;
    if (score < oldAlpha)
      flag = 1;
    else if (score > beta)
      flag = -1;

    Transposition newTransposition = new Transposition(score, depth, flag);
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