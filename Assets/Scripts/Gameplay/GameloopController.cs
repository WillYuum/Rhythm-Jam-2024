using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using GameplayUtils;
using UnityEngine;

public class GameloopController : MonoBehaviour
{
    public Vector2Int PlayerCellPosition;
    private InputManager _inputManager;
    private RhythmController _rhythmController;
    private int _currentRoomLevel;

    private MainGameUI _mainGameUI;

    public int CurrentRoomLevel
    {
        get => _currentRoomLevel;
        set
        {
            _currentRoomLevel = value;
            _rhythmController.SetCurrentLayer(value);
        }
    }

    private GridController _selectedGrid;

    private Player _player;


    private bool _gameIsActive = false;

    private void Awake()
    {
        _inputManager = FindObjectOfType<InputManager>();
        _selectedGrid = FindObjectOfType<GridController>();
        _player = FindObjectOfType<Player>();
        _rhythmController = FindObjectOfType<RhythmController>();
        _mainGameUI = FindObjectOfType<MainGameUI>();
    }


    private void Start()
    {
        PlayerCellPosition = new Vector2Int(0, 0);


        // Tilemap currentTileMap = _mapSwitcher.GetActiveMap();
        Vector2 newWorldPos = _selectedGrid.GetWorldPosFromCellPos(PlayerCellPosition);
        _player.UpdatePosition(newWorldPos);
    }

    private void Update()
    {

    }

    public void StartLoop()
    {
        if (_gameIsActive)
        {
            Debug.LogWarning("|GameLoopController| Game is already active!");
            return;
        }

        print("|GameLoopController| Starting game loop");

        _gameIsActive = true;
        _inputManager.OnMoveInput += MovePlayer;
        _inputManager.OnClickTransition += HandlePlayerClickTransition;
        _inputManager.Toggle(true);

        _rhythmController.LoadMusic();
        _rhythmController.ToggleMusic(true);
        CurrentRoomLevel = 1;

        _selectedGrid.BuildUpObjectsInRoom(CurrentRoomLevel);
    }



    private void MovePlayer(Vector2Int direction)
    {
        if (!_gameIsActive)
            return;


        MusicTracker beatTracker_V2 = FindObjectOfType<MusicTracker>();

        bool clickedAroundBeat = beatTracker_V2.IsWithinBeatWindow(0.85f, 0.5f);
        if (!clickedAroundBeat)
        {
            Debug.Log("Not near a beat");
            return;
        }

        Vector2Int newCellPos = PlayerCellPosition + direction;


        Debug.Log("Moving to " + newCellPos);

        if (_selectedGrid.CheckIfCanMoveToPosition(newCellPos))
        {
            Debug.Log("Can't move to " + newCellPos);
            return;
        }


        PlayerCellPosition = newCellPos;

        Vector2 newWorldPos = _selectedGrid.GetWorldPosFromCellPos(newCellPos);
        _player.UpdatePosition(newWorldPos);
    }

    private void OnGUI()
    {
        //draw the player pos
        // GUI.Label(new Rect(10, 10, 100, 20), "Player pos: " + PlayerCellPosition.x + " " + PlayerCellPosition.y);

        string playerPos = "Player pos: " + PlayerCellPosition.x + " " + PlayerCellPosition.y;
        // GUI.Box(new Rect(10, 30, 100, 20), "Room level: " + CurrentRoomLevel);
        GUILayout.Box(playerPos, GUILayout.Width(100), GUILayout.Height(20));
    }


    private void HandlePlayerClickTransition()
    {
        if (!_gameIsActive)
            return;


        TransitionRoomDetector transitionRoomDetector = _rhythmController.GetComponent<TransitionRoomDetector>();

        if (transitionRoomDetector.CheckIfOnTransitionBeat() && CheckIfPlayerIsOnExitDoor())
        {
            //Handle logic for checking if player clicked near beat when transition queue is played
            // Debug.Break();
            CurrentRoomLevel++;


            bool currentRoomIsLast = _selectedGrid.CurrentRoomIsLastRoom(CurrentRoomLevel);
            if (currentRoomIsLast)
            {
                Debug.Log("Last room");
                WinGame();
            }
            else
            {
                _selectedGrid.BuildUpObjectsInRoom(CurrentRoomLevel);
            }
        }
    }


    private bool CheckIfPlayerIsOnExitDoor()
    {
        if (!_gameIsActive)
            return false;

        Vector2Int playerCellPos = PlayerCellPosition;

        for (int i = 0; i < _selectedGrid.CurrentRoomData.DoorsToExitPositions.Length; i++)
        {
            Vector2Int exitDoorPos = _selectedGrid.CurrentRoomData.DoorsToExitPositions[i];
            if (playerCellPos == exitDoorPos)
            {
                return true;
            }
        }


        return false;
    }


    public void AfterEnemyMove(EnemyFollowAndDance enemy)
    {
        if (!_gameIsActive)
            return;

        Vector2Int enemyCellPos = _selectedGrid.GetCellPosFromWorldPos(enemy.transform.position);
        if (enemyCellPos == PlayerCellPosition)
        {
            Debug.Log("Player got caught by enemy");
            CurrentRoomLevel--;
            _selectedGrid.BuildUpObjectsInRoom(CurrentRoomLevel);
        }
    }



    private void WinGame()
    {
        if (!_gameIsActive)
            return;

        _gameIsActive = false;
        _inputManager.Toggle(false);
        _rhythmController.ToggleMusic(false);

        _mainGameUI.ShowEndGameScreen();
    }
}
