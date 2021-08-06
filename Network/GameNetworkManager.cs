using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;

public class GameNetworkManager : BaseNetworkGameManager
{
    public static new GameNetworkManager Singleton
    {
        get { return SimplePhotonNetworkManager.Singleton as GameNetworkManager; }
    }

    [PunRPC]
    protected override void RpcAddPlayer()
    {
        var position = Vector3.zero;
        var rotation = Quaternion.identity;
        RandomStartPoint(out position, out rotation);

        // Get character prefab
        CharacterEntity characterPrefab = GameInstance.Singleton.characterPrefab;
        if (gameRule != null && gameRule is IONetworkGameRule)
        {
            var ioGameRule = gameRule as IONetworkGameRule;
            if (ioGameRule.overrideCharacterPrefab != null)
                characterPrefab = ioGameRule.overrideCharacterPrefab;
        }
        var characterGo = PhotonNetwork.Instantiate(characterPrefab.name, position, rotation, 0);
        var character = characterGo.GetComponent<CharacterEntity>();
        // Weapons
        var savedWeapons = PlayerSave.GetWeapons();
        var selectWeapons = new List<int>();
        foreach (var savedWeapon in savedWeapons.Values)
        {
            var data = GameInstance.GetAvailableWeapon(savedWeapon);
            if (data != null)
                selectWeapons.Add(data.GetHashId());
        }
        // Custom Equipments
        var savedCustomEquipments = PlayerSave.GetCustomEquipments();
        var selectCustomEquipments = new List<int>();
        foreach (var savedCustomEquipment in savedCustomEquipments)
        {
            var data = GameInstance.GetAvailableCustomEquipment(savedCustomEquipment.Value);
            if (data != null)
                selectCustomEquipments.Add(data.GetHashId());
        }
        var headData = GameInstance.GetAvailableHead(PlayerSave.GetHead());
        var characterData = GameInstance.GetAvailableCharacter(PlayerSave.GetCharacter());
        character.CmdInit(headData != null ? headData.GetHashId() : 0,
            characterData != null ? characterData.GetHashId() : 0,
            selectWeapons.ToArray(),
            selectCustomEquipments.ToArray(),
            "");
    }

    protected override void Awake()
    {
        base.Awake();
        PhotonPeer.RegisterType(typeof(CharacterStats), 0, CharacterStats.SerializeMethod, CharacterStats.DeserializeMethod);
        PhotonPeer.RegisterType(typeof(AttributeAmounts), 1, AttributeAmounts.SerializeMethod, AttributeAmounts.DeserializeMethod);
    }

    protected override void UpdateScores(NetworkGameScore[] scores)
    {
        var uiGameplay = FindObjectOfType<UIGameplay>();
        if (uiGameplay != null)
            uiGameplay.UpdateRankings(scores);
    }

    protected override void KillNotify(string killerName, string victimName, string weaponId)
    {
        var uiGameplay = FindObjectOfType<UIGameplay>();
        if (uiGameplay != null)
            uiGameplay.KillNotify(killerName, victimName, weaponId);
    }
}
