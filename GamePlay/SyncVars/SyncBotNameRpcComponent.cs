﻿using Photon.Pun;

public class SyncBotNameRpcComponent : BaseSyncVarRpcComponent<string>
{
    public override bool HasChanges(string value)
    {
        return string.IsNullOrEmpty(_value) || base.HasChanges(value);
    }

    [PunRPC]
    protected virtual void RpcUpdateExtra(string value)
    {
        _value = value;
    }
}
