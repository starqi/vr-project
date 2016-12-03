using UnityEngine;
using System.Collections.Generic;
using System;

public class NetworkRedirector<T> {

    public Action<T> playFunction;
    PhotonInitSetup photonStuff;

    public NetworkRedirector(PhotonInitSetup photonStuff)
    {
        this.photonStuff = photonStuff;
    }

    public void LocalGiveNote(T note)
    {
        playFunction(note);
        photonStuff.
    }

    void PhotonStuffGiveNote(T note)
    {

    }
}
