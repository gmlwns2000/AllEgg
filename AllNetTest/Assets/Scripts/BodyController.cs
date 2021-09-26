using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;
using Photon.Pun;
using Photon.Realtime;

public class BodyController : MonoBehaviourPunCallbacks, IPunObservable
{
    bool focused = false;

    public GameObject arrorCenterGameObject;
    public float accelPerSec = 800;
    public float rotatePerSec = 100;
    public float maxAccel = 2000;

    public float accel = 0;
    public float direction = 0;

    public float health = 1.0f;

    public event EventHandler BodyReleased;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!focused) return;

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
    }

    void OnTriggerEnter(Collider col)
    {

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
        }
        else
        {
            this.arrorCenterGameObject.SetActive(false);
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
            health = (float)stream.ReceiveNext();
        }
    }
}

