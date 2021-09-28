using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static readonly string GAME_VERSION = "1";

    public GameObject cameraObject;
    public GameObject scoreTextboxObject;

    public GameObject playerPrefab;

    public bool alwaysCreateRoom = false;

    void Start()
    {
        Connect();
    }

    public void Connect()
    {
        var names = new[] { "AinL", "boerk", "Hello", "World", "Wow", "Test", "HelloWorld", "Joe", "Foo", "Bar", "foobar", "ASCII"};
        PhotonNetwork.NickName = names[UnityEngine.Random.Range(0, names.Length)];
        if (PhotonNetwork.IsConnected)
        {
            JoinRandomRoom();
        }
        else
        {
            PhotonNetwork.GameVersion = GAME_VERSION;
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    void JoinRandomRoom()
    {
        if (alwaysCreateRoom)
        {
            PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 20 });
        }
        else
        {
            PhotonNetwork.JoinRandomRoom();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("PUN Basics Tutorial/Launcher: OnConnectedToMaster() was called by PUN");
        JoinRandomRoom();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarningFormat("PUN Basics Tutorial/Launcher: OnDisconnected() was called by PUN with reason {0}", cause);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("PUN Basics Tutorial/Launcher:OnJoinRandomFailed() was called by PUN. No random room available, so we create one.\nCalling: PhotonNetwork.CreateRoom");

        // #Critical: we failed to join a random room, maybe none exists or they are all full. No worries, we create a new room.
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 20 });
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("PUN Basics Tutorial/Launcher: OnJoinedRoom() called by PUN. Now this client is in a room.");
        Debug.Log($"IsMaster:{PhotonNetwork.IsMasterClient}, PlayerCount:{PhotonNetwork.CurrentRoom.PlayerCount}");

        var player = PhotonNetwork.Instantiate(playerPrefab.name, new Vector3(0, 0, 0), Quaternion.identity);
        if (player != null)
        {
            var manager = player.GetComponent<GameManager>();
            manager.scoreTextboxObject = scoreTextboxObject;

            var cameraWork = player.GetComponent<CameraFollowing>();
            cameraWork.cameraObject = cameraObject;
        }
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
