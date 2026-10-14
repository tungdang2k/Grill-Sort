using UnityEngine;

public class LevelLoader : MonoBehaviour
{
    public bool HasLevel(int level)
    {
        return level > 0 && Resources.Load<TextAsset>(CONSTANTS.DATA_PATH + level) != null;
    }

    public LevelData Load(int level)
    {
        string path = CONSTANTS.DATA_PATH + level;

        TextAsset jsonFile = Resources.Load<TextAsset>(path);

        if (jsonFile == null)
        {
            
            return null;
        }

        return JsonUtility.FromJson<LevelData>(jsonFile.text);
    }

    

}
