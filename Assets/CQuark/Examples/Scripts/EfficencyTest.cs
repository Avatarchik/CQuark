﻿using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

class EfficencyTest : MonoBehaviour
{

	Transform trans;
    public void Start()
    {
	trans = gameObject.transform;
   //     Debug.Log("Start" + this.gameObject);
        gameObject.name = "Cool name.";
    }

    public void Update()
    {
        trans.Rotate(Vector3.up, 180 * Time.deltaTime);
    }

}
