﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnhanceScrollView : MonoBehaviour
{
    // Control the item's scale curve
    public AnimationCurve scaleCurve;
    // Control the position curve
    public AnimationCurve positionCurve;
    // Control the "depth"'s curve(In 3d version just the Z value, in 2D UI you can use the depth(NGUI))
    // NOTE:
    // 1. In NGUI set the widget's depth may cause performance problem
    // 2. If you use 3D UI just set the Item's Z position
    public AnimationCurve depthCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
    // The start center index
    [Tooltip("The Start center index")]
    public int startCenterIndex = 0;
    public float posCurveFactor = 500.0f;
    // vertical fixed position value 
    public float yFixedPositionValue = 46.0f;

    // Lerp duration
    public float lerpDuration = 0.2f;
    private float mCurrentDuration = 0.0f;
    private int mCenterIndex = 0;
    private bool enableLerpTween = true;

    // center and preCentered item
    private EnhanceItem curCenterItem;
    private EnhanceItem preCenterItem;

    // if we can change the target item
    private bool canChangeItem = true;
    private float dFactor = 0.2f;

    // originHorizontalValue Lerp to horizontalTargetValue
    private float originHorizontalValue = 0.1f;
    public float targetHorizontalValue = 0.5f;

    // "depth" factor (2d widget depth or 3d Z value)
    private int depthFactor = 20;

    // Drag enhance scroll view
    [Tooltip("Camera for drag ray cast")]
    public Camera sourceCamera;
    private EnhanceScrollViewDragController dragController;

    public void EnableDrag(bool isEnabled)
    {
        if (isEnabled)
        {
            if (sourceCamera == null)
            {
                Debug.LogError("## Source Camera for drag scroll view is null ##");
                return;
            }

            if (dragController == null)
                dragController = gameObject.AddComponent<EnhanceScrollViewDragController>();
            dragController.enabled = true;
            // set the camera and mask
            dragController.SetTargetCameraAndMask(sourceCamera, (1 << LayerMask.NameToLayer("UI")));
        }
        else
        {
            if (dragController != null)
                dragController.enabled = false;
        }
    }

    // targets enhance item in scroll view
    public List<EnhanceItem> listEnhanceItems;

    private static EnhanceScrollView instance;
    public static EnhanceScrollView GetInstance
    {
        get { return instance; }
    }

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        canChangeItem = true;
        int enhanceItemCount = listEnhanceItems.Count;
        dFactor = (Mathf.RoundToInt((1f / enhanceItemCount) * 10000f)) * 0.0001f;
        mCenterIndex = enhanceItemCount / 2;
        if (enhanceItemCount % 2 == 0)
            mCenterIndex = enhanceItemCount / 2 - 1;
        Debug.Log("## calculate factor : " + dFactor + " CenterIndex :" + mCenterIndex);

        for (int i = 0; i < enhanceItemCount; i++)
        {
            listEnhanceItems[i].CurveOffSetIndex = i;
            listEnhanceItems[i].CenterOffSet = dFactor * (mCenterIndex - i);
            listEnhanceItems[i].SetSelectState(false);
            GameObject obj = listEnhanceItems[i].gameObject;
            DragEnhanceView script = obj.GetComponent<DragEnhanceView>();
            if (script != null)
                script.SetScrollView(this);
        }

        // set the center item with startCenterIndex
        if (startCenterIndex < 0 || startCenterIndex >= listEnhanceItems.Count)
        {
            Debug.LogError("## startCenterIndex < 0 || startCenterIndex >= listEnhanceItems.Count  out of index ##");
            startCenterIndex = mCenterIndex;
        }
        curCenterItem = listEnhanceItems[startCenterIndex];
        targetHorizontalValue = 0.5f - curCenterItem.CenterOffSet;
        LerpTweenToTarget(0f, targetHorizontalValue, false);
        // enable the drag action
        // EnableDrag(true);
    }

    private void LerpTweenToTarget(float originValue, float targetValue, bool needTween = false)
    {
        if (!needTween)
        {
            List<EnhanceItem> tmpList = new List<EnhanceItem>(this.listEnhanceItems);
            SortViewItem(tmpList);
            originHorizontalValue = targetValue;
            UpdateEnhanceScrollView(targetValue);
        }
        else
        {
            originHorizontalValue = originValue;
            mCurrentDuration = 0.0f;
        }
        enableLerpTween = needTween;
    }

    public void DisableLerpTween()
    {
        this.enableLerpTween = false;
    }

    /// 
    /// Update EnhanceItem state with curve fTime value
    /// 
    public void UpdateEnhanceScrollView(float fValue)
    {
        for (int i = 0; i < listEnhanceItems.Count; i++)
        {
            EnhanceItem itemScript = listEnhanceItems[i];
            float xValue = GetXPosValue(fValue, itemScript.CenterOffSet);
            float scaleValue = GetScaleValue(fValue, itemScript.CenterOffSet);
            float depthValue = depthCurve.Evaluate(fValue + itemScript.CenterOffSet);
            itemScript.UpdateScrollViewItems(xValue, -depthValue * depthFactor, yFixedPositionValue, scaleValue);
        }
    }

    void Update()
    {
        // if (enableLerpTween)
        TweenViewToTarget();
    }

    private void TweenViewToTarget()
    {
        mCurrentDuration += Time.deltaTime;
        if (mCurrentDuration > lerpDuration)
        {
            mCurrentDuration = lerpDuration;
            canChangeItem = true;
            OnTweenOver();
        }

        float percent = mCurrentDuration / lerpDuration;
        float value = Mathf.Lerp(originHorizontalValue, targetHorizontalValue, percent);
        UpdateEnhanceScrollView(value);
    }

    private void OnTweenOver()
    {
        if (curCenterItem != null)
            curCenterItem.SetSelectState(true);
        if (preCenterItem != null)
            preCenterItem.SetSelectState(false);
    }

    // Get the evaluate value to set item's scale
    private float GetScaleValue(float sliderValue, float added)
    {
        float scaleValue = scaleCurve.Evaluate(sliderValue + added);
        return scaleValue;
    }


    // Get the X value set the Item's position
    private float GetXPosValue(float sliderValue, float added)
    {
        float evaluateValue = positionCurve.Evaluate(sliderValue + added) * posCurveFactor;
        return evaluateValue;
    }

    private int GetMoveCurveFactorCount(EnhanceItem preCenterItem, EnhanceItem newCenterItem)
    {
        List<EnhanceItem> tmpList = new List<EnhanceItem>(this.listEnhanceItems);
        SortViewItem(tmpList);
        int factorCount = Mathf.Abs(newCenterItem.RealIndex) - Mathf.Abs(preCenterItem.RealIndex);
        return Mathf.Abs(factorCount);
    }

    // sort item with X so we can know how much distance we need to move the timeLine(curve time line)
    static public int SortPosition(EnhanceItem a, EnhanceItem b) { return a.transform.localPosition.x.CompareTo(b.transform.localPosition.x); }
    private void SortViewItem(List<EnhanceItem> items)
    {
        items.Sort(SortPosition);
        for (int i = items.Count - 1; i >= 0; i--)
            items[i].RealIndex = i;
    }

    public void SetHorizontalTargetItemIndex(EnhanceItem selectItem)
    {
        if (!canChangeItem)
            return;

        if (curCenterItem == selectItem)
            return;

        canChangeItem = false;
        preCenterItem = curCenterItem;
        curCenterItem = selectItem;

        // calculate the direction of moving
        float centerXValue = positionCurve.Evaluate(0.5f) * posCurveFactor;
        bool isRight = false;
        if (selectItem.transform.localPosition.x > centerXValue)
            isRight = true;

        // calculate the offset * dFactor
        int moveIndexCount = GetMoveCurveFactorCount(preCenterItem, selectItem);
        float dvalue = 0.0f;
        if (isRight)
        {
            dvalue = -dFactor * moveIndexCount;
        }
        else
        {
            dvalue = dFactor * moveIndexCount;
        }
        float originValue = targetHorizontalValue;
        targetHorizontalValue += dvalue;
        LerpTweenToTarget(originValue, targetHorizontalValue, true);
    }

    // Click the right button to select the next item.
    public void OnBtnRightClick()
    {
        if (!canChangeItem)
            return;
        int targetIndex = curCenterItem.CurveOffSetIndex + 1;
        if (targetIndex > listEnhanceItems.Count - 1)
            targetIndex = 0;
        SetHorizontalTargetItemIndex(listEnhanceItems[targetIndex]);
    }

    // Click the left button the select next next item.
    public void OnBtnLeftClick()
    {
        if (!canChangeItem)
            return;
        int targetIndex = curCenterItem.CurveOffSetIndex - 1;
        if (targetIndex < 0)
            targetIndex = listEnhanceItems.Count - 1;
        SetHorizontalTargetItemIndex(listEnhanceItems[targetIndex]);
    }

    public float factor = 0.01f;
    // On Drag Move
    public void OnDragEnhanceViewMove(Vector2 delta)
    {
        // In developing
        return;
        if (Mathf.Abs(delta.x) > 0.0f)
        {
            targetHorizontalValue += delta.x * factor;
            LerpTweenToTarget(0.0f, targetHorizontalValue, false);
        }
    }
}