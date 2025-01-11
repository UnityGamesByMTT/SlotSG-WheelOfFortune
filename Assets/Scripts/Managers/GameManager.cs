using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private SlotManager slotManager;
    [SerializeField] private SocketIOManager socketManager;
    [SerializeField] private UIManager uIManager;
    [SerializeField] private BonusManager bonusManager;
    [SerializeField] private AudioController audioController;

    [Header("Buttons")]
    [SerializeField] private Button SlotStart_Button;
    [SerializeField] private Button AutoSpin_Button;
    [SerializeField] private Button AutoSpinStop_Button;
    [SerializeField] private Button Maxbet_button;
    [SerializeField] private Button BetPlus_Button;
    [SerializeField] private Button BetMinus_Button;
    [SerializeField] private Button Turbo_Button;
    [SerializeField] private Button StopSpin_Button;
    private bool IsSpinning = false;
    private bool IsAutoSpin = false;

    internal static bool turboMode;

    [SerializeField] private GameObject turboAnim;
    internal static bool immediateStop;
    [SerializeField] private int BetCounter = 0;

    private double currentBalance = 0;
    private double currentTotalBet = 0;

    private bool inititated = false;

    internal static bool checkWin;
    [SerializeField] private int betMultiplier;
    private void Awake()
    {
        socketManager.InitGameData ??= StartGame;

        SlotStart_Button.onClick.AddListener(StartSpin);

        AutoSpin_Button.onClick.AddListener(StartAutoSpin);
        AutoSpinStop_Button.onClick.AddListener(StopAutoSpin);

        socketManager.ShowAnotherDevicePopUp = () => uIManager.ADPopUp();
        socketManager.ShowDisconnectionPopUp = () => uIManager.DisconnectionPopup();

        BetPlus_Button.onClick.AddListener(delegate { OnBetChange(true); });
        BetMinus_Button.onClick.AddListener(delegate { OnBetChange(false); });

        StopSpin_Button.onClick.AddListener(() => { audioController.PlayButtonAudio(); StartCoroutine(StopSpin()); });

        slotManager.shuffleInitialMatrix();

        bonusManager.PlayButtonAudio = () => audioController.PlayButtonAudio("spin");
        bonusManager.PlaySpinAudio = () => audioController.PlaySpinAudio("bonus");
        bonusManager.StopSpinAudio = () => audioController.StopSpinAudio();
        bonusManager.PlayWinAudio = () => audioController.PlayWLAudio("bonuswin");
        bonusManager.StopWinAudio = () => audioController.StopWLAaudio();

        uIManager.PlayButtonAudio = () => audioController.PlayButtonAudio();
        uIManager.ToggleAudio = (float value, string type) => audioController.ToggleMute(value, type);

        Turbo_Button.onClick.AddListener(()=>{ audioController.PlayButtonAudio(); ToggleTurboMode();});
        // uIManager.Clos

    }

    void ToggleTurboMode()
    {
        turboMode = !turboMode;
        if (turboMode)
            turboAnim.SetActive(true);
        else
            turboAnim.SetActive(false);

    }

    IEnumerator StopSpin()
    {
        if (IsAutoSpin || immediateStop)
            yield break;
        immediateStop = true;
        StopSpin_Button.interactable = false;
        yield return new WaitUntil(() => !IsSpinning);
        immediateStop = false;
        StopSpin_Button.interactable = true;


    }
    void StartGame()
    {
        if (inititated)
        {
            uIManager.InitialiseUIData(socketManager.socketModel.uIData.paylines);
            return;
        }

        slotManager.UpdatePlayerData(socketManager.socketModel.playerData);
        slotManager.paylines = socketManager.socketModel.initGameData.Lines;
        slotManager.UpdateBetText(socketManager.socketModel.initGameData.Bets[BetCounter], betMultiplier);
        currentTotalBet = socketManager.socketModel.initGameData.Bets[BetCounter] * betMultiplier;
        currentBalance = socketManager.socketModel.playerData.Balance;
        uIManager.InitialiseUIData(socketManager.socketModel.uIData.paylines);
        bonusManager.values = socketManager.socketModel.initGameData.BonusPayout;


        CompareBalance();
        inititated = true;
        Application.ExternalCall("window.parent.postMessage", "OnEnter", "*");
    }


    void StartAutoSpin()
    {
        if (IsSpinning) return;

        IsAutoSpin = true;

        ErrorHandler.RunSafely(() =>
        {
            audioController.PlayButtonAudio("spin");
            if (AutoSpinStop_Button) AutoSpinStop_Button.gameObject.SetActive(true);
            if (AutoSpin_Button) AutoSpin_Button.gameObject.SetActive(false);
            StartCoroutine(ErrorHandler.RunSafely(AutoSpinRoutine(), OnError));
        }, OnError);
    }

    private void StopAutoSpin()
    {
        ErrorHandler.RunSafely(() =>
        {
            if (IsAutoSpin)
            {
                IsAutoSpin = false;
                if (AutoSpinStop_Button) AutoSpinStop_Button.gameObject.SetActive(false);
                if (AutoSpin_Button) AutoSpin_Button.gameObject.SetActive(true);
                StartCoroutine(ErrorHandler.RunSafely(StopAutoSpinCoroutine(), OnError));
            }
        }, OnError);
    }

    private IEnumerator StopAutoSpinCoroutine()
    {
        yield return new WaitUntil(() => !IsSpinning);
        ToggleButtonGrp(true);
        StopAllCoroutines();
    }

    void StartSpin()
    {
        ErrorHandler.RunSafely(() =>
        {
            audioController.PlayButtonAudio("spin");
            StartCoroutine(ErrorHandler.RunSafely(SpinRoutine(), OnError));
        }, OnError);
    }


    void OnBetChange(bool IncDec)
    {

        // if (audioController) audioController.PlayButtonAudio();

        if (IncDec)
        {
                BetCounter++;
            if (BetCounter > socketManager.socketModel.initGameData.Bets.Count - 1)
            {
                BetCounter=0;
            }
        }
        else
        {
                BetCounter--;
            if (BetCounter < 0)
            {
                BetCounter=socketManager.socketModel.initGameData.Bets.Count - 1;
            }
        }
        // TODO: WF to be done
        currentTotalBet = socketManager.socketModel.initGameData.Bets[BetCounter] * betMultiplier;

        slotManager.UpdateBetText(socketManager.socketModel.initGameData.Bets[BetCounter], betMultiplier);
        CompareBalance();

    }


    bool OnSpinStart()
    {
        return ErrorHandler.RunSafely(() =>
        {

            slotManager.StopGameAnimation();
            slotManager.WinningsAnim(false);
            slotManager.ResetLinesAndWins();
            bool start = CompareBalance();
            ToggleButtonGrp(false);
            if (start)
            {
            slotManager.BalanceDeduction();

            }
            return start;

        }, OnError);
    }

    void OnSpin(List<List<int>> result)
    {
        ErrorHandler.RunSafely(() =>
        {
            slotManager.InitiateForAnimation(result);
        }, OnError);
    }

    IEnumerator OnSpinEnd(bool lowbal = false)
    {
        if (!lowbal)
        {
            audioController.StopSpinAudio();
            currentBalance = socketManager.socketModel.playerData.Balance;
            Debug.Log(currentBalance);
            slotManager.UpdatePlayerData(socketManager.socketModel.playerData);
            slotManager.ProcessPayoutLines(socketManager.socketModel.resultGameData.linesToEmit);
            // TODO: WF enable animation
            slotManager.ProcessPointsAnimations(socketManager.socketModel.resultGameData.linesToEmit);
            if (socketManager.socketModel.resultGameData.isbonus)
            {
                bonusManager.targetIndex = socketManager.socketModel.resultGameData.BonusIndex;
                bonusManager.multipler = socketManager.socketModel.initGameData.Bets[BetCounter];
                bonusManager.StartBonus(IsAutoSpin);
                audioController.playBgAudio("bonus");
                yield return new WaitUntil(() => !bonusManager.isBonusPlaying);
                audioController.playBgAudio();
            }
            else if (socketManager.socketModel.playerData.currentWining > 0)
            {
                int wintype = CheckWinPopups(socketManager.socketModel.playerData.currentWining);
                // wintype=1;
                if (wintype > 0)
                {
                    checkWin=true;
                    audioController.PlayWLAudio("win");
                    uIManager.PopulateWin(wintype, socketManager.socketModel.playerData.currentWining);
                    Debug.Log($"checking, {checkWin}");
                    yield return new WaitUntil(() => !checkWin);
                    checkWin=false;
                    Debug.Log($"checking, {checkWin}");
                    audioController.StopWLAaudio();
                }

                slotManager.WinningsAnim(true);
            }
        }

        if (!IsAutoSpin) ToggleButtonGrp(true);

        yield return null;

    }

    IEnumerator SpinRoutine()
    {

        bool start = OnSpinStart();
        if (!start)
        {
            OnSpinEnd(true);
            if (IsAutoSpin)
            {
                // IsAutoSpin=false;
                StopAutoSpin();
                yield return new WaitForSeconds(1);
            }
            ToggleButtonGrp(true);
            yield break;
        }
        IsSpinning = true;
        var spinData = new { data = new { currentBet = BetCounter, currentLines = socketManager.socketModel.initGameData.Lines.Count, spins = 1 }, id = "SPIN" };
        socketManager.SendData("message", spinData);
        audioController.PlaySpinAudio();
        if (!IsAutoSpin)
            StopSpin_Button.gameObject.SetActive(true);

        yield return ErrorHandler.RunSafely(slotManager.InitiateSpin(), OnError);

        yield return new WaitUntil(() => socketManager.isResultdone);
        OnSpin(socketManager.socketModel.resultGameData.ResultReel);
        // if (!turboMode)
        //     yield return new WaitForSeconds(0.25f);

        yield return ErrorHandler.RunSafely(slotManager.TerminateSpin(), OnError);
        if (StopSpin_Button.gameObject.activeSelf)
            StopSpin_Button.gameObject.SetActive(false);
        if (!IsAutoSpin) IsSpinning = false;
        yield return ErrorHandler.RunSafely(OnSpinEnd(), OnError);
    }

    IEnumerator AutoSpinRoutine()
    {

        while (IsAutoSpin)
        {
            yield return ErrorHandler.RunSafely(SpinRoutine(), OnError);
            if(socketManager.socketModel.playerData.currentWining>0)
            yield return new WaitForSeconds(2f);
            else
            yield return new WaitForSeconds(1.2f);
        }

        IsSpinning = false;
    }

    void OnError()
    {
        slotManager.KillAllTweens();
        // socketManager.CloseSocket();
        // Application.ExternalCall("window.parent.postMessage", "onExit", "*");
    }


    private bool CompareBalance()
    {
        if (currentBalance < currentTotalBet)
        {
            uIManager.LowBalPopup();
            // if (AutoSpin_Button) AutoSpin_Button.interactable = false;
            // if (SlotStart_Button) SlotStart_Button.interactable = false;
            return false;
        }
        else
        {
            // if (AutoSpin_Button) AutoSpin_Button.interactable = true;
            // if (SlotStart_Button) SlotStart_Button.interactable = true;
            return true;

        }
    }


    internal int CheckWinPopups(double WinAmout)
    {
        if (WinAmout >= currentTotalBet * 10 && WinAmout < currentTotalBet * 15)
        {
            return 1;
        }
        else if (WinAmout >= currentTotalBet * 15 && WinAmout < currentTotalBet * 20)
        {
            return 2;

        }
        else if (WinAmout >= currentTotalBet * 20)
        {
            return 3;
        }
        else
        {

            return 0;
        }

    }

    void SkipWin(){

        if(checkWin)
        checkWin=false;
    }
    void ToggleButtonGrp(bool toggle)
    {

        if (SlotStart_Button) SlotStart_Button.interactable = toggle;
        if (AutoSpin_Button) AutoSpin_Button.interactable = toggle;
        if (Maxbet_button) Maxbet_button.interactable = toggle;
        if (BetMinus_Button) BetMinus_Button.interactable = toggle;
        if (BetPlus_Button) BetPlus_Button.interactable = toggle;

    }

}
