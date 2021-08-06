using Photon.Pun;
using UnityEngine;

[DisallowMultipleComponent]
public class CharacterAction : MonoBehaviourPun, IPunObservable
{
    public int attackingActionId { get; set; } = -1;
    public Vector3 aimPosition { get; set; } = Vector3.zero;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsReading)
        {
            attackingActionId = (int)stream.ReceiveNext();
            aimPosition = (Vector3)stream.ReceiveNext();
        }
        else
        {
            stream.SendNext(attackingActionId);
            stream.SendNext(aimPosition);
        }
    }
}
