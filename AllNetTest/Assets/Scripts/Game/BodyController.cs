using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;
using Photon.Pun;
using Photon.Realtime;

public class BodySplitEventArgs : EventArgs
{
    public Vector3 position;
    public Quaternion rotation;
    public float health;

    public BodySplitEventArgs(Vector3 pos, Quaternion rot, float health)
    {
        position = pos;
        rotation = rot;
        this.health = health;
    }
}

public class BodyController : MonoBehaviourPunCallbacks, IPunObservable
{
    bool focused = false;

    public GameObject arrorCenterGameObject;
    public GameObject nicknameTextGameObject;

    public float accelPerSec = 1800;
    public float rotatePerSec = 250;
    public float maxAccel = 4000;

    public float accel = 0;
    public float direction = 0;

    public float health = 1.0f;
    public float size = 1.0f;
    public float splitableHealthThreshold = 2.0f;
    public float eatOtherThresholdFactor = 1.66f;

    public event EventHandler<BodySplitEventArgs> BodySplitRequested;
    public event EventHandler BodyGatherRequested;
    public event EventHandler BodyDestroyRequested;

    public event EventHandler BodyReleased;

    public event EventHandler<float> FoodEaten;
    public event EventHandler<float> PlayerEaten;

    float blockSplitBodyTimer = 0.0f;
    float blockEatOtherTimer = 0.0f;

    public bool GetFocus() { return focused; }

    public void SetFocus(bool focused = true)
    {
        this.focused = focused;

        if (focused)
        {
            this.arrorCenterGameObject.SetActive(true);
            //this.direction = 0;
            this.accel = 0;
            arrorCenterGameObject.transform.localScale = new Vector3(1, 1, 1);
        }
        else
        {
            this.arrorCenterGameObject.SetActive(false);
        }
    }

    public void SetHealth(float health)
    {
        this.health = health;

        size = Mathf.Pow(Math.Max(0, this.health), 0.333333f);

        transform.localScale = new Vector3(size, size, size);
        this.gameObject.GetComponent<Rigidbody>().mass = size;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //always
        nicknameTextGameObject.GetComponent<TextMesh>().text = photonView.Owner.NickName;

        // run update if only and if focused or object is mine.
        if (!focused || !photonView.IsMine) return;

        //update direction
        var dirUpdated = false;
        if (Input.GetKey(KeyCode.A)) { direction -= rotatePerSec * Time.deltaTime; dirUpdated = true; }
        if (Input.GetKey(KeyCode.D)) { direction += rotatePerSec * Time.deltaTime; dirUpdated = true; }
        if (dirUpdated)
        {
            this.arrorCenterGameObject.transform.localRotation = Quaternion.Euler(0, direction, 0);
        }

        //gather body
        if(
            (Input.GetKeyUp(KeyCode.A) && Input.GetKey(KeyCode.D) && Input.GetKey(KeyCode.W)) || 
            (Input.GetKeyUp(KeyCode.W) && Input.GetKey(KeyCode.D) && Input.GetKey(KeyCode.A)) ||
            (Input.GetKeyUp(KeyCode.D) && Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.A)) ||
            (Input.GetKeyUp(KeyCode.D) && Input.GetKeyUp(KeyCode.W) && Input.GetKeyUp(KeyCode.A)) ||
            (Input.GetKeyUp(KeyCode.D) && Input.GetKeyUp(KeyCode.W) && Input.GetKey(KeyCode.A)) ||
            (Input.GetKeyUp(KeyCode.D) && Input.GetKey(KeyCode.W) && Input.GetKeyUp(KeyCode.A)) ||
            (Input.GetKey(KeyCode.D) && Input.GetKeyUp(KeyCode.W) && Input.GetKeyUp(KeyCode.A))
        )
        {
            BodyGatherRequested?.Invoke(this, null);
            blockSplitBodyTimer = 0.15f;
        }

        //handle accel
        if (Input.GetKey(KeyCode.W))
        {
            accel = Math.Min(maxAccel, accel + accelPerSec * Time.deltaTime);
            arrorCenterGameObject.transform.localScale = new Vector3(1, 1, 1 + accel / accelPerSec);
        }
        else if (Input.GetKeyUp(KeyCode.W) && (blockSplitBodyTimer < 0))
        {
            this.SetFocus(false);

            gameObject.GetComponent<Rigidbody>().AddForce(
                accel * (float)Math.Sin(direction / 180 * Math.PI),
                0,
                accel * (float)Math.Cos(direction / 180 * Math.PI)
            );

            BodyReleased?.Invoke(this, EventArgs.Empty);
        }

        //split body
        if (
            ((Input.GetKeyUp(KeyCode.A) && Input.GetKey(KeyCode.D)) || (Input.GetKeyUp(KeyCode.D) && Input.GetKey(KeyCode.A))) &&
            (this.health >= this.splitableHealthThreshold) &&
            (blockSplitBodyTimer < 0)
        )
        {
            //call GameManager to add new body.
            var health = this.health / 2f;
            SetHealth(health);
            BodySplitRequested?.Invoke(this, new BodySplitEventArgs(this.transform.position, this.transform.rotation, health));
        }

        blockSplitBodyTimer -= Time.deltaTime;

        blockEatOtherTimer -= Time.deltaTime;
    }

    void OnTriggerEnter(Collider col)
    {
        //Debug.Log($"{photonView.IsMine}, {col.gameObject.name}");
        if(photonView.IsMine && col.gameObject.name.Contains("SimpleFood"))
        {
            var foodValue = col.gameObject.GetComponent<FoodScript>().foodValue;
            SetHealth(health + foodValue);

            FoodEaten?.Invoke(this, foodValue);
        }
    }

    void OnCollisionStay(Collision col)
    {
        var other = col.gameObject;
        if(other.tag == "Player")
        {
            var photonView = other.GetComponent<PhotonView>();
            if (!photonView.IsMine)
            {
                var controller = other.GetComponent<BodyController>();
                var otherHealth = controller.health;

                if (health > otherHealth * eatOtherThresholdFactor && blockEatOtherTimer < 0)
                {
                    //perform eat other
                    Debug.Log($"RPC: Send request to {photonView.Owner.NickName}");
                    photonView.RPC("RPCRemoveRequest", RpcTarget.All, this.photonView.Owner.NickName);

                    SetHealth(health + otherHealth);
                    PlayerEaten?.Invoke(this, otherHealth);

                    //block eat other for 0.5 sec
                    blockEatOtherTimer = 0.5f;
                }
            }
        }
    }

    [PunRPC]
    void RPCRemoveRequest(string caller)
    {
        //requested
        if (photonView.IsMine)
        {
            Debug.Log($"RPC: Remove requested. called by {caller}");
            BodyDestroyRequested?.Invoke(this, null);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if(stream.IsWriting)
        {
            stream.SendNext(health);
        }
        else
        {
            var health = (float)stream.ReceiveNext();
            if(this.health != health)
            {
                SetHealth(health);
            }
        }
    }
}

