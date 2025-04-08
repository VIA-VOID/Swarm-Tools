#pragma once

#include "Pos.h"

// 맵 정보
struct TileData
{
	// 이동가능 여부
	int32 canMove = 0;
	// x, y 좌표
	Pos pos;
};

class TileMap
{
public:
	TileMap(const TileMap&) = delete;
	TileMap& operator=(const TileMap&) = delete;
	// 싱글톤
	static TileMap* GetInstance(int32 mapSize = 0);
	// 맵 정보 초기화
	void Init(Pos& start, Pos& end, TileData* tiles, int32 tileSize);
	// 전달 좌표가 갈 수 있는 길인지 판단
	bool CanGo(Pos& pos);
	bool CanGoD(Pos& pos);
	// 출발지 리턴
	const Pos& GetStartPos() const;
	// 도착지 리턴
	const Pos& GetEndPos() const;
	// 맵 사이즈 가져오기
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
