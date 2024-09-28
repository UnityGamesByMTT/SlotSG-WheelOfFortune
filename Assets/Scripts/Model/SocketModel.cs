using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal class SocketModel
{

    internal PlayerData playerData;
    internal UIData uIData;

    internal InitGameData initGameData;

    internal ResultGameData resultGameData;

    internal SocketModel()
    {

        this.playerData = new PlayerData();
        this.uIData = new UIData();
        this.initGameData = new InitGameData();
        this.resultGameData = new ResultGameData();
    }


}


[Serializable]
public class AuthData
{
    public string GameID;
    //public double TotalLines;
}


[Serializable]
public class InitGameData
{
    public List<double> Bets { get; set; }
    public List<List<int>> Lines { get; set; }

    public List<int> BonusPayout {get; set;}

}

[Serializable]
public class ResultGameData
{
    public List<int> autoSpin { get; set; }
    public List<List<int>> ResultReel { get; set; }
    public List<int> linesToEmit { get; set; }
    public List<List<string>> symbolsToEmit { get; set; }
    public double WinAmout { get; set; }
    // public FreeSpins freeSpins { get; set; }
    public double jackpot { get; set; }
    public bool isbonus { get; set; }
    public int BonusIndex { get; set; }
}


[Serializable]
public class UIData
{
    public Paylines paylines { get; set; }
    public List<string> spclSymbolTxt { get; set; }
    public string ToULink { get; set; }
    public string PopLink { get; set; }
}

[Serializable]
public class Paylines
{
    public List<Symbol> symbols { get; set; }
}

[Serializable]
public class Symbol
{
    public int ID { get; set; }
    public string Name { get; set; }
    public float payout { get; set; }
    public JToken mixedPayout { get; set; }
    public JToken symbolsCount { get; set; }
    public JToken description { get; set; }
    // public int freeSpin { get; set; }
}

[Serializable]
public class PlayerData
{
    public double Balance { get; set; }
    public double haveWon { get; set; }
    public double currentWining { get; set; }
}

[Serializable]
public class AuthTokenData
{
    public string cookie;
    public string socketURL;
}
