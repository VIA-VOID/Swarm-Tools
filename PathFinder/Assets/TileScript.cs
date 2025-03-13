using System;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class TileScript : MonoBehaviour
{
    // 데이터용
    [SerializeField, ReadOnly] private bool isMovable;
    [SerializeField, ReadOnly] private int[] tilePoint = new[] {0, 0};
    [SerializeField, ReadOnly] private GameObject tileObj;
    [SerializeField, ReadOnly] private Vector3 tilePosition;
    // 타일색상         출발지 도착지 아무색깔 3개 총5개
    // 좌표값 3개       
    // 
    
    // 타일의 좌표를 설정하는 메소드 추가
    public void SetTilePoint(int x, int y)
    {
        tilePoint[0] = x;
        tilePoint[1] = y;

        tilePosition = transform.position;
    }

    // 타일의 좌표를 반환하는 메소드 추가
    public int[] GetTilePoint()
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
