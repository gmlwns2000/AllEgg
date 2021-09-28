using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

using Random = UnityEngine.Random;
using System.Linq;

class BubblePlacer
{
    class IdxFloatComparer : IComparer<(int, float)>
    {
        public int Compare((int, float) x, (int, float) y)
        {
            return y.Item2.CompareTo(x.Item2);
        }
    }

    public static Vector3[] Place(float[] sizes)
    {
        var idxSizes = new (int, float)[sizes.Length];
        for(int i=0; i<sizes.Length; i++)
        {
            idxSizes[i] = (i, sizes[i]/2f);
        }
        Array.Sort(idxSizes, new IdxFloatComparer());

        var posList = new List<(float, Vector3)>();
        for(int i=0; i<sizes.Length; i++)
        {
            var item = idxSizes[i];
            var added = false;
            var centerDist = 0.0f;
            while (!added)
            {
                for (float j = 0; j < Math.PI * 2; j += 0.1f)
                {
                    var center = new Vector3(Mathf.Sin(j)*centerDist,0,Mathf.Cos(j)*centerDist);
                    var conflict = false;
                    foreach(var other in posList)
                    {
                        var otherCenter = other.Item2;
                        if(Vector3.Distance(otherCenter, center) < other.Item1 + item.Item2 + 0.05f)
                        {
                            conflict = true;
                            break;
                        }
                    }

                    if (!conflict)
                    {
                        added = true;
                        posList.Add((item.Item2, center));
                        break;
                    }
                }
                centerDist += 0.33f;
            }
        }

        var ret = new Vector3[sizes.Length];
        for (int i = 0; i < posList.Count; i++)
        {
            ret[idxSizes[i].Item1] = posList[i].Item2;
        }

        return ret;
    }
}

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
                var controller = InitializeBodyGameObject(item);
            }

            currentBodyIdx = -1;
            SetCurrentBodyIdx(0);
        }
    }

    BodyController InitializeBodyGameObject(GameObject body)
    {
        var controller = body.GetComponent<BodyController>();
        controller.BodyReleased += Controller_BodyReleased;
        controller.FoodEaten += Controller_FoodEaten;
        controller.BodySplitRequested += Controller_BodySplitRequested;
        controller.BodyGatherRequested += Controller_BodyGatherRequested;
        return controller;
    }

    //Following controller event calls only from local object.
    void Controller_BodySplitRequested(object sender, BodySplitEventArgs e)
    {
        var body = PhotonNetwork.Instantiate(playerBodyPrefab.name, e.position, e.rotation);
        if (body == null) throw new NullReferenceException();
        var controller = InitializeBodyGameObject(body);
        controller.SetHealth(e.health);

        //move focus to new body
        if (myBodies.Count > 0) myBodies[currentBodyIdx].GetComponent<BodyController>().SetFocus(false);
        myBodies.Add(body);
        SetCurrentBodyIdx(myBodies.Count - 1);
    }

    void Controller_BodyGatherRequested(object sender, EventArgs e)
    {
        var center = new Vector3(0, 0, 0);
        var sizes = new List<float>();
        foreach(var item in myBodies)
        {
            center += item.transform.position;
            sizes.Add(item.GetComponent<BodyController>().size);
        }
        center /= myBodies.Count;

        var poses = BubblePlacer.Place(sizes.ToArray());
        var posesCenter = poses.Aggregate(Vector3.zero, (prod, next) => prod + next) / poses.Length;
        for(int i=0; i<myBodies.Count; i++)
        {
            var item = myBodies[i];

            item.transform.position = poses[i] - posesCenter + center;
            
            var rigidbody = item.GetComponent<Rigidbody>();
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }
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
