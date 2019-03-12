using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;

public class PowerUpEntity : PunBehaviour
{
    public const float DestroyDelay = 1f;
    // We're going to respawn this power up so I decide to keep its prefab name to spawning when character triggered
    protected string _prefabName;
    public virtual string prefabName
    {
        get { return _prefabName; }
        set
        {
            if (PhotonNetwork.isMasterClient && value != prefabName)
            {
                _prefabName = value;
                photonView.RPC("RpcUpdatePrefabName", PhotonTargets.Others, value);
            }
        }
    }
    [Header("Recovery / Stats / Currencies")]
    public int hp;
    public int armor;
    public int exp;
    public InGameCurrency[] currencies;
    [Header("Effect")]
    public EffectEntity powerUpEffect;

    private bool isDead;

    private void Awake()
    {
        var collider = GetComponent<Collider>();
        collider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead)
            return;

        var character = other.GetComponent<CharacterEntity>();
        if (character != null && character.Hp > 0)
        {
            isDead = true;
            EffectEntity.PlayEffect(powerUpEffect, character.effectTransform);
            if (PhotonNetwork.isMasterClient)
            {
                character.Hp += Mathf.CeilToInt(hp * character.TotalHpRecoveryRate);
                character.Armor += Mathf.CeilToInt(armor * character.TotalArmorRecoveryRate);
                character.Exp += Mathf.CeilToInt(exp * character.TotalExpRate);
            }
            if (character.photonView.isMine && !(character is BotEntity))
            {
                foreach (var currency in currencies)
                {
                    MonetizationManager.Save.AddCurrency(currency.id, currency.amount);
                }
            }
            StartCoroutine(DestroyRoutine());
        }
    }

    IEnumerator DestroyRoutine()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.enabled = false;
        }
        yield return new WaitForSeconds(DestroyDelay);
        // Destroy this on all clients
        if (PhotonNetwork.isMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
            GameplayManager.Singleton.SpawnPowerUp(prefabName);
        }
    }

    [PunRPC]
    protected virtual void RpcUpdatePrefabName(string prefabName)
    {
        _prefabName = prefabName;
    }
}
