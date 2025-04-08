#include "pch.h"
#include "TileMap.h"

TileMap* TileMap::_tileMapInstance = nullptr;

TileMap::TileMap(int32 mapSize)
	: _mapSize(mapSize), _startPos({}), _endPos({})
{
	// �� ������ �Ҵ�
	_board = new bool* [_mapSize];
	for (int i = 0; i < _mapSize; i++)
	{
		// �� �ึ�� �� ũ���� bool �迭 �Ҵ�
		_board[i] = new bool[_mapSize];
		// ��� ���� false�� �ʱ�ȭ
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

// ���� ��ǥ�� �� �� �ִ� ������ �Ǵ�
bool TileMap::CanGo(Pos& pos)
{
	if (pos.x < 0 || pos.y < 0 || pos.x >= _mapSize || pos.y >= _mapSize)
	{
		return false;
	}
	return _board[pos.y][pos.x];
}

// ���� ��ǥ�� �� �� �ִ� ������ �Ǵ�
// �� ���� Direct ����
bool TileMap::CanGoD(Pos& pos)
{
	return _board[pos.y][pos.x];
}

// �� ���� �ʱ�ȭ
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

// �� ������ ��������
const int32 TileMap::GetMapSize()
{
	return _mapSize;
}
