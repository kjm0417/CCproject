// 닳고 차는 수치(체력, 허기 등)의 순수 C# 모델. Unity를 모른다 -> 단위 테스트가 쉽다.
// 현재값은 항상 0 ~ 최대 사이로 clamp 된다. 나중에 PlayerHealth도 이걸 그대로 재사용한다.
public class DepletableStat
{
    private float current;
    private float max;

    public float Current => current;
    public float Max => max;
    public bool IsEmpty => current <= 0f;
    public bool IsFull => current >= max;

    // 0~1 비율 (HUD 바 등에서 사용). 최대가 0이면 0 반환(0 나누기 방어).
    public float Percent01 => max > 0f ? current / max : 0f;

    public DepletableStat(float max)
    {
        this.max = max < 0f ? 0f : max;
        current = this.max; // 시작은 가득 찬 상태
    }

    // 최대치 설정. refill이면 현재값도 가득 채우고, 아니면 최대를 넘는 만큼만 깎는다.
    public void SetMax(float value, bool refill)
    {
        max = value < 0f ? 0f : value;
        if (refill) current = max;
        else if (current > max) current = max;
    }

    // 감소 (0 밑으로 내려가지 않음). 음수/0은 무시.
    public void Reduce(float amount)
    {
        if (amount <= 0f) return;
        current -= amount;
        if (current < 0f) current = 0f;
    }

    // 회복 (최대 초과하지 않음). 음수/0은 무시.
    public void Add(float amount)
    {
        if (amount <= 0f) return;
        current += amount;
        if (current > max) current = max;
    }

    // 최대로 리셋 (씬 재진입 등)
    public void Reset()
    {
        current = max;
    }
}
