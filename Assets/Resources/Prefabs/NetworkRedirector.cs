﻿using UnityEngine;
using System.Collections.Generic;
using System;

// Instrument controllers should implement this, where T is some way to identify notes
public interface NetworkSupport<T>
{
    void NoteEvent(bool isDown, T keyCode);
}

// Instrument controllers should have this as a class object
public class NetworkRedirector<T>
{
    NetworkSupport<T> controller; // A reference to the parent controller
    int ownerID; // The networked Photon player ID which owns this instrument

    public bool isLocal { get; private set; }

    // This makes a local instrument
    public NetworkRedirector(NetworkSupport<T> controller) 
        : this(controller, PhotonNetwork.player.ID)
    {
    } 

    // If ownerID is the local player's ID, then this will be a local instrument, otherwise it is a network instrument
    public NetworkRedirector(NetworkSupport<T> controller, int ownerID)
    {
        this.ownerID = ownerID;
        if (ownerID == PhotonNetwork.player.ID)
        {
            PhotonNetwork.OnEventCall += NetworkEventReceive;
            isLocal = true;
        }
        else
        {
            isLocal = false;
        }
        this.controller = controller;
    }

    // This only gets called over the network if this is a network instrument
    void NetworkEventReceive(byte eventCode, object content, int senderID)
    {
        // See if the message is intended for this instrument
        if (senderID != ownerID) return;
        // If so, get the note ID and tell the controller to play that note
        T noteID = (T)content;
        controller.NoteEvent(eventCode == 1, noteID);
    }

    // This should only be called if it is a local instrument
    public void Play(bool isKeyDown, T note)
    {
        if (ownerID != PhotonNetwork.player.ID) return;
        controller.NoteEvent(isKeyDown, note); // Get the controller to play the note
        PhotonNetwork.RaiseEvent((byte)(isKeyDown ? 1 : 0), note, false, null); // Then broadcast it elsewhere
    }

    public void Destroy()
    {
        if (ownerID == PhotonNetwork.player.ID) PhotonNetwork.OnEventCall -= NetworkEventReceive;
    }
}
