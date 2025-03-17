using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Tilemaps;

class PathFinder
{
    private const string DLL_NAME = "PathFinder";

    [DllImport(DLL_NAME)]
    public static extern void InitTileMap(int mapSize, Pos start, Pos end, IntPtr tiles, int tileSize);

    [DllImport(DLL_NAME)]
    public static extern void RunPathFind(out IntPtr outPath, out int pathSize, out IntPtr outAllPath, out int allPathSize);

    [DllImport(DLL_NAME)]
    public static extern void FreePathArray(IntPtr path);
}
