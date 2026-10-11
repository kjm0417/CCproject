# 홀드 방식 스킬 트리

## 바로 사용하기

1. `Assets/04_Prefabs/UI-Canvas/Canvas - SkillTree.prefab`을 씬에 배치합니다.
2. PlayerContext가 있는 플레이어 루트에 `PlayerSkills` 컴포넌트를 추가합니다.
3. PlayerSkills의 Skill Trees에 `Assets/02_Data/SO/SkillTree/HoldSample/FourRoots.asset`을 넣습니다.
   목록이 비어 있으면 패널의 Default Trees를 자동으로 사용합니다.
4. 씬에 EventSystem이 있는지 확인합니다. 프로젝트는 Input System을 사용하므로
   새로 만드는 경우 InputSystemUIInputModule을 사용합니다. 기존 EventSystem이 있으면 추가하지 않습니다.
5. Play하면 전투/생존/채집/탐험 시작 스킬 4개가 표시됩니다. 이 이름들은 동작 확인용 샘플입니다.

기존 Skill.asset / SkillTree.asset은 수정하지 않았습니다.
전투/생존/채집/탐험 스킬의 실제 효과는 아직 없으며 습득 구조를 확인하기 위한 데이터입니다.

## 홀드 동작

- 마우스 왼쪽 버튼이나 터치로 1초 동안 누르면 게이지가 찹니다.
- 포인트가 없어도 게이지는 찹니다. **완료 순간** 비용을 지불할 수 있는지 확인합니다.
- 포인트가 충분하면 차감하고 습득합니다. 바로 연결된 자식 노드와 연결선이 나타납니다.
- 포인트가 부족하면 습득하지 않고 게이지가 0이 되며 부족 안내를 표시합니다.
- 실패한 채 계속 누르고 있어도 재시도하지 않습니다. 손을 떼고 다시 눌러야 합니다.
- 중간에 손을 떼거나 버튼 밖으로 이동하거나 스크롤 드래그를 시작하면 게이지를 초기화합니다.
- 배운 스킬은 중복 습득하거나 포인트를 다시 차감할 수 없습니다.
- 시간 정지 중에도 UI 홀드는 진행됩니다(Time.unscaledDeltaTime).

기본 비용은 모두 1 SP이며 기존 PlayerProgression의 레벨업 포인트를 사용합니다.
포인트 없이 성공 흐름을 잠깐 확인하려면 테스트할 Skill SO의 Point Cost를 0으로 바꾸세요.
테스트가 끝나면 1로 되돌리세요.

## 내 스킬로 바꾸기

Project에서 Create > Scriptable Objects > Skills > Skill로 스킬을 만듭니다.
Skill Id는 저장용 고유 ID, Display Name은 표시 이름, Point Cost는 비용입니다.
Skill Id는 서로 다르게 지정하고 저장 데이터 생성 후에는 변경하지 않습니다.

Create > Scriptable Objects > Skills > Skill Tree로 트리를 만든 뒤 Nodes에 등록합니다.

| Skill | Parent |
| --- | --- |
| 시작 A | 비워 둠 |
| 시작 B | 비워 둠 |
| 시작 C | 비워 둠 |
| 시작 D | 비워 둠 |
| A 왼쪽 | 시작 A |
| A 오른쪽 | 시작 A |
| A 왼쪽 다음 | A 왼쪽 |

Parent를 비운 노드가 처음 보이는 항목입니다. 4개를 등록하면 처음에 4개가 보입니다.
한 부모에 자식은 최대 2개까지 연결할 수 있고 양쪽 모두 배울 수 있습니다.
각 단계는 부모를 배운 뒤에만 나타납니다. 모든 후손이 한꺼번에 나타나는 것은 아닙니다.
노드 배열 순서로 시작 항목과 좌우 자식 순서를 정합니다.
같은 스킬이나 ID 중복, 순환 연결, 트리 밖의 부모는 오류로 처리합니다.

만든 트리를 플레이어의 Skill Trees에 연결하세요.
노드 버튼을 수동으로 복제할 필요 없이 SO에 맞춰 자동 생성됩니다.

## 프리팹 구조와 조절

```text
Canvas - SkillTree
  SkillTreePanel                  [SkillTreePanelUI]
    Title / Help / Points
    CloseButton
    Scroll View                   [ScrollRect, 가로/세로]
      Viewport                    [Image + RectMask2D]
        Content                   [자동 생성 노드/연결선 전용]
    NodeTemplate                  [비활성, Button + SkillTreeNodeUI]
      Name / Cost / Status
      GaugeBackground
        HoldGauge                 [Image, Simple / Sprite ?? / ?? ??? ??? ??]
```

Content에 있는 Preview 항목은 편집 화면에서 시작 항목 4개를 보여주기 위한 것입니다.
Play 시 실제 SO로 다시 생성합니다. Content에 별도 고정 UI는 넣지 마세요.

- NodeTemplate의 Hold Duration: 홀드 시간(기본 1초)
- NodeTemplate: 카드 크기/색상/폰트와 게이지 디자인
- 패널의 Node Spacing: 가로·세로 노드 간격
- 패널의 Padding: 스크롤 영역 여백
- 패널의 Default Trees: PlayerSkills의 트리 목록이 비었을 때 사용할 기본 트리
- Player Skills 참조는 비워 두면 씬의 PlayerContext에서 찾습니다.
- 노드가 공개될 때 Content 크기가 자동으로 늘어납니다. 마우스/터치 드래그와 휠로 이동합니다.
- CloseButton은 연결되어 있습니다. 다시 여는 버튼은 씬에서 패널의 Open()에 연결하세요.
- 버튼 On Click으로 습득 함수를 연결하지 마세요. 홀드 완료로만 습득합니다.

## 저장 및 실제 효과

기존 PlayerSaveData 저장/불러오기에 LearnedSkillIds가 연결되어 있습니다.
습득할 때마다 파일에 즉시 저장하지는 않으며 기존 게임 저장 시 함께 저장됩니다.
플레이어가 교체되는 씬에도 PlayerSkills와 같은 트리 목록을 설정해야 합니다.
실제 효과는 OnSkillLearned / OnSkillsChanged / HasLearned()에 추후 연결합니다.

## 검증 도구

Tools > Skill Tree > Validate Hold And Branches로 격리된 미리보기 씬에서
4개 시작 노드, 홀드 취소, 포인트 부족, 중복 차감 방지, 자식 공개, 스크롤 취소,
저장 복원, Content 확장을 검사할 수 있습니다. 결과는 Temp/SkillTreeBuild.result.txt입니다.
기존 씬과 플레이어의 실제 저장 파일은 수정하지 않습니다.

Tools > Skill Tree > Create Canvas Prefab은 프리팹이 없을 때 생성합니다.
이미 있는 프리팹과 샘플 트리를 덮어쓰지 않습니다.

