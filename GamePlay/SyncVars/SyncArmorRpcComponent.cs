using Photon.Pun;

public class SyncArmorRpcComponent : BaseSyncVarRpcComponent<int>
{
    [PunRPC]
    protected virtual void RpcUpdateArmor(int value)
    {
        _value = value;
    }
}
