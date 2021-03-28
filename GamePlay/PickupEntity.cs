using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PickupEntity : MonoBehaviourPunCallbacks
{
    public const float DestroyDelay = 1f;
    public enum PickupType
    {
        Weapon,
        Ammo,
    }
    // We're going to respawn this item so I decide to keep its prefab name to spawning when character triggered
    protected string _prefabName;
    public virtual string prefabName
    {
        get { return _prefabName; }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != prefabName)
            {
                _prefabName = value;
                photonView.OthersRPC(RpcUpdatePrefabName, value);
            }
        }
    }
    public PickupType type;
    public WeaponData weaponData;
    public int ammoAmount;
    private bool isDead;

    public Texture IconTexture
    {
        get { return weaponData.iconTexture; }
    }

    public Texture PreviewTexture
    {
        get { return weaponData.previewTexture; }
    }

    public string Title
    {
        get { return weaponData.GetTitle(); }
    }

    public string Description
    {
        get { return weaponData.GetDescription(); }
    }

    private void Awake()
    {
        gameObject.layer = GenericUtils.IgnoreRaycastLayer;
        var collider = GetComponent<Collider>();
        collider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead)
            return;

        if (other.gameObject.layer == GenericUtils.IgnoreRaycastLayer)
            return;

        var gameplayManager = GameplayManager.Singleton;
        var character = other.GetComponent<CharacterEntity>();
        if (character != null && character.Hp > 0)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                switch (type)
                {
                    case PickupType.Weapon:
                        if (gameplayManager.autoPickup || character is BotEntity)
                            Pickup(character);
                        break;
                    case PickupType.Ammo:
                        Pickup(character);
                        break;
                }
            }

            if (!gameplayManager.autoPickup && character.photonView.IsMine && type != PickupType.Ammo)
                character.PickableEntities.Add(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isDead)
            return;

        if (other.gameObject.layer == GenericUtils.IgnoreRaycastLayer)
            return;

        var gameplayManager = GameplayManager.Singleton;
        var character = other.GetComponent<CharacterEntity>();
        if (character != null && character.Hp > 0)
        {
            if (!gameplayManager.autoPickup && character.photonView.IsMine)
                character.PickableEntities.Remove(this);
        }
    }

    private void OnDestroy()
    {
        if (BaseNetworkGameCharacter.Local != null)
            (BaseNetworkGameCharacter.Local as CharacterEntity).PickableEntities.Remove(this);
    }

    [PunRPC]
    protected virtual void RpcUpdatePrefabName(string prefabName)
    {
        _prefabName = prefabName;
    }

    public bool Pickup(CharacterEntity character)
    {
        var gameplayManager = GameplayManager.Singleton;
        var isPickedup = false;
        switch (type)
        {
            case PickupType.Weapon:
                isPickedup = character.ServerChangeSelectWeapon(weaponData, ammoAmount);
                break;
            case PickupType.Ammo:
                isPickedup = character.ServerFillWeaponAmmo(weaponData, ammoAmount);
                break;
        }
        // Destroy this on all clients
        if (isPickedup)
        {
            isDead = true;
            PhotonNetwork.Destroy(gameObject);
            if (gameplayManager.respawnPickedupItems)
                gameplayManager.SpawnPickup(prefabName);
        }
        return isPickedup;
    }
}
