using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UIImage = UnityEngine.UI.Image;

public class GameManager : Singleton<GameManager>
{
    public LevelData CurrentLevelData { get; private set; }

    public int CurrentLevel => m_currentLevel;
    public int AllFood => m_allFood;
    public float LevelSeconds => m_levelSeconds;
    public event Action OnAllFoodChanged;
    public event Action OnWin;
    public event Action OnOutOfTime;
    public List<GrillStation> ListGrill => m_listGrill;
    public List<GrillStation> GetBonusGrills() => m_bonusGrills;

    [SerializeField] private LevelLoader m_levelLoader;
    [SerializeField] private int m_allFood;
    [SerializeField] private int m_totalGrill;
    [SerializeField] private int m_toltalFood;

    private List<GrillStation> m_bonusGrills = new List<GrillStation>();
    private List<GrillStation> m_listGrill = new List<GrillStation>();
    private LevelInitializer m_levelInitializer;
    private bool m_isGameEnded = false;
    private Transform m_gridGrill;
    private float m_levelSeconds;
    private int m_mergeCount = 0;
    private int m_currentLevel;
    private bool m_hideTutorialDone = false;
    protected override void Awake()
    {
        base.Awake();
        m_levelInitializer = new LevelInitializer();
        m_currentLevel = PlayerPrefs.GetInt(CONSTANTS.LEVEL, 1);
        Sprite[] loadedSprites = Resources.LoadAll<Sprite>("Items");

    }


    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
 

    void Update()
    {

        //Debug.Log($"[FRAME {Time.frameCount}] RealFoodInScene = {GetTotalRealFoodInScene()}");
        
    }

    public void StartLevel()
    {
       
        m_listGrill?.Clear();
        m_bonusGrills?.Clear();
        m_gridGrill = GameObject.FindWithTag("GridGrill")?.transform;
        if (!m_gridGrill) return;

        m_listGrill = Utils.GetListInChild<GrillStation>(m_gridGrill);
        m_isGameEnded = false;
        m_hideTutorialDone = false;
        m_mergeCount = 0;        
        m_allFood = 0;
        LoadLevel(m_currentLevel);
        if (CurrentLevelData == null)
        {
            m_isGameEnded = true;
            return;
        }
        m_levelInitializer.Init(
         CurrentLevelData,
         m_listGrill,
         m_bonusGrills
        );

        
    }
    private void LoadLevel(int level)
    {
        if (m_levelLoader == null)
        {
            Debug.LogError("GameManager is missing its LevelLoader reference.", this);
            CurrentLevelData = null;
            return;
        }

        CurrentLevelData = m_levelLoader.Load(level);
        if (CurrentLevelData == null || CurrentLevelData.spawnWareData == null ||
            CurrentLevelData.boardData == null || CurrentLevelData.boardData.listTrayData == null)
        {
            Debug.LogError($"Level {level} could not be loaded or is missing required data.", this);
            CurrentLevelData = null;
            return;
        }

        m_levelSeconds = CurrentLevelData.levelSeconds;
        m_allFood = CurrentLevelData.spawnWareData.totalWare;

        m_toltalFood = CurrentLevelData.spawnWareData.totalWarePattern;
        //Debug.Log($"[GameManager] Level {level} loaded. AllFood: {m_allFood} | TotalFood: {m_toltalFood}");
        m_totalGrill = CurrentLevelData.boardData.listTrayData.Count;

        var validTrays = CurrentLevelData.boardData.listTrayData
        .Where(t => t != null &&
                t.id != "bonus_tray" &&
                (t.id == "normal_tray" || t.id == "target_tray"))
        .ToList();


    }
    private void CompleteLevel()
    {
        int nextLevel = m_currentLevel + 1;
        if (m_levelLoader != null && m_levelLoader.HasLevel(nextLevel))
            m_currentLevel = nextLevel;

        PlayerPrefs.SetInt(CONSTANTS.LEVEL, m_currentLevel);
        PlayerPrefs.Save();

    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartLevel();
       
    }
    
    public void OnMinusFood()
    {
        if (m_isGameEnded) return;
        
        m_allFood -= 3;
        m_mergeCount++;
        OnAllFoodChanged?.Invoke();
        UpdateAllLockedGrillText();
        CheckUnlockGrills();
           
        if (m_currentLevel == 1 && !m_hideTutorialDone)
        {
            m_hideTutorialDone = true; 
            FindFirstObjectByType<HandTurorial>()?.HideHandeGuide();
        }

         
        if (m_allFood <= 0)
        {
            this.CompleteLevel();
            AudioManager.Instance.PlaySFX(SFXType.Win);
            ShowWinPanel();
        }
       
    }
     
    private void UpdateAllLockedGrillText()
    {
        foreach (var grill in m_listGrill)
        {
            if (grill.IsLocked)
                grill.OnMergeHappened(m_mergeCount);
        }
    }

    private void CheckUnlockGrills()
    {
        foreach (var grill in m_listGrill)
        {
            if (!grill.IsLocked) continue;

            if (m_mergeCount >= grill.RequiredMerge)
                grill.Unlock();
        }
    }
     
    private void ShowWinPanel()
    { 
        if (m_isGameEnded) return;

        m_isGameEnded = true;
        OnWin?.Invoke();
       
    }

    public void Lose()
    {
        if (m_isGameEnded) return;
        m_isGameEnded = true;
        OnOutOfTime?.Invoke();

    }

    public void GoHome()
    {
        Time.timeScale = 1f;
        LoadingSceneManager.Instance.SwichToScene(CONSTANTS.HOMESCENE);
    }

     int GetTotalRealFoodInScene()
    {
        int count = 0;

        foreach (var grill in m_listGrill)
        {
            if (!grill.gameObject.activeInHierarchy)
                continue;

            //  Đếm food trong SLOT
            foreach (var slot in grill.totalSlot)
            {
                if (slot.HasFood())
                    count++;
            }

            // Đếm food trong TẤT CẢ TRAY
            foreach (var tray in grill.totalTrays)
            {
                foreach (var img in tray.FoodList)
                {
                    if (img.gameObject.activeInHierarchy)
                        count++;
                }
            }
        }

        return count;
    }

}
