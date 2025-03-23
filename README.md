# Swarm-Tools

> 프로젝트 개발에 필요한 툴 제작

</br>

## PathFinder
### 🛠 프로젝트 개요
A-Star 알고리즘을 활용한 길찾기 시스템 구현</br>
서버에서 작성한 C++ 기반 알고리즘을 시각적으로 확인하기 위해 Unity 툴 제작</br>
A-Star 경로 탐색 결과를 보정하여 자연스러운 이동 경로 개선</br>
</br>
### A-Star 길찾기 시각화
기본 A-Star 알고리즘 적용
- 파란색 노드: 출발지
- 빨간색 노드: 도착지
- 노란색 노드: 캐릭터 이동 경로
</br>

<b>1. 맵 크기 10 * 10</b> 
</br>

![play](https://github.com/user-attachments/assets/b78057f7-2f2c-42f2-8621-2ec9667c7b7b)

<b>2. 맵 크기 20 * 20</b> 
</br>
![play](https://github.com/user-attachments/assets/e60b5b2a-72e2-470e-8ff6-6650c88d6513)

<b>3. 맵 크기 30 * 30</b> 
</br>
![play](https://github.com/user-attachments/assets/1c394d19-fe57-4716-b976-7a606a1684e7)

### A-Star 길찾기 보정 (자연스러운 경로 개선)
기존 A-Star 경로의 문제점
- F(G+H)의 가중치가 가장 낮은 최적 경로를 찾지만, 실제 이동 시 비효율적인 경로로 보일 수 있음
- 유저 입장에서 "충분히 직선으로 이동할 수 있는 구간" 도 불필요하게 꺾어서 이동하는 문제 발생

개선
- 브레젠험 직선 알고리즘 적용
- 최종 도착지에서 출발지로 거꾸로 이동하면서 "한 번에 갈 수 있는 길" 을 직선으로 탐색
- 장애물이 없는 경우 불필요한 노드 경유를 생략하여 자연스러운 경로로 보정
</br>

### 개선된 길찾기 결과
<b>1. 맵 크기 10 * 10</b> 
</br>
<p align="center">
  <img src="https://github.com/user-attachments/assets/150af48a-49b5-40ea-8724-6fde35590f32" width="45%">
  <img src="https://github.com/user-attachments/assets/8e081d9c-eeec-4b3c-a4ca-deb42024f086" width="45%">
</p>


<b>2. 맵 크기 20 * 20</b> 
</br>
<p align="center">
  <img src="https://github.com/user-attachments/assets/b2f40b5a-39df-4ffd-a233-aa9e28ed5e31" width="45%">
  <img src="https://github.com/user-attachments/assets/10ae1d26-5019-4d3b-adc1-b2cbf1bc4a7c" width="45%">
</p>

<b>3. 맵 크기 30 * 30</b> 
</br>
<p align="center">
  <img src="https://github.com/user-attachments/assets/0e5c3a86-0407-42c0-ae6c-73d9f2b415a2" width="45%">
  <img src="https://github.com/user-attachments/assets/6aed6fbd-822f-486f-9862-f4d0fe8c9d95" width="45%">
</p>
