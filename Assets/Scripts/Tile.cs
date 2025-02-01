using UnityEngine;

public class Tile : MonoBehaviour
{
    public Vector2Int position;
    private ChessManager chessManager;

    void Start()
    {
        // Find ChessManager in the scene dynamically
        chessManager = FindFirstObjectByType<ChessManager>();

        if (chessManager == null)
        {
            Debug.LogError("ChessManager not found in the scene!");
        }
    }

    void OnMouseDown()
    {
        if (ChessManager.Instance == null) return; // Ensure ChessManager exists

        int boardX = Mathf.RoundToInt(transform.position.x);
        int boardY = Mathf.RoundToInt(transform.position.y);

        // Flip for Black, but ensure it's only applied **once**
        if (!ChessManager.Instance.isWhiteTurn) 
        {
            boardY = 7 - boardY; 
        }

        Vector2Int clickedPosition = new Vector2Int(boardX, boardY);
        Debug.Log($"🟢 Tile Clicked at {clickedPosition}");

        if (ChessManager.Instance.selectedPiece != null)
        {
            ChessManager.Instance.HandleTileClick(clickedPosition);
        }
        else
        {
            ChessManager.Instance.SelectPieceAt(clickedPosition);
        }
    }


}
