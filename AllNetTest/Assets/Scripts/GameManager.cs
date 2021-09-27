using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

using Random = UnityEngine.Random;

public class GameManager : MonoBehaviourPunCallbacks, IPunObservable
{
    // Start is called before the first frame update
    public List<GameObject> myBodies;
    public int currentBodyIdx = 0;

    public GameObject cameraObject;
    public GameObject scoreTextboxObject;

    public GameObject playerPrefab;
    public GameObject foodPrefab;

    public float foodCreateInterval = 0.5f;
    public int maxFoodCount = 1000;
    public int mapRange = 50;
    public float score = 0;

    int foodCount = 0;
    float foodCreateTimeElapsed = 0;

    void Start()
    {
        if (photonView.IsMine)
        {
            var me = PhotonNetwork.Instantiate(playerPrefab.name, new Vector3(0, 0.505f, 0), Quaternion.identity);
            if (me == null) throw new NullReferenceException();
            myBodies.Add(me);

            foreach (var item in myBodies)
            {
                var controller = item.GetComponent<BodyController>();
                controller.BodyReleased += Controller_BodyReleased;
                controller.FoodEaten += Controller_FoodEaten;
            }
        }
    }

    void Controller_FoodEaten(object sender, float e)
    {
        SetScore(score + e * 100);
    }

    void Controller_BodyReleased(object sender, EventArgs args)
    {
        currentBodyIdx = (currentBodyIdx + 1) % myBodies.Count;
    }

    // Update is called once per frame
    void Update()
    {
        if (!photonView.IsMine) return;

        if (myBodies.Count > 0)
        {
            var me = myBodies[currentBodyIdx];
            var controller = me.GetComponent<BodyController>();
            if (!controller.GetFocus()) { controller.SetFocus(true); }

            if (cameraObject != null)
            {
                var cameraPos = cameraObject.transform.position;
                var mePos = me.transform.position;

                cameraObject.transform.position = new Vector3(mePos.x, cameraPos.y, mePos.z);
            }
        }

        foodCreateTimeElapsed -= Time.deltaTime;
        if (foodCreateTimeElapsed < 0)
        {
            foodCreateTimeElapsed = foodCreateInterval;
            if (PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient && foodCount < maxFoodCount)
            {
                foodCount += 1;
                var newFood = PhotonNetwork.Instantiate(
                    foodPrefab.name,
                    new Vector3(Random.Range(-mapRange * 0.5f, mapRange * 0.5f), 0.2f, Random.Range(-mapRange * 0.5f, mapRange * 0.5f)),
                    Quaternion.identity
                );
                newFood.GetComponent<FoodScript>().OnPlayerCollide += (sender, args) =>
                {
                    foodCount -= 1;
                    PhotonNetwork.Destroy(newFood.GetComponent<PhotonView>());
                };
            }
        }
    }

    public void SetScore(float score)
    {
        if (this.score != score) 
        {
            this.score = score;
            if(scoreTextboxObject) this.scoreTextboxObject.GetComponent<UnityEngine.UI.Text>().text = $"Score: {(int)score}";
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if(stream.IsWriting)
        {
            stream.SendNext(score);
        }
        else
        {
            var score = (float)stream.ReceiveNext();
            SetScore(score);
        }
    }
}
