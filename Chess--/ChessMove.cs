namespace Chess;

public class ChessMove {
    private readonly sbyte _eFile;
    private readonly sbyte _eRank;
    private readonly sbyte _sFile;
    private readonly sbyte _sRank;

    public ChessMove(sbyte sFile, sbyte sRank, sbyte eFile, sbyte eRank) {
        _sFile = sFile;
        _sRank = sRank;
        _eFile = eFile;
        _eRank = eRank;
        SFile = _sFile;
        SRank = _sRank;
        EFile = _eFile;
        ERank = _eRank;
    }

    public sbyte SFile { get; }
    public sbyte SRank { get; }
    public sbyte EFile { get; }
    public sbyte ERank { get; }

    public override bool Equals(object obj) {
        if (obj.GetType() != typeof(ChessMove)) {
            return false;
        }

        return _sFile == ((ChessMove)obj).SFile && _sRank == ((ChessMove)obj).SRank &&
               _eFile == ((ChessMove)obj).EFile && _eRank == ((ChessMove)obj).ERank;
    }

    public override int GetHashCode() {
        return base.GetHashCode();
    }
}