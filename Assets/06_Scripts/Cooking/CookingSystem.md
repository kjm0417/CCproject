# 요리 시스템 구현 정리

이 문서는 현재 프로젝트에 구현한 요리 데이터, UI 연결, 조리 흐름과 저장 동작을 정리한다. 기존 데이터 가져오기 안내는 [README.md](README.md)를 참고한다.

## 구성 요소

- `RecipeData`: 요리 ID, 결과 아이템과 수량, 조리 시설 ID, 조리 시간, 재료 목록을 보관하는 레시피 ScriptableObject다. 각 재료는 아이템 데이터와 필요 수량으로 구성된다.
- `CookingData`: 레시피 목록과 해금 조회를 제공한다. 현재는 등록된 모든 레시피가 해금된 상태이며, 시설 ID로 목록을 필터링할 수 있다. 시설 ID `0`은 전체 목록이다.
- `PlayerCooking`: 플레이어의 조리 가능 여부, 진행 중인 조리, 레시피별 성공 횟수와 자동 조리를 관리한다.
- `PlayerInventory`: 재료 보유량을 검사하고, 조리 성공 시 재료를 차감한 뒤 결과물을 추가한다.
- `CookingPanelUI`, `FoodSlotUI`, `NeedFoodSlotUI`: 레시피 선택 화면, 재료 요구량 표시, 버튼과 상태 갱신을 담당한다.
- `CookingMinigame`: 미니게임 교체를 위한 추상 기반 클래스다. 기본 구현인 `CookingTimingMinigame`은 표시된 초록 구간에서 정지 버튼을 누르면 성공한다.
- `CookingItemIcons`: 아이템 데이터에 아이콘이 지정되어 있으면 우선 사용하고, 없으면 기존 리소스 아이콘을 찾아 표시한다.

## Canvas - Cook 연결

`Assets/04_Prefabs/UI-Canvas/Canvas - Cook.prefab`의 `CookingPanelUI`에서 다음 참조를 연결한다.

- `Cooking Data`: 레시피 목록인 `CookingData` 에셋. 플레이어의 `PlayerCooking`에도 같은 데이터가 연결되어야 한다.
- `Recipe Content` / `Recipe Slot Prefab`: 중앙 레시피 목록의 콘텐츠와 `FoodSlot` 프리팹.
- `Ingredient Content` / `Ingredient Slot Prefab`: 오른쪽 재료 목록의 콘텐츠와 `NeedFoodSlot` 프리팹.
- `Selected Name`, `Selected Icon`, `Status Text`: 선택 레시피와 조리 상태 표시.
- `Cook Button`, `Auto Cook Button`, `Close Button`: 각 버튼 컴포넌트.
- `Minigame`: 미니게임 컴포넌트. 기본 프리팹은 `CookingTimingMinigame`을 사용한다.

플레이어 또는 시설에서 패널을 열 때 `CookingPanelUI.Open(playerContext, facilityID)`를 호출한다. 두 번째 인자를 생략하거나 `0`으로 전달하면 전체 레시피를 표시한다. 패널은 선택된 레시피를 오른쪽에 보여 주고, 레시피의 재료마다 `NeedFoodSlot`을 하나씩 만든다. 슬롯에는 현재 보유량과 필요량이 표시되며, 인벤토리가 변하면 목록도 갱신된다.

## 조리 진행 규칙

1. 레시피 슬롯을 선택하면 결과 음식 정보와 재료별 보유량/필요량을 표시한다.
2. 재료와 결과물을 넣을 공간이 있고 조리 중이 아니면 `Cook` 버튼을 사용할 수 있다. 처음 조리할 때는 미니게임을 거친다.
3. 미니게임 성공 후에도 인벤토리를 다시 검사한다. 조리가 완료되면 재료 차감과 결과물 지급을 한 번의 인벤토리 처리로 수행한다.
4. 실패, 취소, 패널 닫기는 재료를 소모하지 않는다. 조리 도중 인벤토리 상태가 달라져 완료할 수 없으면 조리를 지급하지 않는다.
5. 해당 레시피를 한 번 이상 성공하면 `AutoCook`이 활성화된다. 자동 조리는 미니게임 없이 `CraftTimeSec`만큼 기다린 뒤 같은 조리 처리를 수행한다.

조리 요청에는 세션 ID가 있어 이전 미니게임의 늦은 콜백이 새 조리에 영향을 주지 않도록 한다. 자동 조리 중 패널을 닫거나 컴포넌트가 비활성화되면 진행을 취소한다.

## 저장과 해금

레시피별 성공 횟수는 `PlayerCooking`이 관리한다. `PlayerSaveData.CookingRecords`에 요리 ID와 성공 횟수를 저장하고 불러오므로, 저장된 성공 기록에 따라 `AutoCook` 사용 여부가 복원된다. 이 기록은 프로젝트의 기존 플레이어 저장 흐름을 따른다.

현재 `CookingData.IsRecipeUnlocked`는 목록에 등록된 모든 레시피를 해금된 것으로 취급한다. 이후 해금 기능을 추가할 때 이 판정과 플레이어별 해금 데이터 저장 방식을 확장하면 된다.

## 데이터 가져오기와 확장

레시피 JSON을 ScriptableObject로 가져오는 절차와 검증 사항은 [README.md](README.md)에 정리되어 있다. Unity에서 **Tools > Cooking > Import Recipes**를 실행하면 레시피 에셋과 `CookingData` 목록을 갱신한다.

다른 미니게임을 적용하려면 `CookingMinigame`을 상속해 `Begin`과 `Cancel`을 구현하고, Canvas의 `Minigame` 참조를 새 컴포넌트로 바꾼다. 미니게임은 완료 결과를 콜백으로 전달하며, 성공 결과가 한 번만 처리되도록 현재 조리 세션에서 관리한다.
