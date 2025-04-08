#pragma once

#include "Pos.h"

class TileMap;

class AStar
{
public:
	AStar();
	~AStar();

	// 길찾기 실행
	void Run();
	// 최단경로 리턴
	std::vector<Pos>& GetPath();
	// 전체경로 리턴
	std::vector<Pos>& GetAllPath();
	// 보정경로 리턴
	std::vector<Pos>& GetBresenHamPath();

private:
	// 휴리스틱 함수(대각선 거리 구하기)
	static uint32 DiagonalDistance(const Pos& start, const Pos& end);
	// 브레젠험 직선 알고리즘
	// 출발지에서 도착지까지 직선을 긋고, 갈 수 있는 길인지 판단
	bool CanBresenhamLine(const Pos& start, const Pos& end);
	// A* 길찾기 경로 보정
	void SetPathCorrection();

private:
	TileMap* _tileMap;
	std::vector<Pos> _path;					// A* 최단경로
	std::vector<Pos> _bresenhamPath;		// A* 최단경로 + 브레젠험 직선 알고리즘 보정 경로
	std::vector<Pos> _allPath;				// 전체 탐색 경로(클라 UI용)
	std::map<Pos, Pos> _parent;				// 최단 경로 부모 추적
};
