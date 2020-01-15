﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

[RequireComponent(typeof(CharacterEntity))]
public class BRCharacterEntityExtra : MonoBehaviourPunCallbacks
{
    protected bool _isSpawned;
    public bool isSpawned
    {
        get { return _isSpawned; }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != _isSpawned)
            {
                _isSpawned = value;
                photonView.RPC("RpcUpdateIsSpawned", RpcTarget.Others, value);
            }
        }
    }
    public bool isGroundOnce { get; private set; }

    private Transform tempTransform;
    public Transform TempTransform
    {
        get
        {
            if (tempTransform == null)
                tempTransform = GetComponent<Transform>();
            return tempTransform;
        }
    }

    private CharacterEntity tempCharacterEntity;
    public CharacterEntity TempCharacterEntity
    {
        get
        {
            if (tempCharacterEntity == null)
                tempCharacterEntity = GetComponent<CharacterEntity>();
            return tempCharacterEntity;
        }
    }
    private float botRandomSpawn;
    private bool botSpawnCalled;
    private bool botDeadRemoveCalled;
    private float lastCircleCheckTime;

    public bool IsMine { get { return photonView.IsMine && !(TempCharacterEntity is BotEntity); } }

    private void Awake()
    {
        TempCharacterEntity.enabled = false;
        TempCharacterEntity.IsHidding = true;
        var brGameManager = GameplayManager.Singleton as BRGameplayManager;
        var maxRandomDist = 30f;
        if (brGameManager != null)
            maxRandomDist = brGameManager.spawnerMoveDuration * 0.25f;
        botRandomSpawn = Random.Range(0f, maxRandomDist);

        if (IsMine)
        {
            if (brGameManager != null && brGameManager.currentState != BRState.WaitingForPlayers)
                GameNetworkManager.Singleton.LeaveRoom();
        }
    }

    private void Start()
    {
        TempCharacterEntity.onDead += OnDead;
    }

    private void OnDestroy()
    {
        TempCharacterEntity.onDead -= OnDead;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        if (!PhotonNetwork.IsMasterClient)
            return;
        photonView.RPC("RpcUpdateIsSpawned", newPlayer, isSpawned);
    }

    private void Update()
    {
        var brGameManager = GameplayManager.Singleton as BRGameplayManager;
        if (brGameManager == null)
            return;
        var botEntity = TempCharacterEntity as BotEntity;
        if (PhotonNetwork.IsMasterClient)
        {
            if (brGameManager.currentState != BRState.WaitingForPlayers && Time.realtimeSinceStartup - lastCircleCheckTime >= 1f)
            {
                var currentPosition = TempTransform.position;
                currentPosition.y = 0;

                var centerPosition = brGameManager.currentCenterPosition;
                centerPosition.y = 0;
                var distance = Vector3.Distance(currentPosition, centerPosition);
                var currentRadius = brGameManager.currentRadius;
                if (distance > currentRadius)
                    TempCharacterEntity.Hp -= Mathf.CeilToInt(brGameManager.CurrentCircleHpRateDps * TempCharacterEntity.TotalHp);
                lastCircleCheckTime = Time.realtimeSinceStartup;
                if (botEntity != null)
                {
                    botEntity.isFixRandomMoveAroundPoint = currentRadius > 0 && distance > currentRadius;
                    botEntity.fixRandomMoveAroundPoint = centerPosition;
                    botEntity.fixRandomMoveAroundDistance = currentRadius;
                }
            }
        }

        if (brGameManager.currentState == BRState.WaitingForPlayers || isSpawned)
        {
            if (PhotonNetwork.IsMasterClient && !botDeadRemoveCalled && botEntity != null && TempCharacterEntity.IsDead)
            {
                botDeadRemoveCalled = true;
                StartCoroutine(BotDeadRemoveRoutine());
            }
            if (!TempCharacterEntity.CacheRigidbody.useGravity)
                TempCharacterEntity.CacheRigidbody.useGravity = true;
            if (!TempCharacterEntity.enabled)
                TempCharacterEntity.enabled = true;
            TempCharacterEntity.IsHidding = false;
        }

        switch (brGameManager.spawnType)
        {
            case BRSpawnType.BattleRoyale:
                UpdateSpawnBattleRoyale();
                break;
            case BRSpawnType.Random:
                UpdateSpawnRandom();
                break;
        }
    }

    private void UpdateSpawnBattleRoyale()
    {
        var brGameManager = GameplayManager.Singleton as BRGameplayManager;
        if (brGameManager == null)
            return;
        var botEntity = TempCharacterEntity as BotEntity;
        if (brGameManager.currentState != BRState.WaitingForPlayers && !isSpawned)
        {
            if (PhotonNetwork.IsMasterClient && !botSpawnCalled && botEntity != null && brGameManager.CanSpawnCharacter(TempCharacterEntity))
            {
                botSpawnCalled = true;
                StartCoroutine(BotSpawnRoutine());
            }
            // Hide character and disable physics while in airplane
            if (TempCharacterEntity.CacheRigidbody.useGravity)
                TempCharacterEntity.CacheRigidbody.useGravity = false;
            if (TempCharacterEntity.enabled)
                TempCharacterEntity.enabled = false;
            TempCharacterEntity.IsHidding = true;
            // Move position / rotation follow the airplane
            if (PhotonNetwork.IsMasterClient || IsMine)
            {
                TempTransform.position = brGameManager.GetSpawnerPosition();
                TempTransform.rotation = brGameManager.GetSpawnerRotation();
            }
        }
    }

    private void UpdateSpawnRandom()
    {
        var brGameManager = GameplayManager.Singleton as BRGameplayManager;
        if (brGameManager == null)
            return;

        if (brGameManager.currentState != BRState.WaitingForPlayers && !isSpawned && PhotonNetwork.IsMasterClient)
        {
            var position = TempCharacterEntity.GetSpawnPosition();
            TempCharacterEntity.CacheTransform.position = position;
            TempCharacterEntity.photonView.RPC("RpcTargetSpawn", TempCharacterEntity.photonView.Owner, position.x, position.y, position.z);
            isSpawned = true;
        }
    }

    IEnumerator BotSpawnRoutine()
    {
        yield return new WaitForSeconds(botRandomSpawn);
        ServerCharacterSpawn();
    }

    IEnumerator BotDeadRemoveRoutine()
    {
        yield return new WaitForSeconds(5f);
        PhotonNetwork.Destroy(gameObject);
    }

    private void OnDead()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        var brGameplayManager = GameplayManager.Singleton as BRGameplayManager;
        if (brGameplayManager != null)
            photonView.RPC("RpcRankResult", photonView.Owner, BaseNetworkGameManager.Singleton.CountAliveCharacters() + 1);
    }

    IEnumerator ShowRankResultRoutine(int rank)
    {
        yield return new WaitForSeconds(3f);
        var ui = UIBRGameplay.Singleton;
        if (ui != null)
            ui.ShowRankResult(rank);
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (isSpawned && !isGroundOnce && collision.impulse.y > 0)
            isGroundOnce = true;
    }

    protected virtual void OnCollisionStay(Collision collision)
    {
        if (isSpawned && !isGroundOnce && collision.impulse.y > 0)
            isGroundOnce = true;
    }

    public void ServerCharacterSpawn()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        var brGameplayManager = GameplayManager.Singleton as BRGameplayManager;
        if (!isSpawned && brGameplayManager != null)
        {
            isSpawned = true;
            photonView.RPC("RpcCharacterSpawned", RpcTarget.All, brGameplayManager.SpawnCharacter(TempCharacterEntity) + new Vector3(Random.Range(-2.5f, 2.5f), 0, Random.Range(-2.5f, 2.5f)));
        }
    }

    public void CmdCharacterSpawn()
    {
        photonView.RPC("RpcServerCharacterSpawn", RpcTarget.MasterClient);
    }

    [PunRPC]
    protected void RpcServerCharacterSpawn()
    {
        var brGameplayManager = GameplayManager.Singleton as BRGameplayManager;
        if (!isSpawned && brGameplayManager != null && brGameplayManager.CanSpawnCharacter(TempCharacterEntity))
            ServerCharacterSpawn();
    }

    [PunRPC]
    protected void RpcCharacterSpawned(Vector3 spawnPosition)
    {
        TempCharacterEntity.CacheTransform.position = spawnPosition;
        TempCharacterEntity.CacheRigidbody.useGravity = true;
        TempCharacterEntity.CacheRigidbody.isKinematic = false;
    }

    [PunRPC]
    protected virtual void RpcRankResult(int rank)
    {
        if (IsMine)
        {
            if (GameNetworkManager.Singleton.gameRule != null &&
                GameNetworkManager.Singleton.gameRule is BattleRoyaleNetworkGameRule)
                (GameNetworkManager.Singleton.gameRule as BattleRoyaleNetworkGameRule).SetRewards(rank);
            StartCoroutine(ShowRankResultRoutine(rank));
        }
    }

    [PunRPC]
    protected virtual void RpcUpdateIsSpawned(bool isSpawned)
    {
        _isSpawned = isSpawned;
    }
}
