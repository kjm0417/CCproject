using UnityEngine;

/// <summary>
/// 적 공통 - 컴포넌트 모음
/// </summary>
public class EnemyContext
{
    public EnemyHelath EnemyHelath { get; private set;}

    public SpriteRenderer SpriteRenderer { get; private set; }

    public Animator Animator { get; private set; }

    public EnemyDetector EnemyDetector { get; private set; }

    public MonsterData MonsterData { get; private set; }


    public EnemyContext(EnemyHelath enemyHealth,SpriteRenderer spriteRenderer,Animator animator , EnemyDetector enemyDetector,
        MonsterData monsterData)
    {
        this.EnemyHelath = enemyHealth;
        this.SpriteRenderer = spriteRenderer;
        this.Animator = animator;
        this.EnemyDetector = enemyDetector;
        this.MonsterData = monsterData;
    }
}

/// <summary>
/// 슬라임 전용 - 물리 기반 움직임 필요
/// </summary>
public class EnemySlimeContext : EnemyContext
{
    public Rigidbody2D Rigidbody2D { get; private set; }

    public EnemySlimeContext(EnemyHelath enemyHealth, SpriteRenderer spriteRenderer,Rigidbody2D rigidbody2D,
        Animator animator,EnemyDetector enemyDetector,MonsterData monsterData) 
        : base(enemyHealth, spriteRenderer, animator, enemyDetector, monsterData)
    {
        this.Rigidbody2D = rigidbody2D;
    }
}
