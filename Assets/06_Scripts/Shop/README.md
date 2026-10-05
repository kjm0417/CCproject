# Squirrel 상점 주인 설정

## 동작

- 평소에는 `Wander Points` 중 도달 가능한 지점을 선택해 Walk로 이동합니다.
- 도착하면 1~3초 Idle 후 다음 지점으로 이동합니다.
- 플레이어가 `Shop Area`에 들어오면 배회를 취소하고 `Shop Stand Point`로 Run 복귀합니다.
- 카운터 도착 후 Idle을 유지하며 `IsReadyToServe`가 true가 됩니다.
- 플레이어가 나가면 2초 기다린 후 다시 배회합니다. 복귀 도중 나가더라도 먼저 카운터에 도착합니다.
- 도달 불가능한 배회 지점은 제외하고, 복귀 경로가 없거나 막히면 Idle 상태로 기다렸다가 재시도합니다.

## 씬 설정

1. Unity가 manifest에 추가된 NavMeshPlus 패키지를 가져오고 컴파일할 때까지 기다립니다. Git과 인터넷 연결이 필요합니다.
2. `Assets/04_Prefabs/Character/Squirrel.prefab`을 씬에 배치하고 루트 `Squirrel`을 선택합니다.
3. `Tools > Shopkeeper > Create Scene Setup`을 실행합니다. 복귀 지점, 상점 Trigger, 배회 지점 4개를 생성하고 해당 다람쥐에 연결합니다. 씬별로 한 번만 실행합니다.
4. `ShopStandPoint`를 카운터의 서 있을 위치로 옮기고 `ShopArea`의 BoxCollider2D 크기와 위치를 손님이 들어오는 범위에 맞춥니다.
5. `WanderPoints` 아래 오브젝트들을 맵 곳곳의 이동 가능한 위치로 옮깁니다. 추가 지점은 다람쥐의 `Wander Points` 배열에 연결합니다. 이 지점들을 다람쥐 자식으로 두지 마세요.
6. `Tools > Shopkeeper > Create 2D Navigation Surface`를 실행합니다. 이미 설정된 2D Surface가 있다면 재사용하고 중복 생성하지 않습니다.
7. 바닥 SpriteRenderer/Tilemap을 선택하고 `Mark Selected As Walkable`을 실행합니다. 건물·울타리·물 등 장애물은 `Mark Selected As Obstacle`로 지정합니다.
8. `ShopkeeperNavigation`의 `Navigation Surface`에서 Include Layers를 맵에 맞추고 Bake합니다. 생성되는 영역이 바닥과 장애물 배치에 맞는지 확인합니다.
9. 다람쥐, 배회 지점, 복귀 지점을 같은 연결된 NavMesh 위에 배치한 뒤 씬을 저장하고 Play합니다.

기본 Surface는 스프라이트/타일맵을 사용하는 Render Meshes 모드입니다. 콜라이더 기반 지형이면 Use Geometry를 Physics Colliders로 바꾸고 바닥과 장애물의 Collider2D 및 Navigation Modifier를 설정한 뒤 다시 Bake합니다. Agent Type은 Surface와 다람쥐에서 같아야 하며 기본값은 Humanoid입니다. 좁은 통로를 사용하려면 Navigation의 Agent Radius와 다람쥐의 Radius를 함께 조절하고 다시 Bake합니다.

`ShopArea`는 PlayerContext가 있는 오브젝트의 Collider2D만 플레이어로 인식합니다. 생성 메뉴가 상점 영역에 Kinematic Rigidbody2D를 추가하므로 플레이어와 Trigger 감지를 할 수 있습니다. Physics 2D Layer Collision Matrix에서 상점 영역과 플레이어 레이어의 충돌이 허용되어 있어야 합니다.

NavMeshAgent는 시작 시 AI가 활성화합니다. XY 맵에서 회전하지 않도록 `updateRotation`과 `updateUpAxis`를 끕니다. 이동에 Dynamic Rigidbody2D를 함께 사용하지 않습니다.

## 조절 가능한 값

- Walk Speed / Run Speed: 배회 속도 / 복귀 속도.
- Idle Duration: 배회 목적지 도착 후 대기 시간 범위.
- Leave Shop Delay: 플레이어 퇴장 후 응대 유지 시간.
- Arrival Distance: 도착 판정 거리.
- Nav Mesh Sample Distance: 목적지 및 시작 위치를 가까운 NavMesh에 보정하는 최대 거리.
- Retry Interval / Stuck Timeout: 경로 재시도 간격 / 막힘 감지 시간.
- Sprite Default Faces Right: 원본 스프라이트가 오른쪽을 바라보는지 지정.

상점 UI 연결은 별도입니다. 다람쥐가 실제로 도착했는지는 `ShopkeeperAI.IsReadyToServe`로 확인할 수 있습니다.

NavMeshPlus 설정 참고: https://github.com/h8man/NavMeshPlus/wiki/HOW-TO
