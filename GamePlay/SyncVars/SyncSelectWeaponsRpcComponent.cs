using Photon.Pun;

public class SyncSelectWeaponsRpcComponent : BaseSyncVarRpcComponent<int[]>
{
    private CharacterEntity entity;
    protected override void Awake()
    {
        base.Awake();
        entity = GetComponent<CharacterEntity>();
        onValueChange.AddListener(OnValueChange);
    }

    void OnValueChange(int[] value)
    {
        entity.OnUpdateSelectWeapons(value);
    }

    public override bool HasChanges(int[] value)
    {
        return true;
    }

    [PunRPC]
    protected virtual void RpcUpdateSelectWeapons(int[] value)
    {
        _value = value;
        entity.OnUpdateSelectWeapons(value);
    }
}
