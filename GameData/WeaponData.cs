using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;

public class WeaponData : ItemData
{
    [Range(0, CharacterEntity.MAX_EQUIPPABLE_WEAPON_AMOUNT - 1)]
    public int equipPosition;
    public GameObject rightHandObject;
    public GameObject leftHandObject;
    public GameObject shieldObject;
    public List<AttackAnimation> attackAnimations;
    public DamageEntity damagePrefab;
    public int damage;
    [Header("Reload")]
    public bool reloadOneAmmoAtATime;
    public float reloadDuration;
    [Header("Ammo")]
    public bool unlimitAmmo;
    [Range(1, 999)]
    public int maxAmmo;
    [Range(1, 999)]
    public int maxReserveAmmo;
    [Range(1, 10)]
    public int spread;
    [Range(0, 100)]
    public float staggerX;
    [Range(0, 100)]
    public float staggerY;
    [Header("SFX")]
    public AudioClip[] attackFx;
    public AudioClip clipOutFx;
    public AudioClip clipInFx;
    public AudioClip emptyFx;
    public int weaponAnimId;
    public readonly Dictionary<int, AttackAnimation> AttackAnimations = new Dictionary<int, AttackAnimation>();

    public void Launch(CharacterEntity attacker, bool isLeftHandWeapon)
    {
        if (attacker == null || !attacker.photonView.IsMine)
            return;

        var gameNetworkManager = GameNetworkManager.Singleton;

        for (int i = 0; i < spread; ++i)
        {
            // An transform's rotation, position will be set when set `Attacker`
            // So don't worry about them before damage entity going to spawn
            // Velocity also being set when set `Attacker` too.
            var addRotationX = Random.Range(-staggerY, staggerY);
            var addRotationY = Random.Range(-staggerX, staggerX);
            var direction = attacker.CacheTransform.forward;

            var damageEntity = DamageEntity.InstantiateNewEntity(GetHashId(), isLeftHandWeapon, direction, attacker.photonView.ViewID, addRotationX, addRotationY);
            if (damageEntity)
            {
                damageEntity.weaponDamage = Mathf.CeilToInt(damage / spread);
            }

            gameNetworkManager.photonView.RPC("RpcCharacterAttack",
                RpcTarget.Others,
                GetHashId(),
                isLeftHandWeapon,
                (short)(direction.x * 100f),
                (short)(direction.y * 100f),
                (short)(direction.z * 100f),
                attacker.photonView.ViewID,
                addRotationX,
                addRotationY,
                Mathf.CeilToInt(damage / spread));
        }

        if (damagePrefab.spawnEffectPrefab)
        {
            // Instantiate spawn effect at clients
            attacker.photonView.RPC("RpcEffect", RpcTarget.All, attacker.photonView.ViewID, CharacterEntity.RPC_EFFECT_DAMAGE_SPAWN);
        }

        if (damagePrefab.muzzleEffectPrefab)
        {
            // Instantiate muzzle effect at clients
            if (!isLeftHandWeapon)
                attacker.photonView.RPC("RpcEffect", RpcTarget.All, attacker.photonView.ViewID, CharacterEntity.RPC_EFFECT_MUZZLE_SPAWN_R);
            else
                attacker.photonView.RPC("RpcEffect", RpcTarget.All, attacker.photonView.ViewID, CharacterEntity.RPC_EFFECT_MUZZLE_SPAWN_L);
        }
    }

    public void SetupAnimations()
    {
        foreach (var attackAnimation in attackAnimations)
        {
            AttackAnimations[attackAnimation.actionId] = attackAnimation;
        }
    }

    public AttackAnimation GetRandomAttackAnimation()
    {
        var list = AttackAnimations.Values.ToList();
        var randomedIndex = Random.Range(0, list.Count);
        return list[randomedIndex];
    }
}
