using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class DTOList<TDTO>
{
    public List<TDTO> items;
}

public abstract class BaseDataImporter<TDTO,TSO> where TSO : ScriptableObject
{
    protected abstract string JsonFilePath { get;}
    public abstract string SOFolderPath { get; }

    protected abstract void MapDTOToSO(TDTO dto, TSO so); //�ʵ� ����
    protected abstract string GetDTOID(TDTO dto); //SO�� ID�� �������� �޼���

    public DTOList<TDTO> GetDTOList()
    {
        string path = Application.dataPath + JsonFilePath;
        string json = System.IO.File.ReadAllText(path);
        string wrappedJson = "{\"items\":" + json + "}";
        return JsonUtility.FromJson<DTOList<TDTO>>(wrappedJson);
    }

    public void MapEntry(TDTO dto, TSO data)
    {
        MapDTOToSO(dto, data); 
    }

    public void Import()
    {
        string path = Application.dataPath + JsonFilePath;
        string json = System.IO.File.ReadAllText(path);
        string wrappedJson = "{\"items\":" + json + "}";

        DTOList<TDTO>  data = JsonUtility.FromJson <DTOList<TDTO>>(wrappedJson);

        foreach (TDTO dto in data.items)
        {
            string dtoID = GetDTOID(dto);
            string assetPath = SOFolderPath + dtoID + ".asset";

            // 기존 에셋이 있으면 업데이트, 없으면 새로 생성 (GUID 유지)
            TSO so = AssetDatabase.LoadAssetAtPath<TSO>(assetPath);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<TSO>();
                MapDTOToSO(dto, so);
                AssetDatabase.CreateAsset(so, assetPath);
            }
            else
            {
                MapDTOToSO(dto, so);
                EditorUtility.SetDirty(so);
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}

