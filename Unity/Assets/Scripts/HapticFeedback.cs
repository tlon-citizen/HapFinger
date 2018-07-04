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

public class HapticFeedback : MonoBehaviour
{
	public float vibrationIntensity=0.5f;

    public void DoFeedback()
	{
		HapticController.Instance.doRequest(vibrationIntensity);
	}

    public void StopFeedback()
	{
        HapticController.Instance.doRequest(0);
	}
}
