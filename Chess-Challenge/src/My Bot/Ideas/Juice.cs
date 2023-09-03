using ChessChallenge.API;

public enum ExperimentType
{
  TournamentHeatMap,
  KingSlayer,
  MaxEuwe,
  MobilityEvaluator,
  CapturePriority
}

public static class Juice
{
  public static int GetJuice(ExperimentType[] experiments, Board board)
  {
    int mod = 0;

    foreach (ExperimentType experiment in experiments)
    {
      switch (experiment)
      {
        case ExperimentType.MobilityEvaluator:
          mod += MobilityEvaluator.Evaluate(board);
          break;

        default:
          break;
      }
    }

    return mod;
  }

  public static int GetJuice(ExperimentType[] experiments, Board board, Move move)
  {
    int mod = 0;

    foreach (ExperimentType experiment in experiments)
    {
      switch (experiment)
      {
        case ExperimentType.KingSlayer:
          mod += KingSlayer.Evaluate(board, move);
          break;

        default:
          break;
      }
    }

    return mod;
  }

  public static int GetJuice(ExperimentType[] experiments, Piece piece)
  {
    int mod = 0;

    foreach (ExperimentType experiment in experiments)
    {
      switch (experiment)
      {
        case ExperimentType.TournamentHeatMap:
          mod += TournamentHeatMap.Evaluate(piece) + MyBot3_Base.PieceVal[piece.PieceType];
          break;

        case ExperimentType.MaxEuwe:
          mod += MaxEuwe.Evaluate(piece);
          break;

        default:
          break;
      }
    }

    return mod == 0 ? MyBot3_Base.PieceVal[piece.PieceType] : mod;
  }
}



