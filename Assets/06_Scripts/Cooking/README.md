# 요리 / 레시피 데이터

음식의 이름, 최대 스택, 허기 회복량 등은 기존 `InvenItemData`를 사용한다.
`RecipeData`는 만드는 방법, `CookingData`는 전체 레시피 목록과 해금 여부 조회를 담당한다.

## 엑셀 → JSON → SO

1. 기존 엑셀 레시피 시트를 수정하고 `Assets/02_Data/Json/Receipe/Receipe.json`으로 변환한다.
2. 새 아이템이면 인벤토리 아이템 SO를 먼저 생성한다.
3. Unity 메뉴 **Tools > Cooking > Import Recipes**를 실행한다.
4. `Assets/02_Data/SO/Recipes/{CraftRecipeID}.asset`과
   `Assets/02_Data/SO/Cooking/CookingData.asset`이 갱신된다.

기존 JSON 필드명을 그대로 사용한다. `Material1ID/Count`부터 `Material3ID/Count`까지
읽어서 `Ingredients` 리스트로 변환한다. 빈 재료 칸은 ID와 수량을 모두 0으로 둔다.
같은 재료를 여러 칸에 적으면 수량을 합산한다.
존재하지 않는 아이템 ID, 중복 레시피 ID, 잘못된 수량은 저장 전에 오류로 처리한다.

재임포트는 기존 SO를 갱신하여 GUID와 씬/프리팹 참조를 유지한다.
재료 목록은 교체하므로 중복 누적되지 않는다.
JSON에서 삭제한 레시피는 CookingData 목록에서 빠지지만 기존 SO 파일은 자동 삭제하지 않는다.

## 데이터 사용

요리 레시피(모두 모닥불 42002, 결과 1개). 수치는 제안값이다.

| ID | 재료 | 결과 (허기 회복) | 시간 |
|---|---|---|---|
| 62001 | 사과 1 | 구운 사과 13002 (20) | 5초 |
| 62002 | 마늘 2 | 구운 마늘 13008 (25) | 5초 |
| 62003 | 양파 1 | 구운 양파 13009 (25) | 5초 |
| 62004 | 보리 3 | 보리밥 13010 (45) | 10초 |
| 62005 | 양파 2, 마늘 1 | 양파 수프 13011 (50) | 12초 |
| 62006 | 매실 3, 설탕 1 | 매실청 13012 (35) | 10초 |
| 62007 | 오디 3, 설탕 1 | 오디잼 13013 (35) | 10초 |
| 62008 | 사과 2, 벌꿀 1 | 꿀 사과 13014 (40) | 8초 |
| 62009 | 보리 2, 마늘 1, 양파 1 | 마늘 보리죽 13015 (60) | 15초 |

레시피 JSON에는 작업대 제작식(61xxx)도 함께 들어 있다. 가져오기는 결과 아이템의 ItemType이
`요리 아이템`인 행만 CookingData에 넣고 나머지는 건너뛴다.

```csharp
// Inspector에서 CookingData.asset을 연결한다.
[SerializeField] private CookingData cookingData;

// 현재는 등록된 모든 레시피가 해금되어 있다.
foreach (RecipeData recipe in cookingData.GetUnlockedRecipes())
{
    // recipe.ResultItem.ItemName / HungerRecovery
    // recipe.Ingredients[i].Item / Count
}

// 특정 시설의 레시피만 조회
var facilityRecipes = cookingData.GetUnlockedRecipes(42002);
```

해금 여부는 `CookingData.IsRecipeUnlocked()`로 조회한다.
현재는 전체 해금 정책이며, 이후 플레이어별 해금 ID/저장 데이터를 도입할 때 이 지점에 연결한다.
플레이어의 해금 상태를 공유 SO에 저장하지 않는다.
해금은 재료 충족 여부 및 AutoCook 숙련 여부와 별개다.

## Canvas - Cook

`Assets/04_Prefabs/UI-Canvas/Canvas - Cook.prefab`을 씬에 배치하면 활성 플레이어를 찾아 연결한다.
외부 상호작용에서는 `CookingPanelUI.Open(playerContext, facilityID)`로 열 수 있다.
시설 ID 0은 전체 레시피를 표시한다.

- Pnl_Center: FoodSlot으로 해금된 레시피 목록을 생성한다.
- 레시피 선택: Pnl_Right에 동일한 음식 이름/아이콘을 표시한다.
- 재료 목록: NeedFoodSlot을 재료 수만큼 표시하며 텍스트는 `보유량/필요량`이다.
  인벤토리 변경 시 즉시 갱신하며 부족한 재료는 빨간색이다.
- Cook: 재료와 결과를 담을 공간이 있으면 활성화된다. 첫 조리는 미니게임 성공이 필요하다.
- AutoCook: 해당 레시피의 성공 횟수가 1 이상일 때 사용할 수 있다.
  한 번 누를 때 한 회 조리하며 CraftTimeSec가 지난 뒤 완성된다.
- 성공하면 재료 차감과 음식 지급을 한 번에 처리한다. 실패·취소·창 닫기에는 재료를 소모하지 않는다.
  조리 도중 인벤토리가 변경되면 완료 시 재료/공간을 다시 확인한다.
- 성공 횟수는 PlayerCooking에서 관리하며 기존 PlayerSaveData의 CookingRecords로 저장/복원한다.
  기존 게임 저장 시 포함되며 음식 한 개마다 별도 저장 파일을 쓰지 않는다.

기본 미니게임은 `CookingTimingMinigame`이다. 초록 구간에서 멈추기를 누르면 성공하며,
시간 초과 또는 X는 실패/취소다. 구간 폭·속도·제한 시간은 Canvas의 컴포넌트에서 조절한다.
다른 미니게임을 쓰려면 `CookingMinigame`을 상속하고 Begin의 콜백에 성공 여부를 전달한 뒤
CookingPanelUI의 Minigame 참조를 교체한다. 같은 결과 콜백은 여러 번 와도 중복 지급하지 않는다.

음식/재료 아이콘은 InvenItemData.Icon을 우선 사용한다.
미지정된 기존 음식은 HUD와 같은 Resources/Inven_Drop 아이콘을 사용한다.
