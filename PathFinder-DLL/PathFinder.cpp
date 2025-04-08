#pragma once

#include "pch.h"
#include "TileMap.h"
#include "AStar.h"

#define DLL_EXPORT extern "C" __declspec(dllexport)

DLL_EXPORT void InitTileMap(int mapSize, Pos start, Pos end, TileData* tiles, int tileSize)
{
	TileMap* tileMap = TileMap::GetInstance(mapSize);
	tileMap->Init(start, end, tiles, tileSize);
}

DLL_EXPORT void RunPathFind(Pos** outPath, int* outPathSize, Pos** outBresenhamPath, int* outBresenhamSize, Pos** outAllPath, int* outAllPathSize)
{
	AStar* aStar = new AStar();
	aStar->Run();

	std::vector<Pos> path = aStar->GetPath();
	std::vector<Pos> bresenhamPath = aStar->GetBresenHamPath();
	std::vector<Pos> allPath = aStar->GetAllPath();

	*outPathSize = static_cast<int>(path.size());
	*outBresenhamSize = static_cast<int>(bresenhamPath.size());
	*outAllPathSize = static_cast<int>(allPath.size());

	Pos* pathArray = new Pos[*outPathSize];
	Pos* pathBresenhamArray = new Pos[*outBresenhamSize];
	Pos* allPathArray = new Pos[*outAllPathSize];

	if (*outPathSize > 0)
	{
		std::copy(path.begin(), path.end(), pathArray);
	}

	if (*outBresenhamSize > 0)
	{
		std::copy(bresenhamPath.begin(), bresenhamPath.end(), pathBresenhamArray);
	}

	if (*outAllPathSize > 0)
	{
		std::copy(allPath.begin(), allPath.end(), allPathArray);
	}

	*outPath = pathArray;
	*outBresenhamPath = pathBresenhamArray;
	*outAllPath = allPathArray;

	delete aStar;
}

DLL_EXPORT void FreePathArray(Pos* arr)
{
	delete[] arr;
}
