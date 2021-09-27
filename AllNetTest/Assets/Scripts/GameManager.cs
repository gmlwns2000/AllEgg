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

    public GameObject scoreTextboxObject;

    public GameObject playerBodyPrefab;
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
            var me = PhotonNetwork.Instantiate(playerBodyPrefab.name, new Vector3(0, 0.505f, 0), Quaternion.identity);
            if (me == null) throw new NullReferenceException();
            myBodies.Add(me);

            foreach (var item in myBodies)
            {
                var controller = item.GetComponent<BodyController>();
                controller.BodyReleased += Controller_BodyReleased;
                controller.FoodEaten += Controller_FoodEaten;
                controller.BodySplitRequested += Controller_BodySplitRequested;
            }

            currentBodyIdx = -1;
            SetCurrentBodyIdx(0);
        }
    }

    //Following controller event calls only from local object.
    private void Controller_BodySplitRequested(object sender, BodySplitEventArgs e)
    {
        var body = PhotonNetwork.Instantiate(playerBodyPrefab.name, e.position, e.rotation);
        if (body == null) throw new NullReferenceException();
        var controller = body.GetComponent<BodyController>();
        controller.BodyReleased += Controller_BodyReleased;
        controller.FoodEaten += Controller_FoodEaten;
        controller.BodySplitRequested += Controller_BodySplitRequested;
        controller.SetHealth(e.health);

        //move focus to new body
        if (myBodies.Count > 0) myBodies[currentBodyIdx].GetComponent<BodyController>().SetFocus(false);
        myBodies.Add(body);
        SetCurrentBodyIdx(myBodies.Count - 1);
    }

    void Controller_FoodEaten(object sender, float e)
    {
        SetScore(score + e * 100);
    }

    void Controller_BodyReleased(object sender, EventArgs args)
    {
        SetCurrentBodyIdx((currentBodyIdx + 1) % myBodies.Count);
    }

    // Update is called once per frame
    void Update()
    {
        if (!photonView.IsMine) return;

        //manage bodies
        if (myBodies.Count > 0)
        {
            //set focus (activate inputs)
            var me = myBodies[currentBodyIdx];
            var controller = me.GetComponent<BodyController>();
            if (!controller.GetFocus()) { controller.SetFocus(true); }
        }

        //food timer
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

    public void SetCurrentBodyIdx(int idx)
    {
        if (currentBodyIdx != idx)
        {
            currentBodyIdx = idx;

            if (idx >= 0 && idx < myBodies.Count)
            {
                var cameraWork = GetComponent<CameraFollowing>();
                cameraWork.targetObject = myBodies[idx];
                cameraWork.activeSmoothing = true;
            }
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
