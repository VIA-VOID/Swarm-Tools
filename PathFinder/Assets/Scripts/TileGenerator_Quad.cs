using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
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
    
    [Title("맵 데이터 관련")]
    [LabelText("맵 데이터 리스트")]
    [SerializeField]private List<TileScript> tileDatas;
    [LabelText("시작 지점 데이터")]
    [SerializeField, ReadOnly] private TileScript startTileData;
    [LabelText("종료 지점 데이터")]
    [SerializeField, ReadOnly] private TileScript endTileData;
    
    [Title("현재 상태")]
    [EnumToggleButtons, HideLabel]
    public CreateStatus createStatus;
    
    private float hexWidth = 1.732f;
    private float hexHeight = 1.5f;
    private float spacingFactor = 1.15f;

    private Dictionary<Vector3, GameObject> hexTiles = new Dictionary<Vector3, GameObject>();
    private Dictionary<GameObject, GameObject> tileValueTexts = new Dictionary<GameObject, GameObject>(); // 타일별 UI 관리
    private int currentMapSize;
    private GameObject spawnedCharacter;

    private Camera mainCamera;
    private Transform tileSpawnPoint;
    private GameObject startTileObj;
    private GameObject endTileObj;
    private Coroutine moveCoroutine = null;

    private void Start()
    {
        tileSpawnPoint = transform;
        mainCamera = Camera.main;
    }

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
        
        Debug.Log("길찾기 알고리즘 실행");

        SpawnCharacterAtStart();
    }
    
        
    [Button("타일 값 표시")]
    public void ShowTileValues()
    {
        foreach (var tileEntry in hexTiles)
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
            Vector3 spawnPosition = startTileObj.transform.position + Vector3.up * 0.5f; // 살짝 위로 올려서 스폰
            spawnedCharacter = Instantiate(characterPrefab, spawnPosition, Quaternion.identity);

            // 애니메이터 가져오기
            Animator characterAnimator = spawnedCharacter.GetComponent<Animator>();

            // 도착 지점이 있다면 캐릭터가 도착 지점을 바라보도록 설정
            if (endTileObj != null)
            {
                Vector3 lookDirection = endTileObj.transform.position - startTileObj.transform.position;
                lookDirection.y = 0; // 수직 방향 회전 방지
                spawnedCharacter.transform.rotation = Quaternion.LookRotation(lookDirection);


                if (characterAnimator != null)
                {
                    // 이동 시작
                    if(moveCoroutine != null)
                        StopCoroutine(moveCoroutine);
                    
                    moveCoroutine = StartCoroutine(MoveCharacterToTarget(characterAnimator));
                }
            }
        }
    }

    private IEnumerator MoveCharacterToTarget(Animator characterAnimator)
    {
        bool isMoving = true;

        Vector3 targetPos = endTileObj.transform.position;
        
        // 애니메이션을 Walking 상태로 변경
        if (characterAnimator != null)
        {
            characterAnimator.SetBool("walking", true);
        }
        
        while (Vector3.Distance(spawnedCharacter.transform.position, targetPos) > 0.1f)
        {
            Vector3 moveDirection = (targetPos - spawnedCharacter.transform.position).normalized;
            spawnedCharacter.transform.position += moveDirection * moveSpeed * Time.deltaTime;
            yield return null;
        }

        // 이동이 끝나면 애니메이션을 멈춤
        if (characterAnimator != null)
        {
            characterAnimator.SetBool("walking", false);
        }

        isMoving = false;
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
                
                if (hexTiles.ContainsKey(pos) && hexTiles[pos].CompareTag("QuadTile"))
                {
                    GameObject selectedTile = hexTiles[pos];

                    if (createStatus == CreateStatus.EraseToNormal)
                    {
                        TileScript getTileScript = selectedTile.gameObject.GetComponent<TileScript>();
                        
                        getTileScript.SetTilePrefab(baseTilePrefab);
                        
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

                        selectedTile.tag = "WaterTile";
                        
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
            GenerateQuadTileMap(currentMapSize);
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
        currentMapSize = size;
    
        hexTiles.Clear();
    
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
    
        for (int y = 0; y < size; y++) // 위에서 아래로
        {
            for (int x = 0; x < size; x++) // 왼쪽에서 오른쪽으로
            {
                Vector3 worldPos = new Vector3(y, 0, x); // 좌측 상단이 (0,0,0) 기준으로 조정, Z축 변경
                GameObject tile = Instantiate(tilePrefab, tileSpawnPoint.position + worldPos, Quaternion.identity, tileSpawnPoint);

                TileScript getTile = tile.GetComponent<TileScript>();
                getTile.SetTilePrefab(baseTilePrefab);
                getTile.SetMovable(true);
                tile.tag = "QuadTile"; // 태그 설정
                hexTiles.Add(tileSpawnPoint.position + worldPos, tile);
                tileDatas.Add(tile.GetComponent<TileScript>());
            }
        }
    
        // 맵이 초기화될 때, 시작점과 도착점 초기화
        startTileObj = null;
        endTileObj = null;

        float cameraPos = (currentMapSize - 1) / 2f;

        Camera.main.transform.position = new Vector3(cameraPos, 15, cameraPos);
    }
    
    [Button("타일 데이터화")]
    public void SetTileData()
    {
        tileDatas.Clear(); // 기존 데이터를 초기화

        List<KeyValuePair<Vector3, GameObject>> sortedTiles = hexTiles.ToList();
        sortedTiles.Sort((a, b) => 
        {
            if (a.Key.z != b.Key.z) return b.Key.z.CompareTo(a.Key.z); // 위에서 아래로
            return a.Key.x.CompareTo(b.Key.x); // 왼쪽부터 오른쪽으로
        });

        int row = 0;
        int col = 0;
        int rowSize = currentMapSize + 1;
        int maxRow = currentMapSize * 2;
        
        foreach (var tileEntry in sortedTiles)
        {
            GameObject tile = tileEntry.Value;
            TileScript tileScript = tile.GetComponent<TileScript>();
            
            if (tileScript != null)
            {
                tileDatas.Add(tileScript);
                tileScript.SetTilePoint(row, col);
                col++;
                
                // 다음 줄로 이동할 조건
                if (col >= rowSize)
                {
                    row++;
                    col = 0;
                    
                    if (row <= currentMapSize)
                        rowSize++;
                    else
                        rowSize--;
                }
                
                if (tile.CompareTag("WaterTile"))
                {
                    tileScript.SetMovable(false);
                }
            }
        }
        
        // TODO data to dll
        
        Debug.Log("타일 데이터가 설정되었습니다. 타일 개수: " + tileDatas.Count);
    }
    Vector3 HexToWorldPosition(int q, int r)
    {
        float x = hexWidth * spacingFactor * (q + r * 0.5f);
        float z = hexHeight * spacingFactor * r;
        return new Vector3(x, 0, z);
    }

    void ChangeTileColor(GameObject tile, Color color)
    {
        Transform child = tile.transform.childCount > 0 ? tile.transform.GetChild(0) : null;
        if (child == null) return;
    
        Renderer tileRenderer = child.GetComponent<Renderer>();
        if (tileRenderer == null) return;
    
        Material newMaterial = new Material(tileRenderer.material);
        newMaterial.color = color;
        tileRenderer.material = newMaterial;
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
