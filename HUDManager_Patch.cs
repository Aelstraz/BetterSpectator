using BetterSpectator;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[HarmonyPatch(typeof(HUDManager))]
internal class HUDManager_Patch
{
    private static GameObject canvasObj = null;
    private static Text clockText = null;
    private static bool onLocalPlayerDeadTriggered = false;
    private static bool onLocalPlayerAliveTriggered = false;

    [HarmonyPatch("Update")]
    [HarmonyPostfix]
    public static void HarmonyPatch_Update()
    {
        if(IsLocalPlayerDeadAndSpectating() && !onLocalPlayerDeadTriggered)
        {
            onLocalPlayerDeadTriggered = true;
            onLocalPlayerAliveTriggered = false;
            OnLocalPlayerDead();
        }
        else if (!IsLocalPlayerDeadAndSpectating() && !onLocalPlayerAliveTriggered)
        {
            onLocalPlayerAliveTriggered = true;
            onLocalPlayerDeadTriggered = false;
            OnLocalPlayerAlive();
        }

        if (IsLocalPlayerDeadAndSpectating())
        {
            UpdateExtraInput();
            UpdateClock();
        }
    }

    private static void OnLocalPlayerDead()
    {
        SetupClock();
        SetupChat();
    }

    private static void OnLocalPlayerAlive()
    {
        RemoveClock();
        RemoveChat();
    }

    private static void SetupClock()
    {
        if (Settings.isClockEnabled.Value)
        {
            canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.worldCamera = Camera.main;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            clockText = CreateText(HUDManager.Instance.clockNumber.text, HUDManager.Instance.clockNumber.font.sourceFontFile, 30, canvasObj.transform, new Vector2(20, -20), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
        }
    }

    private static void RemoveClock()
    {
        if (clockText != null)
        {
            GameObject.Destroy(clockText.gameObject);
            clockText = null;
        }
        if (canvasObj != null)
        {
            GameObject.Destroy(canvasObj);
            canvasObj = null;
        }
    }

    private static void UpdateExtraInput()
    {
        if (Settings.isExtraInputEnabled.Value)
        {
            //get scroll wheel input
            float scrollWheelInput = IngamePlayerSettings.Instance.playerInput.actions.FindAction("SwitchItem").ReadValue<float>();

            //go forward in the spectator list
            if (Keyboard.current[Key.RightArrow].wasPressedThisFrame || scrollWheelInput > 0f)
            {
                SpectateNextPlayer(true);
            }
            //go back in the spectator list
            else if (Keyboard.current[Key.LeftArrow].wasPressedThisFrame || scrollWheelInput < 0f)
            {
                SpectateNextPlayer(false);
            }
        }
    }

    private static void UpdateClock()
    {
        if (Settings.isClockEnabled.Value)
        {
            clockText.text = HUDManager.Instance.clockNumber.text;
        }
    }

    private static void SetupChat()
    {
        if (Settings.isChatEnabled.Value)
        {
            HUDManager.Instance.HideHUD(false);
            //{ Inventory, Chat, PlayerInfo, Tooltips, InstabilityCounter, Clock }
            HUDManager.Instance.HUDElements[0].targetAlpha = 0f;
            HUDManager.Instance.HUDElements[2].targetAlpha = 0f;
            HUDManager.Instance.HUDElements[5].targetAlpha = 0f;
            HUDManager.Instance.HUDElements[5].canvasGroup.gameObject.SetActive(false);
        }
    }

    private static void RemoveChat()
    {
        if (Settings.isChatEnabled.Value)
        {
            HUDManager.Instance.HUDElements[0].targetAlpha = 1f;
            HUDManager.Instance.HUDElements[2].targetAlpha = 1f;
            HUDManager.Instance.HUDElements[5].targetAlpha = 1f;
            HUDManager.Instance.HUDElements[5].canvasGroup.gameObject.SetActive(true);
        }
    }

    [HarmonyPatch("UpdateBoxesSpectateUI")]
    [HarmonyPostfix]
    public static void HarmonyPatch_UpdateBoxesSpectateUI()
    {
        if (Settings.isCauseOfDeathEnabled.Value && IsLocalPlayerDeadAndSpectating())
        {
            //update spectate UI boxes with cause of death info
            PlayerControllerB currentPlayer;
            Animator key = null;
            TextMeshProUGUI[] textObjs;
            ContentSizeFitter fitter;

            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                currentPlayer = StartOfRound.Instance.allPlayerScripts[i];
                if (currentPlayer.isPlayerDead && HUDManager.Instance.spectatingPlayerBoxes.Values.Contains(currentPlayer))
                {
                    key = HUDManager.Instance.spectatingPlayerBoxes.FirstOrDefault((KeyValuePair<Animator, PlayerControllerB> x) => x.Value == currentPlayer).Key;
                    if (key != null && key.gameObject.activeSelf)
                    {
                        textObjs = key.gameObject.GetComponentsInChildren<TextMeshProUGUI>();
                        if (textObjs[1].gameObject.GetComponent<ContentSizeFitter>() == null)
                        {
                            fitter = textObjs[1].gameObject.AddComponent<ContentSizeFitter>();
                            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                        }
                        textObjs[1].alignment = TextAlignmentOptions.BaselineLeft;
                        textObjs[1].text = "\nDead: " + Enum.GetName(typeof(CauseOfDeath), currentPlayer.causeOfDeath);
                    }
                }
            }
        }
    }

    [HarmonyPatch("AddPlayerChatMessageClientRpc")]
    [HarmonyPostfix]
    [ClientRpc]
    public static void HarmonyPatch_AddPlayerChatMessageClientRpc(ref string chatMessage, ref int playerId)
    {
        if (Settings.isChatEnabled.Value && IsLocalPlayerDeadAndSpectating())
        {
            //show chat messages between dead players
            NetworkManager networkManager = HUDManager.Instance.NetworkManager;
            PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
            StartOfRound playersManager = HUDManager.Instance.playersManager;

            if ((object)networkManager != null && networkManager.IsListening && localPlayer.isPlayerDead)
            {
                if ((networkManager.IsClient || networkManager.IsHost) && (playersManager.allPlayerScripts[playerId].isPlayerDead || IsSpectatedPlayer(playerId) || IsInRangeOfSpectatedPlayer(playerId)))
                {
                    HUDManager.Instance.AddChatMessage(chatMessage, playersManager.allPlayerScripts[playerId].playerUsername);
                }
            }
        }
    }

    [HarmonyPatch("AddChatMessage")]
    [HarmonyPostfix]
    private static void AddChatMessage(ref string chatMessage, string nameOfUserWhoTyped = "")
    {
        if (Settings.isChatEnabled.Value)
        {
            PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
            int playerIndex = GetPlayerIndex(nameOfUserWhoTyped);
            string newName = nameOfUserWhoTyped + "(Dead)";
            if (playerIndex >= 0 && localPlayer.playersManager.allPlayerScripts[playerIndex].isPlayerDead && !HUDManager.Instance.ChatMessageHistory[HUDManager.Instance.ChatMessageHistory.Count - 1].Contains(newName))
            {
                HUDManager.Instance.ChatMessageHistory[HUDManager.Instance.ChatMessageHistory.Count - 1] = HUDManager.Instance.ChatMessageHistory[HUDManager.Instance.ChatMessageHistory.Count - 1].Replace(nameOfUserWhoTyped, newName);
                HUDManager.Instance.chatText.text = "";
                for (int i = 0; i < HUDManager.Instance.ChatMessageHistory.Count; i++)
                {
                    TextMeshProUGUI textMeshProUGUI = HUDManager.Instance.chatText;
                    textMeshProUGUI.text = textMeshProUGUI.text + "\n" + HUDManager.Instance.ChatMessageHistory[i];
                }
            }
        }
    }

    private static int GetPlayerIndex(string playerName)
    {
        int index = -1;
        PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
        for (int i = 0; i < localPlayer.playersManager.allPlayerScripts.Length; i++)
        {
            if (localPlayer.playersManager.allPlayerScripts[i].playerUsername.Equals(playerName))
            {
                index = i;
                break;
            }
        }
        return index;
    }

    private static bool IsSpectatedPlayer(int playerId)
    {
        PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
        if (localPlayer.spectatedPlayerScript != null && (int)localPlayer.spectatedPlayerScript.playerClientId == playerId)
        {
            return true;
        }
        return false;
    }

    private static bool IsInRangeOfSpectatedPlayer(int playerId)
    {
        PlayerControllerB specatedPlayer = GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript;
        StartOfRound playersManager = HUDManager.Instance.playersManager;
        if ((specatedPlayer.holdingWalkieTalkie && playersManager.allPlayerScripts[playerId].holdingWalkieTalkie) || Vector3.Distance(specatedPlayer.transform.position, playersManager.allPlayerScripts[playerId].transform.position) <= 25f)
        {
            return true;
        }
        return false;
    }

    [HarmonyPatch("EnableChat_performed")]
    [HarmonyPostfix]
    private static void HarmonyPatch_EnableChat_performed(ref InputAction.CallbackContext context)
    {
        if (Settings.isChatEnabled.Value && IsLocalPlayerDeadAndSpectating())
        {
            //allow chat box UI to be interactable when dead
            PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
            if (context.performed && !(localPlayer == null) && ((localPlayer.IsOwner && (!HUDManager.Instance.IsServer || localPlayer.isHostPlayerObject)) || localPlayer.isTestingPlayer) && localPlayer.isPlayerDead && !localPlayer.inTerminalMenu)
            {
                ShipBuildModeManager.Instance.CancelBuildMode();
                localPlayer.isTypingChat = true;
                HUDManager.Instance.chatTextField.Select();
                HUDManager.Instance.PingHUDElement(HUDManager.Instance.Chat, 0.1f, 1f, 1f);
                HUDManager.Instance.typingIndicator.enabled = true;
            }
        }
    }

    [HarmonyPatch("SubmitChat_performed")]
    [HarmonyPostfix]
    public static void HarmonyPatch_SubmitChat_performed(ref InputAction.CallbackContext context)
    {
        if (Settings.isChatEnabled.Value && IsLocalPlayerDeadAndSpectating())
        {
            PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
            if (!context.performed || localPlayer == null || !localPlayer.isTypingChat || ((!localPlayer.IsOwner || (HUDManager.Instance.IsServer && !localPlayer.isHostPlayerObject)) && !localPlayer.isTestingPlayer))
            {
                return;
            }

            if (!string.IsNullOrEmpty(HUDManager.Instance.chatTextField.text) && HUDManager.Instance.chatTextField.text.Length < 50)
            {
                HUDManager.Instance.AddTextToChatOnServer(HUDManager.Instance.chatTextField.text, (int)localPlayer.playerClientId);
            }

            localPlayer.isTypingChat = false;
            HUDManager.Instance.chatTextField.text = "";
            EventSystem.current.SetSelectedGameObject(null);
            HUDManager.Instance.PingHUDElement(HUDManager.Instance.Chat);
            HUDManager.Instance.typingIndicator.enabled = false;
        }
    }

    private static void SpectateNextPlayer(bool forward)
    {
        PlayerControllerB localPlayer = GameNetworkManager.Instance.localPlayerController;
        int num = 0;
        bool foundPlayer = false;

        if (localPlayer.spectatedPlayerScript != null)
        {
            num = (int)localPlayer.spectatedPlayerScript.playerClientId;
        }
        if (forward)
        {
            for (int i = 0; i < localPlayer.playersManager.allPlayerScripts.Length; i++)
            {
                num = (num + 1) % localPlayer.playersManager.allPlayerScripts.Length;
                if (!localPlayer.playersManager.allPlayerScripts[num].isPlayerDead && localPlayer.playersManager.allPlayerScripts[num].isPlayerControlled && localPlayer.playersManager.allPlayerScripts[num] != localPlayer)
                {
                    localPlayer.spectatedPlayerScript = localPlayer.playersManager.allPlayerScripts[num];
                    localPlayer.SetSpectatedPlayerEffects();
                    foundPlayer = true;
                    break;
                }
            }
        }
        else
        {
            for (int i = localPlayer.playersManager.allPlayerScripts.Length - 1; i >= 0; i--)
            {
                if(num == 0)
                {
                    num = (int)localPlayer.playersManager.allPlayerScripts[localPlayer.playersManager.allPlayerScripts.Length - 1].playerClientId;
                }
                num = (num - 1) % localPlayer.playersManager.allPlayerScripts.Length;
                if (!localPlayer.playersManager.allPlayerScripts[num].isPlayerDead && localPlayer.playersManager.allPlayerScripts[num].isPlayerControlled && localPlayer.playersManager.allPlayerScripts[num] != localPlayer)
                {
                    localPlayer.spectatedPlayerScript = localPlayer.playersManager.allPlayerScripts[num];
                    localPlayer.SetSpectatedPlayerEffects();
                    foundPlayer = true;
                    break;
                }
            }
        }

        if (!foundPlayer)
        {
            if (localPlayer.deadBody != null && localPlayer.deadBody.gameObject.activeSelf)
            {
                localPlayer.spectateCameraPivot.position = localPlayer.deadBody.bodyParts[0].position;
                localPlayer.RaycastSpectateCameraAroundPivot();
            }
            StartOfRound.Instance.SetPlayerSafeInShip();
        }
    }

    private static Text CreateText(string text, Font font, int fontSize, Transform parent, Vector2 localPosition, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        GameObject textObj = new GameObject();
        Text textComponent = textObj.AddComponent<Text>();
        textComponent.font = font;
        textComponent.fontSize = fontSize;
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

    private static bool IsLocalPlayerDeadAndSpectating()
    {
        return GameNetworkManager.Instance != null && GameNetworkManager.Instance.localPlayerController != null && GameNetworkManager.Instance.localPlayerController.isPlayerDead && HUDManager.Instance.hasLoadedSpectateUI;
    }
}
