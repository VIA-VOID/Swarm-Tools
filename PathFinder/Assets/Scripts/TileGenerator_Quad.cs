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
    [LabelText("물 타일")]
    [SerializeField] private GameObject hexPrefabWater;
    [LabelText("타일 값 UI 프리팹")]
    [SerializeField] private GameObject tileValueTextObj; // UI 프리팹
    [LabelText("맵 크기")]
    [SerializeField] private int mapSize = 3; // 맵 크기
    [LabelText("메인 UI 캔버스")]
    [SerializeField] private Canvas mainCanvas;
    [LabelText("캐릭터 이동속도")]
    [SerializeField] private float moveSpeed = 2.0f;
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
    // 타일 UI 관리 딕셔너리
    [SerializeField]
    private Dictionary<GameObject, GameObject> tileValueTexts = new Dictionary<GameObject, GameObject>(); // 타일별 UI 관리
    // 타일 좌표 딕셔너리
    private Dictionary<Vector2Int, TileScript> tileMap = new Dictionary<Vector2Int, TileScript>();

    // 생성된 캐릭터
    private GameObject spawnedCharacter;
    // 사용중인 메인 카메라
    private Camera mainCamera;
    // 타일 생성 지점 (현재는 이 스크립트)
    private Transform tileSpawnPoint;
    // 시작 타일 오브젝트
    private GameObject startTileObj;
    // 종료 타일 오브젝트
    private GameObject endTileObj;
    // 움직임 코루틴
    private Coroutine moveCoroutine = null;

    private List<TileScript> path;
    private List<TileScript> allPath;

    private void Start()
    {
        tileSpawnPoint = transform;
        mainCamera = Camera.main;
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
        IntPtr allPathPtr;
        int pathSize;
        int allPathSize;

        PathFinder.RunPathFind(out pathPtr, out pathSize, out allPathPtr, out allPathSize);

        ConvertToTileScriptList(out path, pathPtr, pathSize);
        ConvertToTileScriptList(out allPath, allPathPtr, allPathSize);

        PathFinder.FreePathArray(pathPtr);
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


    #region Public Functions

    [Title("제어 버튼")]
    [Button("맵 생성")]
    public void GenerateTileMap()
    {
        GenerateQuadTileMap(Mathf.Max(mapSize, 1));
    }

    [Button("길찾기 알고리즘 실행")]
    public void FindBestLoad()
    {
        if (startTileObj == null || endTileObj == null)
        {
            Debug.LogWarning("시작 지점과 도착지점을 모두 지정해야 합니다.");
            return;
        }

        SetTileData();

        Debug.Log("길찾기 알고리즘 실행");

        SpawnCharacterAtStart();
    }


    [Button("타일 값 표시")]
    public void ShowTileValues()
    {
        foreach (var tileEntry in quadTiles)
        {
            GameObject tile = tileEntry.Value;

            if (tileValueTexts.ContainsKey(tile))
            {
                // UI가 이미 존재하면 활성화
                tileValueTexts[tile].SetActive(true);
            }
            else
            {
                // UI가 없으면 새로 생성
                GameObject textObj = Instantiate(tileValueTextObj, mainCanvas.transform);
                textObj.SetActive(true);
                tileValueTexts[tile] = textObj;
            }
        }

        UpdateTileTextPositions();
    }

    [Button("타일 값 숨기기")]
    public void HideTileValues()
    {
        foreach (var textObj in tileValueTexts.Values)
        {
            if (textObj != null)
            {
                textObj.SetActive(false);
            }
        }
    }

    #endregion

    void SpawnCharacterAtStart()
    {
        if (spawnedCharacter != null)
        {
            Destroy(spawnedCharacter); // 기존 캐릭터 제거
        }

        if (startTileObj != null)
        {
            // 모든 타일을 기본 색상(lilac)으로 순차적으로 변경
            StartCoroutine(ChangeTilesSequentially(allPath, path));
        }
    }

    // 순차적으로 색 변경 (기본 색 설정)
    private IEnumerator ChangeTilesSequentially(List<TileScript> allTiles, List<TileScript> calTiles)
    {
        HashSet<TileScript> calTilesSet = new HashSet<TileScript>(calTiles);

        foreach (var tile in allTiles)
        {
            // 시작 타일과 종료 타일은 건너뛰기
            if (tile == startTileData || tile == endTileData) continue;

            Color color = lilac;
            StartCoroutine(AnimateTilePop(tile.gameObject));

            if (calTilesSet.Contains(tile))
            {
                color = orange;
            }

            ChangeTileColor(tile.gameObject, color);
            yield return new WaitForSeconds(0.05f); // 타일을 하나씩 색 변경
        }

        Vector3 spawnPosition = startTileObj.transform.position;
        spawnedCharacter = Instantiate(characterPrefab, spawnPosition, Quaternion.identity);

        // 애니메이터 가져오기
        Animator characterAnimator = spawnedCharacter.GetComponent<Animator>();

        if (endTileObj != null)
        {
            Vector3 lookDirection = endTileObj.transform.position - startTileObj.transform.position;
            lookDirection.y = 0; // 수직 방향 회전 방지
            spawnedCharacter.transform.rotation = Quaternion.LookRotation(lookDirection);

            if (characterAnimator != null)
            {
                // 이동 시작
                if (moveCoroutine != null)
                    StopCoroutine(moveCoroutine);

                moveCoroutine = StartCoroutine(MoveCharacterToTarget(characterAnimator));
            }
        }
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

    // 길찾기
    private IEnumerator MoveCharacterToTarget(Animator characterAnimator)
    {
        if (path == null)
        {
            Debug.Log("이동 불가 처리 받음");
            yield break;
        }

        // 애니메이션을 Walking 상태로 변경
        if (characterAnimator != null)
        {
            characterAnimator.SetBool("walking", true);
        }

        // 리스트 수신후 반복처리
        foreach (TileScript tilePos in path)
        {
            // 타일 위치까지 이동
            yield return StartCoroutine(MovePosToTarget(tilePos.transform.position));
        }

        // 이동이 끝나면 애니메이션을 멈춤
        if (characterAnimator != null)
        {
            characterAnimator.SetBool("walking", false);
        }

        RotateCharacterToFront();
    }

    private IEnumerator MovePosToTarget(Vector3 targetPos)
    {
        while (Vector3.Distance(spawnedCharacter.transform.position, targetPos) > 0.1f) // 일정 거리 이하로 도달할 때까지 반복
        {
            RotateCharacter(targetPos);
            // 방향 계산
            Vector3 moveDirection = (targetPos - spawnedCharacter.transform.position).normalized;

            // 이동 처리
            spawnedCharacter.transform.position += moveDirection * moveSpeed * Time.deltaTime;

            yield return null; // 다음 프레임까지 대기
        }
        // 정확한 위치로 스냅
        spawnedCharacter.transform.position = targetPos;
    }

    void RotateCharacter(Vector3 targetPos)
    {
        Vector3 direction = targetPos - spawnedCharacter.transform.position;
        direction.y = 0; // 수직 방향 회전 방지

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            spawnedCharacter.transform.rotation = Quaternion.Slerp(spawnedCharacter.transform.rotation, targetRotation, 0.2f); // 부드러운 회전
        }
    }

    private void RotateCharacterToFront()
    {
        Quaternion frontRotation = Quaternion.Euler(0, 180, 0); // 0, 180, 0 방향 설정
        spawnedCharacter.transform.rotation = frontRotation;
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
    }

    void ChangeCameraHeight()
    {
        Vector3 originPos = mainCamera.transform.position;

        mainCamera.transform.position = new Vector3(originPos.x, cameraHeight, originPos.z);
    }

    void ChangeCameraAngle()
    {

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

        if (spawnedCharacter != null)
        {
            Destroy(spawnedCharacter);
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

        Renderer tileRenderer = child.GetComponent<Renderer>();
        if (tileRenderer == null) return;

        // 기본 흰색 머티리얼을 생성한 후 색상을 적용
        Material whiteMaterial = new Material(Shader.Find("Standard"));
        whiteMaterial.color = Color.white; // 기본 흰색 설정
        tileRenderer.material = whiteMaterial; // 흰색으로 초기화 후
        tileRenderer.material.color = color; // 원하는 색상 적용
    }

    void UpdateTileTextPositions()
    {
        foreach (var tileEntry in tileValueTexts)
        {
            GameObject tile = tileEntry.Key;
            GameObject textObj = tileEntry.Value;

            if (tile == null || textObj == null) continue;

            Vector3 worldPosition = tile.transform.position + Vector3.up * 1.5f; // 타일 위쪽으로 배치
            Vector2 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);

            // UI 요소의 위치를 조정
            textObj.GetComponent<RectTransform>().position = screenPosition;
        }
    }
}
