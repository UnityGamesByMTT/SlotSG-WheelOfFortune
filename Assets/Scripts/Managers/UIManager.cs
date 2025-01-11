using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class UIManager : MonoBehaviour
{

    [Header("Popus UI")]
    [SerializeField] private GameObject MainPopup_Object;


    [Header("Paytable Texts")]
    [SerializeField] private TMP_Text[] SymbolsText;
    [SerializeField] private TMP_Text[] SymbolsTripleText;
    [SerializeField] private TMP_Text AnyComboText;
    [SerializeField] private TMP_Text SevenComboText;
    [SerializeField] private TMP_Text BarComboText;
    [SerializeField] private TMP_Text Bonus_Text;
    [SerializeField] private TMP_Text Rule_Text;
    [SerializeField] private Button PaytableExit_Button;
    [SerializeField] private Button Paytable_Button;
    [SerializeField] private GameObject Paytable_Object;


    [Header("Settings Popup")]
    [SerializeField] private GameObject SettingsPopup_Object;
    [SerializeField] private Button Settings_Button;
    [SerializeField] private Button SettingsExit_Button;

    [SerializeField] private Slider MusicSlider;
    [SerializeField] private Slider SoundSlider;

    [Header("all Win Popup")]
    [SerializeField] private TMP_Text Win_text_title;
    [SerializeField] private GameObject WinPopup_Object;
    [SerializeField] private TMP_Text Win_Text;
    [SerializeField] private Button SkipWin_Button;


    [Header("low balance popup")]
    [SerializeField] private GameObject LowBalancePopup_Object;
    [SerializeField] private Button Close_Button;


    [Header("disconnection popup")]
    [SerializeField] private Button CloseDisconnect_Button;
    [SerializeField] private GameObject DisconnectPopup_Object;

    [Header("Quit Popup")]
    [SerializeField] private GameObject quitPopupObject;
    [SerializeField] private Button yes_Button;
    [SerializeField] private Button GameExit_Button;
    [SerializeField] private Button no_Button;
    internal Action closeSocket;


    [Header("Splash Screen")]
    [SerializeField] private GameObject spalsh_screen;
    [SerializeField] private Image progressbar;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField]
    private Button QuitSplash_button;

    [Header("AnotherDevice Popup")]
    [SerializeField] private Button CloseAD_Button;
    [SerializeField] private GameObject ADPopup_Object;
    //private int FreeSpins;
    [Header("Pagination")]
    [SerializeField] private int CurrentIndex = 0;
    [SerializeField] private GameObject[] paytableList;
    [SerializeField] private Button RightBtn;
    [SerializeField] private Button LeftBtn;
    private bool isExit = false;

    internal GameObject ActivePopup = null;

    internal Action PlayButtonAudio;

    internal Action<float, string> ToggleAudio;
    private void Awake()
    {
        // if (spalsh_screen) spalsh_screen.SetActive(true);
        // StartCoroutine(LoadingRoutine());
    }

    private void Start()
    {

        SetButton(yes_Button, CallOnExitFunction);
        SetButton(no_Button, () => ClosePopup());
        SetButton(GameExit_Button, () => OpenPopup(quitPopupObject));

        SetButton(Settings_Button, () => OpenPopup(SettingsPopup_Object));
        SetButton(SettingsExit_Button, () => ClosePopup());

        SetButton(Paytable_Button, () => OpenPopup(Paytable_Object));
        SetButton(PaytableExit_Button, () => ClosePopup());

        MusicSlider.onValueChanged.AddListener(ToggleMusic);
        SoundSlider.onValueChanged.AddListener(ToggleSound);

        SetButton(LeftBtn, () => Slide(false));
        SetButton(RightBtn, () => Slide(true));

        SetButton(CloseDisconnect_Button, CallOnExitFunction);
        SetButton(Close_Button, () => ClosePopup());
        SetButton(QuitSplash_button, () => OpenPopup(quitPopupObject));

        SetButton(CloseAD_Button, CallOnExitFunction);

        SkipWin_Button.onClick.AddListener(()=>CloseWinPopUp());

    }

    void SetButton(Button button, Action action)
    {
        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                PlayButtonAudio?.Invoke();
                action();
            });
        }
    }
    internal void PopulateWin(int value, double amount)
    {
        switch (value)
        {
            case 1:
                if (Win_text_title) Win_text_title.text = "Big Win";
                break;
            case 2:
                if (Win_text_title) Win_text_title.text = "Huge Win";
                break;
            case 3:
                if (Win_text_title) Win_text_title.text = "Mega Win";
                break;
        }

        double initAmount = 0;

        OpenPopup(WinPopup_Object);

        DOTween.To(() => initAmount, (val) => initAmount = val, amount, 3f).OnUpdate(() =>
        {
            if (Win_Text) Win_Text.text = initAmount.ToString("f3");
        });

        Invoke(nameof(CloseWinPopUp),4f);



    }


    void CloseWinPopUp(){

        GameManager.checkWin=false;
        DOTween.Kill(Win_Text.transform);
        ClosePopup();

    }

    internal void LowBalPopup()
    {
        OpenPopup(LowBalancePopup_Object);
    }

    internal void InitialiseUIData(Paylines paylines)
    {
        for (int i = 0; i < 5; i++)
        {
            string text = "";
            text += "<color=#ffa500ff>3X </color>- " + paylines.symbols[i + 1].payout + "x";
            if (SymbolsText[i]) SymbolsText[i].text = text;
        }

        for (int i = 6; i < 11; i++)
        {
            string text = "";

            text += "<color=#ffa500ff>3X </color>- " + paylines.symbols[i].payout + "x";


            if (SymbolsTripleText[i - 6]) SymbolsTripleText[i - 6].text = text;
        }

        SymbolsText[SymbolsText.Length - 1].text = "<color=#ffa500ff>3X </color>- " + paylines.symbols[11].payout.ToString() + "x";

        AnyComboText.text = "Any <color=#ffa500ff>3X </color>- " + paylines.symbols[13].payout.ToString() + "x";

        SevenComboText.text = paylines.symbols[1].mixedPayout.Type != JTokenType.Object
            ? $"Any <color=#ffa500ff>3X </color>- {paylines.symbols[1].mixedPayout}x"
            : "";

        BarComboText.text = paylines.symbols[4].mixedPayout.Type != JTokenType.Object
            ? $"Any <color=#ffa500ff>3X </color>- {paylines.symbols[4].mixedPayout}x"
            : "";

        Bonus_Text.text = paylines.symbols[12].symbolsCount.Type != JTokenType.Object
            ? $"Any <color=#ffa500ff>{paylines.symbols[12].symbolsCount}X </color>- triggers bonus game. \n Tap the spin button to spin the wheel and get exciting reward."
            : "_";


        Rule_Text.text = paylines.symbols[13].description.Type != JTokenType.Object ? paylines.symbols[13].description.ToString() : "";


        // for (int i = 0; i < paylines.symbols.Count; i++)
        // {

        //     if (paylines.symbols[i].Name.ToUpper() == "BONUS")
        //     {
        //         if (Bonus_Text) Bonus_Text.text = paylines.symbols[i].description.ToString();
        //     }
        // }
    }

    internal void ADPopUp()
    {
        OpenPopup(ADPopup_Object);
    }

    private void CallOnExitFunction()
    {
        isExit = true;
        closeSocket?.Invoke();
        Application.ExternalCall("window.parent.postMessage", "onExit", "*");
    }


    private void OpenPopup(GameObject Popup)
    {
        // if(playAudio)
        // audioController.PlayButtonAudio();

        if (ActivePopup != null && !DisconnectPopup_Object.activeSelf)
        {
            ActivePopup.SetActive(false);
            ActivePopup = null;
        }

        if (Popup) Popup.SetActive(true);
        if (MainPopup_Object) MainPopup_Object.SetActive(true);
        ActivePopup = Popup;
        paytableList[CurrentIndex = 0].SetActive(true);
    }

    private void ClosePopup()
    {
        // if(playAudio)
        // audioController.PlayButtonAudio();
        if (!DisconnectPopup_Object.activeSelf)
        {
            if (MainPopup_Object) MainPopup_Object.SetActive(false);
            if (ActivePopup) ActivePopup.SetActive(false);
        }
        paytableList[CurrentIndex].SetActive(false);
    }



    private void Slide(bool inc)
    {
        if(inc){
                CurrentIndex++;
            if(CurrentIndex>paytableList.Length-1)
            CurrentIndex=0;


        }else{

            CurrentIndex--;
            if(CurrentIndex<0)
            CurrentIndex=paytableList.Length-1;

        }

        for (int i = 0; i < paytableList.Length; i++)
        {
            paytableList[i].SetActive(false);
        }

        paytableList[CurrentIndex].SetActive(true);




    }

    internal void DisconnectionPopup()
    {

        //ClosePopup(ReconnectPopup_Object);
        if (!isExit)
        {
            OpenPopup(DisconnectPopup_Object);
        }

    }



    private void ToggleMusic(float value)
    {

        ToggleAudio?.Invoke(value, "bg");
    }

    private void ToggleSound(float value)
    {
        ToggleAudio?.Invoke(value, "button");
        ToggleAudio?.Invoke(value, "wl");

    }

}
