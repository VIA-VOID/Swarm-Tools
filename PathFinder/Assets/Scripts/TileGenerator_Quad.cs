using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;
using UnityEngine.WSA;

public class TileGenerator_Quad : MonoBehaviour
{
    [LabelText("캐릭터 프리팹")]
    [SerializeField] private GameObject characterPrefab; // 캐릭터 프리팹
    [LabelText("타일 프리팹")]
    [SerializeField] private GameObject tilePrefab;
    [LabelText("기본 타일")]
    [SerializeField] private GameObject baseTilePrefab;
    [LabelText("디폴트 Material")]
    [OnValueChanged("ChangeOriginMaterial")]
    [SerializeField] private Material originMaterial;
    [LabelText("물 타일")]
    [SerializeField] private GameObject hexPrefabWater;
    [LabelText("타일 값 UI 프리팹")]
    [SerializeField] private GameObject tileValueTextObj; // UI 프리팹
    [LabelText("맵 크기")]
    [SerializeField] private int mapSize = 3; // 맵 크기
    [LabelText("메인 UI 캔버스")]
    [SerializeField] private Canvas mainCanvas;
    [LabelText("카메라 거리")]
    [OnValueChanged("ChangeCameraHeight")]
    [SerializeField] private int cameraHeight;
    [LabelText("카메라 각도")]
    //[OnValueChanged("")]
    [SerializeField] private float cameraAngle;
    [LabelText("탐색한 타일 컬러")]
    [SerializeField] private Color lilac;
    [LabelText("최적 경로 컬러")]
    [SerializeField] private Color orange;

    [Title("맵 데이터 관련")]
    [LabelText("맵 데이터 리스트")]
    [SerializeField] private List<TileScript> tileDatas;
    [LabelText("시작 지점 데이터")]
    [SerializeField, ReadOnly] private TileScript startTileData;
    [LabelText("종료 지점 데이터")]
    [SerializeField, ReadOnly] private TileScript endTileData;

    [Title("현재 상태")]
    [EnumToggleButtons, HideLabel]
    public CreateStatus createStatus;

    // 타일 관리 딕셔너리
    private Dictionary<Vector3, GameObject> quadTiles = new Dictionary<Vector3, GameObject>();
    // 타일 좌표 딕셔너리
    private Dictionary<Vector2Int, TileScript> tileMap = new Dictionary<Vector2Int, TileScript>();
    // 사용중인 메인 카메라
    private Camera mainCamera;
    // 타일 생성 지점 (현재는 이 스크립트)
    private Transform tileSpawnPoint;
    // 시작 타일 오브젝트
    private GameObject startTileObj;
    // 종료 타일 오브젝트
    private GameObject endTileObj;
    // 길찾기 데이터 있는지 여부
    private bool isPathDataInit;
    // 움직임 코루틴
    private Coroutine moveCoroutine = null;
    
    private LineRenderer pathLine;

    private List<TileScript> path;
    
    private List<TileScript> bresenhamPath;
    
    private List<TileScript> allPath;

    private void Start()
    {
        tileSpawnPoint = transform;
        mainCamera = Camera.main;
    }

    private void ChangeOriginMaterial()
    {
        originMaterial = baseTilePrefab.GetComponent<MeshRenderer>().material;
    }
    
    // ============================== DLL PATH-FIND START ==============================
    private void LoadPathFindDLL(List<TileScript> tileDatas)
    {
        TileData[] arr = new TileData[tileDatas.Count];

        for (int i = 0; i < tileDatas.Count; i++)
        {
            arr[i].isMovable = tileDatas[i].IsMovable() ? 1 : 0;
            arr[i].tilePoint = tileDatas[i].GetTilePoint();
        }

        // GC 동작 방지
        GCHandle handle = GCHandle.Alloc(arr, GCHandleType.Pinned);
        IntPtr pointer = handle.AddrOfPinnedObject();

        Pos start = startTileData.GetTilePoint();
        Pos end = endTileData.GetTilePoint();

        // C++ DLL 함수 호출
        PathFinder.InitTileMap(mapSize, start, end, pointer, tileDatas.Count);

        // handle 해제
        handle.Free();
    }

    private void RunPathFind()
    {
        IntPtr pathPtr;
        IntPtr bresenhamPathPtr;
        IntPtr allPathPtr;
        int pathSize;
        int bresenhamPathSize;
        int allPathSize;

        PathFinder.RunPathFind(out pathPtr, out pathSize, out bresenhamPathPtr, out bresenhamPathSize, out allPathPtr, out allPathSize);

        ConvertToTileScriptList(out path, pathPtr, pathSize);
        ConvertToTileScriptList(out bresenhamPath, bresenhamPathPtr, bresenhamPathSize);
        ConvertToTileScriptList(out allPath, allPathPtr, allPathSize);

        PathFinder.FreePathArray(pathPtr);
        PathFinder.FreePathArray(bresenhamPathPtr);
        PathFinder.FreePathArray(allPathPtr);
    }

    private void ConvertToTileScriptList(out List<TileScript> toPath, IntPtr ptr, int size)
    {
        toPath = new List<TileScript>();

        for (int i = 0; i < size; i++)
        {
            IntPtr posPtr = new IntPtr(ptr.ToInt64() + i * Marshal.SizeOf(typeof(Pos)));
            Pos pos = Marshal.PtrToStructure<Pos>(posPtr);

            TileScript foundTile = tileDatas.Find(tile => tile.GetTilePoint().x == pos.x && tile.GetTilePoint().y == pos.y);
            if (foundTile != null)
            {
                toPath.Add(foundTile);
            }
            else
            {
                // TODO: 에러!!
            }
        }
    }

    // =============================== DLL PATH-FIND END ===============================

    [TitleGroup("제어 버튼")]
    [PropertyOrder(0)]
    [Button("맵 생성")]
    private void GenerateTileMap()
    {
        GenerateQuadTileMap(Mathf.Max(mapSize, 1));
    }
    
    [PropertyOrder(1)]
    [Button ("길찾기 알고리즘 실행")]
    private void FindBestLoad()
    {
        if (startTileData == null || endTileData == null)
        {
            Debug.LogWarning("시작 지점과 도착지점을 모두 지정해야 합니다.");
            return;
        }
        
        if (moveCoroutine != null)
        {
            return;
        }
        
        Debug.Log("길찾기 알고리즘 실행");
        SetTileData();
    }

    private void ViewLoadWithNumPad(bool isBresenham)
    {
        if (moveCoroutine != null)
        {
            return;
        }
        
        moveCoroutine = StartCoroutine(ChangeTilesSequentially(isBresenham));
    }

    // 순차적으로 색 변경 (기본 색 설정)
    private IEnumerator ChangeTilesSequentially(bool isBresenham)
    {
        foreach (var tileData in tileDatas)
        {
            if(tileData == startTileData || tileData == endTileData || !tileData.IsMovable()) continue;
            ChangeTileColor(tileData.GameObject(), Color.white);
        }
        
        List<TileScript> useList = isBresenham ? bresenhamPath : path;
        
        HashSet<TileScript> calTilesSet = new HashSet<TileScript>(useList);

        if(pathLine != null)
            pathLine.gameObject.SetActive(false);
        
        foreach (var tile in allPath)
        {
            // 시작 타일과 종료 타일은 건너뛰기
            if (tile == startTileData || tile == endTileData) continue;

            Color color = lilac;
            
            if (!isBresenham)
            {
                StartCoroutine(AnimateTilePop(tile.gameObject));
                
                if (calTilesSet.Contains(tile))
                {
                    color = orange;
                }
                
                ChangeTileColor(tile.gameObject, color);
                
                yield return new WaitForSeconds(0.05f);
            }
            else
            {
                ChangeTileColor(tile.gameObject, color);
                
                if (calTilesSet.Contains(tile))
                {
                    color = orange;
                    ChangeTileColor(tile.gameObject, color);
                }
            }
            
             // 타일을 하나씩 색 변경
        }

        moveCoroutine = null;

        Color usingColor = isBresenham ? Color.magenta : Color.green;
        
        DrawPathLine(useList, usingColor);
    }

    // 뿅뿅 애니메이션 (타일 크기 변화)
    private IEnumerator AnimateTilePop(GameObject tile)
    {
        Vector3 originalScale = tile.transform.localScale;
        Vector3 enlargedScale = originalScale * 1.3f; // 커지게 만들기

        float duration = 0.15f;
        float time = 0;

        // 커지는 애니메이션
        while (time < duration)
        {
            tile.transform.localScale = Vector3.Lerp(originalScale, enlargedScale, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        tile.transform.localScale = enlargedScale;

        time = 0;

        // 다시 원래 크기로 돌아오는 애니메이션
        while (time < duration)
        {
            tile.transform.localScale = Vector3.Lerp(enlargedScale, originalScale, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        tile.transform.localScale = originalScale;
    }

    void Update()
    {
        if (Mouse.current.leftButton.isPressed)
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(ray, out hit))
            {
                Vector3 pos = hit.collider.transform.position;

                if (quadTiles.ContainsKey(pos) && quadTiles[pos].CompareTag("QuadTile"))
                {
                    GameObject selectedTile = quadTiles[pos];

                    if (createStatus == CreateStatus.EraseToNormal)
                    {
                        TileScript getTileScript = selectedTile.gameObject.GetComponent<TileScript>();

                        getTileScript.SetTilePrefab(baseTilePrefab);

                        getTileScript.SetMovable(true);

                        selectedTile.tag = "QuadTile";

                        if (getTileScript == startTileData)
                        {
                            startTileData = null;
                        }

                        if (getTileScript == endTileData)
                        {
                            endTileData = null;
                        }

                        if (getTileScript != null)
                        {
                            getTileScript.SetMovable(true); // 이동 가능 블록으로 재변경
                        }
                    }

                    if (createStatus == CreateStatus.SetStartPoint)
                    {
                        // 기존 시작점이 존재하면 원래 색상으로 되돌림
                        if (startTileObj != null)
                        {
                            ChangeTileColor(startTileObj, Color.white);
                        }
                        // 새로운 시작점 설정
                        startTileObj = selectedTile;
                        startTileData = startTileObj.GetComponent<TileScript>();
                        ChangeTileColor(selectedTile, Color.blue);
                    }

                    if (createStatus == CreateStatus.SetEndPoint)
                    {
                        // 기존 도착점이 존재하면 원래 색상으로 되돌림
                        if (endTileObj != null)
                        {
                            ChangeTileColor(endTileObj, Color.white);
                        }
                        // 새로운 도착점 설정
                        endTileObj = selectedTile;
                        endTileData = endTileObj.GetComponent<TileScript>();
                        ChangeTileColor(selectedTile, Color.red);
                    }

                    if (createStatus == CreateStatus.SetWater)
                    {
                        TileScript getTileScript = selectedTile.gameObject.GetComponent<TileScript>();

                        getTileScript.SetTilePrefab(hexPrefabWater);

                        getTileScript.SetMovable(false);

                        if (getTileScript != null)
                        {
                            getTileScript.SetMovable(false); // 물타일은 이동 불가
                        }
                    }
                }
            }
        }

        // R 키를 누르면 맵을 초기화
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            GenerateQuadTileMap(mapSize);
        }

        if (Keyboard.current.numpad1Key.wasPressedThisFrame || Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            ViewLoadWithNumPad(false);
        }

        if (Keyboard.current.numpad2Key.wasPressedThisFrame || Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            ViewLoadWithNumPad(true);
        }
    }

    void ChangeCameraHeight()
    {
        Vector3 originPos = mainCamera.transform.position;

        mainCamera.transform.position = new Vector3(originPos.x, cameraHeight, originPos.z);
    }

    void GenerateQuadTileMap(int size)
    {
        mapSize = size;

        tileDatas.Clear();
        quadTiles.Clear();

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }

        foreach (Transform child in tileSpawnPoint)
        {
            Destroy(child.gameObject);
        }

        Dictionary<Vector3, Vector2Int> worldToGridMap = new Dictionary<Vector3, Vector2Int>();

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector3 worldPos = new Vector3(x, 0, size - 1 - y);
                GameObject tile = Instantiate(tilePrefab, tileSpawnPoint.position + worldPos, Quaternion.identity, tileSpawnPoint);

                TileScript getTile = tile.GetComponent<TileScript>();
                getTile.SetTilePrefab(baseTilePrefab);
                getTile.SetMovable(true);
                getTile.SetTilePoint(x, y);
                tile.tag = "QuadTile"; // 태그 설정
                quadTiles.Add(tileSpawnPoint.position + worldPos, tile);
                tileDatas.Add(tile.GetComponent<TileScript>());
                tileMap[new Vector2Int(x, y)] = getTile;
            }
        }

        // 맵이 초기화될 때, 시작점과 도착점 초기화
        startTileObj = null;
        endTileObj = null;

        float cameraPos = (mapSize - 1) / 2f;

        Camera.main.transform.position = new Vector3(cameraPos, mapSize, cameraPos);

        cameraHeight = mapSize;

        if (pathLine != null)
        {
            Destroy(pathLine.gameObject);
        }

        ChangeCameraHeight();
    }


    void SetTileData()
    {
        Debug.Log("타일 데이터가 설정되었습니다. 타일 개수: " + tileDatas.Count);
        // DLL 필요 데이터 로드
        LoadPathFindDLL(tileDatas);
        // 길찾기 로직 실행 후 데이터 받아옴
        RunPathFind();
    }

    void ChangeTileColor(GameObject tile, Color color)
    {
        Transform child = tile.transform.childCount > 0 ? tile.transform.GetChild(0) : null;
        if (child == null) return;

        MeshRenderer tileRenderer = child.GetComponent<MeshRenderer>();
        if (tileRenderer == null) return;

        if (color == Color.white)
        {
            tileRenderer.material = originMaterial;
            tileRenderer.material.color = Color.white; // 기본 흰색 설정
        }
        else
        {
            // 기본 흰색 머티리얼을 생성한 후 색상을 적용
            Material whiteMaterial = new Material(Shader.Find("Standard"));
            whiteMaterial.color = Color.white; // 기본 흰색 설정
            tileRenderer.material = whiteMaterial; // 흰색으로 초기화 후
            tileRenderer.material.color = color; // 원하는 색상 적용
        }
    }

    // 경로선 그리기
    private void DrawPathLine(List<TileScript> calTiles, Color color)
    {
        if (pathLine != null)
        {
            Destroy(pathLine.gameObject);
        }

        GameObject lineObj = new GameObject("pathLine");
        pathLine = lineObj.AddComponent<LineRenderer>();

        pathLine.material = new Material(Shader.Find("Sprites/Default"));
        pathLine.widthMultiplier = 0.1f;
        pathLine.positionCount = 0;
        pathLine.startColor = color;
        pathLine.endColor = color;
        pathLine.useWorldSpace = true;

        pathLine.positionCount = calTiles.Count;

        for (int i = 0; i < calTiles.Count; i++)
        {
            Vector3 pos = calTiles[i].transform.position;
            pos.y = 1f; // y값 1로 고정
            pathLine.SetPosition(i, pos);
        }
    }
}
