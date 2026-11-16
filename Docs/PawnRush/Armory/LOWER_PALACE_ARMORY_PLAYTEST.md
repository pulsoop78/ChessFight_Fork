# 아랫궁 무구실 · 회색 프리팹 플레이 검증

2026-11-16. 새4종 약도를 저장된 회색 블록아웃과 오프라인 테스트 씬으로 구현했다. **Unity Editor는 이 클라우드에 없어 실제 입력·물리 완주·4~5분 통과 시간은 아직 확인하지 못했다.**

## 바로 실행

- 씬: `Assets/Scenes/LowerPalaceArmoryTrial.unity`
- 프리팹: `Assets/Prefabs/Obstacles/LowerPalaceArmoryTrial.prefab`
- 메뉴: **ChessFight → Prototype → Armory → Open Grayscale Trial**
- 씬을 열고 Play. 기존 `RagdollPawn`과 기존 `RagdollTuning`을 사용한다. 구조물은 저장된 프리팹이며 실행 중 새로 생성하지 않는다. 테스트 캐릭터·구간 이름 표시와 필요 시 카메라만 실행 시 만든다.

프리팹을 별도 씬에 놓아 시험할 수도 있다. `ArmoryCourseTrial`은 오프라인 테스트 드라이버이므로 다른 게임의 플레이어/카메라 드라이버와 함께 켜지 않는다. 실제 게임 구간으로 통합할 때는 이 드라이버를 끄고 기존 스폰/진행 시스템에 `CourseModule`과 장치를 연결하는 작업이 필요하다. 로비·Steam·멀티플레이는 연결하지 않았다.

| 조작 | 동작 |
|---|---|
| WASD / 마우스 | 이동 / 시점 |
| Space / Shift | 기존 점프 / 질주 |
| 마우스 왼쪽 / 오른쪽 | 밀치기 / 기존 물리 잡기 |
| 1~4 | 선택 캐릭터를 각 구간 바로 앞 체크포인트로 이동 |
| R | 선택 캐릭터의 현재 체크포인트 복귀 |
| Tab | 12명 중 조작 캐릭터 전환 |
| F3 | 전체 캐릭터 진행/시간과 장치 초기화, 새 창 배치 |
| Esc | 입력과 장치 시계 일시정지/계속 |

12명을 띄워 잡기·방패·동시 복귀를 시험할 수 있지만, 선택하지 않은 캐릭터는 **자동으로 코스를 풀지 않는다**. 자동 봇이나 네트워크 참가자로 구현한 것이 아니다. 정지 중에도 Rigidbody의 일반 물리는 계속되므로 물리 전체를 멈추는 메뉴가 아니다.

## 저장된 구조

[새4종 약도](LOWER_PALACE_ARMORY_MINIMAP.png)의342m 중심선/12m 폭, 두 고정 경사로, 높이0→3→6m, S/C1~C4/도착 안전 단과4단 하강 출구를 저장했다. 낙하 시 약1.5초 뒤 구간 직전에서 복귀한다. C4는 주동선이 플랫폼 가장자리를 지나므로 실제 접근을 감지하도록 체크포인트 범위를 깊이±3.4m로 잡았다. 높은 벽·진열대·갑옷·엄폐물에는 `NoClimbSurface`를 붙여 외곽 우회를 제한했다.

구조물 재질은 `ArmoryTrialGray`, `ArmoryTrialDarkGray`, `ArmoryTrialLightGray` 세 무채색이다. 전용 씬 배경/환경광도 회색이다. 창 예고·타격 범위·감시 시선은 색상 없이 밝기/점멸로 구별한다. 캐릭터 외형과 기존 팀 표시는 별개이며 공유 재질을 바꾸지 않았다.

## 네 장치와 Inspector

| 계층/컴포넌트 | 구현된 동작 | 주요 조절값 |
|---|---|---|
| `01 Random spear floor` / `ArmorySpearFloor` | 24칸 창, 예고→상승→복귀. 매 판 연결된2칸 폭 피난 경로를 정하고 파동마다 그 밖의 칸을 무작위 변경. 발 위치/높이로 판정하고 한 파동에 캐릭터당1회 피격 | Warning/Raised/Rest Seconds, Spear Height, Knockback/Upward Speed, Stun Seconds |
| `02a/02b Dullahan` / `ArmoryDullahan` | 대검의 좁고 긴 직사각형, 도끼의 넓은 부채꼴. 들어올림→급강하→유지→회수. 실제 무기 길이와 예고/충격 표시를 맞추고 주기당1회 피격 | Windup/Strike/Hold/Recovery/Rest Seconds, Phase Offset, Push/Upward Speed, Stun Seconds, Axe Half Angle/Attack Reach |
| `03 Carry shield crossbow gallery` / `ArmoryCrossbowGallery` | 예고 후 석궁3개 발사. 화살9개 시각 풀과 이동 구간 Raycast. 방패·엄폐물에 막히고 지정된 사격 영역 밖에서는 피해 없음 | Volley/Telegraph Seconds, Arrow Speed, Hit Speed, Stun, Hit Cooldown |
| 방패3개 / `ArmoryCarryShield` | 기존 손 잡기 관절로 운반. 분실/방치/구역 이탈 또는 장기 대여 시 회수. Collider를 끈 뒤 기존 손이 놓기를 기다리고 관절이 없을 때만 복귀 | Recovery Seconds, Abandoned Seconds, Lost Below Spawn, Maximum Loan Seconds |
| `04 Watching armour gallery` / `ArmoryWatcherGallery` | 번갈아 고개를 돌리는 갑옷2개. 실제 눈에서 Raycast해3.5m 엄폐물의 가림 확인. 구역/거리/각도 안에서 자발적 이동 어댑터가 참인 경우만 피격 | Look/Look Away Seconds, Range, View Angle, Hit Speed, Stun, Hit Cooldown |

창 피난 경로는 **판이 바뀔 때** 변경하고 한 판 도중에는 보호한다. 예고가 바뀌는 순간 느린 플레이어의 발밑이 위험해지는 일을 피하는 보수적인 테스트 규칙이다. 새 패턴의 공간/시간 연결을 더 공격적으로 바꾸는 알고리즘은 이번 버전의 범위가 아니다.

`ArmoryPawnIntent`는 실제 이동량과 입력 방향을 함께 보고, 입력 없음·벽에 막힘·수직 흔들림·피격 후1.2초·기절/래그돌/잡힘/공중을 제외한다. 일반 외력과 자발적 이동이 정확히 같은 방향으로 섞이는 모든 상황을 분해하는 물리 해석기는 아니므로, 사람끼리 계속 비비거나 동시에 입력하며 밀리는 경우의 감시 판정은 별도 수동 확인 대상이다.

방패 대여 상한은 기본35초로, 누군가 사격 구역 안에서 방패를 계속 잡고 있어도 보급을 영구 봉쇄하지 못하게 한다. 회수 시 기존 `PawnHand`의 disabled collider 해제 경로를 사용하며 물리 콜백에서 관절을 즉시 파괴하거나 잡힌 물체를 순간이동하지 않는다. 방패는 양면 모두 일반 충돌체로 화살을 막는다. 방향/틈은 실제 면적에 따른다.

## 자동 확인과 실제 확인의 구분

- `python3 Tools/RoyalPalace/build_armory_trial.py --check`: 새 프리팹/씬/무채색 재질이 생성 원본과 일치하는지 확인한다. 다시 생성하려면 `--overwrite`지만 **Unity에서 수동 편집한 새 프리팹의 값이 덮이므로 저장본을 먼저 보존**한다.
- 자산 검사: 모든 참조, 단일 프리팹 인스턴스,4종 바인딩,24창/2듀라한/3방패/9화살,6개CP, 경사로의11.8m 폭/65단계 높이 연결,4단 계단,3개 무채색 재질/조명.
- 실제 C# 컨트롤러를 stand-in으로 실행: 10,000창 파동 안전 경로·예고/피격/쿨다운, 듀라한 각도/도끼 거리/타격, 화살 경계/가림/방패 회수, 감시 시선/엄폐/초기화, 캐릭터 의도 판정31조건.
- Linux Roslyn 외부 런타임 컴파일은 Unity 참조 DLL로 타입/API 연결을 검사한다. 실제 Unity Editor/PhysX 실행 검증이 아니다.

Unity에서 확인할 순서:

1. 씬 Play 후1번에서 창 예고 가독성·피격·안전길·R 복귀를 확인한다.
2. 2번에서 대검의 좁은 범위와 도끼 부채꼴, 무기 회수 틈을 확인한다.
3. 3번에서 방패에 접근해 우클릭으로 잡고 석궁을 향해 걸어가며 차단한다. 방패를 버리거나 구역 밖으로 가져가 재보급도 확인한다.
4. 4번에서 시선 안 정지/전진, 진열대 엄폐, 상대에게 밀리거나 잡힌 상태의 감시 제외를 확인한다.
5. Tab으로 캐릭터를 바꾸고 C4 자동 기록·낙하1.5초 복구·F3 초기화·Esc 시계 정지, 출구까지 연속 주행을 확인한다.
6. 실제12인/멀티플레이 성능 및 평균4~5분 통과 시간은 이 오프라인 테스트 결과와 구분해서 측정한다.
