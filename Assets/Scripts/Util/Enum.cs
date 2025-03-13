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
