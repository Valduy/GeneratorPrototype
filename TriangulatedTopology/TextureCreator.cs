﻿using GameEngine.Graphics;
using GameEngine.Helpers;
using GameEngine.Mathematics;
using MeshTopology;
using OpenTK.Mathematics;
using System.Drawing;
using TextureUtils;

namespace TriangulatedTopology
{
    public static class TextureCreator
    {
        public static void FillWithColor(byte[] texture, int size, Color color)
        {
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    texture.SetColor(size, x, y, Color.White);
                }
            }
        }

        public static void FillCellsWithColor(byte[] texture, TopologyNode node, int size, Color color)
        {
            var from = node.Face[0].TextureCoords * size;
            var to = node.Face[2].TextureCoords * size;
            var direction = to - from;
            var bounds = new Vector2(MathF.Abs(direction.X), MathF.Abs(direction.Y));
            var axis = new Vector2i(Math.Sign(direction.X), Math.Sign(direction.Y));

            for (int x = 0; x < bounds.X; x++)
            {
                for (int y = 0; y < bounds.Y; y++)
                {
                    var position = from + new Vector2(x * axis.X, y * axis.Y);
                    texture.SetColor(size, (int)position.X, (int)position.Y, color);
                }
            }
        }

        public static void DrawGrid(byte[] texture, TopologyNode node, Vertex initial, int size, int step)
        {
            var initialIndex = node.Face.IndexOf(initial);
            var from = node.Face[initialIndex].TextureCoords * size;
            var to = node.Face.GetCircular(initialIndex + 2).TextureCoords * size;
            var direction = to - from;
            var bounds = new Vector2(MathF.Abs(direction.X), MathF.Abs(direction.Y));
            var axis = new Vector2i(Math.Sign(direction.X), Math.Sign(direction.Y));

            for (int x = 0; x < bounds.X; x += step)
            {
                for (int y = 0; y < bounds.Y; y++)
                {
                    var position = from + new Vector2(x * axis.X, y * axis.Y);
                    texture.SetColor(size, (int)position.X, (int)position.Y, Color.Black);
                }
            }

            for (int x = 0; x < bounds.X; x++)
            {
                for (int y = 0; y < bounds.Y; y += step)
                {
                    var position = from + new Vector2(x * axis.X, y * axis.Y);
                    texture.SetColor(size, (int)position.X, (int)position.Y, Color.Black);
                }
            }
        }

        public static byte[] CreateGridTexture(Topology topology, Dictionary<TopologyNode, Vertex> initials, int size, int step)
        {
            var texture = new byte[size * size * 4];

            foreach (var node in topology)
            {
                if (initials.TryGetValue(node, out var initial))
                {
                    FillCellsWithColor(texture, node, size, Color.White);
                    DrawGrid(texture, node, initial, size, step);
                }
                else
                {
                    FillCellsWithColor(texture, node, size, Color.Red);
                }
            }

            return texture;
        }

        public static byte[] CreateDebugGridTexture(
            Topology topology,
            Dictionary<TopologyNode, Vertex> initials,
            Dictionary<TopologyNode, Cell[,]> grids, 
            int size, 
            int step)
        {
            var texture = new byte[size * size * 4];

            foreach (var node in topology)
            {
                if (grids.TryGetValue(node, out var grid))
                {
                    FillCellsWithColor(texture, node, size, Color.White);

                    var from = node.Face[0].TextureCoords * size;
                    var ilandBottomDirection = node.Face[2].TextureCoords - node.Face[1].TextureCoords;
                    var ilandBottomBound = (ilandBottomDirection * size).Length;
                    ilandBottomDirection.Normalize();

                    var ilandLeftDirection = node.Face[1].TextureCoords - node.Face[0].TextureCoords;
                    var rightBound = step / 8;
                    ilandLeftDirection.Normalize();

                    for (int x = 0; x < rightBound; x++)
                    {
                        for (int y = 0; y < ilandBottomBound; y++)
                        {
                            var position = from + x * ilandLeftDirection + y * ilandBottomDirection;
                            texture.SetColor(size, (int)position.X, (int)position.Y, Color.Red);
                        }
                    }

                    foreach (var cell in grid.Enumerate())
                    {
                        var cellBottomDirection = cell[2] - cell[1];
                        var cellBottomBound = cellBottomDirection.Length;
                        cellBottomDirection.Normalize();

                        var cellRightDirection = cell[0] - cell[1];
                        var cellLeftBound = step / 8;
                        cellRightDirection.Normalize();

                        for (int x = 0; x < cellLeftBound; x++)
                        {
                            for (int y = 0; y < cellBottomBound; y++)
                            {
                                var position = cell[1] + x * cellRightDirection + y * cellBottomDirection;
                                texture.SetColor(size, (int)position.X, (int)position.Y, Color.Green);
                            }
                        }
                    }

                    DrawGrid(texture, node, initials[node], size, step);
                }
                else
                {
                    FillCellsWithColor(texture, node, size, Color.Purple);
                }
            }

            return texture;
        }

        public static byte[] CreateDebugCellTexture(
            Dictionary<TopologyNode, Cell[,]> grids,
            Dictionary<TopologyNode, Vertex> initials,
            int size, 
            int step)
        {
            var texture = new byte[size * size * 4];

            FillWithColor(texture, size, Color.White);

            foreach (var pair in grids)
            {
                var node = pair.Key;
                var grid = pair.Value;

                foreach (var cell in grid.Enumerate())
                {
                    var width = step / 8;

                    var horizontal = cell[0] - cell[1];
                    var horizontalAxis = horizontal.Normalized();

                    var vertical = cell[2] - cell[1];
                    var verticalAxis = vertical.Normalized();

                    var bounds = new Vector2(MathF.Abs(horizontal.SumComponents()), MathF.Abs(vertical.SumComponents()));

                    for (int x = 0; x < bounds.X; x++)
                    {
                        for (int y = 0; y < width; y++)
                        {
                            var position = cell[1] + x * horizontalAxis + y * verticalAxis;
                            texture.SetColor(size, (int)position.X, (int)position.Y, Color.Red);
                        }
                    }

                    for (int x = 0; x < bounds.X; x++)
                    {
                        for (int y = 0; y < width; y++)
                        {
                            var position = cell[1] + x * horizontalAxis + (bounds.Y - y) * verticalAxis;
                            texture.SetColor(size, (int)position.X, (int)position.Y, Color.Green);
                        }
                    }

                    for (int y = 0; y < bounds.Y; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            var position = cell[1] + x * horizontalAxis + y * verticalAxis;
                            texture.SetColor(size, (int)position.X, (int)position.Y, Color.Blue);
                        }
                    }

                    for (int y = 0; y < bounds.Y; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            var position = cell[1] + (bounds.X - x) * horizontalAxis + y * verticalAxis;
                            texture.SetColor(size, (int)position.X, (int)position.Y, Color.Purple);
                        }
                    }
                }

                DrawGrid(texture, node, initials[node], size, step);
            }

            return texture;
        }

        public static byte[] CreateDetailedTexture(
            Dictionary<TopologyNode, Cell[,]> grids, 
            Dictionary<TopologyNode, Vertex> initials,
            int size, 
            int step)
        {
            return CreateTexture(grids, initials, size, step, c => c.Rules[0].Detailed);
        }

        public static byte[] CreateLogicalTexture(
            Dictionary<TopologyNode, Cell[,]> grids,
            Dictionary<TopologyNode, Vertex> initials, 
            int size, 
            int step)
        {
            return CreateTexture(grids, initials, size, step, c => c.Rules[0].Logical);
        }

        private static byte[] CreateTexture(
            Dictionary<TopologyNode, Cell[,]> grids,
            Dictionary<TopologyNode, Vertex> initials, 
            int size, 
            int step, 
            Func<Cell, Color[,]> acessor)
        {
            var texture = new byte[size * size * 4];

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    texture.SetColor(size, x, y, Color.White);
                }
            }

            foreach (var pair in grids)
            {
                var node = pair.Key;
                var grid = pair.Value;

                foreach (var cell in grid)
                {
                    var rule = acessor(cell);
                    var from = cell[1];
                    var to = cell[3];

                    var horizontal = cell[0] - cell[1];
                    var horizontalAxis = horizontal.Normalized();

                    var vertical = cell[2] - cell[1];
                    var verticalAxis = vertical.Normalized();

                    var bounds = new Vector2(MathF.Abs(horizontal.SumComponents()), MathF.Abs(vertical.SumComponents()));

                    if (MathF.Abs(bounds.X - bounds.Y) > 0.1)
                    {
                        var t = 0;
                    }

                    for (int x = 0; x < bounds.X; x++)
                    {
                        int colorX = (int)Mathematics.Map(x, 0, bounds.X, 0, rule.GetLength(0));

                        for (int y = 0; y < bounds.Y; y++)
                        {
                            int colorY = (int)Mathematics.Map(y, 0, bounds.Y, 0, rule.GetLength(1));
                            //var color = rule[rule.GetLength(0) - 1 - colorX, rule.GetLength(1) - 1 - colorY];
                            //var color = rule[rule.GetLength(0) - 1 - colorX, colorY];
                            var color = rule[colorX, colorY];
                            var position = from + horizontalAxis * x + verticalAxis * y;
                            texture.SetColor(size, (int)position.X, (int)position.Y, color);
                        }
                    }
                }

                if (initials.TryGetValue(node, out var initial))
                {
                    DrawGrid(texture, node, initial, size, step);
                }                
            }

            return texture;
        }
    }
}