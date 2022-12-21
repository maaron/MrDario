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
}
