# 필드 오브젝트 / 자연 생성물 작업 가이드

## 목적

플레이어가 필드 오브젝트와 상호작용했을 때 데미지가 들어가고, HP가 0 이하가 되면 오브젝트가 파괴되며, 필요하면 아이템이 드랍되는 구조를 만든다.

현재 작업 범위는 자연 생성물 상호작용 구조까지다.

- 바위 채광
- 나무 벌목
- 수풀 제거
- 광석 채광

리스폰은 현재 구조에서 직접 처리하지 않는다. 리스폰은 KJ의 스폰/지역 시스템에서 이어서 연결한다.

## 현재 코드 위치

필드 오브젝트 관련 코드는 아래 폴더에 있다.

```text
Assets/06_Scripts/FieldObjects
```

주요 스크립트:

```text
FieldObjectContracts.cs
FieldObjectBase.cs
DamageableFieldObject.cs
FieldObjectDropper.cs
DropTableData.cs
PickupItem.cs
```

종류별 보조 타입은 하위 폴더에 있다.

```text
Natural/NaturalResourceObject.cs
Dungeon/DungeonGimmickObject.cs
FixedStructureObject/FixedStructureObject.cs
FixedStructureObject/InteractiveFixedStructureObject.cs
PlacedFieldObject/PlacedFieldObject.cs
PlacedFieldObject/InteractivePlacedFieldObject.cs
PlacedFieldObject/GrowablePlacedFieldObject.cs
```

플레이어 상호작용 코드는 아래에 있다.

```text
Assets/06_Scripts/Player/Interaction/PlayerInteraction.cs
```

## 전체 흐름

플레이어가 상호작용 버튼을 누르면 아래 순서로 동작한다.

```text
PlayerInteraction.Interact()
-> 주변 IInteractable 오브젝트 탐색
-> 오브젝트가 원하는 도구 타입 확인
-> 플레이어 애니메이션 재생
-> 오브젝트 Interact 호출
-> DamageableFieldObject.TakeDamage
-> HP 감소
-> HP 0 이하이면 드랍 처리 후 오브젝트 삭제
```

현재는 장비/인벤토리 판정이 확정되지 않았기 때문에, 플레이어가 오브젝트 근처에 있으면 해당 오브젝트가 원하는 도구를 들고 있다고 가정한다.

예시:

```text
바위 Preferred Tool Type = Pickaxe
-> 플레이어가 바위 근처에서 상호작용
-> Pickaxe를 든 것으로 간주
-> Player_Pick 애니메이션 재생
-> 바위 HP 감소
```

## 공통 인터페이스 구조

`FieldObjectContracts.cs`는 enum과 interface만 관리한다.

### IFieldObject

모든 필드 오브젝트의 최소 공통 정보다.

```text
FieldObjData Data
```

HP나 드랍은 모든 오브젝트가 가지는 값이 아니므로 여기에 넣지 않는다.

### IInteractable

플레이어가 상호작용할 수 있는 오브젝트가 구현한다.

```text
CanInteract()
Interact()
```

### IDamageableFieldObject

HP가 있고 데미지를 받을 수 있는 오브젝트가 구현한다.

```text
CurrentHp
IsDepleted
TakeDamage()
```

자연 생성물은 대부분 여기에 해당한다.

### IDropProvider

파괴되거나 수확될 때 드랍 그룹을 사용하는 오브젝트가 구현한다.

```text
DropGroupId
```

### IToolInteractionTarget

오브젝트가 어떤 도구를 기대하는지 알려준다.

```text
PreferredToolType
```

현재는 플레이어 장비 판정이 없으므로, 플레이어가 이 값을 보고 임시로 맞는 도구를 들고 있다고 가정한다.

### IGrowable

성장 단계가 필요한 오브젝트가 구현한다.

```text
GrowthStage
IsHarvestable
```

농작물이나 성장형 배치 오브젝트에서 사용한다.

## 자연 생성물 기본 구조

자연 생성물은 현재 `DamageableFieldObject`를 사용한다.

바위, 나무, 수풀처럼 HP가 있고 도구로 때려서 파괴되는 오브젝트는 이 컴포넌트를 붙인다.

```text
Rock.prefab
- SpriteRenderer
- Collider2D
- DamageableFieldObject
- FieldObjectDropper 선택
```

`Natural/NaturalResourceObject.cs`는 자연 생성물 전용 기능이 생길 때를 위한 확장 지점이다.

현재는 내용이 거의 없는 래퍼 클래스이므로, 자연 생성물 테스트에는 필수로 사용하지 않아도 된다.

이유:

- 현재 자연 생성물 전용 로직이 아직 없다.
- `DamageableFieldObject`만으로 채광/벌목/파괴/드랍 구조를 처리할 수 있다.
- 비어 있는 컴포넌트는 작업자가 헷갈릴 수 있다.

나중에 자연 생성물 전용 로직이 생기면 `NaturalResourceObject`에 연결 지점을 추가하거나, 별도 컴포넌트를 붙여서 확장한다.

예시:

```text
자연 생성물 전용 사운드
자연 생성물 전용 이펙트
자연 생성물 전용 흔들림
자연 생성물 전용 리스폰 연결
자연 생성물 전용 그리드 점유 처리
```

## 던전/기믹 오브젝트 작업 방향

던전/기믹 오브젝트는 아래 클래스를 기준으로 사용한다.

```text
Assets/06_Scripts/FieldObjects/Dungeon/DungeonGimmickObject.cs
```

사용 대상:

```text
문
스위치
레버
퍼즐 장치
기믹 발판
던전 클리어 조건용 오브젝트
```

기본 구조:

```text
DungeonGimmickObject
- FieldObjectBase 상속
- IInteractable 구현
```

던전/기믹 오브젝트는 기본적으로 "플레이어가 사용한다"는 성격이 강하므로 상호작용을 가진다.

단, 자연 생성물처럼 무조건 HP를 가지지는 않는다.

### 파괴되지 않는 기믹

스위치, 문, 레버처럼 데미지를 받지 않는 오브젝트는 `DungeonGimmickObject`를 그대로 사용하거나 상속한다.

예시:

```text
SwitchObject : DungeonGimmickObject
DoorObject : DungeonGimmickObject
```

주로 수정할 위치:

```text
Interact()
```

여기에 아래 같은 동작을 넣는다.

```text
문 열기
스위치 상태 변경
퍼즐 진행도 갱신
기믹 작동 이벤트 발생
```

### 파괴 가능한 기믹

부술 수 있는 벽, 파괴 가능한 장애물처럼 HP가 필요한 오브젝트는 `DamageableFieldObject`를 기준으로 별도 클래스를 만든다.

예시:

```text
BreakableDungeonObject : DamageableFieldObject
```

필요한 흐름:

```text
HP 0 이하
-> 벽 파괴
-> 길 열림
-> 던전 매니저에 알림
```

나중에 추가할 수 있는 연결 지점:

```text
OnDepleted 이벤트
DungeonManager 연동
```

### 던전/기믹 프리팹 세팅

기본 세팅:

```text
DungeonGimmick.prefab
- SpriteRenderer 선택
- Collider2D
- DungeonGimmickObject 또는 상속 클래스
```

상호작용 대상이면 Collider2D가 필요하다.

`Data`에는 던전/기믹용 `FieldObjData`를 연결한다.

예상 타입:

```text
Gimmick
Dungeon
Interactive
```

정확한 타입명은 데이터 테이블 기준으로 맞춘다.

## 고정 구조물 작업 방향

고정 구조물은 아래 클래스를 기준으로 사용한다.

```text
Assets/06_Scripts/FieldObjects/FixedStructureObject/FixedStructureObject.cs
Assets/06_Scripts/FieldObjects/FixedStructureObject/InteractiveFixedStructureObject.cs
```

사용 대상:

```text
암시장
행운 분수
마을 시설
NPC 건물
파괴되지 않는 구조물
스토리용 구조물
```

고정 구조물은 기본적으로 파괴되지 않는다고 본다.

### 상호작용 없는 고정 구조물

장식용 구조물, 배경 구조물처럼 플레이어가 사용할 일이 없으면 `FixedStructureObject`만 붙인다.

```text
FixedStructureObject
- FieldObjectBase 상속
- 상호작용 없음
- HP 없음
- 드랍 없음
```

프리팹 세팅:

```text
Structure.prefab
- SpriteRenderer
- Collider2D 선택
- FixedStructureObject
```

Collider2D는 이동 방해나 충돌이 필요할 때만 붙인다.

### 상호작용 있는 고정 구조물

플레이어가 말을 걸거나 기능을 열어야 하는 구조물은 `InteractiveFixedStructureObject`를 사용한다.

예시:

```text
BlackMarketStructure : InteractiveFixedStructureObject
FortuneFountainStructure : InteractiveFixedStructureObject
```

주로 수정할 위치:

```text
Interact()
```

여기에 아래 같은 동작을 넣는다.

```text
상점 UI 열기
분수 버프 적용
NPC 대화 시작
지역 기능 UI 열기
```

프리팹 세팅:

```text
InteractiveStructure.prefab
- SpriteRenderer
- Collider2D
- InteractiveFixedStructureObject 또는 상속 클래스
```

상호작용 대상이면 Collider2D가 필요하다.

기능별로 `InteractiveFixedStructureObject`를 직접 수정하기보다는, 기능별 클래스를 따로 만드는 것을 추천한다.

## 플레이어 배치 오브젝트 작업 방향

플레이어 배치 오브젝트는 아래 클래스를 기준으로 사용한다.

```text
Assets/06_Scripts/FieldObjects/PlacedFieldObject/PlacedFieldObject.cs
Assets/06_Scripts/FieldObjects/PlacedFieldObject/InteractivePlacedFieldObject.cs
Assets/06_Scripts/FieldObjects/PlacedFieldObject/GrowablePlacedFieldObject.cs
```

사용 대상:

```text
상자
농작물
씨앗
제작대
설치물
농사 관련 배치물
플레이어가 필드에 직접 놓는 오브젝트
```

플레이어 배치 오브젝트는 종류가 다양하므로 기능별로 나눠서 사용한다.

### 기본 배치 오브젝트

상호작용이 없고 배치 정보만 필요하면 `PlacedFieldObject`를 사용한다.

```text
PlacedFieldObject
- FieldObjectBase 상속
- 기본 배치 데이터만 가짐
```

예시:

```text
장식용 설치물
단순 배치물
```

### 상호작용 가능한 배치 오브젝트

플레이어가 열거나 사용할 수 있는 배치물은 `InteractivePlacedFieldObject`를 사용한다.

예시:

```text
상자
제작대
화로
보관함
```

주로 수정할 위치:

```text
Interact()
```

여기에 아래 같은 동작을 넣는다.

```text
상자 열기
제작 UI 열기
설비 작동
아이템 넣기/빼기
```

프리팹 세팅:

```text
PlacedInteractive.prefab
- SpriteRenderer
- Collider2D
- InteractivePlacedFieldObject 또는 상속 클래스
```

### 성장하는 배치 오브젝트

농작물처럼 시간이 지나면서 단계가 변하는 오브젝트는 `GrowablePlacedFieldObject`를 기준으로 확장한다.

```text
GrowablePlacedFieldObject
- PlacedFieldObject 상속
- IGrowable 구현
```

기본 제공 값:

```text
GrowthStage
IsHarvestable
```

농작물에서는 이 값을 실제 성장 데이터와 연결한다.

예시:

```text
CropObject : GrowablePlacedFieldObject
```

필요한 기능:

```text
성장 단계 증가
단계별 스프라이트 변경
수확 가능 여부 판단
수확 시 드랍
수확 후 상태 변경 또는 삭제
```

### 파괴 가능한 배치 오브젝트

나무 상자처럼 플레이어가 부술 수 있는 배치물은 `DamageableFieldObject`를 기준으로 별도 클래스를 만든다.

예시:

```text
BreakablePlacedObject : DamageableFieldObject
```

사용 대상:

```text
나무 상자
부술 수 있는 설치물
내구도가 있는 배치물
```

기준:

```text
사용해서 여는 오브젝트 -> InteractivePlacedFieldObject
때려서 부수는 오브젝트 -> DamageableFieldObject 계열
성장하는 오브젝트 -> GrowablePlacedFieldObject
```

## 4종류별 선택 기준

어떤 클래스를 붙일지 헷갈리면 아래 기준으로 고른다.

```text
자연 생성물
-> DamageableFieldObject

던전/기믹
-> DungeonGimmickObject
-> 파괴 가능하면 DamageableFieldObject 계열 별도 클래스

고정 구조물
-> FixedStructureObject
-> 상호작용 필요하면 InteractiveFixedStructureObject 계열

플레이어 배치 오브젝트
-> PlacedFieldObject
-> 상호작용 필요하면 InteractivePlacedFieldObject 계열
-> 성장 필요하면 GrowablePlacedFieldObject 계열
-> 파괴 가능하면 DamageableFieldObject 계열
```

중요한 기준:

```text
모든 오브젝트에 HP를 넣지 않는다.
모든 오브젝트에 상호작용을 넣지 않는다.
필요한 기능만 인터페이스나 상속 클래스로 붙인다.
```

## 바위 세팅 방법

바위 프리팹 위치 예시:

```text
Assets/04_KJ/Prefabs/Resources/Rock.prefab
```

바위 프리팹에 아래 컴포넌트를 붙인다.

```text
SpriteRenderer
Collider2D
DamageableFieldObject
```

### Collider2D

플레이어가 주변 상호작용 대상을 찾을 때 Collider2D가 필요하다.

설정:

```text
Collider2D: BoxCollider2D 또는 CircleCollider2D
Is Trigger: 꺼도 됨
```

현재 탐색은 물리 충돌이 아니라 `OverlapCircleAll`로 주변 Collider를 찾는 방식이다.

### DamageableFieldObject

Inspector에서 아래 값을 연결한다.

```text
Data: Assets/02_Data/SO/FieldObjData/41002.asset
Preferred Tool Type: Pickaxe
Preferred Tool Multiplier: 1.5
Fallback Damage: 1
Dropper: 비워둬도 됨
```

`Data`에는 바위용 `FieldObjData`를 연결한다.

바위 데이터 기준:

```text
ObjectID: 41002
ObjectType: Rock
HP: 5
DropGroupID: 71002
```

`Preferred Tool Type`은 `Pickaxe`로 설정한다.

현재 플레이어가 장비를 실제로 들고 있는지 판단하지 않기 때문에, 바위 근처에서 상호작용하면 `Pickaxe`를 든 것으로 간주한다.

## 나무 세팅 방법

나무 프리팹에도 바위와 같은 구조를 사용한다.

```text
SpriteRenderer
Collider2D
DamageableFieldObject
```

Inspector 설정:

```text
Data: Assets/02_Data/SO/FieldObjData/41001.asset
Preferred Tool Type: Axe
Preferred Tool Multiplier: 1.5
Fallback Damage: 1
Dropper: 비워둬도 됨
```

나무 데이터 기준:

```text
ObjectID: 41001
ObjectType: Tree
HP: 5
DropGroupID: 71001
```

## 수풀 세팅 방법

수풀도 기본적으로 `DamageableFieldObject`를 사용한다.

```text
SpriteRenderer
Collider2D
DamageableFieldObject
```

Inspector 설정 예시:

```text
Data: Assets/02_Data/SO/FieldObjData/41004.asset
Preferred Tool Type: Sword 또는 None
Preferred Tool Multiplier: 1.5
Fallback Damage: 1
Dropper: 비워둬도 됨
```

수풀 데이터 기준:

```text
ObjectID: 41004
ObjectType: Bush
HP: 1
DropGroupID: 0
```

드랍이 없는 자연물은 `DropGroupID`가 0이어도 된다.

## 드랍 세팅 방법

드랍까지 사용하려면 오브젝트 프리팹에 아래 컴포넌트를 추가한다.

```text
FieldObjectDropper
```

Inspector 설정:

```text
Drop Table: DropTableData 에셋
Pickup Prefab: PickupItem이 붙은 프리팹
Scatter Radius: 드랍 아이템 흩어지는 거리
```

드랍 흐름:

```text
FieldObjData.DropGroupID
-> DropTableData에서 같은 DropGroupID 검색
-> ItemID 또는 ItemData 확인
-> PickupItem 생성
```

현재 드랍 데이터가 완성되지 않았으면 `FieldObjectDropper`를 붙이지 않아도 된다.

이 경우 오브젝트는 HP가 0 이하가 되면 삭제되지만 아이템은 나오지 않는다.

## DropTableData 세팅 기준

`DropTableData`는 나중에 `DropTable.json` 내용을 옮겨서 사용한다.

필드:

```text
DropGroupID
ItemID
Item
DropRate
MinCount
MaxCount
```

`Item`은 직접 연결할 수도 있고, 비워둔 뒤 `ItemDatabase`가 `ItemID`로 찾게 할 수도 있다.

추천:

```text
데이터가 완성되기 전: ItemID만 입력
ItemData 에셋이 완성된 후: ItemDatabase 연결
특정 항목만 수동 테스트: Item 직접 연결
```

## ItemDatabase 역할

`ItemDatabase`는 `ItemID`를 `ItemData`로 찾기 위한 연결용 ScriptableObject다.

현재 구조:

```text
DropTableData
-> ItemDatabase
-> ItemData 목록
```

나중에 KJ가 아이템 SO를 모두 만들면 `ItemDatabase`에 `ItemData`들을 등록한다.

그러면 `DropTableData`는 `ItemID`만 가지고 있어도 실제 아이템을 찾을 수 있다.

## 플레이어와 연결되는 부분

플레이어 쪽은 `PlayerInteraction`이 담당한다.

위치:

```text
Assets/06_Scripts/Player/Interaction/PlayerInteraction.cs
```

현재 설정:

```text
Current Tool Type: None
Assume Correct Tool In Range: true
```

`Assume Correct Tool In Range`가 켜져 있으면 플레이어가 실제 도구를 들고 있지 않아도, 대상 오브젝트의 `Preferred Tool Type`을 사용한다.

기획이 확정되면 이 부분을 수정한다.

예시 수정 방향:

```text
인벤토리에 도구가 있으면 사용 가능
장착 슬롯에 도구가 있어야 사용 가능
퀵슬롯 선택 도구만 사용 가능
```

그때 수정할 위치:

```text
PlayerInteraction.ResolveToolType()
```

## 애니메이션 연결

플레이어가 상호작용에 성공하면 도구 타입에 맞는 애니메이션을 재생한다.

현재 연결:

```text
ToolType.Pickaxe -> Player_Pick
ToolType.Axe -> Player_Axe
```

수정 위치:

```text
Assets/06_Scripts/Player/Animation/PlayerAnimation.cs
```

메서드:

```text
PlayToolInteraction(ToolType toolType)
GetToolStateHash(ToolType toolType)
```

새 도구 애니메이션을 추가하면 `GetToolStateHash`에 매핑을 추가한다.

예시:

```text
ToolType.Shovel -> Player_Shovel
ToolType.Sword -> Player_Sword
```

## 나중에 사운드/이펙트를 추가하는 방법

사운드나 이펙트를 `DamageableFieldObject`에 직접 많이 넣지 않는 것을 추천한다.

추천 구조:

```text
Rock.prefab
- DamageableFieldObject
- FieldObjectFeedback
```

`DamageableFieldObject`는 데미지/파괴 이벤트만 발생시키고, `FieldObjectFeedback`이 사운드와 이펙트를 처리한다.

나중에 추가할 수 있는 이벤트:

```text
OnDamaged
OnDepleted
```

추가 위치:

```text
DamageableFieldObject.cs
```

예시 구조:

```csharp
public event Action<InteractionContext> OnDamaged;
public event Action OnDepletedEvent;
```

이후 `FieldObjectFeedback`에서 이 이벤트를 구독한다.

```text
OnDamaged -> 피격 사운드, 흔들림, 타격 이펙트
OnDepleted -> 파괴 사운드, 파괴 이펙트
```

이렇게 하면 자연 생성물뿐 아니라 파괴 가능한 상자, 광석, 기타 오브젝트에도 같은 연출 구조를 재사용할 수 있다.

## 나중에 리스폰을 연결하는 방법

현재 `DamageableFieldObject`는 HP가 0 이하가 되면 `Destroy(gameObject)`를 호출한다.

리스폰 시스템을 연결할 때는 바로 삭제하기 전에 스폰 시스템에 알리는 구조가 필요하다.

추가할 수 있는 방향:

```text
DamageableFieldObject
-> OnDepleted 이벤트 발생
-> ResourceSpawner 또는 ResourceRespawnManager가 이벤트 수신
-> 기존 위치와 프리팹 정보를 저장
-> 일정 시간 후 다시 생성
```

수정 후보 위치:

```text
DamageableFieldObject.OnDepleted()
ResourceSpawner
새 ResourceRespawnManager
```

리스폰은 현재 작업 범위가 아니므로, 지금은 삭제만 처리한다.

## 나중에 오브젝트 풀링을 연결하는 방법

현재 파괴 처리는 임시로 `Destroy(gameObject)`를 사용한다.

오브젝트 풀링 시스템이 생기면 이 부분은 실제 삭제가 아니라 풀로 반환하는 방식으로 바꾼다.

현재 구조:

```text
HP 0 이하
-> Drop 처리
-> Destroy(gameObject)
```

풀링 적용 후 목표 구조:

```text
HP 0 이하
-> Drop 처리
-> 상태 초기화
-> SetActive(false)
-> 오브젝트 풀로 반환
```

수정 후보 위치:

```text
DamageableFieldObject.OnDepleted()
```

예상 변경 방향:

```text
Destroy(gameObject)
-> ObjectPool 반환 메서드 호출
```

예시:

```csharp
objectPool.Release(gameObject);
```

또는 풀링용 인터페이스를 따로 만들 수 있다.

```csharp
public interface IPoolableObject
{
    void OnSpawnedFromPool();
    void OnReturnedToPool();
}
```

이 경우 `DamageableFieldObject`는 직접 풀을 알기보다, 풀 반환을 담당하는 컴포넌트나 매니저에 요청하는 방식이 좋다.

추천 방향:

```text
DamageableFieldObject
-> OnDepleted 이벤트 발생
-> PoolReturner 또는 ResourceSpawner가 이벤트 수신
-> 풀로 반환
```

이렇게 하면 `DamageableFieldObject`는 데미지와 파괴 판정만 담당하고, 풀링 방식은 나중에 별도로 교체할 수 있다.

## 새 자연 생성물 추가 순서

예시: 새 광석 추가

1. 광석 프리팹 생성
2. `SpriteRenderer` 추가
3. `Collider2D` 추가
4. `DamageableFieldObject` 추가
5. `Data`에 광석 `FieldObjData` 연결
6. `Preferred Tool Type`을 `Pickaxe`로 설정
7. 드랍이 필요하면 `FieldObjectDropper` 추가
8. 드랍 테이블이 준비되면 `DropTableData`와 `PickupPrefab` 연결
9. KJ의 `ResourceSpawner` 테이블에 광석 프리팹 등록

이후 스폰된 광석은 플레이어 상호작용 구조를 자동으로 사용한다.

## 최소 테스트 세팅

드랍 없이 채광 동작만 확인하려면 아래만 필요하다.

```text
Rock.prefab
- Collider2D
- DamageableFieldObject
  - Data: 41002
  - Preferred Tool Type: Pickaxe
  - Preferred Tool Multiplier: 1.5
  - Fallback Damage: 1
```

플레이어:

```text
PlayerInteraction
  - Current Tool Type: None
  - Assume Correct Tool In Range: true
```

UI 버튼:

```text
OnClick -> PlayerInteraction.Interact()
```

테스트 결과:

```text
플레이어가 바위 근처에서 버튼 클릭
-> Player_Pick 애니메이션 재생
-> 바위 HP 감소
-> HP 0 이하이면 바위 삭제
```

## 작업 시 주의할 점

`ResourceSpawner`는 오브젝트를 생성만 한다.

상호작용 가능 여부는 프리팹에 붙은 컴포넌트가 결정한다.

따라서 스폰 테이블에 등록할 프리팹에는 최소한 아래가 있어야 한다.

```text
Collider2D
DamageableFieldObject 또는 IInteractable 구현 컴포넌트
```

자연 생성물은 당분간 `DamageableFieldObject`를 기준으로 세팅한다.

`NaturalResourceObject`는 자연 생성물 전용 기능이 생길 때 사용하는 확장 지점이다.

현재 바위/나무/수풀 세팅은 `DamageableFieldObject` 기준으로 설명한다.
