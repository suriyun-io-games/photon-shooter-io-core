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
                photonView.OthersRPC(RpcUpdateIsSpawned, value);
            }
        }
    }
    public bool isGroundOnce { get; private set; }
    public Transform CacheTransform { get; private set; }
    public CharacterEntity CacheCharacterEntity { get; private set; }
    private float botRandomSpawn;
    private bool botSpawnCalled;
    private bool botDeadRemoveCalled;
    private float lastCircleCheckTime;

    public bool IsMine { get { return photonView.IsMine && !(CacheCharacterEntity is BotEntity); } }

    private void Awake()
    {
        CacheTransform = transform;
        CacheCharacterEntity = GetComponent<CharacterEntity>();
        CacheCharacterEntity.enabled = false;
        CacheCharacterEntity.IsHidding = true;
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
        CacheCharacterEntity.onDead += OnDead;
    }

    private void OnDestroy()
    {
        CacheCharacterEntity.onDead -= OnDead;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        if (!PhotonNetwork.IsMasterClient)
            return;
        photonView.TargetRPC(RpcUpdateIsSpawned, newPlayer, isSpawned);
    }

    private void Update()
    {
        var brGameManager = GameplayManager.Singleton as BRGameplayManager;
        if (brGameManager == null)
            return;
        var botEntity = CacheCharacterEntity as BotEntity;
        if (PhotonNetwork.IsMasterClient)
        {
            if (brGameManager.currentState != BRState.WaitingForPlayers && Time.realtimeSinceStartup - lastCircleCheckTime >= 1f)
            {
                var currentPosition = CacheTransform.position;
                currentPosition.y = 0;

                var centerPosition = brGameManager.currentCenterPosition;
                centerPosition.y = 0;
                var distance = Vector3.Distance(currentPosition, centerPosition);
                var currentRadius = brGameManager.currentRadius;
                if (distance > currentRadius)
                    CacheCharacterEntity.Hp -= Mathf.CeilToInt(brGameManager.CurrentCircleHpRateDps * CacheCharacterEntity.TotalHp);
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
            if (PhotonNetwork.IsMasterClient && !botDeadRemoveCalled && botEntity != null && CacheCharacterEntity.IsDead)
            {
                botDeadRemoveCalled = true;
                StartCoroutine(BotDeadRemoveRoutine());
            }
            if (!CacheCharacterEntity.CacheRigidbody.useGravity)
                CacheCharacterEntity.CacheRigidbody.useGravity = true;
            if (!CacheCharacterEntity.enabled)
                CacheCharacterEntity.enabled = true;
            CacheCharacterEntity.IsHidding = false;
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
        var botEntity = CacheCharacterEntity as BotEntity;
        if (brGameManager.currentState != BRState.WaitingForPlayers && !isSpawned)
        {
            if (PhotonNetwork.IsMasterClient && !botSpawnCalled && botEntity != null && brGameManager.CanSpawnCharacter(CacheCharacterEntity))
            {
                botSpawnCalled = true;
                StartCoroutine(BotSpawnRoutine());
            }
            // Hide character and disable physics while in airplane
            if (CacheCharacterEntity.CacheRigidbody.useGravity)
                CacheCharacterEntity.CacheRigidbody.useGravity = false;
            if (CacheCharacterEntity.enabled)
                CacheCharacterEntity.enabled = false;
            CacheCharacterEntity.IsHidding = true;
            // Move position / rotation follow the airplane
            if (PhotonNetwork.IsMasterClient || IsMine)
            {
                CacheTransform.position = brGameManager.GetSpawnerPosition();
                CacheTransform.rotation = brGameManager.GetSpawnerRotation();
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
            var position = CacheCharacterEntity.GetSpawnPosition();
            CacheCharacterEntity.CacheTransform.position = position;
            CacheCharacterEntity.photonView.TargetRPC(CacheCharacterEntity.RpcTargetSpawn, CacheCharacterEntity.photonView.Owner, position.x, position.y, position.z);
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
            photonView.TargetRPC(RpcRankResult, photonView.Owner, BaseNetworkGameManager.Singleton.CountAliveCharacters() + 1);
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
            photonView.AllRPC(RpcCharacterSpawned, brGameplayManager.SpawnCharacter(CacheCharacterEntity) + new Vector3(Random.Range(-2.5f, 2.5f), 0, Random.Range(-2.5f, 2.5f)));
        }
    }

    public void CmdCharacterSpawn()
    {
        photonView.MasterRPC(RpcServerCharacterSpawn);
    }

    [PunRPC]
    protected void RpcServerCharacterSpawn()
    {
        var brGameplayManager = GameplayManager.Singleton as BRGameplayManager;
        if (!isSpawned && brGameplayManager != null && brGameplayManager.CanSpawnCharacter(CacheCharacterEntity))
            ServerCharacterSpawn();
    }

    [PunRPC]
    protected void RpcCharacterSpawned(Vector3 spawnPosition)
    {
        CacheCharacterEntity.CacheTransform.position = spawnPosition;
        CacheCharacterEntity.CacheRigidbody.useGravity = true;
        CacheCharacterEntity.CacheRigidbody.isKinematic = false;
    }

    [PunRPC]
    public virtual void RpcRankResult(int rank)
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
