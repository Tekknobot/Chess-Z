using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChessManager : MonoBehaviour
{
    public static ChessManager Instance { get; private set; } // Singleton instance

    public GameObject tilePrefab;
    public int projection = 8;
    public GameObject[] whitePiecePrefabs; 
    public GameObject[] blackPiecePrefabs;
    private GameObject[,] tiles = new GameObject[8, 8];

    private List<GameObject> whitePieces = new List<GameObject>();
    private List<GameObject> blackPieces = new List<GameObject>();

    public string turn = "white";
    public bool isPlayerWhite = true;

    public Vector2Int selectedPiece;
    public bool pieceSelected = false;
    public GameObject highlightPrefab;
    private List<GameObject> highlightedTiles = new List<GameObject>();
    public bool isWhiteTurn = true; // True if it's White's turn, false if Black
    private Dictionary<Vector2Int, GameObject> boardPieces = new Dictionary<Vector2Int, GameObject>();

    // Transform containers for captured pieces.
    // Assign these in the Inspector to empty GameObjects positioned below (or above) the board.
    public Transform whiteCaptureContainer;
    public Transform blackCaptureContainer;
    // When the player clicks a capture tile, we wait for confirmation.
    private Vector2Int? pendingCaptureTile = null;

    // --- Board Setup Arrays ---
    private readonly string[,] startBoardWhite = {
        { "r", "n", "b", "q", "k", "b", "n", "r" },
        { "p", "p", "p", "p", "p", "p", "p", "p" },
        { "",  "",  "",  "",  "",  "",  "",  ""  },
        { "",  "",  "",  "",  "",  "",  "",  ""  },
        { "",  "",  "",  "",  "",  "",  "",  ""  },
        { "",  "",  "",  "",  "",  "",  "",  ""  },
        { "P", "P", "P", "P", "P", "P", "P", "P" },
        { "R", "N", "B", "Q", "K", "B", "N", "R" }
    };

    private readonly string[,] startBoardBlack = {
        { "R", "N", "B", "K", "Q", "B", "N", "R" },
        { "P", "P", "P", "P", "P", "P", "P", "P" },
        { "",  "",  "",  "",  "",  "",  "",  ""  },
        { "",  "",  "",  "",  "",  "",  "",  ""  },
        { "",  "",  "",  "",  "",  "",  "",  ""  },
        { "",  "",  "",  "",  "",  "",  "",  ""  },
        { "p", "p", "p", "p", "p", "p", "p", "p" },
        { "r", "n", "b", "k", "q", "b", "n", "r" }
    };

    private Dictionary<string, int> piecePrefabIndex = new Dictionary<string, int>()
    {
        { "P", 0 }, { "R", 1 }, { "N", 2 }, { "B", 3 }, { "Q", 4 }, { "K", 5 }, 
        { "p", 0 }, { "r", 1 }, { "n", 2 }, { "b", 3 }, { "q", 4 }, { "k", 5 }  
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("⚠️ Another instance of ChessManager found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Ensure the instance persists across scenes
    }

    void Start()
    {
        PositionCamera();
        StartCoroutine(InitializeBoard());
    }

    public void SelectPieceAt(Vector2Int position)
    {
        GameObject piece = GetPieceAtPosition(position);

        if (piece != null)
        {
            string pieceTag = piece.tag;
            bool isWhitePiece = pieceTag.Contains("White");
            bool isWhiteTurn = Instance.isWhiteTurn;

            if (isWhitePiece != isWhiteTurn)
            {
                Debug.Log($"⛔ Cannot select {pieceTag} at {position}, it's not your turn!");
                return;
            }

            selectedPiece = position;
            pieceSelected = true;
            Debug.Log($"✅ Selected {piece.tag} at {position}");
            // When selecting, highlight only legal moves.
            HighlightValidMoves(selectedPiece);
        }
        else
        {
            Debug.Log("❌ No piece found at this tile");
        }
    }

    public void EndTurn()
    {
        ToggleTurn();
    }

    void PositionCamera()
    {
        Camera.main.orthographic = true;
        Camera.main.orthographicSize = projection;
        Camera.main.transform.position = new Vector3(3.5f, 3.5f, -10);
        Camera.main.transform.rotation = Quaternion.identity;
        Debug.Log($"Camera set. Player is White: {isPlayerWhite}");
    }

    public void HandleTileClick(Vector2Int gridPos)
    {
        Debug.Log($"🟢 Tile Clicked at {gridPos}");
        Vector2Int adjustedPos = gridPos;
        Debug.Log($"🔄 Adjusted for White's view: {adjustedPos}");

        if (!IsValidPosition(adjustedPos))
        {
            Debug.Log("❌ Clicked outside the board!");
            return;
        }

        GameObject clickedPiece = GetPieceAtPosition(adjustedPos);

        // If no piece is selected, try to select one.
        if (!pieceSelected)
        {
            if (clickedPiece != null)
            {
                string pieceTag = clickedPiece.tag;
                bool isWhitePiece = pieceTag == "WhitePawn" || pieceTag == "WhiteRook" ||
                                    pieceTag == "WhiteKnight" || pieceTag == "WhiteBishop" ||
                                    pieceTag == "WhiteQueen" || pieceTag == "WhiteKing";
                bool isBlackPiece = pieceTag == "BlackPawn" || pieceTag == "BlackRook" ||
                                    pieceTag == "BlackKnight" || pieceTag == "BlackBishop" ||
                                    pieceTag == "BlackQueen" || pieceTag == "BlackKing";
                bool isPlayerPiece = (isPlayerWhite && isWhitePiece) || (!isPlayerWhite && isBlackPiece);
                bool isCorrectTurn = (isWhiteTurn && isWhitePiece) || (!isWhiteTurn && isBlackPiece);

                Debug.Log($"🔎 Found piece: {pieceTag} at {adjustedPos} - Player's Piece? {isPlayerPiece} - Correct Turn? {isCorrectTurn}");

                if (isPlayerPiece && isCorrectTurn)
                {
                    selectedPiece = adjustedPos;
                    pieceSelected = true;
                    pendingCaptureTile = null;
                    Debug.Log($"✅ Selected {pieceTag} at {adjustedPos}");
                    HighlightValidMoves(selectedPiece);
                }
                else
                {
                    Debug.Log("⛔ You cannot select this piece. It's either not yours or not your turn.");
                }
            }
            else
            {
                Debug.Log("❌ No piece found at this tile.");
            }
        }
        // A piece is already selected.
        else
        {
            // If the clicked tile is one of the highlighted moves:
            if (IsHighlightedTile(adjustedPos))
            {
                GameObject targetPiece = GetPieceAtPosition(adjustedPos);
                if (targetPiece != null)
                {
                    if (pendingCaptureTile.HasValue && pendingCaptureTile.Value == adjustedPos)
                    {
                        MovePiece(selectedPiece, adjustedPos);
                        // After the move, check for check/checkmate.
                        PostMoveCheck();
                        pieceSelected = false;
                        pendingCaptureTile = null;
                        ClearHighlights();
                        return;
                    }
                    else
                    {
                        pendingCaptureTile = adjustedPos;
                        Debug.Log($"⏳ Capture move at {adjustedPos} pending confirmation. Click again to confirm.");
                        return;
                    }
                }
                else
                {
                    MovePiece(selectedPiece, adjustedPos);
                    PostMoveCheck();
                    pieceSelected = false;
                    pendingCaptureTile = null;
                    ClearHighlights();
                    return;
                }
            }

            // If the player clicks on a different friendly piece, reselect that piece.
            if (clickedPiece != null)
            {
                string pieceTag = clickedPiece.tag;
                bool isWhitePiece = pieceTag == "WhitePawn" || pieceTag == "WhiteRook" ||
                                     pieceTag == "WhiteKnight" || pieceTag == "WhiteBishop" ||
                                     pieceTag == "WhiteQueen" || pieceTag == "WhiteKing";
                bool isBlackPiece = pieceTag == "BlackPawn" || pieceTag == "BlackRook" ||
                                     pieceTag == "BlackKnight" || pieceTag == "BlackBishop" ||
                                     pieceTag == "BlackQueen" || pieceTag == "BlackKing";
                bool isPlayerPiece = (isPlayerWhite && isWhitePiece) || (!isPlayerWhite && isBlackPiece);
                bool isCorrectTurn = (isWhiteTurn && isWhitePiece) || (!isWhiteTurn && isBlackPiece);

                if (isPlayerPiece && isCorrectTurn)
                {
                    Debug.Log("🔄 Switching selection to a new piece.");
                    ClearHighlights();
                    selectedPiece = adjustedPos;
                    pieceSelected = true;
                    pendingCaptureTile = null;
                    Debug.Log($"✅ Selected {pieceTag} at {adjustedPos}");
                    HighlightValidMoves(selectedPiece);
                    return;
                }
            }
            Debug.Log("⛔ Invalid move! The clicked tile is not a valid move.");
        }
    }

    void MovePiece(Vector2Int from, Vector2Int to)
    {
        if (!IsMoveValid(from, to))
        {
            Debug.Log("Illegal move: your king remains in check. Please try another move.");
            return;
        }   

        if (!boardPieces.ContainsKey(from))
        {
            Debug.LogError($"❌ MovePiece() failed: No piece found at {from}");
            return;
        }

        GameObject piece = boardPieces[from];

        // If destination has an enemy piece (capture)
        if (boardPieces.ContainsKey(to))
        {
            GameObject capturedPiece = boardPieces[to];

            if (capturedPiece.tag.Contains("White"))
                whitePieces.Remove(capturedPiece);
            else
                blackPieces.Remove(capturedPiece);

            boardPieces.Remove(to);

            if (capturedPiece.tag.Contains("White"))
            {
                capturedPiece.transform.position = GetNextCapturePosition(whiteCaptureContainer);
                capturedPiece.transform.SetParent(whiteCaptureContainer);
            }
            else
            {
                capturedPiece.transform.position = GetNextCapturePosition(blackCaptureContainer);
                capturedPiece.transform.SetParent(blackCaptureContainer);
            }
            Debug.Log($"🔥 Captured {capturedPiece.tag} at {to} and moved to capture area");
        }

        piece.transform.position = new Vector3(to.x, to.y, 0);
        boardPieces.Remove(from);
        boardPieces[to] = piece;
        Debug.Log($"✅ Moved {piece.tag} from {from} to {to}");

        ClearHighlights();
        ToggleTurn();
    }

    private Vector3 GetNextCapturePosition(Transform container)
    {
        int count = container.childCount;
        float spacing = 1.0f;
        return container.position + new Vector3(count * spacing, 0, 0);
    }

    private bool IsHighlightedTile(Vector2Int position)
    {
        foreach (GameObject highlight in highlightedTiles)
        {
            if (highlight.transform.position.x == position.x && highlight.transform.position.y == position.y)
            {
                return true;
            }
        }
        return false;
    }

    void HighlightValidMoves(Vector2Int pos)
    {
        // For the player's side, get only legal moves.
        bool sideIsWhite = isPlayerWhite;
        List<Vector2Int> legalMoves = GetLegalMoves(pos, sideIsWhite);

        if (legalMoves.Count == 0)
        {
            Debug.Log($"No legal moves to highlight for {pos}");
            return;
        }

        Debug.Log($"HighlightValidMoves called for {pos}. Legal Moves: {legalMoves.Count}");

        foreach (Vector2Int move in legalMoves)
        {
            if (!IsValidPosition(move))
            {
                Debug.LogError($"Invalid position detected in legalMoves: {move}");
                continue;
            }

            GameObject targetPiece = GetPieceAtPosition(move);
            bool isCapture = targetPiece != null;

            Debug.Log($"Highlighting tile at {move} - Capture: {isCapture}");

            GameObject highlight = Instantiate(highlightPrefab, new Vector3(move.x, move.y, -0.5f), Quaternion.identity);
            if (highlight == null)
            {
                Debug.LogError("Highlight Prefab is NULL! Check if it's assigned in the Inspector.");
                return;
            }

            SpriteRenderer sr = highlight.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = -1;
                sr.color = isCapture ? new Color(1f, 0f, 0f, 0.5f) : new Color(0f, 1f, 0f, 0.5f);
            }
            else
            {
                Debug.LogError("Highlight Prefab is missing a SpriteRenderer!");
            }

            highlightedTiles.Add(highlight);
        }
    }

    IEnumerator InitializeBoard()
    {
        CreateBoard();
        yield return new WaitUntil(() => BoardIsReady());
        SpawnPieces();
    }

    void CreateBoard()
    {
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                GameObject tile = Instantiate(tilePrefab, new Vector2(x, y), Quaternion.identity);
                tile.name = $"Tile {x}, {y}";

                Tile tileScript = tile.AddComponent<Tile>();
                tileScript.position = new Vector2Int(x, y);

                SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = (x + y) % 2 == 0 ? new Color(0.85f, 0.85f, 0.85f) : new Color(0.25f, 0.25f, 0.25f);
                    sr.sortingOrder = -2;
                }

                tiles[x, y] = tile;
            }
        }
    }

    bool BoardIsReady()
    {
        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
                if (tiles[x, y] == null)
                    return false;
        return true;
    }

    void SpawnPieces()
    {
        string[,] boardToUse = isPlayerWhite ? startBoardWhite : startBoardBlack;

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                int boardY = isPlayerWhite ? y : 7 - y;
                string pieceSymbol = boardToUse[boardY, x];

                if (!string.IsNullOrEmpty(pieceSymbol))
                {
                    int adjustedX = isPlayerWhite ? x : 7 - x;
                    int adjustedY = isPlayerWhite ? y : 7 - y;
                    Vector2Int boardPos = new Vector2Int(adjustedX, adjustedY);

                    GameObject piece = CreatePiece(pieceSymbol, boardPos);

                    if (piece != null)
                    {
                        if (char.IsUpper(pieceSymbol[0]))
                            whitePieces.Add(piece);
                        else
                            blackPieces.Add(piece);

                        Debug.Log($"✅ Spawned {pieceSymbol} at {boardPos}, Assigned Tag: {piece.tag}, Player is White: {isPlayerWhite}");
                    }
                }
            }
        }
        AdjustPiecePositions();
    }

    void AdjustPiecePositions()
    {
        Dictionary<Vector2Int, GameObject> updatedPositions = new Dictionary<Vector2Int, GameObject>();

        foreach (GameObject piece in whitePieces.Concat(blackPieces))
        {
            Vector2Int oldPos = new Vector2Int(
                Mathf.RoundToInt(piece.transform.position.x),
                Mathf.RoundToInt(piece.transform.position.y)
            );

            Vector2Int newPos = isPlayerWhite 
                ? new Vector2Int(oldPos.x, 7 - oldPos.y)
                : oldPos;

            piece.transform.position = new Vector3(newPos.x, newPos.y, 0);
            updatedPositions[newPos] = piece;
        }

        boardPieces = updatedPositions;
        Debug.Log($"✅ Adjusted piece positions & updated board. Player is White: {isPlayerWhite}");
    }

    GameObject CreatePiece(string pieceSymbol, Vector2 position)
    {
        bool isWhite = char.IsUpper(pieceSymbol[0]);
        int prefabIndex = piecePrefabIndex[pieceSymbol];
        GameObject piecePrefab = isWhite ? whitePiecePrefabs[prefabIndex] : blackPiecePrefabs[prefabIndex];

        if (piecePrefab == null)
            return null;

        Vector2Int gridPos = new Vector2Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y));
        GameObject piece = Instantiate(piecePrefab, new Vector3(gridPos.x, gridPos.y, 0), Quaternion.identity);

        string tag = isWhite ? 
            (pieceSymbol == "P" ? "WhitePawn" : 
             pieceSymbol == "R" ? "WhiteRook" : 
             pieceSymbol == "N" ? "WhiteKnight" : 
             pieceSymbol == "B" ? "WhiteBishop" : 
             pieceSymbol == "Q" ? "WhiteQueen" : "WhiteKing")
            :
            (pieceSymbol == "p" ? "BlackPawn" : 
             pieceSymbol == "r" ? "BlackRook" : 
             pieceSymbol == "n" ? "BlackKnight" : 
             pieceSymbol == "b" ? "BlackBishop" : 
             pieceSymbol == "q" ? "BlackQueen" : "BlackKing");

        piece.tag = tag;
        Debug.Log($"🛠 Created {pieceSymbol} at {gridPos}, Assigned Tag: {piece.tag}, Player is White: {isPlayerWhite}");
        return piece;
    }

    void ClearHighlights()
    {
        foreach (GameObject highlight in highlightedTiles)
        {
            Destroy(highlight);
        }
        highlightedTiles.Clear();
    }

    List<Vector2Int> GetValidMoves(Vector2Int pos)
    {
        List<Vector2Int> validMoves = new List<Vector2Int>();
        GameObject piece = GetPieceAtPosition(pos);

        if (piece == null)
        {
            Debug.Log($"❌ No piece found at {pos} in GetValidMoves()");
            return validMoves;
        }

        string pieceTag = piece.tag;
        Debug.Log($"🔎 Finding valid moves for {pieceTag} at {pos}");

        switch (pieceTag)
        {
            case "WhitePawn":
            case "BlackPawn":
                validMoves = GetPawnMoves(pos, pieceTag == "WhitePawn");
                break;
            case "WhiteRook":
            case "BlackRook":
                validMoves = GetRookMoves(pos, pieceTag == "WhiteRook");
                break;
            case "WhiteKnight":
            case "BlackKnight":
                validMoves = GetKnightMoves(pos, pieceTag == "WhiteKnight");
                break;
            case "WhiteBishop":
            case "BlackBishop":
                validMoves = GetBishopMoves(pos, pieceTag == "WhiteBishop");
                break;
            case "WhiteQueen":
            case "BlackQueen":
                validMoves = GetQueenMoves(pos, pieceTag == "WhiteQueen");
                break;
            case "WhiteKing":
            case "BlackKing":
                validMoves = GetKingMoves(pos, pieceTag == "WhiteKing");
                break;
            default:
                Debug.LogError($"❌ Unrecognized piece tag: {pieceTag} at {pos}");
                break;
        }

        Debug.Log($"✅ Found {validMoves.Count} valid moves for {pieceTag} at {pos}");
        return validMoves;
    }

    GameObject GetPieceAtPosition(Vector2Int pos)
    {
        if (boardPieces.TryGetValue(pos, out GameObject piece))
        {
            Debug.Log($"✅ Found piece at {pos} | Name: {piece.name} | Tag: {piece.tag}");
            return piece;
        }

        Debug.Log($"❌ No piece found at {pos}");
        return null;
    }

    public void ToggleTurn()
    {
        isWhiteTurn = !isWhiteTurn;
        Debug.Log("🔄 Turn switched. Now it's " + (isWhiteTurn ? "White" : "Black") + "'s turn.");

        if (!IsPlayerTurn())
        {
            StartCoroutine(AIMoveCoroutine());
        }
    }

    private bool IsPlayerTurn()
    {
        return (isPlayerWhite && isWhiteTurn) || (!isPlayerWhite && !isWhiteTurn);
    }

    private IEnumerator AIMoveCoroutine()
    {
        yield return new WaitForSeconds(1f);
        AIMove();
    }

    private void AIMove()
    {
        bool enemyIsWhite = !isPlayerWhite;
        List<(Vector2Int from, Vector2Int to)> possibleMoves = new List<(Vector2Int, Vector2Int)>();

        foreach (var kvp in boardPieces.ToList())
        {
            Vector2Int pos = kvp.Key;
            GameObject piece = kvp.Value;

            if (enemyIsWhite)
            {
                if (!piece.tag.StartsWith("White"))
                    continue;
            }
            else
            {
                if (!piece.tag.StartsWith("Black"))
                    continue;
            }

            // Use only legal moves for the AI.
            List<Vector2Int> moves = GetLegalMoves(pos, enemyIsWhite);
            foreach (Vector2Int move in moves)
            {
                possibleMoves.Add((pos, move));
            }
        }

        var captureMoves = possibleMoves.Where(m => GetPieceAtPosition(m.to) != null).ToList();
        if (captureMoves.Count > 0)
        {
            possibleMoves = captureMoves;
        }

        if (possibleMoves.Count > 0)
        {
            var randomMove = possibleMoves[Random.Range(0, possibleMoves.Count)];
            Debug.Log($"AI moving piece from {randomMove.from} to {randomMove.to}");
            MovePiece(randomMove.from, randomMove.to);
            // After the AI moves, check if the player's king is in checkmate.
            bool opponentKingIsWhite = isPlayerWhite;
            if (IsKingInCheck(opponentKingIsWhite))
            {
                Debug.Log("Check on player's king!");
                if (IsCheckmate(opponentKingIsWhite))
                {
                    Debug.Log("Checkmate! Game over.");
                    // Flash the checkmated king:
                    GameObject king = FindKing(opponentKingIsWhite);
                    if (king != null)
                        StartCoroutine(FlashCheckmatedKing(king));
                }
            }
        }
        else
        {
            Debug.Log("AI has no valid moves!");
        }
    }

    // --- Legal Move Methods ---

    private bool IsMoveValid(Vector2Int from, Vector2Int to)
    {
        if (!boardPieces.ContainsKey(from))
            return false;

        GameObject movingPiece = boardPieces[from];
        GameObject capturedPiece = null;
        if (boardPieces.ContainsKey(to))
            capturedPiece = boardPieces[to];

        boardPieces.Remove(from);
        boardPieces[to] = movingPiece;
        Vector3 originalPos = movingPiece.transform.position;
        movingPiece.transform.position = new Vector3(to.x, to.y, 0);

        bool movingSideIsWhite = movingPiece.tag.StartsWith("White");
        bool kingSafe = !IsKingInCheck(movingSideIsWhite);

        movingPiece.transform.position = originalPos;
        boardPieces.Remove(to);
        boardPieces[from] = movingPiece;
        if (capturedPiece != null)
            boardPieces[to] = capturedPiece;

        return kingSafe;
    }

    private List<Vector2Int> GetLegalMoves(Vector2Int pos, bool isWhite)
    {
        List<Vector2Int> candidateMoves = GetValidMoves(pos);
        List<Vector2Int> legalMoves = new List<Vector2Int>();
        foreach (Vector2Int move in candidateMoves)
        {
            if (IsMoveValid(pos, move))
                legalMoves.Add(move);
        }
        return legalMoves;
    }

    // --- Check / Checkmate Methods ---

    public bool IsKingInCheck(bool kingIsWhite)
    {
        Vector2Int kingPos = new Vector2Int(-1, -1);
        foreach (var kvp in boardPieces)
        {
            GameObject piece = kvp.Value;
            if (kingIsWhite && piece.tag == "WhiteKing")
            {
                kingPos = kvp.Key;
                break;
            }
            else if (!kingIsWhite && piece.tag == "BlackKing")
            {
                kingPos = kvp.Key;
                break;
            }
        }

        if (kingPos.x == -1)
        {
            Debug.LogError("King not found on board!");
            return false;
        }

        foreach (var kvp in boardPieces)
        {
            GameObject piece = kvp.Value;
            bool pieceIsWhite = piece.tag.StartsWith("White");
            if (pieceIsWhite == kingIsWhite)
                continue;

            List<Vector2Int> moves = GetValidMoves(kvp.Key);
            if (moves.Contains(kingPos))
            {
                return true;
            }
        }
        return false;
    }

    public bool IsCheckmate(bool kingIsWhite)
    {
        if (!IsKingInCheck(kingIsWhite))
            return false;

        foreach (var kvp in boardPieces)
        {
            GameObject piece = kvp.Value;
            bool pieceIsWhite = piece.tag.StartsWith("White");
            if (pieceIsWhite != kingIsWhite)
                continue;

            List<Vector2Int> moves = GetValidMoves(kvp.Key);
            foreach (Vector2Int move in moves)
            {
                Vector2Int from = kvp.Key;
                GameObject capturedPiece = null;
                if (boardPieces.ContainsKey(move))
                    capturedPiece = boardPieces[move];

                boardPieces.Remove(from);
                boardPieces[move] = piece;
                Vector3 oldPos = piece.transform.position;
                piece.transform.position = new Vector3(move.x, move.y, 0);

                bool stillInCheck = IsKingInCheck(kingIsWhite);

                piece.transform.position = oldPos;
                boardPieces.Remove(move);
                boardPieces[from] = piece;
                if (capturedPiece != null)
                    boardPieces[move] = capturedPiece;

                if (!stillInCheck)
                    return false;
            }
        }
        return true;
    }

    // --- Flashing Checkmate Highlight ---

    private IEnumerator FlashCheckmatedKing(GameObject king)
    {
        SpriteRenderer sr = king.GetComponent<SpriteRenderer>();
        if (sr == null)
            yield break;

        Color originalColor = sr.color;
        while (true)
        {
            sr.color = Color.red;
            yield return new WaitForSeconds(0.5f);
            sr.color = originalColor;
            yield return new WaitForSeconds(0.5f);
        }
    }

    // --- Helper to find the king object ---
    private GameObject FindKing(bool kingIsWhite)
    {
        foreach (var kvp in boardPieces)
        {
            if (kingIsWhite && kvp.Value.tag == "WhiteKing")
                return kvp.Value;
            else if (!kingIsWhite && kvp.Value.tag == "BlackKing")
                return kvp.Value;
        }
        return null;
    }

    // --- After Move Check (used after a player or AI move) ---
    private void PostMoveCheck()
    {
        // Check the opponent's king after a move.
        bool opponentKingIsWhite = !isPlayerWhite; // assuming player is white; adjust accordingly if necessary.
        if (IsKingInCheck(opponentKingIsWhite))
        {
            Debug.Log("Check!");
            if (IsCheckmate(opponentKingIsWhite))
            {
                Debug.Log("Checkmate! Game over.");
                GameObject king = FindKing(opponentKingIsWhite);
                if (king != null)
                    StartCoroutine(FlashCheckmatedKing(king));
                // Optionally disable further moves, display a message, etc.
            }
        }
    }

    List<Vector2Int> GetPawnMoves(Vector2Int pos, bool isWhite)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        int direction = isWhite ? 1 : -1;

        Vector2Int forward = new Vector2Int(pos.x, pos.y + direction);

        // ✅ Forward move if no piece is blocking
        if (IsValidPosition(forward) && GetPieceAtPosition(forward) == null)
        {
            moves.Add(forward);

            // ✅ Double step only if it's the pawn's first move
            int startingRow = isWhite ? 1 : 6;
            if (pos.y == startingRow)
            {
                Vector2Int doubleStep = new Vector2Int(pos.x, pos.y + (2 * direction));
                if (IsValidPosition(doubleStep) && GetPieceAtPosition(doubleStep) == null)
                {
                    moves.Add(doubleStep);
                }
            }
        }

        // ✅ Capturing diagonally
        Vector2Int[] attackOffsets = { new Vector2Int(1, direction), new Vector2Int(-1, direction) };
        foreach (Vector2Int offset in attackOffsets)
        {
            Vector2Int attackPos = pos + offset;
            GameObject targetPiece = GetPieceAtPosition(attackPos);

            if (IsValidPosition(attackPos) && targetPiece != null)
            {
                bool isEnemy = isWhite ? targetPiece.tag.StartsWith("Black") : targetPiece.tag.StartsWith("White");
                if (isEnemy)
                {
                    moves.Add(attackPos);
                }
            }
        }

        return moves;
    }


    List<Vector2Int> GetRookMoves(Vector2Int pos, bool isWhite)
    {
        return GetSlidingMoves(pos, isWhite, new Vector2Int[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) });
    }


    List<Vector2Int> GetBishopMoves(Vector2Int pos, bool isWhite)
    {
        return GetSlidingMoves(pos, isWhite, new Vector2Int[] { new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1) });
    }


    List<Vector2Int> GetQueenMoves(Vector2Int pos, bool isWhite)
    {
        List<Vector2Int> moves = GetRookMoves(pos, isWhite);
        moves.AddRange(GetBishopMoves(pos, isWhite));
        return moves;
    }


    List<Vector2Int> GetKingMoves(Vector2Int pos, bool isWhite)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        Vector2Int[] directions = {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1)
        };
        
        foreach (Vector2Int dir in directions)
        {
            Vector2Int newPos = pos + dir;
            if (IsValidPosition(newPos))
            {
                GameObject target = GetPieceAtPosition(newPos);
                if (target == null)
                {
                    moves.Add(newPos);
                }
                else
                {
                    bool isEnemy = isWhite ? target.tag.StartsWith("Black") : target.tag.StartsWith("White");
                    if (isEnemy)
                        moves.Add(newPos);
                }
            }
        }
        return moves;
    }


    List<Vector2Int> GetKnightMoves(Vector2Int pos, bool isWhite)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        Vector2Int[] knightMoves = {
            new Vector2Int(2, 1), new Vector2Int(2, -1), new Vector2Int(-2, 1), new Vector2Int(-2, -1),
            new Vector2Int(1, 2), new Vector2Int(1, -2), new Vector2Int(-1, 2), new Vector2Int(-1, -2)
        };

        foreach (Vector2Int move in knightMoves)
        {
            Vector2Int newPos = pos + move;
            if (IsValidPosition(newPos))
            {
                GameObject target = GetPieceAtPosition(newPos);
                if (target == null)
                {
                    moves.Add(newPos);
                }
                else
                {
                    bool isEnemy = isWhite ? target.tag.StartsWith("Black") : target.tag.StartsWith("White");
                    if (isEnemy)
                        moves.Add(newPos);
                }
            }
        }
        return moves;
    }


    List<Vector2Int> GetSlidingMoves(Vector2Int pos, bool isWhite, Vector2Int[] directions)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        foreach (Vector2Int dir in directions)
        {
            Vector2Int newPos = pos + dir;
            // Add empty squares.
            while (IsValidPosition(newPos) && GetPieceAtPosition(newPos) == null)
            {
                moves.Add(newPos);
                newPos += dir;
            }
            // If we reached a valid square with a piece, check if it's enemy.
            if (IsValidPosition(newPos))
            {
                GameObject target = GetPieceAtPosition(newPos);
                if (target != null)
                {
                    bool isEnemy = isWhite ? target.tag.StartsWith("Black") : target.tag.StartsWith("White");
                    if (isEnemy)
                        moves.Add(newPos);
                }
            }
        }
        return moves;
    }


    bool IsValidPosition(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < 8 && pos.y >= 0 && pos.y < 8;
    }

}
