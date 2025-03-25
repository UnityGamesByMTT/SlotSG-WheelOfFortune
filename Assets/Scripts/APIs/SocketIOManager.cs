using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using DG.Tweening;
using Newtonsoft.Json;
using Best.SocketIO;
using Best.SocketIO.Events;
using Newtonsoft.Json.Linq;

public class SocketIOManager : MonoBehaviour
{

    internal SocketModel socketModel = new SocketModel();

    private Helper helper = new Helper();

    //WebSocket currentSocket = null;
    internal bool isResultdone = false;

    private SocketManager manager;


    protected string SocketURI = null;

    // TODO: WF to be changed
    // protected string TestSocketURI = "http://localhost:5000";
    // protected string TestSocketURI = "https://game-crm-rtp-backend.onrender.com/";
    protected string TestSocketURI = "http://localhost:5001";
    //protected string SocketURI = "http://localhost:5001";

    [SerializeField]
    private string TestToken;

    // protected string nameSpace="game"; //BackendChanges
    protected string nameSpace = ""; //BackendChanges
    private Socket gameSocket; //BackendChanges

    // TODO: WF to be added
    // protected string gameID = "";
    protected string gameID = "SL-WOF";
    [SerializeField] internal JSFunctCalls JSManager;
    internal bool isLoading;
    internal bool SetInit = false;
    private const int maxReconnectionAttempts = 6;
    private readonly TimeSpan reconnectionDelay = TimeSpan.FromSeconds(10);

    internal Action InitGameData = null;
    internal Action ShowDisconnectionPopUp = null;
    internal Action ShowAnotherDevicePopUp = null;
    private void Awake()
    {
        isLoading = true;
        SetInit = false;
        // OpenSocket();

        // Debug.unityLogger.logEnabled = false;
    }

    private void Start()
    {
        //OpenWebsocket();
        OpenSocket();
    }




    void ReceiveAuthToken(string jsonData)
    {
        Debug.Log("Received data: " + jsonData);

        // Parse the JSON data
        var data = JsonUtility.FromJson<AuthTokenData>(jsonData);
        SocketURI = data.socketURL;
        myAuth = data.cookie;
        nameSpace = data.nameSpace;
        // Proceed with connecting to the server using myAuth and socketURL
    }

    string myAuth = null;

    private void OpenSocket()
    {
        //Create and setup SocketOptions
        SocketOptions options = new SocketOptions();
        options.ReconnectionAttempts = maxReconnectionAttempts;
        options.ReconnectionDelay = reconnectionDelay;
        options.Reconnection = true;
        options.ConnectWith = Best.SocketIO.Transports.TransportTypes.WebSocket; //BackendChanges

        //Application.ExternalCall("window.parent.postMessage", "authToken", "*");

#if UNITY_WEBGL && !UNITY_EDITOR
        JSManager.SendCustomMessage("authToken");
        StartCoroutine(WaitForAuthToken(options));
#else
        Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
        {
            return new
            {
                token = TestToken,
                gameId = gameID
            };
        };
        options.Auth = authFunction;
        // Proceed with connecting to the server
        SetupSocketManager(options);
#endif
    }

    private IEnumerator WaitForAuthToken(SocketOptions options)
    {
        // Wait until myAuth is not null
        while (myAuth == null)
        {
            yield return null;
        }

        // Once myAuth is set, configure the authFunction
        Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
        {
            return new
            {
                token = myAuth,
                gameId = gameID
            };
        };
        options.Auth = authFunction;

        Debug.Log("Auth function configured with token: " + myAuth);

        // Proceed with connecting to the server
        SetupSocketManager(options);
    }
    private void OnSocketState(bool state)
    {
        if (state)
        {
            Debug.Log("my state is " + state);
            InitRequest("AUTH");
        }
        else
        {

        }
    }
    private void OnSocketError(string data)
    {
        Debug.Log("Received error with data: " + data);
    }
    private void OnSocketAlert(string data)
    {
        Debug.Log("Received alert with data: " + data);
        // AliveRequest("YES I AM ALIVE");
    }

    private void OnSocketOtherDevice(string data)
    {
        Debug.Log("Received Device Error with data: " + data);
        // uIManager.ADfunction();
        ShowAnotherDevicePopUp?.Invoke();
    }

    private void AliveRequest()
    {
        SendData("YES I AM ALIVE");
    }

    void OnConnected(ConnectResponse resp)
    {
        Debug.Log("Connected!");
        SendPing();

        //InitRequest("AUTH");
    }

    private void SendPing()
    {
        InvokeRepeating("AliveRequest", 0f, 3f);
    }

    private void OnDisconnected(string response)
    {
        Debug.Log("Disconnected from the server");
        StopAllCoroutines();
        ShowDisconnectionPopUp?.Invoke();
        // uIManager.DisconnectionPopup();
    }

    private void OnError(string response)
    {
        Debug.LogError("Error: " + response);
    }

    private void OnListenEvent(string data)
    {
        Debug.Log("Received some_event with data: " + data);
        ParseResponse(data);
    }

    private void SetupSocketManager(SocketOptions options)
    {
        // Create and setup SocketManager
#if UNITY_EDITOR
        this.manager = new SocketManager(new Uri(TestSocketURI), options);
#else
        this.manager = new SocketManager(new Uri(SocketURI), options);
#endif

        if (string.IsNullOrEmpty(nameSpace))
        {  //BackendChanges Start
            gameSocket = this.manager.Socket;
        }
        else
        {
            print("nameSpace: " + nameSpace);
            gameSocket = this.manager.GetSocket("/" + nameSpace);
        }
        // Set subscriptions
        gameSocket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnConnected);
        gameSocket.On<string>(SocketIOEventTypes.Disconnect, OnDisconnected);
        gameSocket.On<string>(SocketIOEventTypes.Error, OnError);
        gameSocket.On<string>("message", OnListenEvent);
        gameSocket.On<bool>("socketState", OnSocketState);
        gameSocket.On<string>("internalError", OnSocketError);
        gameSocket.On<string>("alert", OnSocketAlert);
        gameSocket.On<string>("AnotherDevice", OnSocketOtherDevice); //BackendChanges Finish    }
    }
        // Connected event handler implementation

        internal void InitRequest(string eventName)
    {
        var initmessage = new { Data = new { GameID = gameID }, id = "Auth" };
        SendData(eventName, initmessage);
    }

    internal void CloseSocket()
    {
        SendData("EXIT");

    }

    private void ParseResponse(string jsonObject)
    {
        Debug.Log(jsonObject);
        // Root myData = JsonConvert.DeserializeObject<Root>(jsonObject);
        JObject resp = JObject.Parse(jsonObject);
        // string id = myData.id;
        string messageId = resp["id"].ToString();

        // var message = resp["message"];
        // var gameData = message["GameData"];
        if (resp["message"].Type == JTokenType.Object && resp["message"]["PlayerData"] != null)
        {
            socketModel.playerData = resp["message"]["PlayerData"].ToObject<PlayerData>();
        }


        if (messageId == "InitData")
        {
            socketModel.uIData = resp["message"]["UIData"].ToObject<UIData>();
            socketModel.initGameData.Bets = resp["message"]["GameData"]["Bets"].ToObject<List<double>>();
            socketModel.initGameData.Lines = resp["message"]["GameData"]["Lines"].ToObject<List<List<int>>>();
            socketModel.initGameData.BonusPayout = resp["message"]["GameData"]["BonusPayout"].ToObject<List<int>>();
#if UNITY_WEBGL && !UNITY_EDITOR
        JSManager.SendCustomMessage("OnEnter");
#endif
            // COMPLETED: WF multiple parsheet
            InitGameData?.Invoke();

        }
        else if (messageId == "ResultData")
        {
            socketModel.resultGameData.ResultReel = helper.ConvertStringListsToIntLists(resp["message"]["GameData"]["resultSymbols"].ToObject<List<List<string>>>());
            socketModel.resultGameData.linesToEmit = resp["message"]["GameData"]["linestoemit"].ToObject<List<int>>();
            socketModel.resultGameData.isbonus = resp["message"]["GameData"]["isbonus"].ToObject<bool>();
            socketModel.resultGameData.BonusIndex = resp["message"]["GameData"]["BonusIndex"].ToObject<int>();
            isResultdone = true;
            print("result data: " + JsonConvert.SerializeObject(socketModel.resultGameData.ResultReel));

        }
        else if (messageId == "ExitUser")
        {
            if (this.manager != null)
            {
                Debug.Log("Dispose my Socket");
                this.manager.Close();
            }
            //   Application.ExternalCall("window.parent.postMessage", "onExit", "*");
#if UNITY_WEBGL && !UNITY_EDITOR
                        JSManager.SendCustomMessage("onExit");
#endif
           
        }

    }


    internal void ReactNativeCallOnFailedToConnect() //BackendChanges
    {
#if UNITY_WEBGL && !UNITY_EDITOR
    JSManager.SendCustomMessage("onExit");
#endif
    }


    // private void RefreshUI()
    // {
    //     uIManager.InitialiseUIData(initUIData.AbtLogo.link, initUIData.AbtLogo.logoSprite, initUIData.ToULink, initUIData.PopLink, initUIData.paylines);
    // }

    // private void PopulateSlotSocket(List<string> slotPop, List<string> LineIds)
    // {
    //     slotManager.shuffleInitialMatrix();
    //     for (int i = 0; i < LineIds.Count; i++)
    //     {
    //         slotManager.FetchLines(LineIds[i], i);
    //     }

    //     // slotManager.SetInitialUI();
    //     isLoading = false;

    //     // Application.ExternalCall("window.parent.postMessage", "OnEnter", "*");
    // }

    internal void SendData(string eventName, object message = null)
    {

        if (gameSocket == null || !gameSocket.IsOpen)
        {
            Debug.LogWarning("Socket is not connected.");
            return;
        }
        if (message == null)
        {
            gameSocket.Emit(eventName);
            return;
        }
        isResultdone = false;
        string json = JsonConvert.SerializeObject(message);
        gameSocket.Emit(eventName, json);
        Debug.Log("JSON data sent: " + json);

    }



}





