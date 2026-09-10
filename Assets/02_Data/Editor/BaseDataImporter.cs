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
    protected abstract string SOFolderPath { get; }

    protected abstract void MapDTOToSO(TDTO dto, TSO so); //필드 매핑
    protected abstract string GetDTOID(TDTO dto); //SO의 ID를 가져오는 메서드

    public void Import()
    {
        string path = Application.dataPath + JsonFilePath;
        string json = System.IO.File.ReadAllText(path);
        string wrappedJson = "{\"items\":" + json + "}";

        DTOList<TDTO> data = JsonUtility.FromJson <DTOList<TDTO>>(wrappedJson);

        foreach (TDTO dto in data.items)
        {
            TSO so = ScriptableObject.CreateInstance<TSO>();
           
            MapDTOToSO(dto, so);

            string dtoID = GetDTOID(dto);

            string assetPath = SOFolderPath + dtoID + ".asset";

            AssetDatabase.CreateAsset(so, assetPath);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}

