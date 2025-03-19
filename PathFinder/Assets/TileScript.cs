using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;

[StructLayout(LayoutKind.Sequential)] // 메모리 배치 강제
public struct Pos
{
    public int x;
    public int y;

    public Pos(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public override bool Equals(object obj)
    {
        if (obj is Pos other)
        {
            return this.x == other.x && this.y == other.y;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(x, y);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct TileData
{
    public int isMovable;
    public Pos tilePoint;
}

[Serializable]
public class TileScript : MonoBehaviour
{
    // 데이터용
    [SerializeField, ReadOnly] private bool isMovable;
    [SerializeField, ReadOnly] private Pos tilePoint;
    [SerializeField, ReadOnly] private GameObject tileObj;
    // 타일색상         출발지 도착지 아무색깔 3개 총5개
    // 좌표값 3개       
    // 

    // 타일의 좌표를 설정하는 메소드 추가
    public void SetTilePoint(Pos pos)
    {
        tilePoint = pos;
    }

    public void SetTilePoint(int x, int y)
    {
        tilePoint.x = x;
        tilePoint.y = y;
    }

    // 타일의 좌표를 반환하는 메소드 추가
    public Pos GetTilePoint()
    {
        return tilePoint;
    }

    // isMovable 값을 설정하는 메소드
    public void SetMovable(bool movable)
    {
        isMovable = movable;
    }

    // isMovable 값을 반환하는 메소드
    public bool IsMovable()
    {
        return isMovable;
    }

    public void SetTilePrefab(GameObject tilePrefab)
    {
        if (tileObj != null)
        {
            Destroy(tileObj);
        }

        tileObj = Instantiate(tilePrefab, transform);
    }
}
