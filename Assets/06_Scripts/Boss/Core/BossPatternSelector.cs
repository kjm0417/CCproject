using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 가중치 랜덤 패턴 선택 ( 쿨타임 / HP 구간 / 연속 사용 방지 반영 )
/// </summary>
public class BossPatternSelector
{
    private readonly List<BossPattern> patterns;
    private readonly List<BossPattern> candidates = new List<BossPattern>();
    private readonly bool avoidRepeat;
    private BossPattern lastPattern;

    public BossPatternSelector(List<BossPattern> patterns, bool avoidRepeat)
    {
        this.patterns = patterns;
        this.avoidRepeat = avoidRepeat;
    }

    public BossPattern Select()
    {
        candidates.Clear();
        float totalWeight = 0f;

        foreach (BossPattern pattern in patterns)
        {
            if (pattern == null || !pattern.enabled || pattern.Weight <= 0f || !pattern.CanUse()) continue;
            candidates.Add(pattern);
            totalWeight += pattern.Weight;
        }

        // 후보가 2개 이상일 때만 직전 패턴 제외 ( 1개뿐이면 그대로 사용 )
        if (avoidRepeat && lastPattern != null && candidates.Count > 1 && candidates.Remove(lastPattern))
        {
            totalWeight -= lastPattern.Weight;
        }

        if (candidates.Count == 0) return null;

        float pick = Random.Range(0f, totalWeight);
        foreach (BossPattern pattern in candidates)
        {
            pick -= pattern.Weight;
            if (pick <= 0f)
            {
                lastPattern = pattern;
                return pattern;
            }
        }

        lastPattern = candidates[candidates.Count - 1];
        return lastPattern;
    }

    public BossPattern FindById(string patternId)
    {
        foreach (BossPattern pattern in patterns)
        {
            if (pattern != null && pattern.PatternId == patternId) return pattern;
        }
        return null;
    }

    public void NotifyUsed(BossPattern pattern)
    {
        lastPattern = pattern;
    }
}
