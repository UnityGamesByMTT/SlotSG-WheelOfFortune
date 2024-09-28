
using UnityEngine;
using DG.Tweening;
using System.Collections;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

public class BonusManager : MonoBehaviour
{


    [SerializeField] private GameObject bonusPanel;
    [SerializeField] private Transform rotator;

    [SerializeField] private TMP_Text winText;
    [SerializeField] private GameObject winPopup;
    [SerializeField] private Tween rotationTween = null;

    // [SerializeField] internal int stopIndex;
    [SerializeField] int segmentCount;
    private float degreesPerSegment;
    private float offSet;

    [SerializeField] private TMP_Text[] valueTextList;
    [SerializeField] internal List<int> values;

    [SerializeField] private GameObject lightOff;

    [SerializeField] private Button Spin_Button;

    [SerializeField] private TMP_Text betPerLineText;
    internal int targetIndex;
    internal double multipler;

    internal Action PlayButtonAudio;
    internal Action PlaySpinAudio;
    internal Action PlayWinAudio;
    internal Action StopSpinAudio;
    internal Action StopWinAudio;

    internal bool isBonusPlaying=false;
    void Start()
    {
        degreesPerSegment = 360f / segmentCount;
        offSet = degreesPerSegment / 2;

        Spin_Button.onClick.AddListener(() => OnSpinStart());
        // OnSpinStart();
    }

    internal void StartBonus()
    {
        bonusPanel.SetActive(true);
        for (int i = 0; i < valueTextList.Length; i++)
        {
            valueTextList[i].text = $"x {values[i].ToString()}";
        }
        betPerLineText.text=multipler.ToString();
        isBonusPlaying=true;
    }
    private void OnSpinStart()
    {

        PlayButtonAudio?.Invoke();
        Spin_Button.interactable = false;
        rotationTween ??= rotator.DOLocalRotate(new Vector3(0, 0, -360), 2f, RotateMode.LocalAxisAdd)
              .SetLoops(-1, LoopType.Incremental)
              .SetEase(Ease.Linear);

        InvokeRepeating(nameof(LightAnimation), 0, 0.25f);

        StartCoroutine(WaitForTargetIndex());
    }

    private IEnumerator WaitForTargetIndex()
    {
        Debug.Log("started stopping");
        if (rotationTween == null)
            yield break;
        
        PlaySpinAudio?.Invoke();
        yield return new WaitForSeconds(2f);
        winText.text = (values[targetIndex] * multipler).ToString();
        rotationTween.timeScale = 0.5f;
        float targetAngle = targetIndex * degreesPerSegment;
        float threshold = 5f;
        bool hasCrossedTarget = false;
        float distanceToTarget = 0;
        float currentAngle = 0;

        while (true)
        {
            currentAngle = rotator.localEulerAngles.z % 360f;
            if (currentAngle < 0) currentAngle += 360f;
            distanceToTarget = Mathf.Abs(currentAngle - targetAngle);

            if (targetAngle == 0)
            {
                hasCrossedTarget = currentAngle > 180;
            }
            else
            {
                hasCrossedTarget = (currentAngle > targetAngle && currentAngle - targetAngle < 180) || (currentAngle < targetAngle && targetAngle - currentAngle > 180);
            }

            if (distanceToTarget < threshold && !hasCrossedTarget)
            {
                rotationTween.Kill();
                rotationTween=null;
                rotator.DOLocalRotate(new Vector3(0, 0, targetAngle - offSet), 0.2f).SetEase(Ease.Linear);
                CancelInvoke(nameof(LightAnimation));
                lightOff.SetActive(false);
                StopSpinAudio?.Invoke();
                PlayWinAudio?.Invoke();
                yield return new WaitForSeconds(1f);
                StopWinAudio?.Invoke();
                OnSpinEnd();
                break;
            }
            yield return null;
        }
    }



    void OnSpinEnd()
    {
        winPopup.transform.localScale = Vector3.zero;
        winPopup.SetActive(true);
        winPopup.transform.DOScale(Vector3.one, 1.3f).SetEase(Ease.OutElastic).OnComplete(() =>
        {
            winPopup.SetActive(false);
            bonusPanel.SetActive(false);
            winText.text = "";
            lightOff.SetActive(true);
            Spin_Button.interactable = true;
            isBonusPlaying=false;
        });
        
    }

    void LightAnimation()
    {
        lightOff.SetActive(!lightOff.activeSelf);
    }
}