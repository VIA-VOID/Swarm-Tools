#include "pch.h"
#include "AStar.h"
#include "TileMap.h"

// ����ġ
static constexpr uint16 MOVENUM = 8;	// 8�� �̵�
static constexpr uint16 STRAIGHT = 10;	// ����
static constexpr uint16 DIAGONAL = 14;	// �밢��

static Pos moveTo[MOVENUM] =
{
	Pos {-1, 0 }, // ��
	Pos { 0,-1 }, // ����
	Pos { 1, 0 }, // �Ʒ�
	Pos { 0, 1 }, // ������
	Pos {-1, -1}, // ���� �� �밢��
	Pos { 1, -1}, // ���� �Ʒ� �밢��
	Pos { 1, 1 }, // ������ �Ʒ� �밢��
	Pos {-1, 1 }, // ������ �� �밢��
};

static uint16 cost[MOVENUM] =
{
	STRAIGHT, // ��
	STRAIGHT, // ����
	STRAIGHT, // �Ʒ�
	STRAIGHT, // ������
	DIAGONAL, // ���� �� �밢��
	DIAGONAL, // ���� �Ʒ� �밢��
	DIAGONAL, // ������ �Ʒ� �밢��
	DIAGONAL, // ������ �� �밢��
};

AStar::AStar()
{
	_tileMap = TileMap::GetInstance();
}

AStar::~AStar()
{
}

// �޸���ƽ �Լ�(�밢�� �Ÿ� ���ϱ�)
uint32 AStar::DiagonalDistance(const Pos& start, const Pos& end)
{
	return DIAGONAL * max(abs(end.x - start.x), abs(end.y - start.y));
}

// �극���� ���� �˰���
// ��������� ���������� ������ �߰�, �� �� �ִ� ������ �Ǵ�
bool AStar::CanBresenhamLine(const Pos& start, const Pos& end)
{
	int32 startX = start.x;
	int32 startY = start.y;
	int32 endX = end.x;
	int32 endY = end.y;

	// ����
	const int32 dx = abs(endX - startX);
	const int32 dy = abs(endY - startY);

	// �밢�� ���� ���� : ������ or ����, ���� or �Ʒ���
	const int32 sx = (startX < endX) ? 1 : -1;
	const int32 sy = (startY < endY) ? 1 : -1;
	// �� �� ���� ������
	int32 err = dx - dy;

	// ���������� �� �� �ִ� ������ Ȯ��
	while (startX != endX || startY != endY)
	{
		int32 e2 = err << 1;
		// x ���� �̵�
		if (e2 > -dy)
		{
			err -= dy; // x �̵��� ���� ���� ����
			startX += sx; // ������ Ȥ�� �������� �� ĭ �̵�
		}
		// y ���� �̵�
		if (e2 < dx)
		{
			err += dx; // y �̵��� ���� ���� ����
			startY += sy; // ���� Ȥ�� �Ʒ������� �� ĭ �̵�
		}

		// �� �� �ִ� ������ Ȯ��
		Pos pos = { startY, startX };
		if (_tileMap->CanGoD(pos) == false)
		{
			return false;
		}
	}

	return true;
}

// A* ��ã�� ��� ����
void AStar::SetPathCorrection()
{
	int32 pathSize = static_cast<int32>(_path.size());
	if (pathSize == 0)
	{
		return;
	}
	/*
		_path ��ã�� ����� �Ųٷ� ������
		endIdx							startIdx
		������ -> 5 -> 4 -> 3 -> 2 -> 1->�����
	*/
	int32 startIdx = pathSize - 1;
	int32 endIdx = 0;

	// ��������� ���������� �ִ��� �Ÿ��� �հ����� ���� üũ
	while (endIdx < startIdx)
	{
		bool foundJump = false;

		for (int32 index = startIdx; index > endIdx; index--)
		{
			Pos startPos = _path[index];
			Pos endPos = _path[endIdx];

			// �������� �� �� �ִ� �Ÿ���� PUSH
			if (CanBresenhamLine(startPos, endPos))
			{
				endIdx = index;
				_bresenhamPath.push_back(_path[endIdx]);
				foundJump = true;
				break;
			}
		}
		// ���� �ٷ� �� �� �ִ� ��尡 ������, �� ĭ�� ����
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

// ��ã�� ����
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

	// �ʿ䰪 �ʱ�ȭ
	bool isFind = false;
	const Pos start = _tileMap->GetStartPos();
	const Pos end = _tileMap->GetEndPos();
	int32 g = 0;
	int32 h = DiagonalDistance(start, end);

	_closeList[start.y][start.x] = true;
	_best[start.y][start.x] = g + h;
	_parent[start] = start;
	_openListPQ.push(PQNode{ g + h, g, start });

	// ��ã�� ����
	while (_openListPQ.empty() == false)
	{
		if (isFind) break;
		PQNode node = _openListPQ.top();
		_openListPQ.pop();

		// ���� ������ΰ� �־��ٸ� ��ŵ
		if (_best[node.pos.y][node.pos.x] < node.f)
		{
			continue;
		}

		for (int32 dir = 0; dir < MOVENUM; dir++)
		{
			Pos nextPos = node.pos + moveTo[dir];
			// �������� �����ߴٸ� ����
			if (nextPos == end)
			{
				_parent[nextPos] = node.pos;
				isFind = true;
				break;
			}
			// �������ٸ� ��ŵ
			if (_tileMap->CanGo(nextPos) == false)
			{
				continue;
			}
			// �̹� �湮�Ѱ��̸� ��ŵ
			if (_closeList[nextPos.y][nextPos.x] == true)
			{
				continue;
			}
			// UI�� path
			_allPath.push_back(nextPos);
			// ��� ���
			int32 g = node.g + cost[dir];
			int32 h = DiagonalDistance(nextPos, end);
			int32 f = g + h;
			// �� ���� ���� ã�Ҵٸ� ��ŵ
			if (_best[nextPos.y][nextPos.x] <= f)
			{
				continue;
			}
			// �湮ó��
			_closeList[nextPos.y][nextPos.x] = true;
			_best[nextPos.y][nextPos.x] = f;
			_parent[nextPos] = node.pos;
			_openListPQ.push(PQNode{ g + h, g, nextPos });
		}
	}

	Pos pos;
	if (isFind == false)
	{
		// ���� ���� ��� �������� ���
		int32 allPathSize = static_cast<int32>(_allPath.size());
		if (allPathSize == 0)
		{
			return;
		}
		pos = _allPath[allPathSize - 1];
	}
	else
	{
		// ������ ���� ���� ��� ã�Ҵٸ�
		pos = end;
	}

	// ��ã�� �Ϸ� �� ��ü ��� ����
	while (true)
	{
		_path.push_back(pos);
		// ����������
		if (pos == _parent[pos])
		{
			break;
		}
		pos = _parent[pos];
	}

	// ������� ����
	SetPathCorrection();
}

// �ִܰ�� ����
std::vector<Pos>& AStar::GetPath()
{
	return _path;
}

// ��ü��� ����
std::vector<Pos>& AStar::GetAllPath()
{
	return _allPath;
}

// ������� ����
std::vector<Pos>& AStar::GetBresenHamPath()
{
	return _bresenhamPath;
}
