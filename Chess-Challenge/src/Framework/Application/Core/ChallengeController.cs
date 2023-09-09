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

    int MaxGames = 20;
    int MaxGenerations = 2;

    bool ContinuousPlay = false;
    List<Chromosomes> Gen;
    Chromosomes A;
    Chromosomes B;

    List<Chromosomes> Elite = new();

    public ChallengeController()
    {
      Gen = new() {
        new Chromosomes(dc: 4862, mw: 9, fdm: 7, rl: 2, r: 1, p: 17380, pd: 1, lgp: 50, tm: 4411025),
        new Chromosomes(dc: 3700, mw: 10, fdm: 6, rl: 2, r: 1, p: 17064, pd: 1, lgp: 58, tm: 4137511),
        new Chromosomes(dc: 3626, mw: 14, fdm: 7, rl: 2, r: 2, p: 19165, pd: 1, lgp: 47, tm: 9350988),
        new Chromosomes(dc: 3726, mw: 10, fdm: 9, rl: 2, r: 2, p: 14419, pd: 2, lgp: 66, tm: 6217579),
        new Chromosomes(dc: 2520, mw: 9, fdm: 9, rl: 2, r: 1, p: 10711, pd: 1, lgp: 84, tm: 5937307),
        new Chromosomes(dc: 3661, mw: 9, fdm: 6, rl: 2, r: 1, p: 19644, pd: 1, lgp: 49, tm: 3722018),
        new Chromosomes(dc: 3622, mw: 10, fdm: 6, rl: 2, r: 1, p: 12341, pd: 2, lgp: 71, tm: 7597468),
        new Chromosomes(dc: 2520, mw: 13, fdm: 4, rl: 2, r: 1, p: 10592, pd: 1, lgp: 49, tm: 9161247),
        new Chromosomes(dc: 3790, mw: 11, fdm: 9, rl: 2, r: 1, p: 12154, pd: 2, lgp: 67, tm: 4417077),
        new Chromosomes(dc: 4615, mw: 7, fdm: 4, rl: 2, r: 1, p: 12715, pd: 2, lgp: 72, tm: 7794850),
        new Chromosomes(dc: 4970, mw: 9, fdm: 9, rl: 3, r: 1, p: 14335, pd: 0, lgp: 57, tm: 8947337),
        new Chromosomes(dc: 4902, mw: 14, fdm: 7, rl: 2, r: 1, p: 13024, pd: 2, lgp: 85, tm: 3308359),
        new Chromosomes(dc: 3626, mw: 7, fdm: 9, rl: 2, r: 1, p: 10763, pd: 2, lgp: 70, tm: 5983224),
        new Chromosomes(dc: 4019, mw: 3, fdm: 9, rl: 3, r: 2, p: 15949, pd: 2, lgp: 44, tm: 7032579),
        new Chromosomes(dc: 4054, mw: 9, fdm: 9, rl: 2, r: 1, p: 10695, pd: 2, lgp: 45, tm: 9446886),
        new Chromosomes(dc: 1569, mw: 3, fdm: 9, rl: 2, r: 2, p: 10706, pd: 2, lgp: 59, tm: 3780545),
        new Chromosomes(dc: 4451, mw: 10, fdm: 6, rl: 3, r: 1, p: 6791, pd: 2, lgp: 66, tm: 6642950),
        new Chromosomes(dc: 4771, mw: 9, fdm: 7, rl: 3, r: 1, p: 13372, pd: 2, lgp: 79, tm: 7284764),
        new Chromosomes(dc: 4977, mw: 7, fdm: 2, rl: 3, r: 1, p: 15491, pd: 2, lgp: 82, tm: 6304458),
        new Chromosomes(dc: 1978, mw: 14, fdm: 9, rl: 2, r: 1, p: 19869, pd: 1, lgp: 55, tm: 9015687),
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
      string path = Path.Combine(Directory.GetCurrentDirectory(), "src", "My Bot", "ChessButt2", "ChessButt2.cs");

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

              if (Gen.Count == 0)
              {
                List<Chromosomes> NextGeneration = new();
                Elite = Elite.OrderByDescending(a => a.Fitness).ToList();
                Console.WriteLine("------------------");
                Log(Elite[0].NextGen());
                NextGeneration.Add(Elite[0]);
                Log(Elite[1].NextGen());
                NextGeneration.Add(Elite[1]);

                Elite[2].CrossWith(Elite[3]).ForEach((e) =>
                {
                  Log(e.NextGen());
                  NextGeneration.Add(e);
                });
                Elite[4].CrossWith(Elite[5]).ForEach((e) =>
                {
                  Log(e.NextGen());
                  NextGeneration.Add(e);
                });
                Elite[6].CrossWith(Elite[7]).ForEach((e) =>
                {
                  Log(e.NextGen());
                  NextGeneration.Add(e);
                });
                Elite[8].CrossWith(Elite[9]).ForEach((e) =>
                {
                  Log(e.NextGen());
                  NextGeneration.Add(e);
                });
                Elite[10].CrossWith(Elite[11]).ForEach((e) =>
                {
                  Log(e.NextGen());
                  NextGeneration.Add(e);
                });
                Elite[12].CrossWith(Elite[13]).ForEach((e) =>
                {
                  Log(e.NextGen());
                  NextGeneration.Add(e);
                });
                Elite[14].CrossWith(Elite[15]).ForEach((e) =>
                {
                  Log(e.NextGen());
                  NextGeneration.Add(e);
                });
                Elite[16].CrossWith(Elite[17]).ForEach((e) =>
                {
                  Log(e.NextGen());
                  NextGeneration.Add(e);
                });
                Elite[18].CrossWith(Elite[19]).ForEach((e) =>
                {
                  Log(e.NextGen());
                  NextGeneration.Add(e);
                });

                Log($"DeltaCutoff = {Elite.Min(e => e.DeltaCutoff)}, MobilityWeight = {Elite.Min(e => e.MobilityWeight)}, FullDepthMoves = {Elite.Min(e => e.FullDepthMoves)}, ReductionLimit = {Elite.Min(e => e.ReductionLimit)}, R = {Elite.Min(e => e.R)}, Panic = {Elite.Min(e => e.Panic)}, PanicD = {Elite.Min(e => e.PanicD)}, LateGamePly = {Elite.Min(e => e.LateGamePly)}, TMax = {Elite.Min(e => e.TMax)}", false, ConsoleColor.Cyan);

                Elite.Clear();
                Gen.Clear();
                Gen = NextGeneration.ToList();
                MaxGenerations--;

                if (MaxGenerations == 0)
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
        A = Gen[rand.Next(Gen.Count)];
        Gen.Remove(A);

        B = Gen[rand.Next(Gen.Count)];
        Gen.Remove(B);

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
