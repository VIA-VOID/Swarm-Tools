using Sirenix.OdinInspector;
using UnityEngine;

public enum PathFinderEnum
{
    [LabelText("지우기")]
    EraseToNormal,
    [LabelText("시작 지점 설정")]
    SetStartPoint,
    [LabelText("도착 지점 설정")]
    SetEndPoint,
    [LabelText("이동 불가 설정(물)")]
    SetWater
}

public enum EditStatus
{
    [LabelText("지우기")]
    EraseToNormal,
    [LabelText("타일 변경")]
    ChangeTile,
    [LabelText("오브젝트 설치")]
    SetObject,
    [LabelText("타일 쌓기")]
    StackTile
}

public enum PrefabType
{
    [LabelText("타일 프리팹")]
    TilePrefab,
    [LabelText("오브젝트 프리팹")]
    ObjectPrefab
}

public enum Direction8
{
    Up,         // 위
    UpRight,    // 오른쪽 위
    Right,      // 오른쪽
    DownRight,  // 오른쪽 아래
    Down,       // 아래
    DownLeft,   // 왼쪽 아래
    Left,       // 왼쪽
    UpLeft      // 왼쪽 위
}
