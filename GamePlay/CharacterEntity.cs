using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Photon.Pun;
using Photon.Realtime;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CharacterMovement))]
[RequireComponent(typeof(CharacterAction))]
[DisallowMultipleComponent]
public class CharacterEntity : BaseNetworkGameCharacter
{
    public const float DISCONNECT_WHEN_NOT_RESPAWN_DURATION = 60;
    public const int MAX_EQUIPPABLE_WEAPON_AMOUNT = 10;

    public enum ViewMode
    {
        TopDown,
        ThirdPerson,
    }

    [System.Serializable]
    public class ViewModeSettings
    {
        public Vector3 targetOffsets = Vector3.zero;
        public float zoomDistance = 3f;
        public float minZoomDistance = 3f;
        public float maxZoomDistance = 3f;
        public float xRotation = 45f;
        public float minXRotation = 45f;
        public float maxXRotation = 45f;
        public float yRotation = 0f;
        public float fov = 60f;
        public float nearClipPlane = 0.3f;
        public float farClipPlane = 1000f;
    }

    public ViewMode viewMode;
    public ViewModeSettings topDownViewModeSettings;
    public ViewModeSettings thirdPersionViewModeSettings;
    public bool doNotLockCursor;
    public Transform damageLaunchTransform;
    public Transform effectTransform;
    public Transform characterModelTransform;
    public GameObject[] localPlayerObjects;
    public float dashDuration = 1.5f;
    public float dashMoveSpeedMultiplier = 1.5f;
    public float returnToMoveDirectionDelay = 1f;
    public float endActionDelay = 0.75f;
    [Header("UI")]
    public Transform hpBarContainer;
    public Image hpFillImage;
    public Text hpText;
    public Image armorFillImage;
    public Text armorText;
    public Text nameText;
    public Text levelText;
    public GameObject attackSignalObject;
    public GameObject attackSignalObjectForTeamA;
    public GameObject attackSignalObjectForTeamB;
    [Header("Effect")]
    public GameObject invincibleEffect;

    #region Sync Vars
    [Header("Online data")]
    protected int _hp = 0;
    protected int _armor = 0;
    protected int _exp = 0;
    protected int _level = 1;
    protected int _statPoint = 0;
    protected int _watchAdsCount = 0;
    protected int _selectCharacter = 0;
    protected int _selectHead = 0;
    protected int[] _selectWeapons = new int[0];
    protected int[] _selectCustomEquipments = new int[0];
    protected int _selectWeaponIndex = 0;
    protected bool _isInvincible = false;
    protected AttributeAmounts _attributeAmounts = new AttributeAmounts(0);
    protected string _extra = string.Empty;

    public virtual int hp
    {
        get { return _hp; }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != hp)
            {
                _hp = value;
                photonView.OthersRPC(RpcUpdateHp, value);
            }
        }
    }
    public int Hp
    {
        get { return hp; }
        set
        {
            if (!PhotonNetwork.IsMasterClient)
                return;

            if (value <= 0)
            {
                value = 0;
                if (!isDead)
                {
                    photonView.TargetRPC(RpcTargetDead, photonView.Owner);
                    deathTime = Time.unscaledTime;
                    ++dieCount;
                    isDead = true;
                }
            }
            if (value > TotalHp)
                value = TotalHp;
            hp = value;
        }
    }
    public virtual int armor
    {
        get { return _armor; }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != armor)
            {
                _armor = value;
                photonView.OthersRPC(RpcUpdateArmor, value);
            }
        }
    }
    public int Armor
    {
        get { return armor; }
        set
        {
            if (!PhotonNetwork.IsMasterClient)
                return;

            if (value <= 0)
                value = 0;

            if (value > TotalArmor)
                armor = TotalArmor;
        }
    }
    public virtual int exp
    {
        get { return _exp; }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != exp)
            {
                _exp = value;
                photonView.OthersRPC(RpcUpdateExp, value);
            }
        }
    }
    public virtual int Exp
    {
        get { return exp; }
        set
        {
            if (!PhotonNetwork.IsMasterClient)
                return;

            var gameplayManager = GameplayManager.Singleton;
            while (true)
            {
                if (level == gameplayManager.maxLevel)
                    break;

                var currentExp = gameplayManager.GetExp(level);
                if (value < currentExp)
                    break;
                var remainExp = value - currentExp;
                value = remainExp;
                ++level;
                statPoint += gameplayManager.addingStatPoint;
            }
            exp = value;
        }
    }
    public virtual int level
    {
        get { return _level; }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != level)
            {
                _level = value;
                photonView.OthersRPC(RpcUpdateLevel, value);
            }
        }
    }
    public virtual int statPoint
    {
        get { return _statPoint; }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != statPoint)
            {
                _statPoint = value;
                photonView.OthersRPC(RpcUpdateStatPoint, value);
            }
        }
    }
    public virtual int watchAdsCount
    {
        get { return _watchAdsCount; }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != watchAdsCount)
            {
                _watchAdsCount = value;
                photonView.OthersRPC(RpcUpdateWatchAdsCount, value);
            }
        }
    }
    public virtual int selectCharacter
    {
        get { return _selectCharacter; }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != selectCharacter)
            {
                _selectCharacter = value;
                photonView.AllRPC(RpcUpdateSelectCharacter, value);
            }
        }
    }
    public virtual int selectHead
    {
        get { return _selectHead; }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != selectHead)
            {
                _selectHead = value;
                photonView.AllRPC(RpcUpdateSelectHead, value);
            }
        }
    }
    public virtual int[] selectWeapons
    {
        get { return _selectWeapons; }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != selectWeapons)
            {
                _selectWeapons = value;
                photonView.AllRPC(RpcUpdateSelectWeapons, value);
            }
        }
    }
    public virtual int[] selectCustomEquipments
    {
        get { return _selectCustomEquipments; }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != selectCustomEquipments)
            {
                _selectCustomEquipments = value;
                photonView.AllRPC(RpcUpdateSelectCustomEquipments, value);
            }
        }
    }
    public virtual int selectWeaponIndex
    {
        get { return _selectWeaponIndex; }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != selectWeaponIndex)
            {
                _selectWeaponIndex = value;
                photonView.AllRPC(RpcUpdateSelectWeaponIndex, value);
            }
        }
    }
    public virtual bool isInvincible
    {
        get { return _isInvincible; }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != isInvincible)
            {
                _isInvincible = value;
                photonView.OthersRPC(RpcUpdateIsInvincible, value);
            }
        }
    }
    public virtual AttributeAmounts attributeAmounts
    {
        get { return _attributeAmounts; }
        set
        {
            if (PhotonNetwork.IsMasterClient)
            {
                _attributeAmounts = value;
                photonView.OthersRPC(RpcUpdateAttributeAmounts, value);
            }
        }
    }
    public virtual string extra
    {
        get { return _extra; }
        set
        {
            if (PhotonNetwork.IsMasterClient && value != extra)
            {
                _extra = value;
                photonView.OthersRPC(RpcUpdateExtra, value);
            }
        }
    }
    public virtual int attackingActionId
    {
        get { return CacheCharacterAction.attackingActionId; }
        set { CacheCharacterAction.attackingActionId = value; }
    }
    public virtual Vector3 aimPosition
    {
        get { return CacheCharacterAction.aimPosition; }
        set { CacheCharacterAction.aimPosition = value; }
    }
    #endregion

    public override bool IsDead
    {
        get { return hp <= 0; }
    }

    public override bool IsBot
    {
        get { return false; }
    }

    public System.Action onDead;
    public readonly HashSet<PickupEntity> PickableEntities = new HashSet<PickupEntity>();
    public readonly EquippedWeapon[] equippedWeapons = new EquippedWeapon[MAX_EQUIPPABLE_WEAPON_AMOUNT];

    protected ViewMode dirtyViewMode;
    protected Camera targetCamera;
    protected Vector3 cameraForward;
    protected Vector3 cameraRight;
    protected FollowCameraControls followCameraControls;
    protected Coroutine attackRoutine;
    protected Coroutine reloadRoutine;
    protected CharacterModel characterModel;
    protected CharacterData characterData;
    protected HeadData headData;
    protected Dictionary<int, CustomEquipmentData> customEquipmentDict = new Dictionary<int, CustomEquipmentData>();
    protected int defaultWeaponIndex = -1;
    protected bool isMobileInput;
    protected Vector3 inputMove;
    protected Vector3 inputDirection;
    protected bool inputAttack;
    protected bool inputJump;
    protected bool isDashing;
    protected Vector3 dashInputMove;
    protected float dashingTime;
    protected Vector3? previousPosition;
    protected Vector3 currentVelocity;
    protected float lastActionTime;
    protected Coroutine endActionDelayCoroutine;

    public float startReloadTime { get; private set; }
    public float reloadDuration { get; private set; }
    public bool isReady { get; private set; }
    public bool isDead { get; private set; }
    public bool isGrounded { get { return CacheCharacterMovement.IsGrounded; } }
    public bool isPlayingAttackAnim { get; private set; }
    public bool isReloading { get; private set; }
    public bool hasAttackInterruptReload { get; private set; }
    public float deathTime { get; private set; }
    public float invincibleTime { get; private set; }
    public bool currentActionIsForLeftHand { get; protected set; }

    public float FinishReloadTimeRate
    {
        get { return (Time.unscaledTime - startReloadTime) / reloadDuration; }
    }

    public EquippedWeapon CurrentEquippedWeapon
    {
        get
        {
            try
            { return equippedWeapons[selectWeaponIndex]; }
            catch
            { return EquippedWeapon.Empty; }
        }
    }

    public WeaponData WeaponData
    {
        get
        {
            try
            { return CurrentEquippedWeapon.WeaponData; }
            catch
            { return null; }
        }
    }

    private bool isHidding;
    public bool IsHidding
    {
        get { return isHidding; }
        set
        {
            isHidding = value;
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
                renderer.enabled = !isHidding;
            var canvases = GetComponentsInChildren<Canvas>();
            foreach (var canvas in canvases)
                canvas.enabled = !isHidding;
            var projectors = GetComponentsInChildren<Projector>();
            foreach (var projector in projectors)
                projector.enabled = !isHidding;
        }
    }

    public Transform CacheTransform { get; private set; }
    public Rigidbody CacheRigidbody { get; private set; }
    public CharacterMovement CacheCharacterMovement { get; private set; }
    public CharacterAction CacheCharacterAction { get; private set; }

    public virtual CharacterStats SumAddStats
    {
        get
        {
            var stats = new CharacterStats();
            if (headData != null)
                stats += headData.stats;
            if (characterData != null)
                stats += characterData.stats;
            if (WeaponData != null)
                stats += WeaponData.stats;
            if (customEquipmentDict != null)
            {
                foreach (var value in customEquipmentDict.Values)
                {
                    stats += value.stats;
                }
            }
            if (attributeAmounts.Dict != null)
            {
                foreach (var kv in attributeAmounts.Dict)
                {
                    CharacterAttributes attribute;
                    if (GameplayManager.Singleton.Attributes.TryGetValue(kv.Key, out attribute))
                        stats += attribute.stats * kv.Value;
                }
            }
            return stats;
        }
    }

    public int TotalHp
    {
        get
        {
            var total = GameplayManager.Singleton.baseMaxHp + SumAddStats.addMaxHp;
            return total;
        }
    }

    public int TotalArmor
    {
        get
        {
            var total = GameplayManager.Singleton.baseMaxArmor + SumAddStats.addMaxArmor;
            return total;
        }
    }
    
    public int TotalMoveSpeed
    {
        get
        {
            var total = GameplayManager.Singleton.baseMoveSpeed + SumAddStats.addMoveSpeed;
            return total;
        }
    }

    public float TotalWeaponDamageRate
    {
        get
        {
            var total = GameplayManager.Singleton.baseWeaponDamageRate + SumAddStats.addWeaponDamageRate;

            var maxValue = GameplayManager.Singleton.maxWeaponDamageRate;
            if (total < maxValue)
                return total;
            else
                return maxValue;
        }
    }

    public float TotalReduceDamageRate
    {
        get
        {
            var total = GameplayManager.Singleton.baseReduceDamageRate + SumAddStats.addReduceDamageRate;

            var maxValue = GameplayManager.Singleton.maxReduceDamageRate;
            if (total < maxValue)
                return total;
            else
                return maxValue;
        }
    }

    public float TotalArmorReduceDamage
    {
        get
        {
            var total = GameplayManager.Singleton.baseArmorReduceDamage + SumAddStats.addArmorReduceDamage;

            var maxValue = GameplayManager.Singleton.maxArmorReduceDamage;
            if (total < maxValue)
                return total;
            else
                return maxValue;
        }
    }

    public float TotalExpRate
    {
        get
        {
            var total = 1 + SumAddStats.addExpRate;
            return total;
        }
    }

    public float TotalScoreRate
    {
        get
        {
            var total = 1 + SumAddStats.addScoreRate;
            return total;
        }
    }

    public float TotalHpRecoveryRate
    {
        get
        {
            var total = 1 + SumAddStats.addHpRecoveryRate;
            return total;
        }
    }

    public float TotalArmorRecoveryRate
    {
        get
        {
            var total = 1 + SumAddStats.addArmorRecoveryRate;
            return total;
        }
    }

    public float TotalDamageRateLeechHp
    {
        get
        {
            var total = SumAddStats.addDamageRateLeechHp;
            return total;
        }
    }

    protected override void Init()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        base.Init();
        hp = 0;
        armor = 0;
        exp = 0;
        level = 1;
        statPoint = 0;
        watchAdsCount = 0;
        selectCharacter = 0;
        selectHead = 0;
        selectWeapons = new int[0];
        selectCustomEquipments = new int[0];
        selectWeaponIndex = -1;
        isInvincible = false;
        attributeAmounts = new AttributeAmounts(0);
        extra = string.Empty;
    }

    protected override void Awake()
    {
        base.Awake();
        gameObject.layer = GameInstance.Singleton.characterLayer;
        CacheTransform = transform;
        CacheRigidbody = gameObject.GetOrAddComponent<Rigidbody>();
        CacheRigidbody.useGravity = false;
        CacheCharacterMovement = gameObject.GetOrAddComponent<CharacterMovement>();
        CacheCharacterAction = gameObject.GetOrAddComponent<CharacterAction>();
        if (!photonView.ObservedComponents.Contains(CacheCharacterAction))
            photonView.ObservedComponents.Add(CacheCharacterAction);
        if (damageLaunchTransform == null)
            damageLaunchTransform = CacheTransform;
        if (effectTransform == null)
            effectTransform = CacheTransform;
        if (characterModelTransform == null)
            characterModelTransform = CacheTransform;
        foreach (var localPlayerObject in localPlayerObjects)
        {
            localPlayerObject.SetActive(false);
        }
        deathTime = Time.unscaledTime;
    }

    protected override void SyncData()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        base.SyncData();
        photonView.OthersRPC(RpcUpdateHp, hp);
        photonView.OthersRPC(RpcUpdateArmor, armor);
        photonView.OthersRPC(RpcUpdateExp, exp);
        photonView.OthersRPC(RpcUpdateLevel, level);
        photonView.OthersRPC(RpcUpdateStatPoint, statPoint);
        photonView.OthersRPC(RpcUpdateWatchAdsCount, watchAdsCount);
        photonView.OthersRPC(RpcUpdateSelectCharacter, selectCharacter);
        photonView.OthersRPC(RpcUpdateSelectHead, selectHead);
        photonView.OthersRPC(RpcUpdateSelectWeapons, selectWeapons);
        photonView.OthersRPC(RpcUpdateSelectCustomEquipments, selectCustomEquipments);
        photonView.OthersRPC(RpcUpdateSelectWeaponIndex, selectWeaponIndex);
        photonView.OthersRPC(RpcUpdateIsInvincible, isInvincible);
        photonView.OthersRPC(RpcUpdateAttributeAmounts, attributeAmounts);
        photonView.OthersRPC(RpcUpdateExtra, extra);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        base.OnPlayerEnteredRoom(newPlayer);
        photonView.TargetRPC(RpcUpdateHp, newPlayer, hp);
        photonView.TargetRPC(RpcUpdateArmor, newPlayer, armor);
        photonView.TargetRPC(RpcUpdateExp, newPlayer, exp);
        photonView.TargetRPC(RpcUpdateLevel, newPlayer, level);
        photonView.TargetRPC(RpcUpdateStatPoint, newPlayer, statPoint);
        photonView.TargetRPC(RpcUpdateWatchAdsCount, newPlayer, watchAdsCount);
        photonView.TargetRPC(RpcUpdateSelectCharacter, newPlayer, selectCharacter);
        photonView.TargetRPC(RpcUpdateSelectHead, newPlayer, selectHead);
        photonView.TargetRPC(RpcUpdateSelectWeapons, newPlayer, selectWeapons);
        photonView.TargetRPC(RpcUpdateSelectCustomEquipments, newPlayer, selectCustomEquipments);
        photonView.TargetRPC(RpcUpdateSelectWeaponIndex, newPlayer, selectWeaponIndex);
        photonView.TargetRPC(RpcUpdateIsInvincible, newPlayer, isInvincible);
        photonView.TargetRPC(RpcUpdateAttributeAmounts, newPlayer, attributeAmounts);
        photonView.TargetRPC(RpcUpdateExtra, newPlayer, extra);
    }

    protected override void OnStartLocalPlayer()
    {
        if (photonView.IsMine)
        {
            followCameraControls = FindObjectOfType<FollowCameraControls>();
            followCameraControls.target = CacheTransform;
            targetCamera = followCameraControls.CacheCamera;

            foreach (var localPlayerObject in localPlayerObjects)
            {
                localPlayerObject.SetActive(true);
            }

            StartCoroutine(DelayReady());
        }
    }

    IEnumerator DelayReady()
    {
        yield return new WaitForSeconds(0.5f);
        // Add some delay before ready to make sure that it can receive team and game rule
        // TODO: Should improve this (Or remake team system, one which made by Photon is not work well)
        var uiGameplay = FindObjectOfType<UIGameplay>();
        if (uiGameplay != null)
            uiGameplay.FadeOut();
        CmdReady();
    }

    protected override void Update()
    {
        base.Update();
        if (NetworkManager != null && NetworkManager.IsMatchEnded)
            return;

        if (Hp <= 0)
        {
            if (!PhotonNetwork.IsMasterClient && photonView.IsMine && Time.unscaledTime - deathTime >= DISCONNECT_WHEN_NOT_RESPAWN_DURATION)
                GameNetworkManager.Singleton.LeaveRoom();

            if (photonView.IsMine)
                attackingActionId = -1;
        }

        if (PhotonNetwork.IsMasterClient && isInvincible && Time.unscaledTime - invincibleTime >= GameplayManager.Singleton.invincibleDuration)
            isInvincible = false;
        if (invincibleEffect != null)
            invincibleEffect.SetActive(isInvincible);
        if (nameText != null)
            nameText.text = playerName;
        if (hpBarContainer != null)
            hpBarContainer.gameObject.SetActive(hp > 0);
        if (hpFillImage != null)
            hpFillImage.fillAmount = (float)hp / (float)TotalHp;
        if (hpText != null)
            hpText.text = hp + "/" + TotalHp;
        if (levelText != null)
            levelText.text = level.ToString("N0");
        UpdateViewMode();
        UpdateAimPosition();
        UpdateAnimation();
        UpdateInput();
        // Update dash state
        if (isDashing && Time.unscaledTime - dashingTime > dashDuration)
            isDashing = false;
        // Update attack signal
        if (attackSignalObject != null)
            attackSignalObject.SetActive(isPlayingAttackAnim);
        // TODO: Improve team codes
        if (attackSignalObjectForTeamA != null)
            attackSignalObjectForTeamA.SetActive(isPlayingAttackAnim && playerTeam == 1);
        if (attackSignalObjectForTeamB != null)
            attackSignalObjectForTeamB.SetActive(isPlayingAttackAnim && playerTeam == 2);
    }

    private void FixedUpdate()
    {
        if (!previousPosition.HasValue)
            previousPosition = CacheTransform.position;
        var currentMove = CacheTransform.position - previousPosition.Value;
        currentVelocity = currentMove / Time.deltaTime;
        previousPosition = CacheTransform.position;

        if (NetworkManager != null && NetworkManager.IsMatchEnded)
            return;

        UpdateMovements();
    }

    protected virtual void UpdateInputDirection_TopDown(bool canAttack)
    {
        if (viewMode != ViewMode.TopDown)
            return;
        doNotLockCursor = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        followCameraControls.updateRotation = false;
        followCameraControls.updateZoom = true;
        if (isMobileInput)
        {
            inputDirection = Vector3.zero;
            inputDirection += InputManager.GetAxis("Mouse Y", false) * cameraForward;
            inputDirection += InputManager.GetAxis("Mouse X", false) * cameraRight;
            if (canAttack)
                inputAttack = inputDirection.magnitude != 0;
        }
        else
        {
            inputDirection = (InputManager.MousePosition() - targetCamera.WorldToScreenPoint(CacheTransform.position)).normalized;
            inputDirection = new Vector3(inputDirection.x, 0, inputDirection.y);
            if (canAttack)
                inputAttack = InputManager.GetButton("Fire1");
        }
    }

    protected virtual void UpdateInputDirection_ThirdPerson(bool canAttack)
    {
        if (viewMode != ViewMode.ThirdPerson)
            return;
        if (isMobileInput || doNotLockCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        if (isMobileInput)
        {
            followCameraControls.updateRotation = InputManager.GetButton("CameraRotate");
            followCameraControls.updateZoom = true;
            inputDirection = Vector3.zero;
            inputDirection += InputManager.GetAxis("Mouse Y", false) * cameraForward;
            inputDirection += InputManager.GetAxis("Mouse X", false) * cameraRight;
            if (canAttack)
                inputAttack = InputManager.GetButton("Fire1");
        }
        else
        {
            followCameraControls.updateRotation = true;
            followCameraControls.updateZoom = true;
            inputDirection = (InputManager.MousePosition() - targetCamera.WorldToScreenPoint(CacheTransform.position)).normalized;
            inputDirection = new Vector3(inputDirection.x, 0, inputDirection.y);
            if (canAttack)
                inputAttack = InputManager.GetButton("Fire1");
        }
        if (inputAttack)
            lastActionTime = Time.unscaledTime;
    }

    protected virtual void UpdateViewMode(bool force = false)
    {
        if (!photonView.IsMine)
            return;

        if (force || dirtyViewMode != viewMode)
        {
            dirtyViewMode = viewMode;
            ViewModeSettings settings = viewMode == ViewMode.ThirdPerson ? thirdPersionViewModeSettings : topDownViewModeSettings;
            followCameraControls.limitXRotation = true;
            followCameraControls.limitYRotation = false;
            followCameraControls.limitZoomDistance = true;
            followCameraControls.targetOffset = settings.targetOffsets;
            followCameraControls.zoomDistance = settings.zoomDistance;
            followCameraControls.minZoomDistance = settings.minZoomDistance;
            followCameraControls.maxZoomDistance = settings.maxZoomDistance;
            followCameraControls.xRotation = settings.xRotation;
            followCameraControls.minXRotation = settings.minXRotation;
            followCameraControls.maxXRotation = settings.maxXRotation;
            followCameraControls.yRotation = settings.yRotation;
            targetCamera.fieldOfView = settings.fov;
            targetCamera.nearClipPlane = settings.nearClipPlane;
            targetCamera.farClipPlane = settings.farClipPlane;
        }
    }

    protected virtual void UpdateAimPosition()
    {
        if (!photonView.IsMine || !WeaponData)
            return;

        float attackDist = WeaponData.damagePrefab.GetAttackRange();
        switch (viewMode)
        {
            case ViewMode.TopDown:
                // Update aim position
                currentActionIsForLeftHand = CurrentActionIsForLeftHand();
                Transform launchTransform;
                GetDamageLaunchTransform(currentActionIsForLeftHand, out launchTransform);
                aimPosition = launchTransform.position + (CacheTransform.forward * attackDist);
                break;
            case ViewMode.ThirdPerson:
                float distanceToCharacter = Vector3.Distance(CacheTransform.position, followCameraControls.CacheCameraTransform.position);
                float distanceToTarget = attackDist;
                Vector3 lookAtCharacterPosition = targetCamera.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, distanceToCharacter));
                Vector3 lookAtTargetPosition = targetCamera.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, distanceToTarget));
                aimPosition = lookAtTargetPosition;
                RaycastHit[] hits = Physics.RaycastAll(lookAtCharacterPosition, (lookAtTargetPosition - lookAtCharacterPosition).normalized, attackDist);
                for (int i = 0; i < hits.Length; ++i)
                {
                    if (hits[i].transform.root != transform.root)
                        aimPosition = hits[i].point;
                }
                break;
        }
    }

    protected virtual void UpdateAnimation()
    {
        if (characterModel == null)
            return;
        var animator = characterModel.TempAnimator;
        if (animator == null)
            return;
        if (Hp <= 0)
        {
            animator.SetBool("IsDead", true);
            animator.SetFloat("JumpSpeed", 0);
            animator.SetFloat("MoveSpeed", 0);
            animator.SetBool("IsGround", true);
            animator.SetBool("IsDash", false);
        }
        else
        {
            var velocity = currentVelocity;
            var xzMagnitude = new Vector3(velocity.x, 0, velocity.z).magnitude;
            var ySpeed = velocity.y;
            animator.SetBool("IsDead", false);
            animator.SetFloat("JumpSpeed", ySpeed);
            animator.SetFloat("MoveSpeed", xzMagnitude);
            animator.SetBool("IsGround", Mathf.Abs(ySpeed) < 0.5f);
            animator.SetBool("IsDash", isDashing);
        }

        if (WeaponData != null)
            animator.SetInteger("WeaponAnimId", WeaponData.weaponAnimId);

        animator.SetBool("IsIdle", !animator.GetBool("IsDead") && !animator.GetBool("DoAction") && animator.GetBool("IsGround"));

        if (attackingActionId >= 0 && !isPlayingAttackAnim)
            StartCoroutine(AttackRoutine(attackingActionId));
    }

    protected virtual void UpdateInput()
    {
        if (!photonView.IsMine)
            return;

        bool canControl = true;
        var fields = FindObjectsOfType<InputField>();
        foreach (var field in fields)
        {
            if (field.isFocused)
            {
                canControl = false;
                break;
            }
        }

        isMobileInput = Application.isMobilePlatform;
#if UNITY_EDITOR
        isMobileInput = GameInstance.Singleton.showJoystickInEditor;
#endif
        InputManager.useMobileInputOnNonMobile = isMobileInput;

        var canAttack = isMobileInput || !EventSystem.current.IsPointerOverGameObject();
        inputMove = Vector3.zero;
        inputDirection = Vector3.zero;
        inputAttack = false;
        if (canControl)
        {
            cameraForward = followCameraControls.CacheCameraTransform.forward;
            cameraForward.y = 0;
            cameraForward = cameraForward.normalized;
            cameraRight = followCameraControls.CacheCameraTransform.right;
            cameraRight.y = 0;
            cameraRight = cameraRight.normalized;
            inputMove = Vector3.zero;
            if (!IsDead)
            {
                inputMove += cameraForward * InputManager.GetAxis("Vertical", false);
                inputMove += cameraRight * InputManager.GetAxis("Horizontal", false);
            }

            // Jump
            if (!IsDead && !inputJump)
                inputJump = InputManager.GetButtonDown("Jump") && isGrounded && !isDashing;

            if (!isDashing)
            {
                UpdateInputDirection_TopDown(canAttack);
                UpdateInputDirection_ThirdPerson(canAttack);
                if (!IsDead)
                {
                    if (InputManager.GetButtonDown("Reload"))
                        Reload();
                    if (GameplayManager.Singleton.autoReload &&
                        CurrentEquippedWeapon.currentAmmo == 0 &&
                        CurrentEquippedWeapon.CanReload())
                        Reload();
                }
                if (!IsDead)
                    isDashing = InputManager.GetButtonDown("Dash") && isGrounded;
                if (isDashing)
                {
                    if (isMobileInput)
                        dashInputMove = inputMove.normalized;
                    else
                        dashInputMove = new Vector3(CacheTransform.forward.x, 0f, CacheTransform.forward.z).normalized;
                    inputAttack = false;
                    dashingTime = Time.unscaledTime;
                    CmdDash();
                }
            }
        }
    }

    protected virtual float GetMoveSpeed()
    {
        return TotalMoveSpeed * GameplayManager.REAL_MOVE_SPEED_RATE;
    }

    protected virtual bool CurrentActionIsForLeftHand()
    {
        if (attackingActionId >= 0)
        {
            AttackAnimation attackAnimation;
            if (WeaponData.AttackAnimations.TryGetValue(attackingActionId, out attackAnimation))
                return attackAnimation.isAnimationForLeftHandWeapon;
        }
        return false;
    }

    protected virtual void Move(Vector3 direction)
    {
        if (direction.sqrMagnitude > 1)
            direction = direction.normalized;
        direction.y = 0;

        var targetSpeed = GetMoveSpeed() * (isDashing ? dashMoveSpeedMultiplier : 1f);
        CacheCharacterMovement.UpdateMovement(Time.deltaTime, targetSpeed, direction, inputJump);
    }

    protected virtual void UpdateMovements()
    {
        if (!photonView.IsMine)
            return;

        var moveDirection = inputMove;
        var dashDirection = dashInputMove;

        Move(isDashing ? dashDirection : moveDirection);
        // Turn character to move direction
        if (inputDirection.magnitude <= 0 && inputMove.magnitude > 0 || viewMode == ViewMode.ThirdPerson)
            inputDirection = inputMove;
        if (characterModel && characterModel.TempAnimator && (characterModel.TempAnimator.GetBool("DoAction") || Time.unscaledTime - lastActionTime <= returnToMoveDirectionDelay) && viewMode == ViewMode.ThirdPerson)
            inputDirection = cameraForward;
        if (!IsDead)
            Rotate(isDashing ? dashInputMove : inputDirection);

        if (!IsDead)
        {
            if (inputAttack && GameplayManager.Singleton.CanAttack(this))
                Attack();
            else
                StopAttack();
        }

        inputJump = false;
    }

    protected void Rotate(Vector3 direction)
    {
        if (direction.sqrMagnitude != 0)
            CacheTransform.rotation = Quaternion.LookRotation(direction);
    }

    public void GetDamageLaunchTransform(bool isLeftHandWeapon, out Transform launchTransform)
    {
        if (characterModel == null || !characterModel.TryGetDamageLaunchTransform(isLeftHandWeapon, out launchTransform))
            launchTransform = damageLaunchTransform;
    }

    protected void Attack()
    {
        if (photonView.IsMine)
        {
            // If attacking while reloading, determines that it is reload interrupting
            if (isReloading && FinishReloadTimeRate > 0.8f)
                hasAttackInterruptReload = true;
        }
        if (isPlayingAttackAnim || isReloading || !CurrentEquippedWeapon.CanShoot())
            return;
        
        if (attackingActionId < 0 && photonView.IsMine)
        {
            if (WeaponData != null)
                attackingActionId = WeaponData.GetRandomAttackAnimation().actionId;
            else
                attackingActionId = -1;
        }
    }

    protected void StopAttack()
    {
        if (attackingActionId >= 0 && photonView.IsMine)
            attackingActionId = -1;
    }

    protected void Reload()
    {
        if (isPlayingAttackAnim || isReloading || !CurrentEquippedWeapon.CanReload())
            return;
        if (photonView.IsMine)
            CmdReload();
    }

    IEnumerator AttackRoutine(int actionId)
    {
        if (!isPlayingAttackAnim && 
            !isReloading && 
            CurrentEquippedWeapon.CanShoot() && 
            Hp > 0 &&
            characterModel != null &&
            characterModel.TempAnimator != null)
        {
            isPlayingAttackAnim = true;
            AttackAnimation attackAnimation;
            if (WeaponData != null &&
                WeaponData.AttackAnimations.TryGetValue(actionId, out attackAnimation))
            {
                if (endActionDelayCoroutine != null)
                    StopCoroutine(endActionDelayCoroutine);
                // Play attack animation
                characterModel.TempAnimator.SetBool("DoAction", true);
                characterModel.TempAnimator.SetInteger("ActionID", attackAnimation.actionId);
                characterModel.TempAnimator.Play(0, 1, 0);

                // Wait to launch damage entity
                var speed = attackAnimation.speed;
                var animationDuration = attackAnimation.animationDuration;
                var launchDuration = attackAnimation.launchDuration;
                if (launchDuration > animationDuration)
                    launchDuration = animationDuration;
                yield return new WaitForSeconds(launchDuration / speed);

                WeaponData.Launch(this, attackAnimation.isAnimationForLeftHandWeapon, aimPosition);
                // Manage ammo at master client
                if (PhotonNetwork.IsMasterClient)
                {
                    var equippedWeapon = CurrentEquippedWeapon;
                    equippedWeapon.DecreaseAmmo();
                    equippedWeapons[selectWeaponIndex] = equippedWeapon;
                    photonView.AllRPC(RpcUpdateEquippedWeaponsAmmo, selectWeaponIndex, equippedWeapon.currentAmmo, equippedWeapon.currentReserveAmmo);
                }

                // Random play shoot sounds
                if (WeaponData.attackFx != null && WeaponData.attackFx.Length > 0 && AudioManager.Singleton != null)
                    AudioSource.PlayClipAtPoint(WeaponData.attackFx[Random.Range(0, WeaponData.attackFx.Length - 1)], CacheTransform.position, AudioManager.Singleton.sfxVolumeSetting.Level);

                // Wait till animation end
                yield return new WaitForSeconds((animationDuration - launchDuration) / speed);
            }
            // If player still attacking, random new attacking action id
            if (PhotonNetwork.IsMasterClient && attackingActionId >= 0 && WeaponData != null)
                attackingActionId = WeaponData.GetRandomAttackAnimation().actionId;

            // Attack animation ended
            endActionDelayCoroutine = StartCoroutine(DelayEndAction(endActionDelay));
            isPlayingAttackAnim = false;
        }
    }

    IEnumerator DelayEndAction(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        characterModel.TempAnimator.SetBool("DoAction", false);
    }

    IEnumerator ReloadRoutine()
    {
        hasAttackInterruptReload = false;
        if (!isReloading && CurrentEquippedWeapon.CanReload())
        {
            isReloading = true;
            if (WeaponData != null)
            {
                reloadDuration = WeaponData.reloadDuration;
                startReloadTime = Time.unscaledTime;
                if (WeaponData.clipOutFx != null && AudioManager.Singleton != null)
                    AudioSource.PlayClipAtPoint(WeaponData.clipOutFx, CacheTransform.position, AudioManager.Singleton.sfxVolumeSetting.Level);
                yield return new WaitForSeconds(reloadDuration);
                if (PhotonNetwork.IsMasterClient)
                {
                    var equippedWeapon = CurrentEquippedWeapon;
                    equippedWeapon.Reload();
                    equippedWeapons[selectWeaponIndex] = equippedWeapon;
                    photonView.AllRPC(RpcUpdateEquippedWeaponsAmmo, selectWeaponIndex, equippedWeapon.currentAmmo, equippedWeapon.currentReserveAmmo);
                }
                if (WeaponData.clipInFx != null && AudioManager.Singleton != null)
                    AudioSource.PlayClipAtPoint(WeaponData.clipInFx, CacheTransform.position, AudioManager.Singleton.sfxVolumeSetting.Level);
            }
            // If player still attacking, random new attacking action id
            if (PhotonNetwork.IsMasterClient && attackingActionId >= 0 && WeaponData != null)
                attackingActionId = WeaponData.GetRandomAttackAnimation().actionId;
            yield return new WaitForEndOfFrame();
            isReloading = false;
            if (photonView.IsMine)
            {
                // If weapon is reload one ammo at a time (like as shotgun), automatically reload more bullets
                // When there is no attack interrupt while reload
                if (WeaponData != null && WeaponData.reloadOneAmmoAtATime && CurrentEquippedWeapon.CanReload())
                {
                    if (!hasAttackInterruptReload)
                        Reload();
                    else
                        Attack();
                }
            }
        }
    }
    
    public virtual bool ReceiveDamage(CharacterEntity attacker, int damage)
    {
        if (Hp <= 0 || isInvincible)
            return false;

        if (!GameplayManager.Singleton.CanReceiveDamage(this, attacker))
            return false;

        int reduceHp = damage;
        reduceHp -= Mathf.CeilToInt(damage * TotalReduceDamageRate);
        if (Armor > 0)
        {
            if (Armor - damage >= 0)
            {
                // Armor absorb damage
                reduceHp -= Mathf.CeilToInt(damage * TotalArmorReduceDamage);
                Armor -= damage;
            }
            else
            {
                // Armor remaining less than 0, Reduce HP by remain damage without armor absorb
                // Armor absorb damage
                reduceHp -= Mathf.CeilToInt(Armor * TotalArmorReduceDamage);
                // Remain damage after armor broke
                reduceHp -= Mathf.Abs(Armor - damage);
                Armor = 0;
            }
        }
        // Avoid increasing hp by damage
        if (reduceHp < 0)
            reduceHp = 0;

        Hp -= reduceHp;
        if (attacker != null)
        {
            var leechHpAmount = Mathf.CeilToInt(attacker.TotalDamageRateLeechHp * reduceHp);
            attacker.Hp += leechHpAmount;
            if (Hp == 0)
            {
                if (onDead != null)
                    onDead.Invoke();
                InterruptAttack();
                InterruptReload();
                photonView.OthersRPC(RpcInterruptAttack);
                photonView.OthersRPC(RpcInterruptReload);
                attacker.KilledTarget(this);
                ++dieCount;
            }
        }
        return true;
    }
    
    public void KilledTarget(CharacterEntity target)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        var gameplayManager = GameplayManager.Singleton;
        var targetLevel = target.level;
        var maxLevel = gameplayManager.maxLevel;
        Exp += Mathf.CeilToInt(gameplayManager.GetRewardExp(targetLevel) * TotalExpRate);
        score += Mathf.CeilToInt(gameplayManager.GetKillScore(targetLevel) * TotalScoreRate);
        foreach (var rewardCurrency in gameplayManager.rewardCurrencies)
        {
            var currencyId = rewardCurrency.currencyId;
            var amount = rewardCurrency.amount.Calculate(targetLevel, maxLevel);
            photonView.TargetRPC(RpcTargetRewardCurrency, photonView.Owner, currencyId, amount);
        }
        ++killCount;
        GameNetworkManager.Singleton.SendKillNotify(playerName, target.playerName, WeaponData == null ? string.Empty : WeaponData.GetId());
    }

    public void Heal(int amount)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        if (Hp <= 0)
            return;

        Hp += amount;
    }

    public virtual float GetAttackRange()
    {
        if (WeaponData == null || WeaponData.damagePrefab == null)
            return 0;
        return WeaponData.damagePrefab.GetAttackRange();
    }

    public virtual Vector3 GetSpawnPosition()
    {
        return GameplayManager.Singleton.GetCharacterSpawnPosition(this);
    }

    public void UpdateCharacterModelHiddingState()
    {
        if (characterModel == null)
            return;
        var renderers = characterModel.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
            renderer.enabled = !IsHidding;
    }

    protected void InterruptAttack()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            isPlayingAttackAnim = false;
        }
    }

    protected void InterruptReload()
    {
        if (reloadRoutine != null)
        {
            StopCoroutine(reloadRoutine);
            isReloading = false;
        }
    }

    public virtual void OnSpawn() { }

    public void ServerInvincible()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        invincibleTime = Time.unscaledTime;
        isInvincible = true;
    }

    public void ServerSpawn(bool isWatchedAds)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        if (Respawn(isWatchedAds))
        {
            ServerInvincible();
            OnSpawn();
            var position = GetSpawnPosition();
            CacheTransform.position = position;
            photonView.TargetRPC(RpcTargetSpawn, photonView.Owner, position.x, position.y, position.z);
            ServerRevive();
        }
    }

    public void ServerRespawn(bool isWatchedAds)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        if (CanRespawn(isWatchedAds))
            ServerSpawn(isWatchedAds);
    }
    
    public void ServerRevive()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        for (var i = 0; i < equippedWeapons.Length; ++i)
        {
            var equippedWeapon = equippedWeapons[i];
            equippedWeapon.ChangeWeaponId(equippedWeapon.defaultId, 0);
            equippedWeapon.SetMaxAmmo();
            equippedWeapons[i] = equippedWeapon;
            photonView.AllRPC(RpcUpdateEquippedWeapons, i, equippedWeapon.defaultId, equippedWeapon.weaponId, equippedWeapon.currentAmmo, equippedWeapon.currentReserveAmmo);
        }
        selectWeaponIndex = defaultWeaponIndex;

        isPlayingAttackAnim = false;
        isReloading = false;
        isDead = false;
        Hp = TotalHp;
    }
    
    public void ServerReload()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        if (WeaponData != null)
        {
            // Start reload routine at server to reload ammo
            reloadRoutine = StartCoroutine(ReloadRoutine());
            // Call RpcReload() at clients to play reloading animation
            photonView.OthersRPC(RpcReload);
        }
    }
    
    public void ServerChangeWeapon(int index)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;
        var gameInstance = GameInstance.Singleton;
        if (index >= 0 && index < MAX_EQUIPPABLE_WEAPON_AMOUNT && !equippedWeapons[index].IsEmpty())
        {
            selectWeaponIndex = index;
            InterruptAttack();
            InterruptReload();
            photonView.OthersRPC(RpcInterruptAttack);
            photonView.OthersRPC(RpcInterruptReload);
        }
    }
    
    public bool ServerChangeSelectWeapon(WeaponData weaponData, int ammoAmount)
    {
        if (!PhotonNetwork.IsMasterClient)
            return false;
        if (weaponData == null || weaponData.equipPosition < 0 || weaponData.equipPosition >= equippedWeapons.Length)
            return false;
        var equipPosition = weaponData.equipPosition;
        var equippedWeapon = equippedWeapons[equipPosition];
        var updated = equippedWeapon.ChangeWeaponId(weaponData.GetHashId(), ammoAmount);
        if (updated)
        {
            InterruptAttack();
            InterruptReload();
            photonView.OthersRPC(RpcInterruptAttack);
            photonView.OthersRPC(RpcInterruptReload);
            equippedWeapons[equipPosition] = equippedWeapon;
            if (selectWeaponIndex == equipPosition)
                RpcUpdateSelectWeaponIndex(selectWeaponIndex);
            photonView.AllRPC(RpcUpdateEquippedWeapons, equipPosition, equippedWeapon.defaultId, equippedWeapon.weaponId, equippedWeapon.currentAmmo, equippedWeapon.currentReserveAmmo);
        }
        return updated;
    }
    
    public bool ServerFillWeaponAmmo(WeaponData weaponData, int ammoAmount)
    {
        if (!PhotonNetwork.IsMasterClient)
            return false;
        if (weaponData == null || weaponData.equipPosition < 0 || weaponData.equipPosition >= equippedWeapons.Length)
            return false;
        var equipPosition = weaponData.equipPosition;
        var equippedWeapon = equippedWeapons[equipPosition];
        var updated = false;
        if (equippedWeapon.weaponId == weaponData.GetHashId())
        {
            updated = equippedWeapon.AddReserveAmmo(ammoAmount);
            if (updated)
            {
                equippedWeapons[equipPosition] = equippedWeapon;
                photonView.AllRPC(RpcUpdateEquippedWeaponsAmmo, equipPosition, equippedWeapon.currentAmmo, equippedWeapon.currentReserveAmmo);
            }
        }
        return updated;
    }

    public void CmdInit(int selectHead, int selectCharacter, int[] selectWeapons, int[] selectCustomEquipments, string extra)
    {
        photonView.MasterRPC(RpcServerInit, selectHead, selectCharacter, selectWeapons, selectCustomEquipments, extra);
    }

    [PunRPC]
    protected void RpcServerInit(int selectHead, int selectCharacter, int[] selectWeapons, int[] selectCustomEquipments, string extra)
    {
        var alreadyInit = false;
        var networkManager = BaseNetworkGameManager.Singleton;
        if (networkManager != null)
        {
            networkManager.RegisterCharacter(this);
            var gameRule = networkManager.gameRule;
            if (gameRule != null && gameRule is IONetworkGameRule)
            {
                var ioGameRule = gameRule as IONetworkGameRule;
                ioGameRule.NewPlayer(this, selectHead, selectCharacter, selectWeapons, selectCustomEquipments, extra);
                alreadyInit = true;
            }
        }
        if (!alreadyInit)
        {
            this.selectHead = selectHead;
            this.selectCharacter = selectCharacter;
            this.selectWeapons = selectWeapons;
            this.selectCustomEquipments = selectCustomEquipments;
            this.extra = extra;
        }
        Hp = TotalHp;
    }

    public void CmdReady()
    {
        photonView.MasterRPC(RpcServerReady);
    }

    [PunRPC]
    protected void RpcServerReady()
    {
        if (!isReady)
        {
            ServerSpawn(false);
            isReady = true;
        }
    }

    public void CmdRespawn(bool isWatchedAds)
    {
        photonView.MasterRPC(RpcServerRespawn, isWatchedAds);
    }

    [PunRPC]
    protected void RpcServerRespawn(bool isWatchedAds)
    {
        ServerRespawn(isWatchedAds);
    }
    
    public void CmdReload()
    {
        photonView.MasterRPC(RpcServerReload);
    }

    [PunRPC]
    protected void RpcServerReload()
    {
        ServerReload();
    }
    
    public void CmdAddAttribute(int id)
    {
        photonView.MasterRPC(RpcServerAddAttribute, id);
    }

    [PunRPC]
    protected void RpcServerAddAttribute(int id)
    {
        if (statPoint > 0)
        {
            CharacterAttributes attribute;
            if (GameplayManager.Singleton.Attributes.TryGetValue(id, out attribute))
            {
                attributeAmounts = attributeAmounts.Increase(id, 1);
                --statPoint;
            }
        }
    }
    
    public void CmdChangeWeapon(int index)
    {
        photonView.MasterRPC(RpcServerChangeWeapon, index);
    }

    [PunRPC]
    protected void RpcServerChangeWeapon(int index)
    {
        ServerChangeWeapon(index);
    }

    public void CmdDash()
    {
        // Play dash animation on other clients
        photonView.OthersRPC(RpcDash);
    }
    
    public void CmdPickup(int viewId)
    {
        photonView.MasterRPC(RpcServerPickup, viewId);
    }

    [PunRPC]
    protected void RpcServerPickup(int viewId)
    {
        var go = PhotonView.Find(viewId);
        if (go == null)
            return;
        var pickup = go.GetComponent<PickupEntity>();
        if (pickup == null)
            return;
        pickup.Pickup(this);
    }

    [PunRPC]
    public void RpcReload()
    {
        if (!PhotonNetwork.IsMasterClient)
            reloadRoutine = StartCoroutine(ReloadRoutine());
    }

    [PunRPC]
    public void RpcInterruptAttack()
    {
        if (!PhotonNetwork.IsMasterClient)
            InterruptAttack();
    }

    [PunRPC]
    public void RpcInterruptReload()
    {
        if (!PhotonNetwork.IsMasterClient)
            InterruptReload();
    }

    [PunRPC]
    protected void RpcDash()
    {
        // Just play dash animation on another clients
        isDashing = true;
        dashingTime = Time.unscaledTime;
    }

    [PunRPC]
    protected void RpcTargetDead()
    {
        deathTime = Time.unscaledTime;
    }

    [PunRPC]
    public void RpcTargetSpawn(float x, float y, float z)
    {
        transform.position = new Vector3(x, y, z);
    }

    [PunRPC]
    protected void RpcTargetRewardCurrency(string currencyId, int amount)
    {
        MonetizationManager.Save.AddCurrency(currencyId, amount);
    }

    #region Update RPCs
    [PunRPC]
    protected virtual void RpcUpdateHp(int hp)
    {
        _hp = hp;
    }
    [PunRPC]
    protected virtual void RpcUpdateArmor(int hp)
    {
        _armor = armor;
    }
    [PunRPC]
    protected virtual void RpcUpdateExp(int exp)
    {
        _exp = exp;
    }
    [PunRPC]
    protected virtual void RpcUpdateLevel(int level)
    {
        _level = level;
    }
    [PunRPC]
    protected virtual void RpcUpdateStatPoint(int statPoint)
    {
        _statPoint = statPoint;
    }
    [PunRPC]
    protected virtual void RpcUpdateWatchAdsCount(int watchAdsCount)
    {
        _watchAdsCount = watchAdsCount;
    }
    [PunRPC]
    protected virtual void RpcUpdateSelectCharacter(int selectCharacter)
    {
        _selectCharacter = selectCharacter;

        if (characterModel != null)
            Destroy(characterModel.gameObject);
        characterData = GameInstance.GetCharacter(selectCharacter);
        if (characterData == null || characterData.modelObject == null)
            return;
        characterModel = Instantiate(characterData.modelObject, characterModelTransform);
        characterModel.transform.localPosition = Vector3.zero;
        characterModel.transform.localEulerAngles = Vector3.zero;
        characterModel.transform.localScale = Vector3.one;
        if (headData != null)
            characterModel.SetHeadModel(headData.modelObject);
        if (WeaponData != null)
            characterModel.SetWeaponModel(WeaponData.rightHandObject, WeaponData.leftHandObject, WeaponData.shieldObject);
        if (customEquipmentDict != null)
        {
            characterModel.ClearCustomModels();
            foreach (var value in customEquipmentDict.Values)
            {
                characterModel.SetCustomModel(value.containerIndex, value.modelObject);
            }
        }
        characterModel.gameObject.SetActive(true);
        UpdateCharacterModelHiddingState();
    }
    [PunRPC]
    protected virtual void RpcUpdateSelectHead(int selectHead)
    {
        _selectHead = selectHead;
        headData = GameInstance.GetHead(selectHead);
        if (characterModel != null && headData != null)
            characterModel.SetHeadModel(headData.modelObject);
        UpdateCharacterModelHiddingState();
    }
    [PunRPC]
    protected virtual void RpcUpdateSelectWeapons(int[] selectWeapons)
    {
        _selectWeapons = selectWeapons;
        // Changes weapon list, equip first weapon equipped position
        var minEquipPos = int.MaxValue;
        for (var i = 0; i < _selectWeapons.Length; ++i)
        {
            var weaponData = GameInstance.GetWeapon(_selectWeapons[i]);

            if (weaponData == null)
                continue;

            var equipPos = weaponData.equipPosition;
            if (minEquipPos > equipPos)
            {
                defaultWeaponIndex = equipPos;
                minEquipPos = equipPos;
            }

            var equippedWeapon = new EquippedWeapon();
            equippedWeapon.defaultId = weaponData.GetHashId();
            equippedWeapon.weaponId = weaponData.GetHashId();
            equippedWeapon.SetMaxAmmo();
            equippedWeapons[equipPos] = equippedWeapon;
            if (PhotonNetwork.IsMasterClient)
                photonView.AllRPC(RpcUpdateEquippedWeapons, equipPos, equippedWeapon.defaultId, equippedWeapon.weaponId, equippedWeapon.currentAmmo, equippedWeapon.currentReserveAmmo);
        }
        selectWeaponIndex = defaultWeaponIndex;
    }
    [PunRPC]
    protected virtual void RpcUpdateSelectCustomEquipments(int[] selectCustomEquipments)
    {
        _selectCustomEquipments = selectCustomEquipments;
        if (characterModel != null)
            characterModel.ClearCustomModels();
        customEquipmentDict.Clear();
        if (_selectCustomEquipments != null)
        {
            for (var i = 0; i < _selectCustomEquipments.Length; ++i)
            {
                var customEquipmentData = GameInstance.GetCustomEquipment(_selectCustomEquipments[i]);
                if (customEquipmentData != null &&
                    !customEquipmentDict.ContainsKey(customEquipmentData.containerIndex))
                {
                    customEquipmentDict[customEquipmentData.containerIndex] = customEquipmentData;
                    if (characterModel != null)
                        characterModel.SetCustomModel(customEquipmentData.containerIndex, customEquipmentData.modelObject);
                }
            }
        }
        UpdateCharacterModelHiddingState();
    }
    [PunRPC]
    protected virtual void RpcUpdateSelectWeaponIndex(int selectWeaponIndex)
    {
        _selectWeaponIndex = selectWeaponIndex;
        if (selectWeaponIndex < 0 || selectWeaponIndex >= equippedWeapons.Length)
            return;
        if (characterModel != null && WeaponData != null)
            characterModel.SetWeaponModel(WeaponData.rightHandObject, WeaponData.leftHandObject, WeaponData.shieldObject);
        UpdateCharacterModelHiddingState();
    }
    [PunRPC]
    protected virtual void RpcUpdateIsInvincible(bool isInvincible)
    {
        _isInvincible = isInvincible;
    }
    [PunRPC]
    protected virtual void RpcUpdateAttributeAmounts(AttributeAmounts attributeAmounts)
    {
        _attributeAmounts = attributeAmounts;
    }
    [PunRPC]
    protected virtual void RpcUpdateExtra(string extra)
    {
        _extra = extra;
    }
    [PunRPC]
    protected virtual void RpcUpdateEquippedWeapons(int index, int defaultId, int weaponId, int currentAmmo, int currentReserveAmmo)
    {
        if (index < 0 || index >= equippedWeapons.Length)
            return;
        var weapon = new EquippedWeapon();
        weapon.defaultId = defaultId;
        weapon.weaponId = weaponId;
        weapon.currentAmmo = currentAmmo;
        weapon.currentReserveAmmo = currentReserveAmmo;
        equippedWeapons[index] = weapon;
        if (index == selectWeaponIndex)
            RpcUpdateSelectWeaponIndex(selectWeaponIndex);
    }
    [PunRPC]
    protected virtual void RpcUpdateEquippedWeaponsAmmo(int index, int currentAmmo, int currentReserveAmmo)
    {
        if (index < 0 || index >= equippedWeapons.Length)
            return;
        var weapon = equippedWeapons[index];
        weapon.currentAmmo = currentAmmo;
        weapon.currentReserveAmmo = currentReserveAmmo;
        equippedWeapons[index] = weapon;
    }
    #endregion
}
