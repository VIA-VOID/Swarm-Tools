using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CameraController : MonoBehaviour
{
    [Title("카메라 컨트롤")]
    [LabelText("카메라 거리")]
    [OnValueChanged("ChangeCameraHeight")]
    public int cameraHeight;
    [LabelText("카메라 각도 슬라이더")]
    [SerializeField] private Slider cameraAngleSlider;
    [LabelText("카메라 이동 속도")]
    [SerializeField] private float cameraMoveSpeed = 5f;
    [LabelText("카메라 줌 속도")]
    [SerializeField] private float zoomSpeed = 10f;
    
    // [LabelText("각도 컨트롤 활성화 패널")]
    // [SerializeField] private GameObject controlPanel;
    // [LabelText("각도 컨트롤 비활성화 패널")]
    // [SerializeField] private GameObject defaultPanel;
    //
    // [LabelText("패널 비활성화 버튼")]
    // [SerializeField] private GameObject panelOffButton;

    private TileCreator tileCreator; 
    private Camera mainCamera;
    private float moveDuration = 0.5f;
    private bool isRightMouseDown = false;

    [LabelText("카메라 각도 속도")]
    [OnValueChanged("ChangeCameraAngleSpeed")]
    [SerializeField]private float cameraAngleSpeed = 0.5f;
    private void ChangeCameraAngleSpeed()
    {
        cameraAngleX = cameraAngleSpeed;
        cameraAngleY = cameraAngleSpeed;
    }
    
    private float cameraAngleX = 0.5f;
    private float cameraAngleY = 0.5f;
    
    private Vector2 previousMousePos;

    private void Start()
    {
        mainCamera = Camera.main;

        // Vector3 originPos = defaultPanel.GetComponent<RectTransform>().position;
        //
        // defaultPanel.GetComponent<RectTransform>().position = new Vector3(0, originPos.y, originPos.z);

        tileCreator = TileCreator.Instance;
    }

    IEnumerator WaitForMapCreateCor()
    {
        yield return new WaitUntil(() => tileCreator.GetInitStatus());
        
        SetDefault(tileCreator.GetMapSize());
    }
    
    private void Update()
    {
        HandleCameraMovement();

        HandleCameraZoom();

        ChangeCameraAngle();
    }

    void SetDefault(int mapSize)
    {
        cameraMoveSpeed = mapSize;
        zoomSpeed = mapSize / 0.01f;
    }
    
    // public void MovePanelToZero(bool isOn)
    // {
    //     RectTransform onPanelRect = controlPanel.GetComponent<RectTransform>();
    //     RectTransform offPanelRect = defaultPanel.GetComponent<RectTransform>();
    //
    //     if (isOn)
    //     {
    //         offPanelRect.DOAnchorPosX(-100, moveDuration)
    //             .SetEase(Ease.OutCubic)
    //             .OnComplete(() =>
    //             {
    //                 defaultPanel.SetActive(!isOn);
    //         
    //                 controlPanel.SetActive(isOn);
    //                 onPanelRect.DOAnchorPosX(0, moveDuration).SetEase(Ease.OutCubic);
    //                 
    //                 panelOffButton.SetActive(true);
    //             });
    //     }
    //     else
    //     {
    //         panelOffButton.SetActive(false);
    //         
    //         onPanelRect.DOAnchorPosX(-100, moveDuration)
    //             .SetEase(Ease.OutCubic)
    //             .OnComplete(() =>
    //             {
    //                 controlPanel.SetActive(isOn);
    //                 
    //                 defaultPanel.SetActive(!isOn);
    //                 offPanelRect.DOAnchorPosX(0, moveDuration).SetEase(Ease.OutCubic);
    //             });
    //     }
    // }
    
    public void ChangeCameraHeight()
    {
        Vector3 originPos = mainCamera.transform.position;

        mainCamera.transform.position = new Vector3(originPos.x, cameraHeight, originPos.z);
    }
    
    public void ChangeCameraAngle()
    {
        if (mainCamera == null) return;

        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        // 오른쪽 마우스 버튼 상태 체크
        if (mouse.rightButton.wasPressedThisFrame)
        {
            isRightMouseDown = true;
            previousMousePos = mouse.position.ReadValue();

            // 현재 카메라 각도를 기준으로 초기화
            cameraAngleX = mainCamera.transform.rotation.eulerAngles.x;
        }
        else if (mouse.rightButton.wasReleasedThisFrame)
        {
            isRightMouseDown = false;
        }

        // 드래그 처리
        if (isRightMouseDown)
        {
            Vector2 currentMousePos = mouse.position.ReadValue();
            Vector2 delta = currentMousePos - previousMousePos;

            // 마우스 이동에 따라 회전 각도 조절
            cameraAngleX -= delta.y * 0.2f; // 상하
            cameraAngleY += delta.x * 0.2f; // 좌우

            // 상하 각도는 보통 -90~90 정도로 제한 (원하면 제한 없이도 가능)
            cameraAngleX = Mathf.Clamp(cameraAngleX, -90f, 90f);

            // 회전 적용
            Quaternion rotation = Quaternion.Euler(cameraAngleX, cameraAngleY, 0f);
            mainCamera.transform.rotation = rotation;

            previousMousePos = currentMousePos;
        }
    }
    
    void HandleCameraMovement()
    {
        if (mainCamera == null) return;

        Vector3 moveDirection = Vector3.zero;

        // 키 입력 감지
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
        {
            moveDirection += Vector3.forward;
        }
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
        {
            moveDirection += Vector3.back;
        }
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            moveDirection += Vector3.left;
        }
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            moveDirection += Vector3.right;
        }

        // 이동 실행
        if (moveDirection != Vector3.zero)
        {
            mainCamera.transform.position += moveDirection.normalized * cameraMoveSpeed * Time.deltaTime;
        }
    }

    void HandleCameraZoom()
    {
        if (mainCamera == null) return;

        // 마우스 휠 값 가져오기 (위로 돌리면 양수, 아래로 돌리면 음수)
        float scrollInput = Mouse.current.scroll.ReadValue().y;

        if (scrollInput != 0)
        {
            // 현재 카메라 위치
            Vector3 cameraPos = mainCamera.transform.position;

            // 목표 Y값 계산 (스크롤 방향에 따라 증가/감소)
            float targetY = cameraPos.y - scrollInput * zoomSpeed * Time.deltaTime;

            //int mapSize = tileCreator.GetMapSize();
            
            // 카메라 Y값 적용
            mainCamera.transform.position = new Vector3(cameraPos.x, targetY, cameraPos.z);
        }
    }

}
