
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System;
using VRC.SDK3.Data;
using VRC.SDK3.Components;
using VRC.Udon.Serialization.OdinSerializer;
using TMPro;
using UnityEngine.UIElements;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class OttLogger : UdonSharpBehaviour {
    [Header("Logger configuration")]
    [Tooltip("Do you also want the logs to be outputted in VRC Logs and Unity console ?")]
    [SerializeField] private bool logToConsoleFiles = true;

    [Tooltip("Maximum log items to store, older will be deleted when new ones inserted")]
    [SerializeField] private int maxStoredLogMessages = 500;

    [Tooltip("Do you want to automatically log player joining and leaving ?")]
    [SerializeField] private bool logPlayersJoinLeave = true;

    [Header("Logger internals")]
    [Tooltip("The TMP Dropdown holding the log filter categories")]
    [SerializeField] private TMP_Dropdown loggerDropdownCategories;
    [Tooltip("The TMP Text object for the logs counter")]
    [SerializeField] private TextMeshProUGUI loggerLogsCount;
    [Tooltip("The TMP Text object for the player name")]
    [SerializeField] private TextMeshProUGUI loggerPlayerName;
    [Tooltip("The TMP Text object for the logs themselves")]
    [SerializeField] private TextMeshProUGUI loggerLogs;

    [Header("State colors")]
    [SerializeField] private Color32 colorInfo = new Color(210/255f, 210/255f, 210/255f, 255/255f);
    [SerializeField] private Color32 colorWarn = new Color(255/255f, 136/255f, 0/255f, 255/255f);
    [SerializeField] private Color32 colorError = new Color(135/255f, 0/255f, 0/255f, 255/255f);
    [SerializeField] private Color32 colorTimestamp = new Color(0/255f, 163/255f, 20/255f, 255/255f);
    [SerializeField] private Color32 colorClass = new Color(0/255f, 160/255f, 255/255f, 255/255f);
    [SerializeField] private Color32 colorMessage = new Color(210/255f, 210/255f, 210/255f, 255/255f);

    // Non-exposed variables
    [OdinSerialize] private DataList logsMessages;
    [OdinSerialize] private DataList logsCategories;
    private string playerJoinLeaveClassName = "PlayerEvents";
    private string anythingClassName = "All";
    private string currentLogsCategory;

    void Start() {
        // Initialize some stuff
        addLogCategory(anythingClassName);
        addLogCategory(playerJoinLeaveClassName);
        currentLogsCategory = anythingClassName;

        VRCPlayerApi player = Networking.LocalPlayer;

        if (Utilities.IsValid(player) && Utilities.IsValid(loggerPlayerName)) {
            loggerPlayerName.text = $"{player.displayName}";
        }

        Debug.Log($"[OTT_LOGGER] Started");
    }

    public void Log(UnityEngine.Object classObject, string message) {
        string className = ((UdonSharpBehaviour)classObject).GetUdonTypeName();
        doLog(className, 0, message);
    }

    public void LogWarn(UnityEngine.Object classObject, string message) {
        string className = ((UdonSharpBehaviour)classObject).GetUdonTypeName();
        doLog(className, 1, message);
    }

    public void LogError(UnityEngine.Object classObject, string message) {
        string className = ((UdonSharpBehaviour)classObject).GetUdonTypeName();
        doLog(className, 2, message);
    }

    private void doLog(string className, int level, string message) {
        DateTime logTime = Networking.GetNetworkDateTime();
        string time = logTime.ToString("HH:mm:ss");

        string levelStr;
        switch (level) {
            case 1:
                levelStr = $"<color={ToRGBHex(colorWarn)}>[WARN]</color>";
                break;
            case 2:
                levelStr = $"<color={ToRGBHex(colorError)}>[ERROR]</color>";
                break;
            default:
                levelStr = $"<color={ToRGBHex(colorInfo)}>[INFO]</color>";
                break;
        }

        if (logToConsoleFiles) {
            #if UNITY_EDITOR
            Debug.Log($"<color={ToRGBHex(colorTimestamp)}>[{time}]</color>{levelStr}<color={ToRGBHex(colorClass)}>[{className}]</color> <color={ToRGBHex(colorMessage)}>{message}</color>");
            #else
            Debug.Log($"[{time}]{levelStr}[{className}] {message}");
            #endif
        }
        
        addLogCategory(className);
        refreshLogCategories();

        addLog(className, time, levelStr, message);
        refreshLogs();
    }

    private void addLogCategory(string categoryName) {
        if (!logsCategories.Contains(categoryName)) {
            logsCategories.Add(categoryName);
            Debug.Log($"[OTT_LOGGER] Added category: '{categoryName}'");
        }
    }

    private void refreshLogCategories() {
        // TODO FIXME
        if (!Utilities.IsValid(loggerDropdownCategories)) {
            Debug.Log($"[OTT_LOGGER] Invalid loggerDropdownCategories");
            return;
        }
        loggerDropdownCategories.ClearOptions();
        for (int i=0; i<logsCategories.Count; i++) {
            if (logsCategories.TryGetValue(i, out DataToken DT_category)) {
                string[] opts = new string[1];
                opts[0] = DT_category.String;

                loggerDropdownCategories.AddOptions(opts);
            }
        }
    }

    private void addLog(string className, string time, string level, string message) {
        // TODO FIXME
        if (logsMessages.Count >= maxStoredLogMessages) {
            logsMessages.RemoveAt(0);
        }
        DataDictionary logItem = new DataDictionary();
        logItem.Add("className", className);
        logItem.Add("time", time);
        logItem.Add("level", level);
        logItem.Add("message", message);
        logsMessages.Add(logItem);
    }

    private void refreshLogs() {
        if (!Utilities.IsValid(loggerLogs)) {
            Debug.Log($"[OTT_LOGGER] Invalid loggerLogs");
            return;
        }
        // Empty box
        loggerLogs.text = "";
        
        // Refresh logs based on current log category; in reverse order
        for (int i=logsMessages.Count; i>=0; i--) {
            if (logsMessages.TryGetValue(i, out DataToken DT_logitem)) {
                string className = "";
                string time = "";
                string level = "";
                string message = "";
                if (DT_logitem.DataDictionary.TryGetValue("className", out DataToken DT_className)) {
                    className = DT_className.String;
                }
                if (DT_logitem.DataDictionary.TryGetValue("time", out DataToken DT_time)) {
                    time = DT_time.String;
                }
                if (DT_logitem.DataDictionary.TryGetValue("level", out DataToken DT_level)) {
                    level = DT_level.String;
                }
                if (DT_logitem.DataDictionary.TryGetValue("message", out DataToken DT_message)) {
                    message = DT_message.String;
                }
                // Then add log to text field if category matches or category is All
                if (currentLogsCategory == anythingClassName || className == currentLogsCategory) {
                    string logItem = $"<color={ToRGBHex(colorTimestamp)}>[{time}]</color>{level}<color={ToRGBHex(colorClass)}>[{className}]</color> <color={ToRGBHex(colorMessage)}>{message}</color>";
                    loggerLogs.text = loggerLogs.text + "\n" + logItem;
                }
            }
        }

        // And update the counter
        if (!Utilities.IsValid(loggerLogsCount)) {
            Debug.Log($"[OTT_LOGGER] Invalid loggerLogsCount");
        } else {
            loggerLogsCount.text = $"<size=30>{logsMessages.Count}</size>/<size=20>{maxStoredLogMessages}</size>";
        }
    }

    public void LogCategoryChanged() {
        // Get the new value index from the DropDown
        int ddIndex = loggerDropdownCategories.value;
        // Fetch the value string from the logs categories DataList
        if (logsCategories.TryGetValue(ddIndex, out DataToken category)) {
            currentLogsCategory = category.String;
            Debug.Log($"[OTT_LOGGER] Category changed to '{currentLogsCategory}'");
        }
        // Then refresh logs
        refreshLogs();
    }

    public override void OnPlayerJoined(VRCPlayerApi player) {
        if (Utilities.IsValid(player) && logPlayersJoinLeave) {
            doLog(playerJoinLeaveClassName, 0, $"Player joined: {player.displayName}");
        }
    }

    public override void OnPlayerLeft(VRCPlayerApi player) {
        if (Utilities.IsValid(player) && logPlayersJoinLeave) {
            doLog(playerJoinLeaveClassName, 0, $"Player left: {player.displayName}");
        }
    }

    private static string ToRGBHex(Color c) {
        return string.Format("#{0:X2}{1:X2}{2:X2}", ToByte(c.r), ToByte(c.g), ToByte(c.b));
    }

    private static byte ToByte(float f) {
        f = Mathf.Clamp01(f);
        return (byte)(f * 255);
    }
}
