using Godot;
using System;

public partial class World : Node2D
{
    private Camera2D _camera2D;

    private TileMap _tileMap;
    
    public override void _Ready()
    {
        _camera2D = GetNode<Camera2D>("Player/Camera2D");
        _tileMap = GetNode<TileMap>("TileMap");

        var used = _tileMap.GetUsedRect().Grow(-1);
        var tileSize = _tileMap.TileSet.TileSize;

        _camera2D.LimitTop = used.Position.Y * tileSize.Y;
        _camera2D.LimitBottom = used.End.Y * tileSize.Y;
        _camera2D.LimitRight = used.End.X * tileSize.X;
        _camera2D.LimitLeft = used.Position.X & tileSize.X;

        _camera2D.ResetSmoothing();
    }
    
}
