#include "pch.h"
#include "AStar.h"
#include "TileMap.h"

// 가중치
static constexpr uint16 MOVENUM = 8;	// 8방 이동
static constexpr uint16 STRAIGHT = 10;	// 직선
static constexpr uint16 DIAGONAL = 14;	// 대각선

static Pos moveTo[MOVENUM] =
{
	Pos {-1, 0 }, // 위
	Pos { 0,-1 }, // 왼쪽
	Pos { 1, 0 }, // 아래
	Pos { 0, 1 }, // 오른쪽
	Pos {-1, -1}, // 왼쪽 위 대각선
	Pos { 1, -1}, // 왼쪽 아래 대각선
	Pos { 1, 1 }, // 오른쪽 아래 대각선
	Pos {-1, 1 }, // 오른쪽 위 대각선
};

static uint16 cost[MOVENUM] =
{
	STRAIGHT, // 위
	STRAIGHT, // 왼쪽
	STRAIGHT, // 아래
	STRAIGHT, // 오른쪽
	DIAGONAL, // 왼쪽 위 대각선
	DIAGONAL, // 왼쪽 아래 대각선
	DIAGONAL, // 오른쪽 아래 대각선
	DIAGONAL, // 오른쪽 위 대각선
};

AStar::AStar()
{
	_tileMap = TileMap::GetInstance();
}

AStar::~AStar()
{
}

// 휴리스틱 함수(대각선 거리 구하기)
uint32 AStar::DiagonalDistance(const Pos& start, const Pos& end)
{
	return DIAGONAL * max(abs(end.x - start.x), abs(end.y - start.y));
}

// 브레젠험 직선 알고리즘
// 출발지에서 도착지까지 직선을 긋고, 갈 수 있는 길인지 판단
bool AStar::CanBresenhamLine(const Pos& start, const Pos& end)
{
	int32 startX = start.x;
	int32 startY = start.y;
	int32 endX = end.x;
	int32 endY = end.y;

	// 기울기
	const int32 dx = abs(endX - startX);
	const int32 dy = abs(endY - startY);

	// 대각선 방향 결정 : 오른쪽 or 왼쪽, 위쪽 or 아래쪽
	const int32 sx = (startX < endX) ? 1 : -1;
	const int32 sy = (startY < endY) ? 1 : -1;
	// 두 축 사이 오차값
	int32 err = dx - dy;

	// 도착점까지 갈 수 있는 곳인지 확인
	while (startX != endX || startY != endY)
	{
		int32 e2 = err << 1;
		// x 방향 이동
		if (e2 > -dy)
		{
			err -= dy; // x 이동에 따른 오차 보정
			startX += sx; // 오른쪽 혹은 왼쪽으로 한 칸 이동
		}
		// y 방향 이동
		if (e2 < dx)
		{
			err += dx; // y 이동에 따른 오차 보정
			startY += sy; // 위쪽 혹은 아래쪽으로 한 칸 이동
		}

		// 갈 수 있는 곳인지 확인
		Pos pos = { startY, startX };
		if (_tileMap->CanGoD(pos) == false)
		{
			return false;
		}
	}

	return true;
}

// A* 길찾기 경로 보정
void AStar::SetPathCorrection()
{
	int32 pathSize = static_cast<int32>(_path.size());
	if (pathSize == 0)
	{
		return;
	}
	/*
		_path 길찾기 결과에 거꾸로 들어가있음
		endIdx							startIdx
		도착지 -> 5 -> 4 -> 3 -> 2 -> 1->출발지
	*/
	int32 startIdx = pathSize - 1;
	int32 endIdx = 0;

	// 출발지에서 도착지까지 최대한 거리가 먼곳부터 직선 체크
	while (endIdx < startIdx)
	{
		bool foundJump = false;

		for (int32 index = startIdx; index > endIdx; index--)
		{
			Pos startPos = _path[index];
			Pos endPos = _path[endIdx];

			// 직선으로 갈 수 있는 거리라면 PUSH
			if (CanBresenhamLine(startPos, endPos))
			{
				endIdx = index;
				_bresenhamPath.push_back(_path[endIdx]);
				foundJump = true;
				break;
			}
		}
		// 만약 바로 갈 수 있는 노드가 없으면, 한 칸씩 전진
		if (foundJump == false)
		{
			++endIdx;
		}
	}

	if (_bresenhamPath.empty())
	{
		std::reverse(_path.begin(), _path.end());
	}
	else
	{
		std::reverse(_bresenhamPath.begin(), _bresenhamPath.end());
		_bresenhamPath.push_back(_path[0]);
	}
}

// 길찾기 실행
void AStar::Run()
{
	int32 mapSize = _tileMap->GetMapSize();

	std::priority_queue<PQNode, std::vector<PQNode>, std::greater<>> _openListPQ;
	std::vector<std::vector<int32>> _best = std::vector<std::vector<int32>>(mapSize, std::vector<int32>(mapSize, INT_MAX));
	std::vector<std::vector<bool>> _closeList = std::vector<std::vector<bool>>(mapSize, std::vector<bool>(mapSize, false));

	_path.clear();
	_bresenhamPath.clear();
	_allPath.clear();
	_parent.clear();

	// 필요값 초기화
	bool isFind = false;
	const Pos start = _tileMap->GetStartPos();
	const Pos end = _tileMap->GetEndPos();
	int32 g = 0;
	int32 h = DiagonalDistance(start, end);

	_closeList[start.y][start.x] = true;
	_best[start.y][start.x] = g + h;
	_parent[start] = start;
	_openListPQ.push(PQNode{ g + h, g, start });

	// 길찾기 시작
	while (_openListPQ.empty() == false)
	{
		if (isFind) break;
		PQNode node = _openListPQ.top();
		_openListPQ.pop();

		// 만일 최적경로가 있었다면 스킵
		if (_best[node.pos.y][node.pos.x] < node.f)
		{
			continue;
		}

		for (int32 dir = 0; dir < MOVENUM; dir++)
		{
			Pos nextPos = node.pos + moveTo[dir];
			// 목적지에 도달했다면 종료
			if (nextPos == end)
			{
				_parent[nextPos] = node.pos;
				isFind = true;
				break;
			}
			// 갈수없다면 스킵
			if (_tileMap->CanGo(nextPos) == false)
			{
				continue;
			}
			// 이미 방문한곳이면 스킵
			if (_closeList[nextPos.y][nextPos.x] == true)
			{
				continue;
			}
			// UI용 path
			_allPath.push_back(nextPos);
			// 비용 계산
			int32 g = node.g + cost[dir];
			int32 h = DiagonalDistance(nextPos, end);
			int32 f = g + h;
			// 더 빠른 길을 찾았다면 스킵
			if (_best[nextPos.y][nextPos.x] <= f)
			{
				continue;
			}
			// 방문처리
			_closeList[nextPos.y][nextPos.x] = true;
			_best[nextPos.y][nextPos.x] = f;
			_parent[nextPos] = node.pos;
			_openListPQ.push(PQNode{ g + h, g, nextPos });
		}
	}

	Pos pos;
	if (isFind == false)
	{
		// 만약 길이 모두 막혀있을 경우
		int32 allPathSize = static_cast<int32>(_allPath.size());
		if (allPathSize == 0)
		{
			return;
		}
		pos = _allPath[allPathSize - 1];
	}
	else
	{
		// 도착지 까지 길을 모두 찾았다면
		pos = end;
	}

	// 길찾기 완료 후 전체 경로 삽입
	while (true)
	{
		_path.push_back(pos);
		// 시작점까지
		if (pos == _parent[pos])
		{
			break;
		}
		pos = _parent[pos];
	}

	// 최종경로 보정
	SetPathCorrection();
}

// 최단경로 리턴
std::vector<Pos>& AStar::GetPath()
{
	return _path;
}

// 전체경로 리턴
std::vector<Pos>& AStar::GetAllPath()
{
	return _allPath;
}

// 보정경로 리턴
std::vector<Pos>& AStar::GetBresenHamPath()
{
	return _bresenhamPath;
}
