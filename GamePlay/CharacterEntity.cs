using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody))]
public class CharacterEntity : BaseNetworkGameCharacter
{
    public const float DISCONNECT_WHEN_NOT_RESPAWN_DURATION = 60;
    public const byte RPC_EFFECT_DAMAGE_SPAWN = 0;
    public const byte RPC_EFFECT_DAMAGE_HIT = 1;
    public const byte RPC_EFFECT_TRAP_HIT = 2;
    public const int MAX_EQUIPPABLE_WEAPON_AMOUNT = 10;
    public Transform damageLaunchTransform;
    public Transform effectTransform;
    public Transform characterModelTransform;
    public GameObject[] localPlayerObjects;
    public float jumpHeight = 2f;
    [Header("UI")]
    public Transform hpBarContainer;
    public Image hpFillImage;
    public Text hpText;
    public Image armorFillImage;
    public Text armorText;
    public Text nameText;
    public Text levelText;
    [Header("Effect")]
    public GameObject invincibleEffect;
    [Header("Online data")]

    #region Sync Vars
    protected int _hp;
    protected int _armor;
    protected int _exp;
    protected int _level;
    protected int _statPoint;
    protected int _watchAdsCount;
    protected string _selectCharacter;
    protected string _selectHead;
    protected string _selectWeapons;
    protected int _selectWeaponIndex;
    protected bool _isInvincible;
    protected int _attackingActionId;
    protected CharacterStats _addStats;
    protected string _extra;
    
    public virtual int hp
    {
        get { return _hp; }
        set
        {
            if (PhotonNetwork.isMasterClient && value != hp)
            {
                _hp = value;
                photonView.RPC("RpcUpdateHp", PhotonTargets.Others, value);
            }
        }
    }
    public int Hp
    {
        get { return hp; }
        set
        {
            if (!PhotonNetwork.isMasterClient)
                return;

            if (value <= 0)
            {
                value = 0;
                if (!isDead)
                {
                    photonView.RPC("RpcTargetDead", photonView.owner);
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
            if (PhotonNetwork.isMasterClient && value != armor)
            {
                _armor = value;
                photonView.RPC("RpcUpdateArmor", PhotonTargets.Others, value);
            }
        }
    }
    public int Armor
    {
        get { return armor; }
        set
        {
            if (!PhotonNetwork.isMasterClient)
                return;

            if (value <= 0)
                value = 0;

            if (value > TotalArmor)
                value = TotalArmor;
        }
    }
    public virtual int exp
    {
        get { return _exp; }
        set
        {
            if (PhotonNetwork.isMasterClient && value != exp)
            {
                _exp = value;
                photonView.RPC("RpcUpdateExp", PhotonTargets.Others, value);
            }
        }
    }
    public virtual int Exp
    {
        get { return exp; }
        set
        {
            if (!PhotonNetwork.isMasterClient)
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
            if (PhotonNetwork.isMasterClient && value != level)
            {
                _level = value;
                photonView.RPC("RpcUpdateLevel", PhotonTargets.Others, value);
            }
        }
    }
    public virtual int statPoint
    {
        get { return _statPoint; }
        set
        {
            if (PhotonNetwork.isMasterClient && value != statPoint)
            {
                _statPoint = value;
                photonView.RPC("RpcUpdateStatPoint", PhotonTargets.Others, value);
            }
        }
    }
    public virtual int watchAdsCount
    {
        get { return _watchAdsCount; }
        set
        {
            if (PhotonNetwork.isMasterClient && value != watchAdsCount)
            {
                _watchAdsCount = value;
                photonView.RPC("RpcUpdateWatchAdsCount", PhotonTargets.Others, value);
            }
        }
    }
    public virtual string selectCharacter
    {
        get { return _selectCharacter; }
        set
        {
            if (PhotonNetwork.isMasterClient && value != selectCharacter)
            {
                _selectCharacter = value;
                photonView.RPC("RpcUpdateSelectCharacter", PhotonTargets.All, value);
            }
        }
    }
    public virtual string selectHead
    {
        get { return _selectHead; }
        set
        {
            if (PhotonNetwork.isMasterClient && value != selectHead)
            {
                _selectHead = value;
                photonView.RPC("RpcUpdateSelectHead", PhotonTargets.All, value);
            }
        }
    }
    public virtual string selectWeapons
    {
        get { return _selectWeapons; }
        set
        {
            if (PhotonNetwork.isMasterClient && value != selectWeapons)
            {
                _selectWeapons = value;
                photonView.RPC("RpcUpdateSelectWeapons", PhotonTargets.All, value);
            }
        }
    }
    public virtual int selectWeaponIndex
    {
        get { return _selectWeaponIndex; }
        set
        {
            if (PhotonNetwork.isMasterClient && value != selectWeaponIndex)
            {
                _selectWeaponIndex = value;
                photonView.RPC("RpcUpdateSelectWeaponIndex", PhotonTargets.All, value);
            }
        }
    }
    public virtual bool isInvincible
    {
        get { return _isInvincible; }
        set
        {
            if (PhotonNetwork.isMasterClient && value != isInvincible)
            {
                _isInvincible = value;
                photonView.RPC("RpcUpdateIsInvincible", PhotonTargets.Others, value);
            }
        }
    }
    public virtual int attackingActionId
    {
        get { return _attackingActionId; }
        set
        {
            if (PhotonNetwork.isMasterClient && value != attackingActionId)
            {
                _attackingActionId = value;
                photonView.RPC("RpcUpdateAttackingActionId", PhotonTargets.Others, value);
            }
        }
    }
    public virtual CharacterStats addStats
    {
        get { return _addStats; }
        set
        {
            if (PhotonNetwork.isMasterClient)
            {
                _addStats = value;
                photonView.RPC("RpcUpdateAddStats", PhotonTargets.Others, JsonUtility.ToJson(value));
            }
        }
    }
    public virtual string extra
    {
        get { return _extra; }
        set
        {
            if (PhotonNetwork.isMasterClient && value != extra)
            {
                _extra = value;
                photonView.RPC("RpcUpdateExtra", PhotonTargets.Others, value);
            }
        }
    }
    #endregion

    public override bool IsDead
    {
        get { return hp <= 0; }
    }
    
    public System.Action onDead;
    public readonly Dictionary<int, PickupEntity> PickableEntities = new Dictionary<int, PickupEntity>();
    public readonly EquippedWeapon[] equippedWeapons = new EquippedWeapon[MAX_EQUIPPABLE_WEAPON_AMOUNT];

    protected Coroutine attackRoutine;
    protected Coroutine reloadRoutine;
    protected Camera targetCamera;
    protected CharacterModel characterModel;
    protected CharacterData characterData;
    protected HeadData headData;
    protected int defaultWeaponIndex = -1;
    protected bool isMobileInput;
    protected Vector2 inputMove;
    protected Vector2 inputDirection;
    protected bool inputAttack;
    protected bool inputJump;
    protected Vector3? previousPosition;
    protected Vector3 currentVelocity;

    public float startReloadTime { get; private set; }
    public float reloadDuration { get; private set; }
    public bool isReady { get; private set; }
    public bool isDead { get; private set; }
    public bool isGround { get; private set; }
    public bool isPlayingAttackAnim { get; private set; }
    public bool isReloading { get; private set; }
    public bool hasAttackInterruptReload { get; private set; }
    public float deathTime { get; private set; }
    public float invincibleTime { get; private set; }

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
            if (isHidding == value)
                return;

            isHidding = value;
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
                renderer.enabled = !isHidding;
            var canvases = GetComponentsInChildren<Canvas>();
            foreach (var canvas in canvases)
                canvas.enabled = !isHidding;
        }
    }

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
    private Rigidbody tempRigidbody;
    public Rigidbody TempRigidbody
    {
        get
        {
            if (tempRigidbody == null)
                tempRigidbody = GetComponent<Rigidbody>();
            return tempRigidbody;
        }
    }

    public CharacterStats SumAddStats
    {
        get
        {
            var stats = new CharacterStats();
            stats += addStats;
            if (headData != null)
                stats += headData.stats;
            if (characterData != null)
                stats += characterData.stats;
            if (WeaponData != null)
                stats += WeaponData.stats;
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
        if (!PhotonNetwork.isMasterClient)
            return;
        base.Init();
        hp = 0;
        armor = 0;
        exp = 0;
        level = 1;
        statPoint = 0;
        watchAdsCount = 0;
        selectCharacter = "";
        selectHead = "";
        selectWeapons = "";
        selectWeaponIndex = -1;
        isInvincible = false;
        attackingActionId = -1;
        addStats = new CharacterStats();
        extra = "";
    }

    protected override void Awake()
    {
        base.Awake();
        gameObject.layer = GameInstance.Singleton.characterLayer;
        if (damageLaunchTransform == null)
            damageLaunchTransform = TempTransform;
        if (effectTransform == null)
            effectTransform = TempTransform;
        if (characterModelTransform == null)
            characterModelTransform = TempTransform;
        foreach (var localPlayerObject in localPlayerObjects)
        {
            localPlayerObject.SetActive(false);
        }
        deathTime = Time.unscaledTime;
    }

    protected override void OnStartLocalPlayer()
    {
        if (photonView.isMine)
        {
            var followCam = FindObjectOfType<FollowCamera>();
            followCam.target = TempTransform;
            targetCamera = followCam.GetComponent<Camera>();
            var uiGameplay = FindObjectOfType<UIGameplay>();
            if (uiGameplay != null)
                uiGameplay.FadeOut();

            foreach (var localPlayerObject in localPlayerObjects)
            {
                localPlayerObject.SetActive(true);
            }
            CmdReady();
        }
    }

    public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        base.OnPhotonPlayerConnected(newPlayer);
        if (!PhotonNetwork.isMasterClient)
            return;
        photonView.RPC("RpcUpdateHp", newPlayer, hp);
        photonView.RPC("RpcUpdateArmor", newPlayer, armor);
        photonView.RPC("RpcUpdateExp", newPlayer, exp);
        photonView.RPC("RpcUpdateLevel", newPlayer, level);
        photonView.RPC("RpcUpdateStatPoint", newPlayer, statPoint);
        photonView.RPC("RpcUpdateWatchAdsCount", newPlayer, watchAdsCount);
        photonView.RPC("RpcUpdateSelectCharacter", newPlayer, selectCharacter);
        photonView.RPC("RpcUpdateSelectHead", newPlayer, selectHead);
        photonView.RPC("RpcUpdateSelectWeapons", newPlayer, selectWeapons);
        photonView.RPC("RpcUpdateSelectWeaponIndex", newPlayer, selectWeaponIndex);
        photonView.RPC("RpcUpdateIsInvincible", newPlayer, isInvincible);
        photonView.RPC("RpcUpdateAttackingActionId", newPlayer, attackingActionId);
        photonView.RPC("RpcUpdateAddStats", newPlayer, JsonUtility.ToJson(addStats));
        photonView.RPC("RpcUpdateExtra", newPlayer, extra);
    }

    protected override void Update()
    {
        base.Update();
        if (NetworkManager != null && NetworkManager.IsMatchEnded)
            return;

        if (Hp <= 0)
        {
            if (!PhotonNetwork.isMasterClient && photonView.isMine && Time.unscaledTime - deathTime >= DISCONNECT_WHEN_NOT_RESPAWN_DURATION)
                GameNetworkManager.Singleton.LeaveRoom();

            if (PhotonNetwork.isMasterClient)
                attackingActionId = -1;
        }

        if (PhotonNetwork.isMasterClient && isInvincible && Time.unscaledTime - invincibleTime >= GameplayManager.Singleton.invincibleDuration)
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
        UpdateAnimation();
        UpdateInput();
    }

    private void FixedUpdate()
    {
        if (!previousPosition.HasValue)
            previousPosition = TempTransform.position;
        var currentMove = TempTransform.position - previousPosition.Value;
        currentVelocity = currentMove / Time.deltaTime;
        previousPosition = TempTransform.position;

        if (NetworkManager != null && NetworkManager.IsMatchEnded)
            return;

        UpdateMovements();
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
        }

        if (attackingActionId >= 0 && !isPlayingAttackAnim)
            StartCoroutine(AttackRoutine(attackingActionId));
    }

    protected virtual void UpdateInput()
    {
        if (!photonView.isMine || Hp <= 0)
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

        var canAttack = Application.isMobilePlatform || !EventSystem.current.IsPointerOverGameObject();
        inputMove = Vector2.zero;
        inputDirection = Vector2.zero;
        inputAttack = false;
        inputJump = false;
        if (canControl)
        {
            inputMove = new Vector2(InputManager.GetAxis("Horizontal", false), InputManager.GetAxis("Vertical", false));
            inputJump = InputManager.GetButtonDown("Jump");
            if (isMobileInput)
            {
                inputDirection = new Vector2(InputManager.GetAxis("Mouse X", false), InputManager.GetAxis("Mouse Y", false));
                if (canAttack)
                    inputAttack = inputDirection.magnitude != 0;
            }
            else
            {
                inputDirection = (InputManager.MousePosition() - targetCamera.WorldToScreenPoint(TempTransform.position)).normalized;
                if (canAttack)
                    inputAttack = InputManager.GetButton("Fire1");
            }
            if (InputManager.GetButtonDown("Reload"))
                Reload();
            if (GameplayManager.Singleton.autoReload &&
                CurrentEquippedWeapon.currentAmmo == 0 &&
                CurrentEquippedWeapon.CanReload())
                Reload();
        }
    }

    protected virtual float GetMoveSpeed()
    {
        return TotalMoveSpeed * GameplayManager.REAL_MOVE_SPEED_RATE;
    }

    protected virtual void Move(Vector3 direction)
    {
        if (direction.magnitude != 0)
        {
            if (direction.magnitude > 1)
                direction = direction.normalized;

            var targetSpeed = GetMoveSpeed();
            var targetVelocity = direction * targetSpeed;

            // Apply a force that attempts to reach our target velocity
            Vector3 velocity = TempRigidbody.velocity;
            Vector3 velocityChange = (targetVelocity - velocity);
            velocityChange.x = Mathf.Clamp(velocityChange.x, -targetSpeed, targetSpeed);
            velocityChange.y = 0;
            velocityChange.z = Mathf.Clamp(velocityChange.z, -targetSpeed, targetSpeed);
            TempRigidbody.AddForce(velocityChange, ForceMode.VelocityChange);
        }
    }
    
    protected virtual void UpdateMovements()
    {
        if (!photonView.isMine || Hp <= 0)
            return;

        var moveDirection = new Vector3(inputMove.x, 0, inputMove.y);
        Move(moveDirection);
        Rotate(inputDirection);
        if (inputAttack)
            Attack();
        else
            StopAttack();

        var velocity = TempRigidbody.velocity;
        if (isGround && inputJump)
        {
            TempRigidbody.velocity = new Vector3(velocity.x, CalculateJumpVerticalSpeed(), velocity.z);
            isGround = false;
        }
    }

    protected virtual void OnCollisionStay(Collision collision)
    {
        isGround = true;
    }

    protected float CalculateJumpVerticalSpeed()
    {
        // From the jump height and gravity we deduce the upwards speed 
        // for the character to reach at the apex.
        return Mathf.Sqrt(2f * jumpHeight * -Physics.gravity.y);
    }

    protected void Rotate(Vector2 direction)
    {
        if (direction.magnitude != 0)
        {
            int newRotation = (int)(Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y)).eulerAngles.y + targetCamera.transform.eulerAngles.y);
            Quaternion targetRotation = Quaternion.Euler(0, newRotation, 0);
            TempTransform.rotation = targetRotation;
        }
    }
    
    public void GetDamageLaunchTransform(bool isLeftHandWeapon, out Transform launchTransform)
    {
        launchTransform = null;
        if (characterModel == null || !characterModel.TryGetDamageLaunchTransform(isLeftHandWeapon, out launchTransform))
            launchTransform = damageLaunchTransform;
    }

    protected void Attack()
    {
        if (photonView.isMine)
        {
            // If attacking while reloading, determines that it is reload interrupting
            if (isReloading && FinishReloadTimeRate > 0.8f)
                hasAttackInterruptReload = true;
        }
        if (isPlayingAttackAnim || isReloading || !CurrentEquippedWeapon.CanShoot())
            return;

        if (attackingActionId < 0 && photonView.isMine)
            CmdAttack();
    }

    protected void StopAttack()
    {
        if (attackingActionId >= 0 && photonView.isMine)
            CmdStopAttack();
    }

    protected void Reload()
    {
        if (isPlayingAttackAnim || isReloading || !CurrentEquippedWeapon.CanReload())
            return;
        if (photonView.isMine)
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
            var animator = characterModel.TempAnimator;
            AttackAnimation attackAnimation;
            if (WeaponData != null &&
                WeaponData.AttackAnimations.TryGetValue(actionId, out attackAnimation))
            {
                // Play attack animation
                animator.SetBool("DoAction", false);
                yield return new WaitForEndOfFrame();
                animator.SetBool("DoAction", true);
                animator.SetInteger("ActionID", attackAnimation.actionId);

                // Wait to launch damage entity
                var speed = attackAnimation.speed;
                var animationDuration = attackAnimation.animationDuration;
                var launchDuration = attackAnimation.launchDuration;
                if (launchDuration > animationDuration)
                    launchDuration = animationDuration;
                yield return new WaitForSeconds(launchDuration / speed);

                // Launch damage entity on server only
                if (PhotonNetwork.isMasterClient)
                {
                    WeaponData.Launch(this, attackAnimation.isAnimationForLeftHandWeapon);
                    var equippedWeapon = CurrentEquippedWeapon;
                    equippedWeapon.DecreaseAmmo();
                    equippedWeapons[selectWeaponIndex] = equippedWeapon;
                    photonView.RPC("RpcUpdateEquippedWeapons", PhotonTargets.All, selectWeaponIndex, equippedWeapon.defaultId, equippedWeapon.weaponId, equippedWeapon.currentAmmo, equippedWeapon.currentReserveAmmo);
                }

                // Random play shoot sounds
                if (WeaponData.attackFx != null && WeaponData.attackFx.Length > 0 && AudioManager.Singleton != null)
                    AudioSource.PlayClipAtPoint(WeaponData.attackFx[Random.Range(0, WeaponData.attackFx.Length - 1)], TempTransform.position, AudioManager.Singleton.sfxVolumeSetting.Level);

                // Wait till animation end
                yield return new WaitForSeconds((animationDuration - launchDuration) / speed);
            }
            // If player still attacking, random new attacking action id
            if (PhotonNetwork.isMasterClient && attackingActionId >= 0 && WeaponData != null)
                attackingActionId = WeaponData.GetRandomAttackAnimation().actionId;
            yield return new WaitForEndOfFrame();

            // Attack animation ended
            animator.SetBool("DoAction", false);
            isPlayingAttackAnim = false;
        }
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
                    AudioSource.PlayClipAtPoint(WeaponData.clipOutFx, TempTransform.position, AudioManager.Singleton.sfxVolumeSetting.Level);
                yield return new WaitForSeconds(reloadDuration);
                if (PhotonNetwork.isMasterClient)
                {
                    var equippedWeapon = CurrentEquippedWeapon;
                    equippedWeapon.Reload();
                    equippedWeapons[selectWeaponIndex] = equippedWeapon;
                    photonView.RPC("RpcUpdateEquippedWeapons", PhotonTargets.All, selectWeaponIndex, equippedWeapon.defaultId, equippedWeapon.weaponId, equippedWeapon.currentAmmo, equippedWeapon.currentReserveAmmo);
                }
                if (WeaponData.clipInFx != null && AudioManager.Singleton != null)
                    AudioSource.PlayClipAtPoint(WeaponData.clipInFx, TempTransform.position, AudioManager.Singleton.sfxVolumeSetting.Level);
            }
            // If player still attacking, random new attacking action id
            if (PhotonNetwork.isMasterClient && attackingActionId >= 0 && WeaponData != null)
                attackingActionId = WeaponData.GetRandomAttackAnimation().actionId;
            yield return new WaitForEndOfFrame();
            isReloading = false;
            if (photonView.isMine)
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
    
    public void ReceiveDamage(CharacterEntity attacker, int damage)
    {
        var gameplayManager = GameplayManager.Singleton;
        if (Hp <= 0 || isInvincible || !gameplayManager.CanReceiveDamage(this))
            return;
        
        photonView.RPC("RpcEffect", PhotonTargets.All, attacker.photonView.viewID, RPC_EFFECT_DAMAGE_HIT);
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
                photonView.RPC("RpcInterruptAttack", PhotonTargets.Others);
                photonView.RPC("RpcInterruptReload", PhotonTargets.Others);
                attacker.KilledTarget(this);
                ++dieCount;
            }
        }
    }
    
    public void KilledTarget(CharacterEntity target)
    {
        if (!PhotonNetwork.isMasterClient)
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
            photonView.RPC("RpcTargetRewardCurrency", photonView.owner, currencyId, amount);
        }
        ++killCount;
    }

    public void Heal(int amount)
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        if (Hp <= 0)
            return;

        Hp += amount;
    }

    public float GetAttackRange()
    {
        if (WeaponData == null || WeaponData.damagePrefab == null)
            return 0;
        return WeaponData.damagePrefab.GetAttackRange();
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
        if (!PhotonNetwork.isMasterClient)
            return;
        invincibleTime = Time.unscaledTime;
        isInvincible = true;
    }

    public void ServerSpawn(bool isWatchedAds)
    {
        if (!PhotonNetwork.isMasterClient)
            return;
        if (Respawn(isWatchedAds))
        {
            var gameplayManager = GameplayManager.Singleton;
            ServerInvincible();
            OnSpawn();
            var position = gameplayManager.GetCharacterSpawnPosition();
            TempTransform.position = position;
            photonView.RPC("RpcTargetSpawn", photonView.owner, position.x, position.y, position.z);
            ServerRevive();
        }
    }

    public void ServerRespawn(bool isWatchedAds)
    {
        if (!PhotonNetwork.isMasterClient)
            return;
        if (CanRespawn(isWatchedAds))
            ServerSpawn(isWatchedAds);
    }
    
    public void ServerRevive()
    {
        if (!PhotonNetwork.isMasterClient)
            return;
        for (var i = 0; i < equippedWeapons.Length; ++i)
        {
            var equippedWeapon = equippedWeapons[i];
            equippedWeapon.ChangeWeaponId(equippedWeapon.defaultId, 0);
            equippedWeapon.SetMaxAmmo();
            equippedWeapons[i] = equippedWeapon;
            photonView.RPC("RpcUpdateEquippedWeapons", PhotonTargets.All, i, equippedWeapon.defaultId, equippedWeapon.weaponId, equippedWeapon.currentAmmo, equippedWeapon.currentReserveAmmo);
        }
        selectWeaponIndex = defaultWeaponIndex;

        isPlayingAttackAnim = false;
        isReloading = false;
        isDead = false;
        Hp = TotalHp;
    }
    
    public void ServerReload()
    {
        if (!PhotonNetwork.isMasterClient)
            return;
        if (WeaponData != null)
        {
            // Start reload routine at server to reload ammo
            reloadRoutine = StartCoroutine(ReloadRoutine());
            // Call RpcReload() at clients to play reloading animation
            photonView.RPC("RpcReload", PhotonTargets.Others);
        }
    }
    
    public void ServerChangeWeapon(int index)
    {
        if (!PhotonNetwork.isMasterClient)
            return;
        var gameInstance = GameInstance.Singleton;
        if (index >= 0 && index < MAX_EQUIPPABLE_WEAPON_AMOUNT && !equippedWeapons[index].IsEmpty())
        {
            selectWeaponIndex = index;
            InterruptAttack();
            InterruptReload();
            photonView.RPC("RpcInterruptAttack", PhotonTargets.Others);
            photonView.RPC("RpcInterruptReload", PhotonTargets.Others);
        }
    }
    
    public bool ServerChangeSelectWeapon(WeaponData weaponData, int ammoAmount)
    {
        if (!PhotonNetwork.isMasterClient)
            return false;
        if (weaponData == null || string.IsNullOrEmpty(weaponData.GetId()) || weaponData.equipPosition < 0 || weaponData.equipPosition >= equippedWeapons.Length)
            return false;
        var equipPosition = weaponData.equipPosition;
        var equippedWeapon = equippedWeapons[equipPosition];
        var updated = equippedWeapon.ChangeWeaponId(weaponData.GetId(), ammoAmount);
        if (updated)
        {
            InterruptAttack();
            InterruptReload();
            photonView.RPC("RpcInterruptAttack", PhotonTargets.Others);
            photonView.RPC("RpcInterruptReload", PhotonTargets.Others);
            equippedWeapons[equipPosition] = equippedWeapon;
            photonView.RPC("RpcUpdateEquippedWeapons", PhotonTargets.All, equipPosition, equippedWeapon.defaultId, equippedWeapon.weaponId, equippedWeapon.currentAmmo, equippedWeapon.currentReserveAmmo);
            // Trigger change weapon
            if (selectWeaponIndex == equipPosition)
                selectWeaponIndex = defaultWeaponIndex;
        }
        return updated;
    }
    
    public bool ServerFillWeaponAmmo(WeaponData weaponData, int ammoAmount)
    {
        if (!PhotonNetwork.isMasterClient)
            return false;
        if (weaponData == null || weaponData.equipPosition < 0 || weaponData.equipPosition >= equippedWeapons.Length)
            return false;
        var equipPosition = weaponData.equipPosition;
        var equippedWeapon = equippedWeapons[equipPosition];
        var updated = false;
        if (equippedWeapon.weaponId.Equals(weaponData.GetId()))
        {
            updated = equippedWeapon.AddReserveAmmo(ammoAmount);
            if (updated)
            {
                equippedWeapons[equipPosition] = equippedWeapon;
                photonView.RPC("RpcUpdateEquippedWeapons", PhotonTargets.All, equipPosition, equippedWeapon.defaultId, equippedWeapon.weaponId, equippedWeapon.currentAmmo, equippedWeapon.currentReserveAmmo);
            }
        }
        return updated;
    }

    public void CmdInit(string selectHead, string selectCharacter, string selectWeapons, string extra)
    {
        photonView.RPC("RpcServerInit", PhotonTargets.MasterClient, selectHead, selectCharacter, selectWeapons, extra);
    }

    [PunRPC]
    protected void RpcServerInit(string selectHead, string selectCharacter, string selectWeapons, string extra)
    {
        Hp = TotalHp;
        this.selectHead = selectHead;
        this.selectCharacter = selectCharacter;
        this.selectWeapons = selectWeapons;
        this.extra = extra;
        var networkManager = BaseNetworkGameManager.Singleton;
        if (networkManager != null)
            networkManager.RegisterCharacter(this);
    }

    public void CmdReady()
    {
        photonView.RPC("RpcServerReady", PhotonTargets.MasterClient);
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
        photonView.RPC("RpcServerRespawn", PhotonTargets.MasterClient, isWatchedAds);
    }

    [PunRPC]
    protected void RpcServerRespawn(bool isWatchedAds)
    {
        ServerRespawn(isWatchedAds);
    }

    public void CmdAttack()
    {
        photonView.RPC("RpcServerAttack", PhotonTargets.MasterClient);
    }

    [PunRPC]
    protected void RpcServerAttack()
    {
        if (WeaponData != null)
            attackingActionId = WeaponData.GetRandomAttackAnimation().actionId;
        else
            attackingActionId = -1;
    }

    public void CmdStopAttack()
    {
        photonView.RPC("RpcServerStopAttack", PhotonTargets.MasterClient);
    }

    [PunRPC]
    protected void RpcServerStopAttack()
    {
        attackingActionId = -1;
    }
    
    public void CmdReload()
    {
        photonView.RPC("RpcServerReload", PhotonTargets.MasterClient);
    }

    [PunRPC]
    protected void RpcServerReload()
    {
        ServerReload();
    }
    
    public void CmdAddAttribute(string name)
    {
        photonView.RPC("RpcServerAddAttribute", PhotonTargets.MasterClient, name);
    }

    [PunRPC]
    protected void RpcServerAddAttribute(string name)
    {
        if (statPoint > 0)
        {
            var gameplay = GameplayManager.Singleton;
            CharacterAttributes attribute;
            if (gameplay.attributes.TryGetValue(name, out attribute))
            {
                addStats += attribute.stats;
                --statPoint;
            }
        }
    }
    
    public void CmdChangeWeapon(int index)
    {
        photonView.RPC("RpcServerChangeWeapon", PhotonTargets.MasterClient, index);
    }

    [PunRPC]
    protected void RpcServerChangeWeapon(int index)
    {
        ServerChangeWeapon(index);
    }
    
    [PunRPC]
    public void RpcReload()
    {
        if (!PhotonNetwork.isMasterClient)
            reloadRoutine = StartCoroutine(ReloadRoutine());
    }

    [PunRPC]
    public void RpcInterruptAttack()
    {
        if (!PhotonNetwork.isMasterClient)
            InterruptAttack();
    }

    [PunRPC]
    public void RpcInterruptReload()
    {
        if (!PhotonNetwork.isMasterClient)
            InterruptReload();
    }

    [PunRPC]
    protected void RpcEffect(int triggerViewId, byte effectType)
    {
        var triggerObject = PhotonView.Find(triggerViewId);

        if (triggerObject != null)
        {
            if (effectType == RPC_EFFECT_DAMAGE_SPAWN || effectType == RPC_EFFECT_DAMAGE_HIT)
            {
                var attacker = triggerObject.GetComponent<CharacterEntity>();
                if (attacker != null &&
                    attacker.WeaponData != null &&
                    attacker.WeaponData.damagePrefab != null)
                {
                    var damagePrefab = attacker.WeaponData.damagePrefab;
                    switch (effectType)
                    {
                        case RPC_EFFECT_DAMAGE_SPAWN:
                            EffectEntity.PlayEffect(damagePrefab.spawnEffectPrefab, effectTransform);
                            break;
                        case RPC_EFFECT_DAMAGE_HIT:
                            EffectEntity.PlayEffect(damagePrefab.hitEffectPrefab, effectTransform);
                            break;
                    }
                }
            }
            else if (effectType == RPC_EFFECT_TRAP_HIT)
            {
                var trap = triggerObject.GetComponent<TrapEntity>();
                if (trap != null)
                    EffectEntity.PlayEffect(trap.hitEffectPrefab, effectTransform);
            }
        }
    }

    [PunRPC]
    protected void RpcTargetDead()
    {
        deathTime = Time.unscaledTime;
    }

    [PunRPC]
    protected void RpcTargetSpawn(float x, float y, float z)
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
    protected virtual void RpcUpdateSelectCharacter(string selectCharacter)
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
        characterModel.gameObject.SetActive(true);
        UpdateCharacterModelHiddingState();
    }
    [PunRPC]
    protected virtual void RpcUpdateSelectHead(string selectHead)
    {
        _selectHead = selectHead;
        headData = GameInstance.GetHead(selectHead);
        if (characterModel != null && headData != null)
            characterModel.SetHeadModel(headData.modelObject);
        UpdateCharacterModelHiddingState();
    }
    [PunRPC]
    protected virtual void RpcUpdateSelectWeapons(string selectWeapons)
    {
        _selectWeapons = selectWeapons;
        // Changes weapon list, equip first weapon equipped position
        if (PhotonNetwork.isMasterClient)
        {
            var splitedData = selectWeapons.Split('|');
            var minEquipPos = int.MaxValue;
            for (var i = 0; i < splitedData.Length; ++i)
            {
                var singleData = splitedData[i];
                var weaponData = GameInstance.GetWeapon(singleData);

                if (weaponData == null)
                    continue;

                var equipPos = weaponData.equipPosition;
                if (minEquipPos > equipPos)
                {
                    if (defaultWeaponIndex == -1)
                        defaultWeaponIndex = i;
                    minEquipPos = equipPos;
                }

                var equippedWeapon = new EquippedWeapon();
                equippedWeapon.defaultId = weaponData.GetId();
                equippedWeapon.weaponId = weaponData.GetId();
                equippedWeapon.SetMaxAmmo();
                equippedWeapons[equipPos] = equippedWeapon;
                photonView.RPC("RpcUpdateEquippedWeapons", PhotonTargets.All, equipPos, equippedWeapon.defaultId, equippedWeapon.weaponId, equippedWeapon.currentAmmo, equippedWeapon.currentReserveAmmo);
            }
        }
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
    protected virtual void RpcUpdateAttackingActionId(int attackingActionId)
    {
        _attackingActionId = attackingActionId;
    }
    [PunRPC]
    protected virtual void RpcUpdateAddStats(string json)
    {
        _addStats = JsonUtility.FromJson<CharacterStats>(json);
    }
    [PunRPC]
    protected virtual void RpcUpdateExtra(string extra)
    {
        _extra = extra;
    }
    [PunRPC]
    protected virtual void RpcUpdateEquippedWeapons(int index, string defaultId, string weaponId, int currentAmmo, int currentReserveAmmo)
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
    #endregion
}
