#include "pch.h"
#include "TileMap.h"

TileMap* TileMap::_tileMapInstance = nullptr;

TileMap::TileMap(int32 mapSize)
	: _mapSize(mapSize), _startPos({}), _endPos({})
{
	// 맵 데이터 할당
	_board = new bool* [_mapSize];
	for (int i = 0; i < _mapSize; i++)
	{
		// 각 행마다 열 크기의 bool 배열 할당
		_board[i] = new bool[_mapSize];
		// 모든 값을 false로 초기화
		for (int j = 0; j < _mapSize; j++)
		{
			_board[i][j] = false;
		}
	}
}

TileMap::~TileMap()
{
	for (int i = 0; i < _mapSize; i++)
	{
		delete[] _board[i];
	}
	delete[] _board;
}

TileMap* TileMap::GetInstance(int32 mapSize)
{
	if (_tileMapInstance == nullptr)
	{
		_tileMapInstance = new TileMap(mapSize);
	}
	return _tileMapInstance;
}

// 전달 좌표가 갈 수 있는 길인지 판단
bool TileMap::CanGo(Pos& pos)
{
	if (pos.x < 0 || pos.y < 0 || pos.x >= _mapSize || pos.y >= _mapSize)
	{
		return false;
	}
	return _board[pos.y][pos.x];
}

// 전달 좌표가 갈 수 있는 길인지 판단
// 비교 없이 Direct 리턴
bool TileMap::CanGoD(Pos& pos)
{
	return _board[pos.y][pos.x];
}

// 맵 정보 초기화
void TileMap::Init(Pos& start, Pos& end, TileData* tiles, int32 tileSize)
{
	_startPos = start;
	_endPos = end;

	for (int i = 0; i < tileSize; i++)
	{
		Pos pos = tiles[i].pos;
		bool canMove = tiles[i].canMove;
		_board[pos.y][pos.x] = canMove;
	}
}

const Pos& TileMap::GetStartPos() const
{
	return _startPos;
}

const Pos& TileMap::GetEndPos() const
{
	return _endPos;
}

// 맵 사이즈 가져오기
const int32 TileMap::GetMapSize()
{
	return _mapSize;
}
