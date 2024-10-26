using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using DG.Tweening;
using System.Linq;
using Newtonsoft.Json;
using Best.SocketIO;
using Best.SocketIO.Events;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;

public class SocketIOManager : MonoBehaviour
{

    internal SocketModel socketModel = new SocketModel();

    private Helper helper = new Helper();

    //WebSocket currentSocket = null;
    internal bool isResultdone = false;

    private SocketManager manager;


    protected string SocketURI = null;

    // TODO: WF to be changed
    protected string TestSocketURI = "http://localhost:5000";
    //protected string SocketURI = "http://localhost:5000";

    [SerializeField]
    private string TestToken;

    // TODO: WF to be added
    // protected string gameID = "";
    protected string gameID = "SL-WOF";

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
        // Proceed with connecting to the server using myAuth and socketURL
    }

    string myAuth = null;

    internal void OpenSocket()
    {
        // Create and setup SocketOptions
        SocketOptions options = new SocketOptions();
        options.ReconnectionAttempts = maxReconnectionAttempts;
        options.ReconnectionDelay = reconnectionDelay;
        options.Reconnection = true;

        Application.ExternalCall("window.parent.postMessage", "authToken", "*");

#if UNITY_WEBGL && !UNITY_EDITOR
        Application.ExternalEval(@"
            window.addEventListener('message', function(event) {
                if (event.data.type === 'authToken') {
                    var combinedData = JSON.stringify({
                        cookie: event.data.cookie,
                        socketURL: event.data.socketURL
                    });
                    // Send the combined data to Unity
                    SendMessage('SocketManager', 'ReceiveAuthToken', combinedData);
                }});");
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
        // Set subscriptions
        this.manager.Socket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnConnected);
        this.manager.Socket.On<string>(SocketIOEventTypes.Disconnect, OnDisconnected);
        this.manager.Socket.On<string>(SocketIOEventTypes.Error, OnError);
        this.manager.Socket.On<string>("message", OnListenEvent);
        this.manager.Socket.On<bool>("socketState", OnSocketState);
        this.manager.Socket.On<string>("internalError", OnSocketError);
        this.manager.Socket.On<string>("alert", OnSocketAlert);
        this.manager.Socket.On<string>("AnotherDevice", OnSocketOtherDevice);


        // Start connecting to the server
        this.manager.Open();
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
        DOVirtual.DelayedCall(0.1f, () =>
        {
            if (this.manager != null)
            {
                Debug.Log("Dispose my Socket");
                this.manager.Close();
            }
        });
    }

    private void ParseResponse(string jsonObject)
    {
        Debug.Log(jsonObject);
        // Root myData = JsonConvert.DeserializeObject<Root>(jsonObject);
        JObject resp = JObject.Parse(jsonObject);
        // string id = myData.id;
        string messageId = resp["id"].ToString();

        var message = resp["message"];
        var gameData = message["GameData"];
        socketModel.playerData = message["PlayerData"].ToObject<PlayerData>();

        if (messageId == "InitData")
        {
            socketModel.uIData = message["UIData"].ToObject<UIData>();
            socketModel.initGameData.Bets = gameData["Bets"].ToObject<List<double>>();
            socketModel.initGameData.Lines = gameData["Lines"].ToObject<List<List<int>>>();
            socketModel.initGameData.BonusPayout = gameData["BonusPayout"].ToObject<List<int>>();
            // COMPLETED: WF multiple parsheet
            InitGameData?.Invoke();

        }
        else if (messageId == "ResultData")
        {
            socketModel.resultGameData.ResultReel = helper.ConvertStringListsToIntLists(gameData["resultSymbols"].ToObject<List<List<string>>>());
            socketModel.resultGameData.linesToEmit = gameData["linestoemit"].ToObject<List<int>>();
            socketModel.resultGameData.isbonus = gameData["isbonus"].ToObject<bool>();
            socketModel.resultGameData.BonusIndex = gameData["BonusIndex"].ToObject<int>();
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
            Application.ExternalCall("window.parent.postMessage", "onExit", "*");
        }
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

        if (this.manager.Socket == null || !this.manager.Socket.IsOpen)
        {
            Debug.LogWarning("Socket is not connected.");
            return;
        }
        if (message == null)
        {
            this.manager.Socket.Emit(eventName);
            return;
        }
        isResultdone = false;
        string json = JsonConvert.SerializeObject(message);
        this.manager.Socket.Emit(eventName, json);
        Debug.Log("JSON data sent: " + json);

    }



}





