#pragma once

#include "Pos.h"

class TileMap;

class AStar
{
public:
	AStar();
	~AStar();

	// ��ã�� ����
	void Run();
	// �ִܰ�� ����
	std::vector<Pos>& GetPath();
	// ��ü��� ����
	std::vector<Pos>& GetAllPath();
	// ������� ����
	std::vector<Pos>& GetBresenHamPath();

private:
	// �޸���ƽ �Լ�(�밢�� �Ÿ� ���ϱ�)
	static uint32 DiagonalDistance(const Pos& start, const Pos& end);
	// �극���� ���� �˰���
	// ��������� ���������� ������ �߰�, �� �� �ִ� ������ �Ǵ�
	bool CanBresenhamLine(const Pos& start, const Pos& end);
	// A* ��ã�� ��� ����
	void SetPathCorrection();

private:
	TileMap* _tileMap;
	std::vector<Pos> _path;					// A* �ִܰ��
	std::vector<Pos> _bresenhamPath;		// A* �ִܰ�� + �극���� ���� �˰��� ���� ���
	std::vector<Pos> _allPath;				// ��ü Ž�� ���(Ŭ�� UI��)
	std::map<Pos, Pos> _parent;				// �ִ� ��� �θ� ����
};
