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
    [SerializeField, ReadOnly] private GameObject objectObj;
    [SerializeField, ReadOnly] private List<GameObject> stackObjList;
    [SerializeField, ReadOnly] private List<TileScript> objDisabledTiles;
    
    private bool isStackAble;

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
    
    // isMovable 값을 반환하는 메소드
    public bool GetIsMovable()
    {
        return isMovable;
    }

    // isMovable 값을 설정하는 메소드
    public void SetMovable(bool movable)
    {
        isMovable = movable;
    }

    public void SetTilePrefab(GameObject tilePrefab)
    {
        if (tileObj != null)
        {
            Destroy(tileObj);
        }

        tileObj = Instantiate(tilePrefab, transform);
    }

    public GameObject GetObjectPrefab()
    {
        return objectObj;
    }
    
    public void SetObjectPrefab(GameObject objectPrefab)
    {
        objectObj = objectPrefab;
    }

    public bool CheckObjectPrefab()
    {
        return objectObj != null;
    }

    public bool GetIsStackAble()
    {
        return isStackAble;
    }

    public void SetTileStackAble(bool movable)
    {
        isStackAble = movable;
    }

    public List<GameObject> GetStackList()
    {
        return stackObjList;
    }

    public void SetObjectDisabledTiles(List<TileScript> getList)
    {
        objDisabledTiles = getList;
    }

    public List<TileScript> GetObjectDisabledTiles()
    {
        return objDisabledTiles;
    }
    
    public void RemoveObjectToMoveAble()
    {
        foreach (var tileDatas in objDisabledTiles)
        {
            tileDatas.SetMovable(true);   
        }
    }
}
