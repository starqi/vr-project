using System;
using UnityEngine;
using System.Collections;
using Photon;
using UnityEngine.UI;

using Random = UnityEngine.Random;
using System.Collections.Generic;

public class PhotonInitSetup : PunBehaviour
{
    public static readonly string roomName = "myRoom";
    public static readonly byte maxPlayers = 3;
    public static readonly Vector3[] playerPositions;
    public static readonly float triLength = 4.0f;

    static PhotonInitSetup()
    {
        // Max 3 players, and sit in a triangle
        playerPositions = new Vector3[] {
            new Vector3(-triLength, 0.0f, -triLength),
            new Vector3(triLength, 0.0f, -triLength),
            new Vector3(triLength, 0.0f, triLength)
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
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.playerName = LoginBtnController.loadedName;
        PhotonNetwork.JoinOrCreateRoom(roomName, new RoomOptions() { IsVisible = false, MaxPlayers =  maxPlayers }, null);
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("Made a room");
        OnCreatedOrJoinedRoom();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined a room");
        OnCreatedOrJoinedRoom();
    }

    public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        Debug.Log("Player connected: " + newPlayer.name);
    }

    ///////////////////////////////////////////////////

    // Look up instrument game object from Photon's ID field
    Dictionary<int, GameObject> instrumentObjLookup = new Dictionary<int, GameObject>();

    GameObject CreateInstrument(Instrument instrument, Vector3 position, int ownerID)
    {
        if (instrument != Instrument.Drums)
        {
            var instrumentPf = Resources.Load<GameObject>("Prefabs/QwertyPiano");
            var vizPf = Resources.Load<GameObject>("Prefabs/Viz");
            var instrumentObj = (GameObject)Instantiate(instrumentPf, position, Quaternion.identity);
            var vizObj = (GameObject)Instantiate(vizPf, position + new Vector3(0.0f, 5.0f, 0.0f), Quaternion.identity);
            var instrumentCtl = instrumentObj.GetComponent<InstrumentController>();
            var vizCtl = vizObj.GetComponent<VizController>();
            vizCtl.instrument = instrumentObj;
            instrumentCtl.SetupNetwork(ownerID);
            return instrumentObj;
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    // Get index of player inside the player list, only use this to determine which position the player is
    int GetPlayerIndex(PhotonPlayer player)
    {
        for (var i = 0; i < PhotonNetwork.playerList.Length; ++i)
            if (PhotonNetwork.playerList[i] == player)
                return i;
        throw new Exception("Player not found in player list?");
    }

    void OnCreatedOrJoinedRoom()
    {
        // Make the local instrument
        var instObj = CreateInstrument(LoginBtnController.loadedInstrument, 
            playerPositions[GetPlayerIndex(PhotonNetwork.player)], PhotonNetwork.player.ID);
        instrumentObjLookup[PhotonNetwork.player.ID] = instObj;
        // Tell everyone my instrument type
        photonView.RPC("ProvideInstrument", PhotonTargets.AllBuffered, LoginBtnController.loadedInstrument);
    }

    [PunRPC]
    public void ProvideInstrument(int id, Instrument instrument, PhotonMessageInfo info)
    {    
        // Someone else tells me their instrument type, make an instrument there
        var instObj = CreateInstrument(instrument, playerPositions[GetPlayerIndex(info.sender)], info.sender.ID);
        instrumentObjLookup[info.sender.ID] = instObj;
    }
}
