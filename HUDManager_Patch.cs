using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[HarmonyPatch(typeof(HUDManager))]
internal class HUDManager_Patch
{
    private static GameObject canvasObj = null;
    private static Text spectatorInfoText = null;
    private static PlayerControllerB localPlayer = null;

    [HarmonyPatch("Update")]
    [HarmonyPostfix]
    public static void HarmonyPatch_Update()
    {
        //check if player is dead and spectating
        if (GameNetworkManager.Instance != null && GameNetworkManager.Instance.localPlayerController != null && GameNetworkManager.Instance.localPlayerController.isPlayerDead && HUDManager.Instance.hasLoadedSpectateUI)
        {
            localPlayer = GameNetworkManager.Instance.localPlayerController;
            //UpdateInput();
            UpdateUI();
        }
        //disable canvas if not spectating
        else if (canvasObj != null && canvasObj.activeSelf)
        {
            canvasObj.SetActive(false);
        }
    }

    [HarmonyPatch("UpdateBoxesSpectateUI")]
    [HarmonyPostfix]
    public static void HarmonyPatch_UpdateBoxesSpectateUI()
    {
        PlayerControllerB currentPlayer;
        Animator key = null;
        for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
        {
            currentPlayer = StartOfRound.Instance.allPlayerScripts[i];
            if (currentPlayer.isPlayerDead && HUDManager.Instance.spectatingPlayerBoxes.Values.Contains(currentPlayer)) 
            {
                key = HUDManager.Instance.spectatingPlayerBoxes.FirstOrDefault((KeyValuePair<Animator, PlayerControllerB> x) => x.Value == currentPlayer).Key;
                if(key != null && key.gameObject.activeSelf)
                {
                    TextMeshProUGUI[] textObjs = key.gameObject.GetComponentsInChildren<TextMeshProUGUI>();
                    if(textObjs[1].gameObject.GetComponent<ContentSizeFitter>() == null)
                    {
                        ContentSizeFitter fitter = textObjs[1].gameObject.AddComponent<ContentSizeFitter>();
                        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    }
                    textObjs[1].alignment = TextAlignmentOptions.Left;
                    textObjs[1].text = "\nDead: " + Enum.GetName(typeof(CauseOfDeath), currentPlayer.causeOfDeath);
                }
            }
        }
    }

    private static void UpdateInput()
    {
        //get scroll wheel input
        float scrollWheelInput = IngamePlayerSettings.Instance.playerInput.actions.FindAction("SwitchItem").ReadValue<float>();

        //go forward in the spectator list
        if (scrollWheelInput > 0f)
        {
            SpectateNextPlayer(true);
        }
        //go back in the spectator list
        else if (scrollWheelInput < 0f)
        {
            SpectateNextPlayer(false);
        }
    }

    private static void SpectateNextPlayer(bool forward)
    {

    }

    private static void UpdateUI()
    {
        //setup initial UI if it hasn't been already
        if (canvasObj == null)
        {
            SetupUI();
        }
        canvasObj.SetActive(true);
        spectatorInfoText.text = HUDManager.Instance.clockNumber.text;
    }

    private static int GetNextAlivePlayer(ref int index, int startIndex, bool forward)
    {
        if (!localPlayer.playersManager.allPlayerScripts[index].isPlayerDead && localPlayer.playersManager.allPlayerScripts[index].isPlayerControlled && localPlayer.playersManager.allPlayerScripts[index] != localPlayer)
        {
            return index;
        }
        else if(forward)
        {
            index++;
            if(index >= localPlayer.playersManager.allPlayerScripts.Length)
            {
                index = 0;
            }
        }
        else
        {
            index--;
            if (index < 0)
            {
                index = localPlayer.playersManager.allPlayerScripts.Length - 1;
            }
        }

        if (index == startIndex)
        {
            return -1;
        }
        else
        {
            return GetNextAlivePlayer(ref index, startIndex, forward);
        }
    }

    private static void SetupUI()
    {
        SetupCanvas();
        spectatorInfoText = CreateText(HUDManager.Instance.clockNumber.text, HUDManager.Instance.clockNumber.font.sourceFontFile, 30, canvasObj.transform, new Vector2(20, -20), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
    }

    private static void SetupCanvas()
    {
        canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = Camera.main;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
    }

    private static Text CreateText(string text, Font font, int fontSize, Transform parent, Vector2 localPosition, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        GameObject textObj = new GameObject();
        Text textComponent = textObj.AddComponent<Text>();
        textComponent.font = HUDManager.Instance.clockNumber.font.sourceFontFile;
        textComponent.fontSize = 30;
        textComponent.text = text;
        ContentSizeFitter contentSizeFitter = textObj.AddComponent<ContentSizeFitter>();
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform rectTransform = textObj.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.localPosition = localPosition;
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.localScale = new Vector3(1, 1, 1);

        return textComponent;
    }
}
