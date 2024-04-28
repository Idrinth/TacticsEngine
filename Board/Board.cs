using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace CaptainCoder.TacticsEngine.Board;

public sealed class Board : IEquatable<Board>
{
    public HashSet<Position> Tiles { get; set; } = [];
    public PositionMap<Figure> Figures { get; set; } = new();
    public bool Equals(Board? other)
    {
        return other is not null &&
        Tiles.SetEquals(other.Tiles) &&
        Figures.Equals(other.Figures);
    }
}

public static class BoardExtensions
{
    public static void CreateEmptyTile(this Board board, int x, int y) => board.Tiles.Add(new Position(x, y));
    public static void CreateEmptyTiles(this Board board, IEnumerable<Position> positions)
    {
        foreach (Position p in positions)
        {
            board.CreateEmptyTile(p.X, p.Y);
        }
    }
    public static bool HasTile(this Board board, int x, int y) => board.Tiles.Contains(new Position(x, y));
    public static TileInfo GetTile(this Board board, Position position)
    {
        if (!board.Tiles.Contains(position)) { return TileInfo.None; }
        FigureInfo? info = board.Figures
            .Where(f => new BoundingBox(f.Position, f.Element.Width, f.Element.Height).Positions().Contains(position))
            .Select(f => f.Element)
            .FirstOrDefault();
        Tile tile = new() { Figure = info ?? FigureInfo.None };
        return tile;
    }
    public static TileInfo GetTile(this Board board, int x, int y) => board.GetTile(new Position(x, y));

    public static void AddFigure(this Board board, int x, int y, Figure toAdd)
    {
        Position position = new(x, y);
        BoundingBox bbox = new(position, toAdd.Width, toAdd.Height);
        if (!bbox.Positions().All(board.Tiles.Contains)) { throw new ArgumentOutOfRangeException($"Board does not contain a tile at position {x}, {y}"); }
        board.Figures.Add(position, toAdd);
    }

    public static void RemoveTile(this Board board, int x, int y) => board.RemoveTile(new Position(x, y));
    public static void RemoveTile(this Board board, Position position)
    {
        if (board.Tiles.Remove(position))
        {
            board.RemoveFigure(position);
        }
    }

    public static bool RemoveFigure(this Board board, Position position) => board.Figures.TryRemove(position, out _);
    public static bool MoveFigure(this Board board, int startX, int startY, int endX, int endY) => board.MoveFigure(new Position(startX, startY), new Position(endX, endY));
    public static bool MoveFigure(this Board board, Position start, Position end)
    {
        if (!board.Figures.TryRemove(start, out Positioned<Figure>? toMove)) { return false; };
        BoundingBox endBox = new(end.X, end.Y, toMove.Element.Width, toMove.Element.Height);
        if (endBox.Positions().Any(board.Figures.IsOccupied))
        {
            board.Figures.Add(toMove);
            return false;
        }
        board.AddFigure(end.X, end.Y, toMove.Element);
        return true;
    }
    private static JsonSerializerOptions Options { get; } = new()
    {
        Converters = { FigureMapConverter.Shared }
    };
    public static string ToJson(this Board board) => JsonSerializer.Serialize(board, Options);

    public static bool TryFromJson(string json, [NotNullWhen(true)] out Board? board)
    {
        board = JsonSerializer.Deserialize<Board>(json, Options);
        return board is not null;
    }
}