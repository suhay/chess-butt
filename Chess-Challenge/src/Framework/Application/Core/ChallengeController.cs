using ChessChallenge.Chess;
using ChessChallenge.Example;
using Raylib_cs;
using System;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static ChessChallenge.Application.Settings;
using static ChessChallenge.Application.ConsoleHelper;
using System.Collections.Generic;

namespace ChessChallenge.Application
{
  public class ChallengeController
  {
    public enum PlayerType
    {
      Human,
      MyBot,
      EvilBot,
      TestBot
    }

    // Game state
    readonly Random rng;
    int gameID;
    bool isPlaying;
    Board board;
    public ChessPlayer PlayerWhite { get; private set; }
    public ChessPlayer PlayerBlack { get; private set; }

    float lastMoveMadeTime;
    bool isWaitingToPlayMove;
    Move moveToPlay;
    float playMoveTime;
    public bool HumanWasWhiteLastGame { get; private set; }

    // Bot match state
    readonly string[] botMatchStartFens;
    int botMatchGameIndex;
    public BotMatchStats BotStatsA { get; private set; }
    public BotMatchStats BotStatsB { get; private set; }
    bool botAPlaysWhite;


    // Bot task
    AutoResetEvent botTaskWaitHandle;
    bool hasBotTaskException;
    ExceptionDispatchInfo botExInfo;

    // Other
    readonly BoardUI boardUI;
    readonly MoveGenerator moveGenerator;
    readonly int tokenCount;
    readonly int debugTokenCount;
    readonly StringBuilder pgns;

    int MaxGames = 100;

    bool ContinuousPlay = false;
    List<Chromosomes> GenA;
    Chromosomes A;

    List<Chromosomes> GenB;
    Chromosomes B;

    List<Chromosomes> Elite = new();

    public ChallengeController()
    {
      GenA = new() {
        new Chromosomes(dc: 3626, mw: 3, fdm: 9, rl: 2, r: 1, p: 6791, pd: 1, lgp: 49, tm: 6642950),
        new Chromosomes(dc: 2520, mw: 13, fdm: 9, rl: 2, r: 1, p: 13372, pd: 2, lgp: 56, tm: 3978872),
        new Chromosomes(dc: 1569, mw: 3, fdm: 9, rl: 2, r: 1, p: 10592, pd: 1, lgp: 47, tm: 3322815),
        new Chromosomes(dc: 1978, mw: 9, fdm: 2, rl: 2, r: 1, p: 11015, pd: 2, lgp: 49, tm: 9161247),
        new Chromosomes(dc: 2596, mw: 8, fdm: 9, rl: 2, r: 1, p: 14953, pd: 1, lgp: 89, tm: 4747193),
        new Chromosomes(dc: 3726, mw: 9, fdm: 6, rl: 2, r: 1, p: 10704, pd: 0, lgp: 79, tm: 9175919),
        new Chromosomes(dc: 3626, mw: 3, fdm: 9, rl: 2, r: 1, p: 17380, pd: 1, lgp: 44, tm: 9015687),
        new Chromosomes(true),
        new Chromosomes(true),
        new Chromosomes(true),
      };

      GenB = new() {
        new Chromosomes(dc: 2411, mw: 7, fdm: 4, rl: 3, r: 1, p: 17394, pd: 2, lgp: 50, tm: 8056241),
        new Chromosomes(dc: 2520, mw: 13, fdm: 9, rl: 2, r: 1, p: 10695, pd: 2, lgp: 89, tm: 8968382),
        new Chromosomes(dc: 1569, mw: 3, fdm: 9, rl: 2, r: 1, p: 10592, pd: 1, lgp: 47, tm: 3047476),
        new Chromosomes(dc: 1978, mw: 9, fdm: 2, rl: 2, r: 1, p: 11015, pd: 2, lgp: 70, tm: 8744802),
        new Chromosomes(dc: 4970, mw: 10, fdm: 7, rl: 2, r: 1, p: 9026, pd: 3, lgp: 63, tm: 7597468),
        new Chromosomes(dc: 3726, mw: 9, fdm: 6, rl: 2, r: 1, p: 10704, pd: 0, lgp: 60, tm: 7247345),
        new Chromosomes(dc: 3626, mw: 3, fdm: 9, rl: 2, r: 1, p: 17380, pd: 1, lgp: 72, tm: 7794850),
        new Chromosomes(true),
        new Chromosomes(true),
        new Chromosomes(true),
      };

      Log($"Launching Chess-Challenge version {Settings.Version}");
      (tokenCount, debugTokenCount) = GetTokenCount();
      Warmer.Warm();

      rng = new Random();
      moveGenerator = new();
      boardUI = new BoardUI();
      board = new Board();
      pgns = new();

      BotStatsA = new BotMatchStats("IBot");
      BotStatsB = new BotMatchStats("IBot");
      botMatchStartFens = FileHelper.ReadResourceFile("Fens.txt").Split('\n').Where(fen => fen.Length > 0).ToArray();
      botTaskWaitHandle = new AutoResetEvent(false);

      MaxGames = 7;
      // MaxGames = botMatchStartFens.Length * 2;

      StartNewGame(PlayerType.Human, PlayerType.MyBot);
    }

    public void StartNewGame(PlayerType whiteType, PlayerType blackType)
    {
      // End any ongoing game
      EndGame(GameResult.DrawByArbiter, log: false, autoStartNextBotMatch: false);
      gameID = rng.Next();

      // Stop prev task and create a new one
      if (RunBotsOnSeparateThread)
      {
        // Allow task to terminate
        botTaskWaitHandle.Set();
        // Create new task
        botTaskWaitHandle = new AutoResetEvent(false);
        Task.Factory.StartNew(BotThinkerThread, TaskCreationOptions.LongRunning);
      }
      // Board Setup
      board = new Board();
      bool isGameWithHuman = whiteType is PlayerType.Human || blackType is PlayerType.Human;
      int fenIndex = isGameWithHuman ? 0 : botMatchGameIndex / 2;
      board.LoadPosition(botMatchStartFens[fenIndex]);

      // Player Setup
      PlayerWhite = CreatePlayer(whiteType);
      PlayerBlack = CreatePlayer(blackType);
      PlayerWhite.SubscribeToMoveChosenEventIfHuman(OnMoveChosen);
      PlayerBlack.SubscribeToMoveChosenEventIfHuman(OnMoveChosen);

      // UI Setup
      boardUI.UpdatePosition(board);
      boardUI.ResetSquareColours();
      SetBoardPerspective();

      // Start
      isPlaying = true;
      NotifyTurnToMove();
    }

    void BotThinkerThread()
    {
      int threadID = gameID;
      //Console.WriteLine("Starting thread: " + threadID);

      while (true)
      {
        // Sleep thread until notified
        botTaskWaitHandle.WaitOne();
        // Get bot move
        if (threadID == gameID)
        {
          var move = GetBotMove();

          if (threadID == gameID)
          {
            OnMoveChosen(move);
          }
        }
        // Terminate if no longer playing this game
        if (threadID != gameID)
        {
          break;
        }
      }
      //Console.WriteLine("Exitting thread: " + threadID);
    }

    Move GetBotMove()
    {
      API.Board botBoard = new(board);
      try
      {
        API.Timer timer = new(PlayerToMove.TimeRemainingMs, PlayerNotOnMove.TimeRemainingMs, GameDurationMilliseconds, IncrementMilliseconds);
        API.Move move = PlayerToMove.Bot.Think(botBoard, timer);
        return new Move(move.RawValue);
      }
      catch (Exception e)
      {
        Log("An error occurred while bot was thinking.\n" + e.ToString(), true, ConsoleColor.Red);
        hasBotTaskException = true;
        botExInfo = ExceptionDispatchInfo.Capture(e);
      }
      return Move.NullMove;
    }

    void NotifyTurnToMove()
    {
      //playerToMove.NotifyTurnToMove(board);
      if (PlayerToMove.IsHuman)
      {
        PlayerToMove.Human.SetPosition(FenUtility.CurrentFen(board));
        PlayerToMove.Human.NotifyTurnToMove();
      }
      else
      {
        if (RunBotsOnSeparateThread)
        {
          botTaskWaitHandle.Set();
        }
        else
        {
          double startThinkTime = Raylib.GetTime();
          var move = GetBotMove();
          double thinkDuration = Raylib.GetTime() - startThinkTime;
          PlayerToMove.UpdateClock(thinkDuration);
          OnMoveChosen(move);
        }
      }
    }

    void SetBoardPerspective()
    {
      // Board perspective
      if (PlayerWhite.IsHuman || PlayerBlack.IsHuman)
      {
        boardUI.SetPerspective(PlayerWhite.IsHuman);
        HumanWasWhiteLastGame = PlayerWhite.IsHuman;
      }
      else if (PlayerWhite.Bot is MyBot && PlayerBlack.Bot is MyBot)
      {
        boardUI.SetPerspective(true);
      }
      else
      {
        boardUI.SetPerspective(PlayerWhite.Bot is MyBot);
      }
    }

    ChessPlayer CreatePlayer(PlayerType type)
    {
      if (ContinuousPlay)
      {
        return type switch
        {
          PlayerType.MyBot => new ChessPlayer(new MyBot(A), type, GameDurationMilliseconds),
          PlayerType.EvilBot => new ChessPlayer(new EvilBot(), type, GameDurationMilliseconds),
          PlayerType.TestBot => new ChessPlayer(new TestBot(B), type, GameDurationMilliseconds),
          _ => new ChessPlayer(new HumanPlayer(boardUI), type)
        };
      }

      return type switch
      {
        PlayerType.MyBot => new ChessPlayer(new MyBot(), type, GameDurationMilliseconds),
        PlayerType.EvilBot => new ChessPlayer(new EvilBot(), type, GameDurationMilliseconds),
        PlayerType.TestBot => new ChessPlayer(new TestBot(), type, GameDurationMilliseconds),
        _ => new ChessPlayer(new HumanPlayer(boardUI), type)
      };
    }

    static (int totalTokenCount, int debugTokenCount) GetTokenCount()
    {
      string path = Path.Combine(Directory.GetCurrentDirectory(), "src", "My Bot", "ChessButt", "ChessButt.cs");

      using StreamReader reader = new(path);
      string txt = reader.ReadToEnd();
      return TokenCounter.CountTokens(txt);
    }

    void OnMoveChosen(Move chosenMove)
    {
      if (IsLegal(chosenMove))
      {
        PlayerToMove.AddIncrement(IncrementMilliseconds);
        if (PlayerToMove.IsBot)
        {
          moveToPlay = chosenMove;
          isWaitingToPlayMove = true;
          playMoveTime = lastMoveMadeTime + MinMoveDelay;
        }
        else
        {
          PlayMove(chosenMove);
        }
      }
      else
      {
        string moveName = MoveUtility.GetMoveNameUCI(chosenMove);
        string log = $"Illegal move: {moveName} in position: {FenUtility.CurrentFen(board)}";
        Log(log, true, ConsoleColor.Red);
        GameResult result = PlayerToMove == PlayerWhite ? GameResult.WhiteIllegalMove : GameResult.BlackIllegalMove;
        EndGame(result);
      }
    }

    void PlayMove(Move move)
    {
      if (isPlaying)
      {
        bool animate = PlayerToMove.IsBot;
        lastMoveMadeTime = (float)Raylib.GetTime();

        board.MakeMove(move, false);
        boardUI.UpdatePosition(board, move, animate);

        GameResult result = Arbiter.GetGameState(board);
        if (result == GameResult.InProgress)
        {
          NotifyTurnToMove();
        }
        else
        {
          EndGame(result);
        }
      }
    }

    void EndGame(GameResult result, bool log = true, bool autoStartNextBotMatch = true)
    {
      if (isPlaying)
      {
        isPlaying = false;
        isWaitingToPlayMove = false;
        gameID = -1;

        if (log)
        {
          // Log("Game Over: " + result, false, ConsoleColor.Blue);
        }

        string pgn = PGNCreator.CreatePGN(board, result, GetPlayerName(PlayerWhite), GetPlayerName(PlayerBlack));
        pgns.AppendLine(pgn);

        // If 2 bots playing each other, start next game automatically.
        if (PlayerWhite.IsBot && PlayerBlack.IsBot)
        {
          UpdateBotMatchStats(result);
          botMatchGameIndex++;
          int numGamesToPlay = MaxGames;

          if (botMatchGameIndex < numGamesToPlay && autoStartNextBotMatch)
          {
            botAPlaysWhite = !botAPlaysWhite;
            const int startNextGameDelayMs = 600;
            System.Timers.Timer autoNextTimer = new(startNextGameDelayMs);
            int originalGameID = gameID;
            autoNextTimer.Elapsed += (s, e) => AutoStartNextBotMatchGame(originalGameID, autoNextTimer);
            autoNextTimer.AutoReset = false;
            autoNextTimer.Start();

          }
          else if (autoStartNextBotMatch)
          {
            Log("Match finished", false, ConsoleColor.Blue);
            Log("Result: " + BotStatsA.NumWins + "-" + BotStatsA.NumDraws + "-" + BotStatsA.NumLosses, false, ConsoleColor.Blue);

            if (ContinuousPlay)
            {
              A.Fitness = BotStatsA.NumWins - BotStatsA.NumLosses;
              B.Fitness = BotStatsB.NumWins - BotStatsB.NumLosses;

              Elite.Add(A);
              Elite.Add(B);

              if (GenA.Count == 0)
              {
                Elite = Elite.OrderByDescending(a => a.Fitness).ToList();
                Console.WriteLine("------------------");
                Log(Elite[0].ToString());
                Log(Elite[1].ToString());

                Elite[2].CrossWith(Elite[3]).ForEach((e) => Log(e.ToString()));
                Elite[4].CrossWith(Elite[5]).ForEach((e) => Log(e.ToString()));
                Elite[6].CrossWith(Elite[7]).ForEach((e) => Log(e.ToString()));
                Elite[8].CrossWith(Elite[9]).ForEach((e) => Log(e.ToString()));
                Elite[10].CrossWith(Elite[11]).ForEach((e) => Log(e.ToString()));
                Elite[12].CrossWith(Elite[13]).ForEach((e) => Log(e.ToString()));
                Elite[14].CrossWith(Elite[5]).ForEach((e) => Log(e.ToString()));

                Log($"DeltaCutoff = {Elite.Min(e => e.DeltaCutoff)}, MobilityWeight = {Elite.Min(e => e.MobilityWeight)}, FullDepthMoves = {Elite.Min(e => e.FullDepthMoves)}, ReductionLimit = {Elite.Min(e => e.ReductionLimit)}, R = {Elite.Min(e => e.R)}, Panic = {Elite.Min(e => e.Panic)}, PanicD = {Elite.Min(e => e.PanicD)}, LateGamePly = {Elite.Min(e => e.LateGamePly)}, TMax = {Elite.Min(e => e.TMax)}", false, ConsoleColor.Cyan);

                return;
              }

              Elite.ForEach((e) => Log(e.ToString(), false, ConsoleColor.DarkRed));
              StartNewBotMatch(PlayerType.MyBot, PlayerType.TestBot, ContinuousPlay);
            }
          }
        }
      }
    }

    private void AutoStartNextBotMatchGame(int originalGameID, System.Timers.Timer timer)
    {
      if (originalGameID == gameID)
      {
        StartNewGame(PlayerBlack.PlayerType, PlayerWhite.PlayerType);
      }
      timer.Close();
    }


    void UpdateBotMatchStats(GameResult result)
    {
      UpdateStats(BotStatsA, botAPlaysWhite);
      UpdateStats(BotStatsB, !botAPlaysWhite);

      void UpdateStats(BotMatchStats stats, bool isWhiteStats)
      {
        // Draw
        if (Arbiter.IsDrawResult(result))
        {
          stats.NumDraws++;
        }
        // Win
        else if (Arbiter.IsWhiteWinsResult(result) == isWhiteStats)
        {
          stats.NumWins++;
        }
        // Loss
        else
        {
          stats.NumLosses++;
          stats.NumTimeouts += (result is GameResult.WhiteTimeout or GameResult.BlackTimeout) ? 1 : 0;
          stats.NumIllegalMoves += (result is GameResult.WhiteIllegalMove or GameResult.BlackIllegalMove) ? 1 : 0;
        }
      }
    }

    public void Update()
    {
      if (isPlaying)
      {
        PlayerWhite.Update();
        PlayerBlack.Update();

        PlayerToMove.UpdateClock(Raylib.GetFrameTime());
        if (PlayerToMove.TimeRemainingMs <= 0)
        {
          EndGame(PlayerToMove == PlayerWhite ? GameResult.WhiteTimeout : GameResult.BlackTimeout);
        }
        else
        {
          if (isWaitingToPlayMove && Raylib.GetTime() > playMoveTime)
          {
            isWaitingToPlayMove = false;
            PlayMove(moveToPlay);
          }
        }
      }

      if (hasBotTaskException)
      {
        hasBotTaskException = false;
        botExInfo.Throw();
      }
    }

    public void Draw()
    {
      boardUI.Draw();
      string nameW = GetPlayerName(PlayerWhite);
      string nameB = GetPlayerName(PlayerBlack);
      boardUI.DrawPlayerNames(nameW, nameB, PlayerWhite.TimeRemainingMs, PlayerBlack.TimeRemainingMs, isPlaying);
    }

    public void DrawOverlay()
    {
      BotBrainCapacityUI.Draw(tokenCount, debugTokenCount, MaxTokenCount);
      MenuUI.DrawButtons(this);
      MatchStatsUI.DrawMatchStats(this);
    }

    static string GetPlayerName(ChessPlayer player) => GetPlayerName(player.PlayerType);
    static string GetPlayerName(PlayerType type) => type.ToString();

    public void StartNewBotMatch(PlayerType botTypeA, PlayerType botTypeB, bool continuousPlay = false)
    {
      ContinuousPlay = continuousPlay;
      EndGame(GameResult.DrawByArbiter, log: false, autoStartNextBotMatch: false);
      botMatchGameIndex = 0;
      string nameA = GetPlayerName(botTypeA);
      string nameB = GetPlayerName(botTypeB);
      if (nameA == nameB)
      {
        nameA += " (A)";
        nameB += " (B)";
      }
      BotStatsA = new BotMatchStats(nameA);
      BotStatsB = new BotMatchStats(nameB);
      botAPlaysWhite = true;
      Log($"Starting new match: {nameA} vs {nameB}", false, ConsoleColor.Blue);

      if (ContinuousPlay)
      {
        Random rand = new();
        A = GenA[rand.Next(GenA.Count)];
        GenA.Remove(A);

        B = GenB[rand.Next(GenB.Count)];
        GenB.Remove(B);

        Log(A.ToString(), false, ConsoleColor.Green);
        Log(B.ToString(), false, ConsoleColor.Yellow);
      }

      StartNewGame(botTypeA, botTypeB);
    }


    ChessPlayer PlayerToMove => board.IsWhiteToMove ? PlayerWhite : PlayerBlack;
    ChessPlayer PlayerNotOnMove => board.IsWhiteToMove ? PlayerBlack : PlayerWhite;

    public int TotalGameCount => MaxGames;
    public int CurrGameNumber => Math.Min(TotalGameCount, botMatchGameIndex + 1);
    public string AllPGNs => pgns.ToString();


    bool IsLegal(Move givenMove)
    {
      var moves = moveGenerator.GenerateMoves(board);
      foreach (var legalMove in moves)
      {
        if (givenMove.Value == legalMove.Value)
        {
          return true;
        }
      }

      return false;
    }

    public class BotMatchStats
    {
      public string BotName;
      public int NumWins;
      public int NumLosses;
      public int NumDraws;
      public int NumTimeouts;
      public int NumIllegalMoves;

      public BotMatchStats(string name) => BotName = name;
    }

    public void Release()
    {
      boardUI.Release();
    }
  }
}
