using UnityEngine;

// �÷��̾� ��ǰ���� �� ������ ã�� ���� �������ִ� ������
// ��ǰ���� ���� GetComponent ���� �ʰ�, �׻� �� Context�� ���� ����
public class PlayerContext : MonoBehaviour
{
    [Header("�⺻ ���� ������ (SO) - �� ������ ���� ��ΰ� ����")]
    [SerializeField]
    private PlayerBaseData baseData;
    public PlayerBaseData BaseData => baseData;

    // �÷��̾� ����(�̼�, ���ݷ�, ���ݹ��� �� ���� ��갪)
    public PlayerStats Stats { get; private set; }

    // �̵� ��ǰ(���̽�ƽ �Է� -> �̵�) - ��Ʈ
    public PlayerMovement Movement { get; private set; }

    // �ִϸ��̼� ��ǰ(�̵� ���� -> �ִϸ�����/����) - ���� �ڽ�(PlayerAnim)
    public PlayerAnimation Animation { get; private set; }

    // ����Ż ��ǰ(ü�� + ���) - ��Ʈ
    public PlayerVitals Vitals { get; private set; }

    // ���� ��ǰ(����ġ/����/��ų����Ʈ) - ��Ʈ
    public PlayerProgression Progression { get; private set; }

    // ��ȭ ��ǰ(�÷��̾� ����, ���� ��ȭ) - ��Ʈ
    public PlayerWallet Wallet { get; private set; }

    // ���� �� ���� ��ǰ�� - ����� �Ʒ� �ּ��� Awake�� ¦�� �Բ� Ǭ��.
    public PlayerInteraction Interaction { get; private set; }

    public PlayerInventory Inventory { get; private set; }

    public SpriteRenderer SpriteRenderer { get; private set; }

    private void Awake()
    {
        //��ǰ ������ ���� ��� ã��
        Stats = GetComponent<PlayerStats>();
        Movement = GetComponent<PlayerMovement>();
        Animation = GetComponentInChildren<PlayerAnimation>(true); // �ڽı��� Ž��
        Vitals = GetComponent<PlayerVitals>();
        Progression = GetComponent<PlayerProgression>();
        Wallet = GetComponent<PlayerWallet>();
        Interaction = GetComponent<PlayerInteraction>();
        Inventory = GetComponent<PlayerInventory>();
        SpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        //self + �ڽĿ� �ִ� ��� IPlayerComponent ��ǰ�� �ʱ�ȭ
        InitializeComponents();
    }

    // self�� �ڽĿ� ���� ��� IPlayerComponent ��ǰ�� �ڱ� �ڽ��� �Ѱ� �ʱ�ȭ
    private void InitializeComponents()
    {
        foreach (IPlayerComponent component in GetComponentsInChildren<IPlayerComponent>(true))
        {
            component.Initialize(this);
        }
    }
}
