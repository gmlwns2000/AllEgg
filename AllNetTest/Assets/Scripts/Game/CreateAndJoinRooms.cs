using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Photon.Realtime;

public class CreateAndJoinRooms : MonoBehaviourPunCallbacks
{

    public InputField CreateInput;
    public InputField JoinInput;

    public GameObject cameraObject;
    public GameObject scoreTextboxObject;
    public GameObject playerPrefab;

    public void CreateRoom()
    {
        PhotonNetwork.CreateRoom(CreateInput.text);
    }

    public void JoinRoom()
    {
        PhotonNetwork.JoinRoom(JoinInput.text);
    }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("Game");

        Debug.Log("PUN Basics Tutorial/Launcher: OnJoinedRoom() called by PUN. Now this client is in a room.");
        Debug.Log($"IsMaster:{PhotonNetwork.IsMasterClient}, PlayerCount:{PhotonNetwork.CurrentRoom.PlayerCount}");

    }

    public override void OnPlayerEnteredRoom(Player other)
    {
        Debug.LogFormat("OnPlayerEnteredRoom() {0}", other.NickName); // not seen if you're the player connecting

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.LogFormat("OnPlayerEnteredRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient); // called before OnPlayerLeftRoom
        }
    }

    public override void OnPlayerLeftRoom(Player other)
    {
        Debug.LogFormat("OnPlayerLeftRoom() {0}", other.NickName); // seen when other disconnects

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.LogFormat("OnPlayerLeftRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient); // called before OnPlayerLeftRoom
        }
    }



}
