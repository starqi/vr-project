using UnityEngine;
using System.Collections;
using Photon;
using UnityEngine.UI;
using System;

public class PhotonInitSetup : PunBehaviour
{
    public static readonly string roomName = "myRoom";
    public static readonly byte maxPlayers = 3;
    public static readonly Vector3[] playerPositions;
    public static readonly float triLength = 4.0f;

    static PhotonInitSetup()
    {
        playerPositions = new Vector3[] {
            new Vector3(-triLength, 0.0f, -triLength),
            new Vector3(triLength, 0.0f, -triLength),
            new Vector3(triLength, 0.0f, triLength)
        };
    }

    ///////////////////////////////////////////////////

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

    void CreateInstrument(Instrument instrument, Vector3 position)
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
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    int GetPlayerIndex()
    {
        for (var i = 0; i < PhotonNetwork.playerList.Length; ++i)
            if (PhotonNetwork.playerList[i] == PhotonNetwork.player)
                return i;
        throw new Exception("Player not found in player list?");
    }

    void OnCreatedOrJoinedRoom()
    {
        CreateInstrument(LoginBtnController.loadedInstrument, playerPositions[GetPlayerIndex()]);
    }

    public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        Debug.Log("Player connected: " + newPlayer.name);
        CreateInstrument(LoginBtnController.loadedInstrument, playerPositions[GetPlayerIndex()]);
    }

    /*
     
    [PunRPC]
    public void TestRPC(byte param)
    {
        Debug.Log("RPC received " + param);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1) && PhotonNetwork.room != null)
        {
            byte param = (byte)Random.Range(0.0f, 255.0f);
            photonView.RPC("TestRPC", PhotonTargets.All, param);
            Debug.Log("RPC sent " + param);
        }
    }

    */
}
