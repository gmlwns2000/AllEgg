using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NicknameHandler : MonoBehaviourPunCallbacks
{
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<UnityEngine.UI.InputField>().onValueChanged.AddListener((s) =>
        {
            PhotonNetwork.NickName = s;
        });
    }

    public override void OnJoinedRoom()
    {
        GetComponent<UnityEngine.UI.InputField>().text = PhotonNetwork.NickName;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
