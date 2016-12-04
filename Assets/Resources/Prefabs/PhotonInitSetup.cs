using System;
using UnityEngine;
using System.Collections.Generic;
using Photon;
using UnityEngine.UI;

using Random = UnityEngine.Random; // Name conflict

// Sets up Photon assuming LoginBtnController is set up
public class PhotonInitSetup : PunBehaviour
{
    public static readonly string roomName = "myRoom";
    public static readonly byte maxPlayers = 3;
    public static readonly Vector3[] playerPositions;
    public static readonly float triLength = 2.0f, floorHeight = 2.0f;

    static PhotonInitSetup()
    {
        // Max 3 players, and sit in a triangle
        playerPositions = new Vector3[] {
            new Vector3(-triLength, floorHeight, -triLength),
            new Vector3(triLength, floorHeight, -triLength),
            new Vector3(triLength, floorHeight, triLength)
        };
    }

    ///////////////////////////////////////////////////
    // Photon connection ritual

    void Awake()
    {
        PhotonNetwork.logLevel = PhotonLogLevel.Full;
        PhotonNetwork.autoJoinLobby = false;
        PhotonNetwork.automaticallySyncScene = true;
    }

    void Start()
    {
        PhotonNetwork.ConnectUsingSettings("5.4");
        text.text = "Loading multiplayer";
        positionIndexToID = new int?[maxPlayers];
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.playerName = LoginBtnController.loadedName;
        PhotonNetwork.JoinOrCreateRoom(roomName, new RoomOptions() { IsVisible = false, MaxPlayers =  maxPlayers }, null);
    }

    public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        Debug.Log("Player connected: " + newPlayer.name + " " + newPlayer.ID);
    }

    public override void OnDisconnectedFromPhoton()
    {
        text.text = "DISCONNECTED - PLEASE RESTART";
    }

    ///////////////////////////////////////////////////

    public Text text; // Displays some text to user
    public Image image; // Background image covering up the scene

    // Look up instrument game object from Photon's ID field
    Dictionary<int, GameObject> instrumentObjLookup = new Dictionary<int, GameObject>();
    int?[] positionIndexToID; // Given the index (0 to maxPlayers - 1), look up the Photon ID for that player

    // Creates the instrument and the visualization object
    GameObject CreateInstrument(Instrument instrument, Vector3 position, int ownerID)
    {
        if (instrument != Instrument.Drums)
        {
            var container = new GameObject();

            var instrumentPf = Resources.Load<GameObject>("Prefabs/QwertyPiano");
            var vizPf = Resources.Load<GameObject>("Prefabs/Viz");
            var instrumentObj = (GameObject)Instantiate(instrumentPf, container.transform, false);
            var vizObj = (GameObject)Instantiate(vizPf, container.transform, false);
            instrumentObj.transform.position = position;
            vizObj.transform.position = position + new Vector3(0.0f, 5.0f, 0.0f);
            var instrumentCtl = instrumentObj.GetComponent<InstrumentController>();
            var vizCtl = vizObj.GetComponent<VizController>();
            vizCtl.instrument = instrumentObj;
            instrumentCtl.SetupNetwork(ownerID);

            return container;
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    // When a new player joins, give them a number from 0 to maxPlayers - 1
    // This is stored in the array/hash-table "positionIndexToID"
    int AssignPlayerIndex(int id)
    {
        for (var i = 0; i < positionIndexToID.Length; ++i)
        {
            if (positionIndexToID[i] == null)
            {
                positionIndexToID[i] = id;
                return i;
            }
        }
        return -1; // Failed, no slots available
    }

    // Inverse operation of simply looking up "positionIndexToID", so this is "idToPositionIndex"
    int GetPlayerIndex(int id)
    {
        for (var i = 0; i < positionIndexToID.Length; ++i)
            if (positionIndexToID[i] == id)
                return i;
        return -1; // Not found
    }

    // Free an index 
    void RemovePlayerIndex(int id)
    {
        positionIndexToID[GetPlayerIndex(id)] = null;
    }

    ///////////////////////////////////////////////////
    // Making instruments upon joining rooms etc.

    public override void OnCreatedRoom()
    {
        Debug.Log("Made a room");
        // If we are the first, we just make our local instrument
        MakeLocalInstrument();
    }

    public override void OnJoinedRoom()
    {
        // If we are not the first, we are waiting for buffered messages 
        // from other network instruments, after we receive all of them,
        // then we make our own, this is to see if we've run out of space or not
        Debug.Log("Joined a room");
    }

    // Another networked (not local) player disconnects
    public override void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
    {
        Debug.Log("Player disconnected: " + otherPlayer.name + " " + otherPlayer.ID);
        if (!instrumentObjLookup.ContainsKey(otherPlayer.ID)) return; // In case this gets called multiple times
        // Remove container object owned by this player
        GameObject.Destroy(instrumentObjLookup[otherPlayer.ID]);
        // Remove ID entry
        instrumentObjLookup.Remove(otherPlayer.ID);
    }

    // Put camera on top of the instrument
    void SetupLocalCamera(GameObject instObj, Instrument instrument)
    {
        if (instrument != Instrument.Drums)
        {
            Camera.main.transform.position = instObj.transform.position + new Vector3(1.5f, 2.4f, -1.5f);
        }
        else
        {
            throw new NotImplementedException();
        }

        // Vive instructions...
    }

    void MakeLocalInstrument()
    {
        if (instrumentObjLookup.ContainsKey(PhotonNetwork.player.ID)) return; // In case this gets called multiple times
        // Assign an index
        int index = AssignPlayerIndex(PhotonNetwork.player.ID);
        if (index == -1)
        {
            text.text = "Unexpected server is full error";
            throw new Exception(text.text);
        }
        // Make the local instrument
        var instObj = CreateInstrument(LoginBtnController.loadedInstrument, playerPositions[index], PhotonNetwork.player.ID);
        instrumentObjLookup[PhotonNetwork.player.ID] = instObj;
        // Put camera on top of the instrument
        SetupLocalCamera(instObj.transform.FindChild("QwertyPiano(Clone)").gameObject, LoginBtnController.loadedInstrument);
        // Tell everyone my instrument type, and all future people who join (AllBuffered)
        photonView.RPC("MakeNetInstrument", PhotonTargets.AllBuffered, LoginBtnController.loadedInstrument);
        image.enabled = false; // Show the scene
        text.text = "";
    }

    [PunRPC]
    // Someone else tells me their instrument type, make an instrument at their location
    public void MakeNetInstrument(Instrument instrument, PhotonMessageInfo info)
    {
        if (PhotonNetwork.otherPlayers.Length >= maxPlayers)
        {
            text.text = "Server is full";
            Debug.Log(text.text);
            return;
        }

        if (instrumentObjLookup.ContainsKey(info.sender.ID)) return; // In case this gets called multiple times

        int index = AssignPlayerIndex(info.sender.ID);
        if (index == -1)
        {
            text.text = "Unexpected error - server gave too many instruments";
            throw new Exception(text.text);
        }
        var instObj = CreateInstrument(instrument, playerPositions[index], info.sender.ID);
        instrumentObjLookup[info.sender.ID] = instObj;
        // We've created all the other instruments
        if (instrumentObjLookup.Count == PhotonNetwork.otherPlayers.Length)
            MakeLocalInstrument(); // Make our own
    }
}
