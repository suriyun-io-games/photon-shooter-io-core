using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

public class IONetworkGameRule : BaseNetworkGameRule
{
    public UIGameplay uiGameplayPrefab;
    public CharacterEntity overrideCharacterPrefab;
    public BotEntity overrideBotPrefab;
    public WeaponData[] startWeapons;

    public override bool HasOptionBotCount { get { return true; } }
    public override bool HasOptionMatchTime { get { return false; } }
    public override bool HasOptionMatchKill { get { return false; } }
    public override bool HasOptionMatchScore { get { return false; } }
    public override bool ShowZeroScoreWhenDead { get { return true; } }
    public override bool ShowZeroKillCountWhenDead { get { return true; } }
    public override bool ShowZeroAssistCountWhenDead { get { return true; } }
    public override bool ShowZeroDieCountWhenDead { get { return true; } }

    private int[] GetStartWeapons()
    {
        var selectWeapons = new int[startWeapons.Length];
        for (var i = 0; i < startWeapons.Length; ++i)
        {
            var startWeapon = startWeapons[i];
            if (startWeapon != null)
                selectWeapons[i] = startWeapon.GetHashId();
        }
        return selectWeapons;
    }

    protected override BaseNetworkGameCharacter NewBot()
    {
        var gameInstance = GameInstance.Singleton;
        var botList = gameInstance.bots;
        var bot = botList[Random.Range(0, botList.Length)];
        // Get character prefab
        BotEntity botPrefab = gameInstance.botPrefab;
        if (overrideBotPrefab != null)
            botPrefab = overrideBotPrefab;
        // Set character data
        var botGo = PhotonNetwork.InstantiateRoomObject(botPrefab.name, Vector3.zero, Quaternion.identity, 0, new object[0]);
        var botEntity = botGo.GetComponent<BotEntity>();
        botEntity.PlayerName = bot.name;
        botEntity.SelectHead = bot.GetSelectHead();
        botEntity.SelectCharacter = bot.GetSelectCharacter();
        if (startWeapons != null && startWeapons.Length > 0)
            botEntity.SelectWeapons = GetStartWeapons();
        else
            botEntity.SelectWeapons = bot.GetSelectWeapons();
        return botEntity;
    }

    public virtual void NewPlayer(CharacterEntity character, int selectHead, int selectCharacter, int[] selectWeapons, int[] selectCustomEquipments, string extra)
    {
        character.SelectHead = selectHead;
        character.SelectCharacter = selectCharacter;
        if (startWeapons != null && startWeapons.Length > 0)
            character.SelectWeapons = GetStartWeapons();
        else
            character.SelectWeapons = selectWeapons;
        character.SelectCustomEquipments = selectCustomEquipments;
        character.Extra = extra;
    }

    public override bool CanCharacterRespawn(BaseNetworkGameCharacter character, params object[] extraParams)
    {
        var gameplayManager = GameplayManager.Singleton;
        var targetCharacter = character as CharacterEntity;
        return gameplayManager.CanRespawn(targetCharacter) && Time.unscaledTime - targetCharacter.DeathTime >= gameplayManager.respawnDuration;
    }

    public override bool RespawnCharacter(BaseNetworkGameCharacter character, params object[] extraParams)
    {
        var isWatchedAds = false;
        if (extraParams.Length > 0 && extraParams[0] is bool)
            isWatchedAds = (bool)extraParams[0];

        var targetCharacter = character as CharacterEntity;
        var gameplayManager = GameplayManager.Singleton;
        // For IO Modes, character stats will be reset when dead
        if (!isWatchedAds || targetCharacter.WatchAdsCount >= gameplayManager.watchAdsRespawnAvailable)
        {
            targetCharacter.ResetScore();
            targetCharacter.ResetKillCount();
            targetCharacter.ResetAssistCount();
            targetCharacter.Exp = 0;
            targetCharacter.Level = 1;
            targetCharacter.StatPoint = 0;
            targetCharacter.WatchAdsCount = 0;
            targetCharacter.AttributeAmounts = new AttributeAmounts();
            targetCharacter.Armor = 0;
        }
        else
            ++targetCharacter.WatchAdsCount;

        return true;
    }

    public override void InitialClientObjects()
    {
        var ui = FindObjectOfType<UIGameplay>();
        if (ui == null && uiGameplayPrefab != null)
            ui = Instantiate(uiGameplayPrefab);
        if (ui != null)
            ui.gameObject.SetActive(true);
    }

    protected override List<BaseNetworkGameCharacter> GetBots()
    {
        return new List<BaseNetworkGameCharacter>(FindObjectsOfType<BotEntity>());
    }
}
