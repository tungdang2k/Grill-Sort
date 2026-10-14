using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelInitializer 
{
    private List<Sprite> m_totalSpriteFood = new();

    public LevelInitializer()
    {
        Sprite[] loadedSprites =
            Resources.LoadAll<Sprite>("Items");

        m_totalSpriteFood = loadedSprites.ToList();
    }

    public void Init(LevelData levelData,
    List<GrillStation> listGrills,
    List<GrillStation> bonusGrills)
    {

        if (levelData == null || levelData.boardData == null ||
            levelData.boardData.listTrayData == null || levelData.spawnWareData == null)
        {
            Debug.LogError("Cannot initialize a level with missing board or food data.");
            return;
        }

        if (GameManager.Instance.CurrentLevel == 1)
        {
            InitTutorialLevel1(listGrills);
            return;
        }

        var allTrayData = levelData.boardData.listTrayData;
        if (levelData.spawnWareData.listLayerData == null ||
            levelData.spawnWareData.listLayerData.Count == 0)
        {
            Debug.LogError($"Level {levelData.level} has no layer data.");
            return;
        }
        // filter grill
        List<GrillStation> activeGrills = new List<GrillStation>();
        List<GrillStation> lockedGrills = new List<GrillStation>();

        for (int i = 0; i < listGrills.Count; i++)
        {

            if (i >= allTrayData.Count) { listGrills[i].gameObject.SetActive(false); continue; }
            var t = allTrayData[i];
            var grill = listGrills[i];

            if (t == null)
            {
                grill.SetNullGrill();
                continue;
            }

            if (t.id == "bonus_tray")
            {
                listGrills[i].SetNullGrill();
                bonusGrills.Add(grill);
                continue;
            }
            if (string.IsNullOrEmpty(t.id) || (t.id != "normal_tray" && t.id != "target_tray"))
            {
                grill.SetNullGrill();
                continue;
            }

            if (t.id == "target_tray")
            {
                lockedGrills.Add(grill);
                listGrills[i].SetAsLocked();
                continue;
            }

            if (t.id == "normal_tray")
            {
                activeGrills.Add(grill);
            }

        }

        int totalSlot = activeGrills.Sum(g => g.totalSlot.Count);

        //  global groups
        var groups = CreateGlobalGroups(levelData.spawnWareData.totalWare
            , levelData.spawnWareData.totalWarePattern);

        //  layer1
        BuildLayer1Items(
            groups,
            levelData.spawnWareData.listLayerData[0],
            totalSlot,
            out var slotItems,
            out var remainPool);

        //  distribute slot
        slotItems = slotItems.OrderBy(_ => UnityEngine.Random.value).ToList();
        var slotPerGrill = DistributeSlotToGrill(slotItems, activeGrills);
         
        //  tray 
        var allGrills = activeGrills.Concat(lockedGrills).ToList();
        remainPool = remainPool.OrderBy(_ => UnityEngine.Random.value).ToList();
        var trayPerGrill = BuildTrayPerGrill(remainPool, allGrills.Count);

        // init grill
        for (int i = 0; i < activeGrills.Count; i++)
        {
            var grill = activeGrills[i];

            grill.gameObject.SetActive(true);
            activeGrills[i].OnInitGrill(
                slotPerGrill[i],
                trayPerGrill[i]

            );
        }

        for (int i = 0; i < lockedGrills.Count; i++)
        {
            lockedGrills[i].gameObject.SetActive(true);
            lockedGrills[i].OnInitGrill(
                new List<Sprite>(),
                trayPerGrill[activeGrills.Count + i]

            );
        }

    }
     
    private void InitTutorialLevel1(List<GrillStation> grills)
    {
        List<Sprite> randomFoods = m_totalSpriteFood
    .OrderBy(_ => UnityEngine.Random.value)
    .Take(5)
    .ToList();

        Sprite food1 = randomFoods[0];
        Sprite food2 = randomFoods[1];
        Sprite food3 = randomFoods[2];
        Sprite food4 = randomFoods[3];
        Sprite food5 = randomFoods[4];

        // slot data
        List<List<Sprite>> slotPerGrill = new()
    {    
        new() { food1,food1,null},
        new() { food2,food2,food1},
        new() { null,food2,food3},
        new() { food4,food4,null}, 
        new() { null,food3,null },    
        new() { food5,null, food3 }
    };

        // tray data
        List<List<List<Sprite>>> trayPerGrill = new()
    {
        new()
        {
            new() { food5, food5 }
        },
        new()
        {
            new() { food4, }
        },

        new()
        {
            new() {  }
        },

        new()
        {
            new() {  }
        },

        new()
        {
            new() {  }
        },

        new()
        {
            new() {  }
        },

    };
        int count = Mathf.Min(
            grills.Count,
            slotPerGrill.Count
        );

        for (int i = 0; i < count; i++)
        {
            grills[i].gameObject.SetActive(true);

            grills[i].OnInitGrill(
                slotPerGrill[i],
                trayPerGrill[i]
            );
        }
        for (int i = count; i < grills.Count; i++)
        {
            grills[i].gameObject.SetActive(false);
        }
    }

    private List<List<Sprite>> CreateGlobalGroups(int totalFood,
    int totalPattern)
    {
        int groupCount = totalFood / 3;

        List<Sprite> takeFood = m_totalSpriteFood
            .OrderBy(_ => UnityEngine.Random.value)
            .Take(totalPattern)
            .ToList();

        List<List<Sprite>> groups = new List<List<Sprite>>();

        for (int i = 0; i < groupCount; i++)
        {
            Sprite s = takeFood[i % takeFood.Count];
            groups.Add(new List<Sprite> { s, s, s });
        }

        return groups.OrderBy(_ => UnityEngine.Random.value).ToList();
    }

    private void BuildLayer1Items(
    List<List<Sprite>> groups,
    LayerData layer,
    int totalSlot,
    out List<Sprite> slotItems,
    out List<Sprite> remainPool)
    {
        slotItems = new List<Sprite>();
        remainPool = new List<Sprite>();

        int empty = Mathf.Min(layer.numberEmptySlot, totalSlot);
        int need = totalSlot - empty;

        foreach (var g in groups)
        {
            if (need <= 0)
            {
                remainPool.AddRange(g);
                continue;
            }

            float rand = UnityEngine.Random.value * 100f;

            // match3
            if (rand < layer.match3Ratio && need >= 3)
            {
                slotItems.AddRange(g);
                need -= 3;
            }
            // match2
            else if (rand < layer.match3Ratio + layer.match2Ratio && need >= 2)
            {
                slotItems.Add(g[0]);
                slotItems.Add(g[1]);
                remainPool.Add(g[2]);
                need -= 2;
            }
            // match1
            else
            {
                slotItems.Add(g[0]);
                remainPool.Add(g[1]);
                remainPool.Add(g[2]);
                need -= 1;
            }
        }


        while (need > 0 && remainPool.Count > 0)
        {
            slotItems.Add(remainPool[0]);
            remainPool.RemoveAt(0);
            need--;
        }


        slotItems = slotItems.OrderBy(_ => UnityEngine.Random.value).ToList();


    }

    private List<List<Sprite>> DistributeSlotToGrill(List<Sprite> slotItems, List<GrillStation> grills)
    {
        List<List<Sprite>> result = new List<List<Sprite>>();

        int grillCount = grills.Count;

        for (int i = 0; i < grillCount; i++)
            result.Add(new List<Sprite>());


        slotItems = slotItems.OrderBy(_ => UnityEngine.Random.value).ToList();


        int index = 0;
        for (int i = 0; i < grillCount; i++)
        {
            if (index >= slotItems.Count) break;
            result[i].Add(slotItems[index++]);
        }


        while (index < slotItems.Count)
        {
            int randGrill = UnityEngine.Random.Range(0, grillCount);

            int cap = grills[randGrill].totalSlot.Count;

            if (result[randGrill].Count < cap)
            {
                result[randGrill].Add(slotItems[index++]);
            }
        }

        return result;
    }

    private List<List<List<Sprite>>> BuildTrayPerGrill(
    List<Sprite> pool,
    int grillCount)
    {
        List<List<List<Sprite>>> result = new();

        for (int i = 0; i < grillCount; i++)
            result.Add(new List<List<Sprite>>());

        int index = 0;

        while (index < pool.Count)
        {

            int grill = 0;
            int minTray = result[0].Count;

            for (int i = 1; i < result.Count; i++)
            {
                if (result[i].Count < minTray) 
                {
                    minTray = result[i].Count;
                    grill = i;
                }
            }

            int remain = pool.Count - index;

            int take;

            if (remain == 1) take = 1;
            else if (remain == 2) take = 2;
            else take = UnityEngine.Random.Range(2, 4);

            List<Sprite> tray = new();

            for (int j = 0; j < take; j++)
            {
                if (index >= pool.Count)
                    break;

                tray.Add(pool[index++]);
            }

            result[grill].Add(tray);
        }

        return result;
    }


    

}
