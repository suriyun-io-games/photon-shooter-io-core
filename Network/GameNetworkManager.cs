using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class GameNetworkManager : BaseNetworkGameManager
{
    public static new GameNetworkManager Singleton
    {
        get { return SimplePhotonNetworkManager.Singleton as GameNetworkManager; }
    }

    [PunRPC]
    protected void RpcCharacterAttack(
        int weaponId,
        bool isLeftHandWeapon,
        Vector3 position,
        Vector3 direction,
        int attackerViewId,
        float addRotationX,
        float addRotationY)
    {
        // Instantiates damage entities on clients only
        if (!PhotonNetwork.IsMasterClient)
            DamageEntity.InstantiateNewEntity(weaponId, isLeftHandWeapon, position, direction, attackerViewId, addRotationX, addRotationY);
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
        character.CmdInit(GameInstance.GetAvailableHead(PlayerSave.GetHead()).GetHashId(),
            GameInstance.GetAvailableCharacter(PlayerSave.GetCharacter()).GetHashId(),
            selectWeapons.ToArray(),
            selectCustomEquipments.ToArray(),
            "");
    }

    protected override void UpdateScores(NetworkGameScore[] scores)
    {
        var rank = 0;
        foreach (var score in scores)
        {
            ++rank;
            if (BaseNetworkGameCharacter.Local != null && score.viewId == BaseNetworkGameCharacter.Local.photonView.ViewID)
            {
                (BaseNetworkGameCharacter.Local as CharacterEntity).rank = rank;
                break;
            }
        }
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
