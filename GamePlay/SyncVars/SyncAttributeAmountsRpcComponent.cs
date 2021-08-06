using Photon.Pun;

public class SyncAttributeAmountsRpcComponent : BaseSyncVarRpcComponent<AttributeAmounts>
{
    public override bool HasChanges(AttributeAmounts value)
    {
        return true;
    }

    [PunRPC]
    protected virtual void RpcUpdateAttributeAmounts(AttributeAmounts value)
    {
        _value = value;
    }
}
