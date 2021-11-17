using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class ConnectToServer : MonoBehaviourPunCallbacks
{

    private void Start()
    {

        var names = new[] {
            "Josep", "Visual", "Hello", "World", "Wonder", "Studio", "Android", "Joe",
            "Foo", "Bar", "foobar", "Sony", "Microsoft", "Windows", "Apple", "Dog",
            "Cat", "Frog", "White", "Black", "Pick", "Slime", "Robot", "Haje", "Vaccine",
            "Steam", "Train", "Car", "Cart", "Elephant", "Flower", "Seed", "Food"
        };
        PhotonNetwork.NickName = names[UnityEngine.Random.Range(0, names.Length)];

        PhotonNetwork.ConnectUsingSettings();

    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        SceneManager.LoadScene("Lobby");
    }

}
