#pragma once

struct Pos
{
	bool operator==(Pos& other)
	{
		return y == other.y && x == other.x;
	}

	bool operator==(const Pos& other)
	{
		return y == other.y && x == other.x;
	}

	bool operator!=(Pos& other)
	{
		return !(*this == other);
	}

	Pos operator+(Pos& other)
	{
		Pos ret;
		ret.y = y + other.y;
		ret.x = x + other.x;
		return ret;
	}

	Pos& operator+=(Pos& other)
	{
		y += other.y;
		x += other.x;
		return *this;
	}

	bool operator<(const Pos& other) const
	{
		if (y != other.y)
		{
			return y < other.y;
		}
		return x < other.x;
	}

	int32 y = 0;
	int32 x = 0;
};

struct PQNode
{
	bool operator<(const PQNode& other) const { return f < other.f; }
	bool operator>(const PQNode& other) const { return f > other.f; }
	int32	f = 0;	// f = g + h
	int32	g = 0;
	Pos		pos;
};
