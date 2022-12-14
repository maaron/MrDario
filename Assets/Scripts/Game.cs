using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Game : MonoBehaviour
{
    Board<GamePiece?> board = new(8, 16);

    System.Random r = new ConsistentRandom(0);

    [SerializeField] Virus virusPrefab;
    [SerializeField] GameObject bottleContents;
    [SerializeField] Pill pillPrefab;
    [SerializeField] BrokenPill brokenPillPrefab;
    [SerializeField] float SlowDropInterval = 1.0f;
    [SerializeField] float FastDropInterval = 0.1f;
    [SerializeField] float FallDropInterval = 0.2f;
    [SerializeField] float KeyRepeatDelay = 0.35f;
    [SerializeField] float KeyRepeatInterval = 0.04f;

    [SerializeField] GameObject 
        loseGameObject, 
        winGameObject,
        peerPausedGameObject;

    bool isPaused = false;
    Settings settings = new Settings
    {
        DropSpeedMultiplier = 1.0f,
        VirusDepth = 5
    };

    NetworkPlayer peer = null;

    CancellationTokenSource cts;
    Task gameTask;

    [SerializeField] InputActionAsset inputActions;

    Queue<Drop> pendingDrops = new Queue<Drop>();

    RepeatAction leftAction;
    RepeatAction rightAction;

    void OnValidate()
    {
        foreach (var action in new[] { leftAction, rightAction })
        {
            if (action != null)
            {
                action.RepeatDelay = KeyRepeatDelay;
                action.RepeatInterval = KeyRepeatInterval;
            }
        }
    }

    private void Awake()
    {
        foreach (var map in inputActions.actionMaps)
            map.Enable();

        leftAction = new RepeatAction(inputActions["Left"], KeyRepeatDelay, KeyRepeatInterval);
        rightAction = new RepeatAction(inputActions["Right"], KeyRepeatDelay, KeyRepeatInterval);
    }

    private void Start() { }

    private void OnDestroy()
    {
        cts?.Cancel();
    }

    async Task RunGame(CancellationToken ct)
    {
        Generate();
        
        loseGameObject.SetActive(false);
        winGameObject.SetActive(false);

        while (!ct.IsCancellationRequested)
        {
            if (pendingDrops.Count > 0)
            {
                if (!await DropDrop(ct))
                {
                    await Lose(ct);
                    return;
                }
            }
            else
            {
                var location = await ThrowPill(ct);

                if (!location.HasValue)
                {
                    await Lose(ct);
                    return;
                }

                await PlacePill(location.Value, ct);
            }

            int matchCount = 0;

            while (!ct.IsCancellationRequested)
            {
                var matches = FindMatches();

                if (matches.Count > 0)
                {
                    await KillMatches(matches, ct);

                    if (!AnyVirusesAlive())
                    {
                        await Win(ct);
                        return;
                    }

                    matchCount += matches.Count;

                    await DestroyMatches(matches, ct);

                    await DropUnsupportedPills(ct);

                }
                else
                {
                    break;
                }
            }

            if (matchCount > 1)
                GenerateDrop(matchCount);
        }
    }

    void ResetBoard()
    {
        foreach (Transform child in bottleContents.transform)
            GameObject.Destroy(child.gameObject);

        board.Fill(null);
    }

    void Generate()
    {
        ResetBoard();

        int count = 0;

        board.GenerateRowMajor((i, j) =>
        {
            if (j >= settings.VirusDepth) return null;

            var choices = (VirusType[])Enum.GetValues(typeof(VirusType));

            // Prevent more than two adjacent viruses of the same type, vertically or horizontally.
            var disallowedChoices =
                new[] 
                { 
                    new Vector2Int(-1, 0), 
                    new Vector2Int(0, -1),
                    new Vector2Int(1, 0), 
                    new Vector2Int(0, 1)
                }
                .Select(dir =>
                {
                    var neighbors = Enumerable.Range(1, 2).Select(k =>
                    {
                        var n = new Vector2Int(i, j) + k * dir;
                        if (!board.Region().Contains(n))
                            return null;

                        var p = board[n];
                        if (p.HasValue && p.Value.Kind == PieceKind.Virus)
                            return p.Value.Virus.VirusType;
                        else
                            return (VirusType?)null;
                    })
                    .ToArray();

                    if (neighbors.NonNull().Distinct().Count() == 1)
                        return neighbors[0];
                    else 
                        return (VirusType?)null;
                })
                .NonNull()
                .ToArray();

            var allowed = choices
                .Except(disallowedChoices)
                .ToArray();

            // Make sure there's at least one choice available, otherwise OneOf will throw.
            if (allowed.Length == 0) return null;

            var type = Generator.OneOf(allowed).Nullable()(r);

            Virus virus = null;
            if (type.HasValue)
            {
                count++;

                virus = GameObject.Instantiate<Virus>(virusPrefab, bottleContents.transform);
                virus.transform.localPosition = BoardPosition(i, j);
                virus.VirusType = type.Value;

                return new GamePiece
                {
                    Kind = PieceKind.Virus,
                    GameObject = virus.gameObject
                };
            }
            else
            {
                return null;
            }
        });
    }

    async Task<RectInt?> ThrowPill(CancellationToken ct)
    {
        // Starting location for the new pill
        var location = new RectInt(board.Width / 2 - 1, board.Height - 1, 2, 1);

        // Check for pill jam
        if (board.QueryRect(location).Any(x => x != null))
            return null;

        // Generate a new pill
        var pill = GameObject.Instantiate<Pill>(pillPrefab, bottleContents.transform);
        pill.transform.localPosition = BoardPosition(location.min);
        pill.Left.VirusType = Generator.VirusType(r);
        pill.Right.VirusType = Generator.VirusType(r);

        // TODO: Animate the throwing of the pill
        await Wait(FallDropInterval, ct);

        // Add the pill to the board
        board[location.min] = new GamePiece(PieceKind.PillFirst, pill.gameObject);
        board[location.min + Vector2Int.right] = new GamePiece(PieceKind.PillSecond, pill.gameObject);

        return location;
    }

    async Task Lose(CancellationToken ct)
    {
        loseGameObject.SetActive(true);

        if (peer != null) peer.End(weWon: false);

        await Wait(2, ct);
    }

    async Task Win(CancellationToken ct)
    {
        winGameObject.SetActive(true);

        if (peer != null) peer.End(weWon: true);

        await Wait(2, ct);
    }

    async Task PlacePill(RectInt rect, CancellationToken ct)
    {
        var pill = board[rect.min].Value.Pill;

        var slowStartTime = Time.realtimeSinceStartup;
        var fastStartTime = float.NegativeInfinity;

        var lastFrameCount = Time.frameCount;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var currentTime = Time.realtimeSinceStartup;

            if (inputActions["Down"].IsPressed()
                && currentTime > fastStartTime + FastDropInterval)
            {
                // Reset timers
                slowStartTime = Time.realtimeSinceStartup;
                fastStartTime = Time.realtimeSinceStartup;

                if (!TryTranslatePill(pill, ref rect, Vector2Int.down))
                    return;
            }

            if (leftAction.IsTriggered())
                TryTranslatePill(pill, ref rect, Vector2Int.left);

            if (rightAction.IsTriggered())
                TryTranslatePill(pill, ref rect, Vector2Int.right);

            if (inputActions["RotateLeft"].WasPerformedThisFrame())
                TryRotatePill(pill, ref rect, pill.PillRotation.Ccw());

            if (inputActions["RotateRight"].WasPerformedThisFrame())
                TryRotatePill(pill, ref rect, pill.PillRotation.Cw());

            if (currentTime > slowStartTime + SlowDropInterval / settings.DropSpeedMultiplier)
            {
                // Reset timers
                slowStartTime = Time.realtimeSinceStartup;
                fastStartTime = Time.realtimeSinceStartup;

                if (!TryTranslatePill(pill, ref rect, Vector2Int.down))
                    return;
            }

            await Wait(0, ct);

            var missedFrames = Time.frameCount - lastFrameCount - 1;
            if (missedFrames > 0)
                Debug.Log($"Missed {missedFrames} frames");

            lastFrameCount = Time.frameCount;
        }
    }

    async Task KillMatches(List<RectInt> matches, CancellationToken ct)
    {
        // Show all pieces as destroyed for a little while
        foreach (var match in matches)
        {
            foreach (var pos in match.allPositionsWithin)
            {
                var cell = board[pos];

                cell.Value.Kill();
            }
        }

        await Task.Yield();
    }

    async Task DestroyMatches(List<RectInt> matches, CancellationToken ct)
    {
        await Wait(FallDropInterval, ct);

        // Remove the destroyed pieces, breaking pills as necessary.
        foreach (var match in matches)
        {
            foreach (var pos in match.allPositionsWithin)
            {
                var cell = board[pos];

                if (cell.HasValue)
                {
                    switch (cell.Value.Kind)
                    {
                        case PieceKind.Virus: GameObject.Destroy(cell.Value.GameObject); 
                            break;
                        
                        case PieceKind.PillFirst:
                            {
                                var pill = cell.Value.Pill;
                                var otherLoc = FirstToSecond(pos, pill.PillRotation);
                                if (board[otherLoc].HasValue)
                                {
                                    var brokenPill = GameObject.Instantiate<BrokenPill>(brokenPillPrefab, bottleContents.transform);
                                    
                                    brokenPill.transform.localPosition = BoardPosition(otherLoc);
                                    brokenPill.VirusType = pill.Right.VirusType;

                                    board[otherLoc] = new GamePiece(PieceKind.BrokenPill, brokenPill.gameObject);
                                }
                                GameObject.Destroy(cell.Value.GameObject);
                                break;
                            }

                        case PieceKind.PillSecond:
                            {
                                var pill = cell.Value.Pill;
                                var otherLoc = SecondToFirst(pos, pill.PillRotation);
                                if (board[otherLoc].HasValue)
                                {
                                    var brokenPill = GameObject.Instantiate<BrokenPill>(brokenPillPrefab, bottleContents.transform);
                                    
                                    brokenPill.transform.localPosition = BoardPosition(otherLoc);
                                    brokenPill.VirusType = pill.Left.VirusType;

                                    board[otherLoc] = new GamePiece(PieceKind.BrokenPill, brokenPill.gameObject);
                                }
                                GameObject.Destroy(cell.Value.GameObject);
                                break;
                            }

                        case PieceKind.BrokenPill: GameObject.Destroy(cell.Value.GameObject); 
                            break;
                        
                        default: throw new System.Exception($"Invalid kind {cell.Value.Kind}");
                    }

                    board[pos] = null;
                }
            }
        }
    }

    Vector2Int FirstToSecond(Vector2Int firstHalfLocation, PillRotation rotation)
    {
        switch (rotation)
        {
            case PillRotation.Zero: return firstHalfLocation + Vector2Int.right;
            case PillRotation.Quarter: return firstHalfLocation + Vector2Int.down;
            case PillRotation.Half: return firstHalfLocation + Vector2Int.left;
            case PillRotation.ThreeQuarter: return firstHalfLocation + Vector2Int.up;
            default: throw new Exception($"Invalid rotation {rotation}");
        }
    }

    Vector2Int SecondToFirst(Vector2Int secondHalfLocation, PillRotation rotation)
    {
        switch (rotation)
        {
            case PillRotation.Zero: return secondHalfLocation + Vector2Int.left;
            case PillRotation.Quarter: return secondHalfLocation + Vector2Int.up;
            case PillRotation.Half: return secondHalfLocation + Vector2Int.right;
            case PillRotation.ThreeQuarter: return secondHalfLocation + Vector2Int.down;
            default: throw new Exception($"Invalid rotation {rotation}");
        }
    }

    List<RectInt> FindMatches()
    {
        var vertical = FindVerticalMatches();
        var horizontal = FindHorizontalMatches();
        vertical.AddRange(horizontal);

        return vertical;
    }

    List<RectInt> FindVerticalMatches()
    {
        var matches = new List<RectInt>();

        for (int x = 0; x < board.Width; x++)
        {
            int count = 0;
            VirusType? lastVirusType = null;

            for (int y = 0; y < board.Height; y++)
            {
                var cell = board[x, y];
                var virusType = cell?.VirusType;

                if (cell.HasValue && virusType == lastVirusType)
                {
                    count++;
                }
                else
                {
                    if (count >= 4)
                        matches.Add(new RectInt(x, y - count, 1, count));

                    count = virusType.HasValue ? 1 : 0;

                    lastVirusType = virusType;
                }
            }

            if (count >= 4)
                matches.Add(new RectInt(x, board.Height - count, 1, count));
        }

        return matches;
    }

    List<RectInt> FindHorizontalMatches()
    {
        var matches = new List<RectInt>();

        for (int y = 0; y < board.Height; y++)
        {
            int count = 0;
            VirusType? lastVirusType = null;

            for (int x = 0; x < board.Width; x++)
            {
                var cell = board[x, y];
                var virusType = cell?.VirusType;

                if (cell.HasValue && virusType == lastVirusType)
                {
                    count++;
                }
                else
                {
                    if (count >= 4)
                        matches.Add(new RectInt(x - count, y, count, 1));

                    count = virusType.HasValue ? 1 : 0;

                    lastVirusType = virusType;
                }
            }

            if (count >= 4)
                matches.Add(new RectInt(board.Width - count, y, count, 1));
        }

        return matches;
    }

    async Task DropUnsupportedPills(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            bool anythingMoved = false;

            for (int y = 1; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    var cell = board[x, y];
                    if (cell.HasValue)
                    {
                        if (cell.Value.Kind == PieceKind.BrokenPill)
                        {
                            if (!board[x, y - 1].HasValue)
                            {
                                board[x, y - 1] = board[x, y];
                                board[x, y] = null;
                                cell.Value.GameObject.transform.localPosition = BoardPosition(x, y - 1);
                                anythingMoved = true;
                            }
                        }
                        else if (cell.Value.Kind == PieceKind.PillFirst)
                        {
                            var secondLocation = FirstToSecond(new Vector2Int(x, y), cell.Value.Pill.PillRotation);

                            var rect = new RectInt();
                            rect.xMin = Math.Min(x, secondLocation.x);
                            rect.yMin = Math.Min(y, secondLocation.y);
                            rect.xMax = Math.Max(x, secondLocation.x) + 1;
                            rect.yMax = Math.Max(y, secondLocation.y) + 1;

                            if (TryTranslatePill(cell.Value.Pill, ref rect, Vector2Int.down))
                                anythingMoved = true;
                        }
                    }
                }
            }

            if (!anythingMoved)
                break;

            await Wait(FallDropInterval, ct);
        }
    }

    Vector3 BoardPosition(int row, int col) => BoardPosition(new Vector2Int(row, col));

    Vector3 BoardPosition(Vector2Int l)
    {
        return new Vector3(
                l.x - board.Width / 2 + 0.5f,
                l.y - board.Height / 2 + 0.5f,
                0);
    }

    bool TryTranslatePill(Pill pill, ref RectInt src, Vector2Int step)
    {
        var dst = new RectInt(src.min + step, src.size);

        return TryMovePill(pill, ref src, dst, pill.PillRotation);
    }

    bool TryRotatePill(Pill pill, ref RectInt src, PillRotation rotation)
    {
        var size = new Vector2Int(src.height, src.width);
        var dst = new RectInt(src.min, size);

        if (TryMovePill(pill, ref src, dst, rotation))
            return true;

        if (size.x > size.y)
        {
            // Allow a horizontal pill to shift left or right to avoid obstacles.
            if (TryMovePill(pill, ref src, new RectInt(src.min + Vector2Int.left, size), rotation) ||
                TryMovePill(pill, ref src, new RectInt(src.min + Vector2Int.right, size), rotation))
                return true;
        }
        else
        {
            // Allow a vertical pill to shift up or down to avoid obstacles.
            if (TryMovePill(pill, ref src, new RectInt(src.min + Vector2Int.up, size), rotation) ||
                TryMovePill(pill, ref src, new RectInt(src.min + Vector2Int.down, size), rotation))
                return true;
        }

        return false;
    }

    bool TryMovePill(Pill pill, ref RectInt src, RectInt dst, PillRotation rotation)
    {
        var boardRect = board.Region();

        if (boardRect.Contains(dst.min) && boardRect.Contains(dst.max - new Vector2Int(1, 1)))
        {
            if (board.QueryRect(dst).Any(c => c.HasValue && c.Value.GameObject != pill.gameObject))
                return false;

            board.FillRect(src, null);

            if (rotation == PillRotation.Zero || rotation == PillRotation.ThreeQuarter)
            {
                pill.transform.localPosition = BoardPosition(dst.min);
                board[dst.min] = new GamePiece(PieceKind.PillFirst, pill.gameObject);
                board[dst.max - new Vector2Int(1, 1)] = new GamePiece(PieceKind.PillSecond, pill.gameObject);
            }
            else
            {
                pill.transform.localPosition = BoardPosition(dst.max - new Vector2Int(1, 1));
                board[dst.min] = new GamePiece(PieceKind.PillSecond, pill.gameObject);
                board[dst.max - new Vector2Int(1, 1)] = new GamePiece(PieceKind.PillFirst, pill.gameObject);
            }

            pill.PillRotation = rotation;
            src = dst;

            return true;
        }

        return false;
    }

    bool AnyVirusesAlive()
    {
        return board.ToEnumerable().Any(
            p => p.HasValue && 
            p.Value.Kind == PieceKind.Virus && 
            !p.Value.Virus.IsDead);
    }

    void GenerateDrop(int matchCount)
    {
        int dropCount = Math.Min(4, matchCount);

        // Drop offsets are not based on the pseudo-random seed, but a really random number.
        var dropRand = new System.Random();

        var xOffsets = Generator.Subset(dropCount, Enumerable.Range(0, board.Width).ToArray())(dropRand);

        var drop = new Drop(
            xOffsets: xOffsets,
            colors: Enumerable.Range(0, dropCount).Select(i => Generator.VirusType(dropRand)).ToArray());

        if (peer != null)
            peer.AddDrop(drop);
    }

    public void AddDrop(Drop drop)
    {
        pendingDrops.Enqueue(drop);
    }

    async Task<bool> DropDrop(CancellationToken ct)
    {
        if (pendingDrops.Count < 0) throw new Exception("No drops are pending");

        var drop = pendingDrops.Dequeue();

        for (int i = 0; i < drop.XOffsets.Length; i++)
        {
            var location = new Vector2Int(drop.XOffsets[i], board.Height - 1);
            if (board[location].HasValue)
            {
                return false;
            }
            else
            {
                var brokenPill = GameObject.Instantiate<BrokenPill>(brokenPillPrefab, bottleContents.transform);

                brokenPill.transform.localPosition = BoardPosition(location);
                brokenPill.VirusType = drop.Colors[i];

                board[location] = new GamePiece(PieceKind.BrokenPill, brokenPill.gameObject);
            }
        }

        await DropUnsupportedPills(ct);

        return true;
    }

    async Task Wait(float seconds, CancellationToken ct)
    {
        while (isPaused)
        {
            await this.NextUpdate(ct);
        }

        var duration = seconds - Time.deltaTime;
        if (duration > 0)
            await Task.Delay(TimeSpan.FromSeconds(duration), ct);
        else
            await this.NextUpdate(ct);
    }

    public void Pause()
    {
        isPaused = true;

        if (peer != null)
            peer.Pause();
    }

    public void Resume()
    {
        isPaused = false;

        if (peer != null)
            peer.Resume();
    }

    public void PeerPaused()
    {
        isPaused = true;

        peerPausedGameObject.SetActive(true);
    }

    public void PeerResumed()
    {
        isPaused = false;

        peerPausedGameObject.SetActive(false);
    }

    public async Task Stop(CancellationToken ct)
    {
        if (cts == null)
        {
            Debug.Log("Game already stopped");
            return;
        }

        cts.Cancel();

        try
        {
            Debug.Log("Stopping game...");
            await gameTask;
        }
        catch (OperationCanceledException)
        {
        }

        Debug.Log("Game stopped");
    }

    void Start(Settings newSettings)
    {
        settings = newSettings;
        
        Debug.Log($"Using seed {settings.Seed}");
        r = new ConsistentRandom(settings.Seed);

        cts = new CancellationTokenSource();

        isPaused = false;

        gameTask = RunGame(cts.Token);
    }

    public void StartTwoPlayer(Settings newSettings, NetworkPlayer newPeer)
    {
        peer = newPeer;

        Start(newSettings);
    }

    public void StartOnePlayer(Settings newSettings)
    {
        peer = null;

        Start(newSettings);
    }

    public async void End(bool theyWon)
    {
        await Stop(CancellationToken.None);

        if (theyWon)
            loseGameObject.SetActive(true);
        else
            winGameObject.SetActive(true);
    }
}
