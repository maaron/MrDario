using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEditorInternal;
using UnityEngine;

public class Game : MonoBehaviour
{
    Board<GamePiece?> board = new(8, 16);

    List<Virus> viruses = new();
    List<BrokenPill> brokenPills = new();
    List<Pill> pills = new();

    System.Random r = new System.Random(0);

    [SerializeField] Virus virusPrefab;
    [SerializeField] GameObject virusParent;
    [SerializeField] Pill pillPrefab;
    [SerializeField] float SlowDropInterval = 1.0f;
    [SerializeField] float FastDropInterval = 0.1f;

    CancellationTokenSource cts;

    async void Start()
    {
        Generate();
        cts = new CancellationTokenSource();
        await RunGame(cts.Token);
    }

    private void OnDestroy()
    {
        cts?.Cancel();
    }

    async Task RunGame(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var location = await ThrowPill(ct);
            
            if (!location.HasValue)
            {
                await Lose(ct);
                break;
            }

            await PlacePill(location.Value, ct);

            while (await DestroyMatches(ct))
            {
                await DropBrokenPills(ct);
            }
        }
    }

    public void Generate()
    {
        int count = 0;

        foreach (var v in viruses) GameObject.Destroy(v);

        board.GenerateRowMajor((i, j) =>
        {
            if (j > board.Height / 2) return null;

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
                .NonNull();

            var type = Generator.OneOf(choices.Except(disallowedChoices).ToArray()).Nullable()(r);

            Virus virus = null;
            if (type.HasValue)
            {
                count++;

                virus = GameObject.Instantiate<Virus>(virusPrefab, BoardPosition(i, j), Quaternion.identity, virusParent.transform);
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
        var pill = GameObject.Instantiate<Pill>(pillPrefab, BoardPosition(location.min), Quaternion.identity, transform);
        pill.PillType = Generator.VirusType.PerHalf()(r);

        // TODO: Animate the throwing of the pill
        await Task.Yield();

        // Add the pill to the board
        board[location.min] = new GamePiece(PieceKind.PillFirst, pill.gameObject);
        board[location.min + Vector2Int.right] = new GamePiece(PieceKind.PillSecond, pill.gameObject);

        return location;
    }

    async Task Lose(CancellationToken ct)
    {
        // TODO: Run the happy virus animation until the user presses a button to continue.
        await Task.Yield();
    }

    async Task PlacePill(RectInt rect, CancellationToken ct)
    {
        var pill = board[rect.min].Value.Pill;

        var slowStartTime = Time.realtimeSinceStartup;
        var fastStartTime = float.NegativeInfinity;

        while (!ct.IsCancellationRequested)
        {
            var currentTime = Time.realtimeSinceStartup;

            if (Input.GetKey(KeyCode.DownArrow) && currentTime > fastStartTime + FastDropInterval)
            {
                // Reset timers
                slowStartTime = Time.realtimeSinceStartup;
                fastStartTime = Time.realtimeSinceStartup;

                if (!TryTranslatePill(pill, ref rect, Vector2Int.down))
                    return;
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow))
                TryTranslatePill(pill, ref rect, Vector2Int.left);

            if (Input.GetKeyDown(KeyCode.RightArrow))
                TryTranslatePill(pill, ref rect, Vector2Int.right);

            if (Input.GetKeyDown(KeyCode.A))
                TryRotatePill(pill, ref rect, pill.PillRotation.Ccw());

            if (Input.GetKeyDown(KeyCode.D))
                TryRotatePill(pill, ref rect, pill.PillRotation.Cw());

            if (currentTime > slowStartTime + SlowDropInterval)
            {
                // Reset timers
                slowStartTime = Time.realtimeSinceStartup;
                fastStartTime = Time.realtimeSinceStartup;

                if (!TryTranslatePill(pill, ref rect, Vector2Int.down))
                    return;
            }

            await this.NextUpdate(ct);
        }
    }

    async Task<bool> DestroyMatches(CancellationToken ct)
    {
        // TODO - Look for horizontal and vertical sequences or 4 or more like colored pieces
        // (either viruses, pill halves, or broken pills).  Change the visual of such pieces to
        // indicate destruction, wait for a short time, and then destroy associated viruses and
        // broken pills.  Whole pills that are now broken must be destroyed and replaced with
        // broken pills for the remaining half, if applicable.
        await Task.Yield();
        return false;
    }

    async Task DropBrokenPills(CancellationToken ct)
    {
        // TODO - Allow broken pills unsupported by viruses or the bottom of the bottle to fall
        // until everything is supported.

        await Task.Yield();
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
        var dst = new RectInt(src.min, new Vector2Int(src.height, src.width));

        return TryMovePill(pill, ref src, dst, rotation);
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
                pill.transform.position = BoardPosition(dst.min);
                board[dst.min] = new GamePiece(PieceKind.PillFirst, pill.gameObject);
                board[dst.max - new Vector2Int(1, 1)] = new GamePiece(PieceKind.PillSecond, pill.gameObject);
            }
            else
            {
                pill.transform.position = BoardPosition(dst.max - new Vector2Int(1, 1));
                board[dst.min] = new GamePiece(PieceKind.PillSecond, pill.gameObject);
                board[dst.max - new Vector2Int(1, 1)] = new GamePiece(PieceKind.PillFirst, pill.gameObject);
            }

            pill.PillRotation = rotation;
            src = dst;

            return true;
        }

        return false;
    }
}
