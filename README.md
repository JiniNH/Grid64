# 🧩 Block Puzzle 2D (Unity Prototype)

![Unity](https://img.shields.io/badge/Unity-100000?style=for-the-badge&logo=unity&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![Status](https://img.shields.io/badge/Status-Prototype_v0.1-brightgreen?style=for-the-badge)

## 📌 프로젝트 소개
유니티(Unity) 엔진을 기반으로 개발 중인 2D 블록 퍼즐 게임입니다. 
8x8 보드판에 무작위로 주어지는 블록들을 전략적으로 배치하고, 가로/세로 라인을 완성하여 점수를 획득하는 클래식한 퍼즐 장르의 재미를 구현했습니다.

## 🚀 개발 완료 시스템 (Core Features) 
**[v1.0.0 코어 로직 완성 - 2026.07.18]**

* **그리드 보드 시스템 (Board System):** 
  * 8x8 보드판 생성 및 2차원 배열 데이터 완벽 매핑
* **상호작용 및 물리 (Drag & Drop / Snap):** 
  * 마우스 입력을 통한 블록 드래그 앤 드롭
  * 그리드 인덱스 계산을 통한 빈칸 마그네틱 스냅(Snap) 구현
* **배치 유효성 검사 로직:** 
  * 보드판 이탈 방지 및 기존 블록 겹침(충돌) 방지 예외 처리
* **라인 클리어 및 스코어링 (Line Clear & Score):** 
  * 가로/세로 빙고 감지 및 블록 파괴 로직 
  * 빙고 발생 시 TextMeshPro UI를 통한 실시간 점수(800점) 갱신
* **덱 매니징 시스템 (Deck System):** 
  * 3개의 랜덤 블록 스폰 및 모두 소진 시 자동 리필 시스템 
* **데드락 탐지 (Game Over / Deadlock):** 
  * 덱에 남은 대기 블록을 보드판 전체(64칸)에 대입하는 시뮬레이션 알고리즘 적용
  * 배치 가능한 공간이 단 1칸도 남지 않았을 때 정확한 게임 오버 판정 처리

## 🛠️ 기술 스택 및 환경 (Tech Stack)
* **Engine:** Unity 6.3 LTS (6000.3.18f1)
* **Scripting:** C# 
* **UI:** TextMeshPro (UGUI)

## 🎨 향후 개발 예정 사항 (Upcoming)
* 전체 시각적 테마 확정 및 고품질 블록(1x1, 2x1, L자) 스프라이트 적용
* 라인 클리어 시 발생하는 파티클 효과(VFX) 및 애니메이션 연출
* 데드락 발생 시 게임 오버 팝업 패널 및 재시작 시스템
* 효과음(SFX) 및 배경음악(BGM) 추가
