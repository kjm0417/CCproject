using UnityEngine;

[CreateAssetMenu(fileName = "TestData", menuName = "Game/TestData")]
public class TestData : ScriptableObject
{
    public string id;
    public string objectName;
    public int maxHP;
    public string dropItemId;
    public float dropRate;
}
