using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System;

using Random = UnityEngine.Random;

public class FoodScript : MonoBehaviourPunCallbacks, IPunObservable
{
    Color color;

    public GameObject sphere;
    public float foodValue;

    public event EventHandler OnPlayerCollide;

    void SetColor(Color value)
    {
        if (color != value)
        {
            color = value;
            this.sphere.GetComponent<MeshRenderer>().material.color = color;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        this.SetColor(Random.ColorHSV());
        foodValue = Random.Range(0.05f, 0.2f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider col)
    {
        if(photonView.IsMine && col.gameObject.tag == "Player")
        {
            OnPlayerCollide?.Invoke(this, null);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if(stream.IsWriting)
        {
            stream.SendNext(new Vector3(color.r, color.g, color.b));
            stream.SendNext(foodValue);
        }
        else
        {
            var vec = (Vector3)stream.ReceiveNext();
            var food = (float)stream.ReceiveNext();
            this.foodValue = food;
            SetColor(new Color(vec.x, vec.y, vec.z));
        }
    }
}
