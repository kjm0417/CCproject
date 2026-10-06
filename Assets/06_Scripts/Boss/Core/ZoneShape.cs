using System;
using UnityEngine;

/// <summary>
/// 보스 공격 / 경고 영역 ( 회전 가능한 사각형 )
/// anchor 가 있으면 anchor 위치·회전 기준, 없으면 offset 이 월드 좌표
/// 마름모는 angle 45 로 표현
/// </summary>
[Serializable]
public class ZoneShape
{
    public Transform anchor;
    public Vector2 offset;
    public Vector2 size = Vector2.one;
    public float angle;

    public Vector2 Center => (anchor != null ? (Vector2)anchor.position : Vector2.zero) + offset;
    public float Angle => angle + (anchor != null ? anchor.eulerAngles.z : 0f);

    /// <summary> 영역 로컬 위쪽 방향 ( 회전 반영 ) </summary>
    public Vector2 Up => Rotate(Vector2.up, Angle);

    public static ZoneShape Fixed(Vector2 center, Vector2 size, float angle = 0f)
    {
        return new ZoneShape { offset = center, size = size, angle = angle };
    }

    /// <summary>
    /// 현재 위치 기준으로 고정된 복사본 - 패턴 실행 중 anchor 가 움직여도 영향 X
    /// </summary>
    public ZoneShape Snapshot()
    {
        return Fixed(Center, size, Angle);
    }

    public bool Contains(Vector2 point)
    {
        Vector2 local = Rotate(point - Center, -Angle);
        return Mathf.Abs(local.x) <= size.x * 0.5f && Mathf.Abs(local.y) <= size.y * 0.5f;
    }

    /// <summary> 꼭짓점 4개 ( 좌하 -> 좌상 -> 우상 -> 우하 ) </summary>
    public Vector3[] GetCorners()
    {
        Vector2 half = size * 0.5f;
        Vector2 c = Center;
        float a = Angle;
        return new Vector3[]
        {
            c + Rotate(new Vector2(-half.x, -half.y), a),
            c + Rotate(new Vector2(-half.x,  half.y), a),
            c + Rotate(new Vector2( half.x,  half.y), a),
            c + Rotate(new Vector2( half.x, -half.y), a),
        };
    }

    public void DrawGizmo(Color color)
    {
        Gizmos.color = color;
        Vector3[] corners = GetCorners();
        for (int i = 0; i < corners.Length; i++)
        {
            Gizmos.DrawLine(corners[i], corners[(i + 1) % corners.Length]);
        }
    }

    private static Vector2 Rotate(Vector2 v, float degree)
    {
        float rad = degree * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
}
