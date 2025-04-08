#pragma once

#include "Pos.h"

// �� ����
struct TileData
{
	// �̵����� ����
	int32 canMove = 0;
	// x, y ��ǥ
	Pos pos;
};

class TileMap
{
public:
	TileMap(const TileMap&) = delete;
	TileMap& operator=(const TileMap&) = delete;
	// �̱���
	static TileMap* GetInstance(int32 mapSize = 0);
	// �� ���� �ʱ�ȭ
	void Init(Pos& start, Pos& end, TileData* tiles, int32 tileSize);
	// ���� ��ǥ�� �� �� �ִ� ������ �Ǵ�
	bool CanGo(Pos& pos);
	bool CanGoD(Pos& pos);
	// ����� ����
	const Pos& GetStartPos() const;
	// ������ ����
	const Pos& GetEndPos() const;
	// �� ������ ��������
	const int32 GetMapSize();

private:
	TileMap() = delete;
	TileMap(int32 mapSize);
	~TileMap();

private:
	static TileMap* _tileMapInstance;
	bool** _board;
	int32 _mapSize;
	Pos	_startPos;
	Pos	_endPos;
};
