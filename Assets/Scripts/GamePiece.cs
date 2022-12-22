using UnityEngine;

public struct GamePiece
{
    public PieceKind Kind;
    public GameObject GameObject;

    public GamePiece(PieceKind kind, GameObject gameObject)
    {
        Kind = kind;
        GameObject = gameObject;
    }

    public Virus Virus => GameObject.GetComponent<Virus>();
    public BrokenPill BrokenPill => GameObject.GetComponent<BrokenPill>();
    public Pill Pill => GameObject.GetComponent<Pill>();

    public VirusType VirusType
    {
        get
        {
            switch (Kind)
            {
                case PieceKind.Virus: return Virus.VirusType;
                case PieceKind.PillFirst: return Pill.Left.VirusType;
                case PieceKind.PillSecond: return Pill.Right.VirusType;
                case PieceKind.BrokenPill: return BrokenPill.VirusType;
                default: throw new System.Exception($"Invalid kind {Kind}");
            }
        }
    }

    public void Kill()
    {
        switch (Kind)
        {
            case PieceKind.Virus: Virus.IsDead = true; break;
            case PieceKind.PillFirst: Pill.Left.IsDead = true; break;
            case PieceKind.PillSecond: Pill.Right.IsDead = true; break;
            case PieceKind.BrokenPill: BrokenPill.IsDead = true; break;
            default: throw new System.Exception($"Invalid kind {Kind}");
        }
    }
}
