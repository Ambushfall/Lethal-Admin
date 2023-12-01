using System.Collections.Generic;
using LethalAdmin.Logging;
using UnityEngine;

namespace LethalAdmin;

public class UI : MonoBehaviour
{
    private static readonly List<UI> Instances = new();
    private bool _menuOpen;

    private Rect _windowRect;

    private readonly GUILayoutOption[] _options = {
        GUILayout.Width(800),
        GUILayout.Height(400)
    };

    private readonly GUILayoutOption[] _labelOptions =
    {
        GUILayout.MinWidth(300)
    };

    private readonly GUIStyle _yellowText = new();

    private Vector2 _scrollPosition;
    private ViewMode _currentViewMode = ViewMode.Users;
    private bool _menuAlwaysOpen;
    
    private void Awake()
    {
        Instances.Add(this);
        _yellowText.normal.textColor = Color.yellow;
    }

    private void OnDestroy()
    {
        Instances.Remove(this);
    }

    public static void SetMenuForAll(bool value)
    {
        if (Instances.Count == 0 && value)
        {
            // Create new UI if we want to make it visible, but there exists none yet
            var obj = new GameObject("Lethal Admin UI");
            obj.AddComponent<UI>();

            Plugin.Instance.LogInfo("Creating Admin UI instance");
        }

        foreach (var instance in Instances)
        {
            instance.SetMenu(value);
        }
    }

    private void SetMenu(bool value)
    {
        _menuOpen = value;
    }

    private void OnGUI()
    {
        if (!StartOfRound.Instance.IsServer || (!_menuOpen && !_menuAlwaysOpen)) return;

        var controlID = GUIUtility.GetControlID(FocusType.Passive);
        _windowRect = GUILayout.Window(controlID, _windowRect, DrawUI, "Lethal Admin Menu", _options);
    }

    private void DrawUI(int windowID)
    {
        GUILayout.BeginVertical();
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true);

        switch (_currentViewMode)
        {
            case ViewMode.Users:
                DrawUsers();
                break;
            case ViewMode.Bans:
                DrawBans();
                break;
            case ViewMode.Logs:
                DrawLogs();
                break;
            default:
                DrawUsers();
                break;
        }

        GUILayout.EndScrollView();
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Users"))
        {
            _currentViewMode = ViewMode.Users;
        }

        if (GUILayout.Button("Logs"))
        {
            _currentViewMode = ViewMode.Logs;
        }
        
        if (GUILayout.Button("Bans"))
        {
            _currentViewMode = ViewMode.Bans;
        }

        GUILayout.EndHorizontal();
        if (GUILayout.Button("Toggle ship lights"))
        {
            StartOfRound.Instance.shipRoomLights.ToggleShipLights();
        }
        
        if (StartOfRound.Instance.connectedPlayersAmount + 1 - StartOfRound.Instance.livingPlayers >= 1 && 
            !TimeOfDay.Instance.shipLeavingAlertCalled) // Requires at least 1 dead player and that there has not been any early leave call
        {
            if (GUILayout.Button("Override vote (will trigger auto pilot) [Experimental]"))
            {
                var time = TimeOfDay.Instance;
                time.votesForShipToLeaveEarly = StartOfRound.Instance.connectedPlayersAmount;
                time.votedShipToLeaveEarlyThisRound = false; // Make sure the game is convinced we didn't vote yet
                time.VoteShipToLeaveEarly(); // Trigger the vote
            }
        }

        _menuAlwaysOpen = GUILayout.Toggle(_menuAlwaysOpen, "Always show menu");
        GUILayout.EndVertical();
        GUI.DragWindow(new Rect(0, 0, 10000, 500));
    }

    private void DrawUsers()
    {
        GUILayout.Label("Players that are not connected are shown in yellow!");
        
        var players = KickBanTools.GetPlayers();
        var id = 0;
        
        foreach (var player in players)
        {
            GUILayout.BeginHorizontal();

            if (player.Connected)
            {
                GUILayout.Label($"({id}) {player}", _labelOptions);
            }
            else
            {
                GUILayout.Label($"({id}) {player}", _yellowText, _labelOptions);
            }
            

            if (id != 0) // Owner should not kick/ban themselves
            {
                if (GUILayout.Button("Kick"))
                {
                    KickBanTools.KickPlayer(player);
                }

                if (GUILayout.Button("Ban"))
                {
                    KickBanTools.BanPlayer(player);
                }
            }

            GUILayout.Toggle(player.UsingWalkie, "Using walkie");
            
            GUILayout.EndHorizontal();

            id++;
        }
    }

    private void DrawLogs()
    {
        var logs = LethalLogger.GetLogs();

        foreach (var log in logs)
        {
            GUILayout.Label(log.GetTimeFormattedString());
        }
    }

    private void DrawBans()
    {
        var players = KickBanTools.GetBannedUsers();

        foreach (var player in players)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(player.ToString(), _labelOptions);
            
            if (GUILayout.Button($"Unban"))
            {
                KickBanTools.UnbanPlayer(player);
            }
            
            GUILayout.EndHorizontal();
        }
    }

    private enum ViewMode
    {
        Users, Logs, Bans
    }
}