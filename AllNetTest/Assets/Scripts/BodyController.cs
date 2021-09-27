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

    public float accelPerSec = 800;
    public float rotatePerSec = 100;
    public float maxAccel = 2000;

    public float accel = 0;
    public float direction = 0;

    public float health = 1.0f;
    public float size = 1.0f;
    public float splitableHealthThreshold = 2.0f;

    public event EventHandler BodyReleased;
    public event EventHandler<BodySplitEventArgs> BodySplitRequested;
    public event EventHandler<float> FoodEaten;

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

        //handle accel
        if (Input.GetKey(KeyCode.W))
        {
            accel = Math.Min(maxAccel, accel + accelPerSec * Time.deltaTime);
            arrorCenterGameObject.transform.localScale = new Vector3(1, 1, 1 + accel / accelPerSec);
        }
        else if (Input.GetKeyUp(KeyCode.W))
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
        if((Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D)) && (Input.GetKey(KeyCode.A) && Input.GetKey(KeyCode.D)) && (this.health >= this.splitableHealthThreshold))
        {
            //call GameManager to add new body.
            var health = this.health / 2f;
            SetHealth(health);
            BodySplitRequested?.Invoke(this, new BodySplitEventArgs(this.transform.position, this.transform.rotation, health));
        }
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

