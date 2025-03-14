using Sirenix.OdinInspector;
using UnityEngine;

public enum CreateStatus
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
