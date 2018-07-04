/**
 * Wearable Vibrotactile Fingercap for Tabletop/3D Interaction
 *
 *  Copyright 2017 by HCI Group - Universität Hamburg (https://www.inf.uni-hamburg.de/de/inst/ab/mci.html)
 *
 *  Licensed under "The MIT License (MIT) – military use of this product is forbidden – V 0.2".
 *  Some rights reserved. See LICENSE.
 */

/*
 *   Author: Oscar Javier Ariza Nunez <ariza@informatik.uni-hamburg.de>
 */

 using UnityEngine;
using System.Collections;

public class FingerMarker : MonoBehaviour
{
	void OnCollisionEnter(Collision c)
	{
        HapticFeedback h = c.gameObject.GetComponent<HapticFeedback>();		
		if(h!=null)
		{
			h.DoFeedback();
			//Debug.Log("DoHapticFeedback");
		}
	}

    void OnCollisionExit(Collision c)
	{
		HapticFeedback h = c.gameObject.GetComponent<HapticFeedback>();		
		if(h!=null)
		{
			h.StopFeedback();
			//Debug.Log("StopHapticFeedback");
		}
	}
}
