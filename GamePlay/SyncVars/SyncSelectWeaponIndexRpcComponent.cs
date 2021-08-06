using Photon.Pun;

public class SyncSelectWeaponIndexRpcComponent : BaseSyncVarRpcComponent<int>
{
    private CharacterEntity entity;
    protected override void Awake()
    {
        base.Awake();
        entity = GetComponent<CharacterEntity>();
        onValueChange.AddListener(OnValueChange);
    }

    void OnValueChange(int value)
    {
        entity.OnUpdateSelectWeaponIndex(value);
    }

    [PunRPC]
    protected virtual void RpcUpdateSelectWeaponIndex(int value)
    {
        _value = value;
        entity.OnUpdateSelectWeaponIndex(value);
    }
}
