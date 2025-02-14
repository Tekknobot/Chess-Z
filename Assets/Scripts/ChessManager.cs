using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI; // <-- needed for UI classes

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
    public float moveDuration = 0.5f; // Duration (in seconds) for each move animation.


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
        // For testing checkmate, call the scenario setup after initializing the board.
        // You may want to delay this call slightly to let the board set up.
        // StartCoroutine(DelayedSetupCheckmateScenario());        
    }

    private IEnumerator DelayedSetupCheckmateScenario()
    {
        yield return new WaitForSeconds(1f);
        SetupCheckmateScenario();
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
                        StartCoroutine(MovePiece(selectedPiece, adjustedPos));
                        pieceSelected = false;
                        pendingCaptureTile = null;

                        ChessPiece pieceData = clickedPiece.GetComponent<ChessPiece>();
                        if (pieceData != null)
                            pieceData.hasMoved = true;

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
                    StartCoroutine(MovePiece(selectedPiece, adjustedPos));
                    pieceSelected = false;
                    pendingCaptureTile = null;
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

    IEnumerator MovePiece(Vector2Int from, Vector2Int to)
    {
        // (Existing move validation, capture logic, and animation code here…)
        if (!IsMoveValid(from, to))
        {
            Debug.Log("Illegal move: your king remains in check. Please try another move.");
            yield break;
        }

        if (!boardPieces.ContainsKey(from))
        {
            Debug.LogError($"❌ MovePieceCoroutine() failed: No piece found at {from}");
            yield break;
        }

        GameObject piece = boardPieces[from];
        Vector3 startPos = piece.transform.position;
        Vector3 targetPos = new Vector3(to.x, to.y, 0);

        // Handle captures if any (same as before) …
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
                capturedPiece.transform.position = GetNextWhiteCapturePosition(whiteCaptureContainer);
                capturedPiece.transform.SetParent(whiteCaptureContainer);
            }
            else
            {
                capturedPiece.transform.position = GetNextBlackCapturePosition(blackCaptureContainer);
                capturedPiece.transform.SetParent(blackCaptureContainer);
            }
            Debug.Log($"🔥 Captured {capturedPiece.tag} at {to} and moved to capture area");
        }

        boardPieces.Remove(from);
        boardPieces[to] = piece;
        Debug.Log($"✅ Moving {piece.tag} from {from} to {to}");

        yield return StartCoroutine(AnimateMovement(piece, startPos, targetPos, moveDuration));

        // If the moving piece is a king and it moved two squares horizontally, assume castling.
        if (piece.tag.Contains("King") && Mathf.Abs(to.x - from.x) == 2)
        {
            bool isWhiteKing = piece.tag.StartsWith("White");
            if (to.x > from.x)
            {
                // Kingside castling: move the rook from the corner to the square adjacent to the king.
                Vector2Int rookFrom = isWhiteKing ? new Vector2Int(7, 0) : new Vector2Int(7, 7);
                Vector2Int rookTo = isWhiteKing ? new Vector2Int(5, 0) : new Vector2Int(5, 7);

                if (boardPieces.TryGetValue(rookFrom, out GameObject rook))
                {
                    Vector3 rookStart = rook.transform.position;
                    Vector3 rookTarget = new Vector3(rookTo.x, rookTo.y, 0);
                    boardPieces.Remove(rookFrom);
                    boardPieces[rookTo] = rook;
                    yield return StartCoroutine(AnimateMovement(rook, rookStart, rookTarget, moveDuration));

                    // Mark the rook as having moved.
                    ChessPiece rookData = rook.GetComponent<ChessPiece>();
                    if (rookData != null)
                        rookData.hasMoved = true;
                }
            }
            else
            {
                // Queenside castling.
                Vector2Int rookFrom = isWhiteKing ? new Vector2Int(0, 0) : new Vector2Int(0, 7);
                Vector2Int rookTo = isWhiteKing ? new Vector2Int(3, 0) : new Vector2Int(3, 7);

                if (boardPieces.TryGetValue(rookFrom, out GameObject rook))
                {
                    Vector3 rookStart = rook.transform.position;
                    Vector3 rookTarget = new Vector3(rookTo.x, rookTo.y, 0);
                    boardPieces.Remove(rookFrom);
                    boardPieces[rookTo] = rook;
                    yield return StartCoroutine(AnimateMovement(rook, rookStart, rookTarget, moveDuration));

                    ChessPiece rookData = rook.GetComponent<ChessPiece>();
                    if (rookData != null)
                        rookData.hasMoved = true;
                }
            }
        }

        // Mark the moving piece as having moved.
        ChessPiece pieceData = piece.GetComponent<ChessPiece>();
        if (pieceData != null)
            pieceData.hasMoved = true;

        // --- Pawn Promotion Check (if applicable) ---
        if (piece.tag == "WhitePawn" && to.y == 7)
        {
            PromotePawn(piece, to, true);
        }
        else if (piece.tag == "BlackPawn" && to.y == 0)
        {
            PromotePawn(piece, to, false);
        }

        ClearHighlights();
        ToggleTurn();
        PostMoveCheck();
    }

    // --- New PromotePawn method ---
    private void PromotePawn(GameObject pawn, Vector2Int pos, bool isWhite)
    {
        boardPieces.Remove(pos);
        if (isWhite)
            whitePieces.Remove(pawn);
        else
            blackPieces.Remove(pawn);
        Destroy(pawn);

        // Automatically promote to queen.
        string promotionSymbol = isWhite ? "Q" : "q";
        GameObject newQueen = CreatePiece(promotionSymbol, new Vector2(pos.x, pos.y));
        if (newQueen != null)
        {
            boardPieces[pos] = newQueen;
            if (isWhite)
                whitePieces.Add(newQueen);
            else
                blackPieces.Add(newQueen);
            Debug.Log($"Pawn promoted to Queen at {pos}");
        }
    }

    private Vector3 GetNextWhiteCapturePosition(Transform container)
    {
        int count = container.childCount;
        int maxPerRow = 8;
        float spacing = 1.0f; // Adjust spacing as needed.

        // Calculate which row and column this captured piece should occupy.
        int row = count / maxPerRow;  // integer division: 0 for first row, 1 for second, etc.
        int col = count % maxPerRow;    // remainder: position within the row

        // Assume the container's position is the starting point (top-left, for instance).
        // Adjust the vertical offset as needed (here we subtract for new rows below).
        return container.position + new Vector3(col * spacing, row * spacing, 0);
    }


    private Vector3 GetNextBlackCapturePosition(Transform container)
    {
        int count = container.childCount;
        int maxPerRow = 8;
        float spacing = 1.0f; // Adjust spacing as needed.

        // Calculate which row and column this captured piece should occupy.
        int row = count / maxPerRow;  // integer division: 0 for first row, 1 for second, etc.
        int col = count % maxPerRow;    // remainder: position within the row

        // Assume the container's position is the starting point (top-left, for instance).
        // Adjust the vertical offset as needed (here we subtract for new rows below).
        return container.position + new Vector3(col * spacing, -row * spacing, 0);
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

    List<Vector2Int> GetValidMoves(Vector2Int pos, bool simulate = false)
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
                validMoves = GetKingMoves(pos, pieceTag.StartsWith("White"), includeCastling: !simulate);
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

        if (IsPlayerTurn())
        {
            // When it becomes the player's turn, check for threatened pieces.
            HighlightThreatenedPlayerPieces();
        }
        else
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
        bool enemyIsWhite = !isPlayerWhite;
        var moveOptions = new List<(Vector2Int from, Vector2Int to, float score)>();

        // Make a copy of boardPieces to avoid modification issues.
        var enemyPieces = boardPieces.ToList();

        // Count the total candidate moves for progress tracking.
        int totalCandidateMoves = 0;
        foreach (var kvp in enemyPieces)
        {
            GameObject piece = kvp.Value;
            // Only consider enemy pieces.
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
            totalCandidateMoves += GetLegalMoves(kvp.Key, enemyIsWhite).Count;
        }

        int evaluatedMoves = 0;
        int yieldInterval = 5; // Yield after processing every 5 moves

        // Evaluate candidate moves for each enemy piece.
        foreach (var kvp in enemyPieces)
        {
            Vector2Int pos = kvp.Key;
            GameObject piece = kvp.Value;

            // Skip pieces that are not on the AI's side.
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

            List<Vector2Int> moves = GetLegalMoves(pos, enemyIsWhite);
            foreach (Vector2Int move in moves)
            {
                // Evaluate the move (using your EvaluateMove function plus a small random factor).
                float score = EvaluateMove(pos, move) + Random.Range(0f, 0.2f);
                moveOptions.Add((pos, move, score));

                evaluatedMoves++;

                // Update the board's tint every few iterations.
                if (evaluatedMoves % yieldInterval == 0)
                {
                    float progress = (float)evaluatedMoves / totalCandidateMoves;
                    UpdateBoardTileColors(progress);
                    yield return null; // Yield control so Unity can update the UI.
                }
            }
        }

        // Ensure the board is fully tinted (progress = 1).
        UpdateBoardTileColors(1f);
        
        // Optionally, wait a brief moment to let the player see the completed tint.
        yield return new WaitForSeconds(0.2f);

        // Choose and execute the best move, if any.
        if (moveOptions.Count > 0)
        {
            var bestMove = moveOptions.OrderByDescending(m => m.score).First();
            Debug.Log($"AI moving piece from {bestMove.from} to {bestMove.to} with score {bestMove.score}");
            yield return StartCoroutine(MovePiece(bestMove.from, bestMove.to));
            CheckForCheckmateBothKings();
        }
        else
        {
            Debug.Log("AI has no valid moves!");
        }

        // Reset the board tile colors to normal.
        ResetBoardTileColors();
    }

    private void AIMove()
    {
        bool enemyIsWhite = !isPlayerWhite;
        List<(Vector2Int from, Vector2Int to, float score)> moveOptions = new List<(Vector2Int, Vector2Int, float)>();

        // Loop through all enemy pieces.
        foreach (var kvp in boardPieces.ToList())
        {
            Vector2Int pos = kvp.Key;
            GameObject piece = kvp.Value;

            // Only consider pieces that belong to the AI.
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

            // Get all legal moves for this piece.
            List<Vector2Int> moves = GetLegalMoves(pos, enemyIsWhite);
            foreach (Vector2Int move in moves)
            {
                // Use the new evaluation function.
                float score = EvaluateMove(pos, move) + Random.Range(0f, 0.2f); // Keep a small random factor if desired.
                moveOptions.Add((pos, move, score));
            }
        }

        if (moveOptions.Count > 0)
        {
            // Choose the move with the highest score.
            var bestMove = moveOptions.OrderByDescending(m => m.score).First();
            Debug.Log($"AI moving piece from {bestMove.from} to {bestMove.to} with score {bestMove.score}");
            StartCoroutine(MovePiece(bestMove.from, bestMove.to));
            CheckForCheckmateBothKings();
        }
        else
        {
            Debug.Log("AI has no valid moves!");
        }
    }

    private int EvaluateCapture(GameObject capturedPiece)
    {
        if (capturedPiece == null)
            return 0;

        // Use the tag to determine the type of the piece.
        string tag = capturedPiece.tag;

        if (tag.Contains("Pawn"))
            return 1;
        else if (tag.Contains("Knight"))
            return 3;
        else if (tag.Contains("Bishop"))
            return 3;
        else if (tag.Contains("Rook"))
            return 5;
        else if (tag.Contains("Queen"))
            return 9;
        else if (tag.Contains("King"))
            return 100; // This is mostly for completeness. The king should not be captured in a legal game.
        
        return 0;
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
        // Find the king's current position.
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

        // Now check enemy moves (using simulation if needed).
        foreach (var kvp in boardPieces)
        {
            GameObject piece = kvp.Value;
            bool pieceIsWhite = piece.tag.StartsWith("White");
            if (pieceIsWhite == kingIsWhite)
                continue; // Skip friendly pieces.

            // Use simulate mode to avoid complications (e.g., castling)
            List<Vector2Int> moves = GetValidMoves(kvp.Key, true);
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

        // Iterate over a copy of the boardPieces dictionary
        foreach (var kvp in boardPieces.ToList())
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

                // Simulate the move by modifying the dictionary on the copy
                boardPieces.Remove(from);
                boardPieces[move] = piece;
                Vector3 oldPos = piece.transform.position;
                piece.transform.position = new Vector3(move.x, move.y, 0);

                bool stillInCheck = IsKingInCheck(kingIsWhite);

                // Revert the move
                piece.transform.position = oldPos;
                boardPieces.Remove(move);
                boardPieces[from] = piece;
                if (capturedPiece != null)
                    boardPieces[move] = capturedPiece;

                // If there is at least one legal move that removes check, it's not mate.
                if (!stillInCheck)
                    return false;
            }
        }
        return true;
    }


    public void CheckForCheckmateBothKings()
    {
        // Check if the white king is checkmated.
        if (IsCheckmate(true))
        {
            Debug.Log("White is checkmated! Game Over.");
            GameObject whiteKing = FindKing(true);
            if (whiteKing != null)
            {
                // For example, flash the white king. (Assuming your FlashCheckmatedKing coroutine flashes red for white.)
                StartCoroutine(FlashCheckmatedKing(whiteKing));
                // And fade the board tiles.
                StartCoroutine(FadeBoardTiles());                
            }
        }
        
        // Check if the black king is checkmated.
        if (IsCheckmate(false))
        {
            Debug.Log("Black is checkmated! Game Over.");
            GameObject blackKing = FindKing(false);
            if (blackKing != null)
            {
                // For example, flash the black king. (Assuming your FlashCheckmatedKing coroutine flashes blue for black.)
                StartCoroutine(FlashCheckmatedKing(blackKing));
                // And fade the board tiles.
                StartCoroutine(FadeBoardTiles());                
            }
        }
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
        CheckForCheckmateBothKings();
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


    List<Vector2Int> GetKingMoves(Vector2Int pos, bool isWhite, bool includeCastling = true)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        // Normal king moves (one square any direction)
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
                if (target == null || (isWhite ? target.tag.StartsWith("Black") : target.tag.StartsWith("White")))
                {
                    moves.Add(newPos);
                }
            }
        }

        // Only add castling moves if allowed.
        if (includeCastling)
        {
            GameObject king = GetPieceAtPosition(pos);
            ChessPiece kingData = king?.GetComponent<ChessPiece>();
            // Only allow castling if the king has not moved and is not in check.
            if (kingData != null && !kingData.hasMoved && !IsKingInCheck(isWhite))
            {
                Vector2Int kingsideRookPos, queensideRookPos;
                Vector2Int kingsideBetween1, kingsideBetween2, kingKingsideTarget;
                Vector2Int queensideBetween1, queensideBetween2, queensideBetween3, kingQueensideTarget;

                if (isWhite)
                {
                    kingsideRookPos = new Vector2Int(7, 0);
                    kingsideBetween1 = new Vector2Int(5, 0);
                    kingsideBetween2 = new Vector2Int(6, 0);
                    kingKingsideTarget = new Vector2Int(6, 0);

                    queensideRookPos = new Vector2Int(0, 0);
                    queensideBetween1 = new Vector2Int(3, 0);
                    queensideBetween2 = new Vector2Int(2, 0);
                    queensideBetween3 = new Vector2Int(1, 0);
                    kingQueensideTarget = new Vector2Int(2, 0);
                }
                else
                {
                    kingsideRookPos = new Vector2Int(7, 7);
                    kingsideBetween1 = new Vector2Int(5, 7);
                    kingsideBetween2 = new Vector2Int(6, 7);
                    kingKingsideTarget = new Vector2Int(6, 7);

                    queensideRookPos = new Vector2Int(0, 7);
                    queensideBetween1 = new Vector2Int(3, 7);
                    queensideBetween2 = new Vector2Int(2, 7);
                    queensideBetween3 = new Vector2Int(1, 7);
                    kingQueensideTarget = new Vector2Int(2, 7);
                }

                // Kingside castling: Check that the squares the king passes through are empty and safe.
                if (GetPieceAtPosition(kingsideBetween1) == null &&
                    GetPieceAtPosition(kingsideBetween2) == null &&
                    IsSquareSafeForKing(kingsideBetween1, isWhite) &&
                    IsSquareSafeForKing(kingsideBetween2, isWhite))
                {
                    moves.Add(kingKingsideTarget);
                }

                // Queenside castling: Check that all squares between the king and rook are empty and safe.
                if (GetPieceAtPosition(queensideBetween1) == null &&
                    GetPieceAtPosition(queensideBetween2) == null &&
                    GetPieceAtPosition(queensideBetween3) == null &&
                    IsSquareSafeForKing(queensideBetween1, isWhite) &&
                    IsSquareSafeForKing(queensideBetween2, isWhite))
                {
                    moves.Add(kingQueensideTarget);
                }
            }
        }

        return moves;
    }


    bool IsSquareSafeForKing(Vector2Int pos, bool kingIsWhite)
    {
        // Iterate over all enemy pieces and check if any of their (non–castling) moves attack pos.
        foreach (var kvp in boardPieces)
        {
            GameObject piece = kvp.Value;
            bool pieceIsWhite = piece.tag.StartsWith("White");
            if (pieceIsWhite == kingIsWhite)
                continue;

            List<Vector2Int> moves;
            // For enemy kings, exclude castling moves to avoid recursion.
            if (piece.tag.Contains("King"))
            {
                moves = GetKingMoves(GetGridPosition(piece), pieceIsWhite, includeCastling: false);
            }
            else
            {
                moves = GetValidMoves(GetGridPosition(piece));
            }

            if (moves.Contains(pos))
                return false;
        }
        return true;
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

    private IEnumerator FlashThreatenedPiece(GameObject piece)
    {
        SpriteRenderer sr = piece.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            Debug.LogError("FlashThreatenedPiece: Piece has no SpriteRenderer!");
            yield break;
        }
        
        Color originalColor = sr.color;
        Color flashColor = Color.red; // Use red for threatened pieces.
        
        while (true)
        {
            // Check if it’s still the player’s turn. If not, stop flashing.
            if (!IsPlayerTurn())
            {
                sr.color = originalColor;
                yield break;
            }
            
            // Also check if the piece is still threatened.
            if (!IsPieceThreatened(piece))
            {
                sr.color = originalColor;
                yield break;
            }
            
            sr.color = flashColor;
            yield return new WaitForSeconds(0.5f);
            sr.color = originalColor;
            yield return new WaitForSeconds(0.5f);
        }
    }

    public void HighlightThreatenedPlayerPieces()
    {
        // Determine which side is the player.
        bool playerIsWhite = isPlayerWhite;

        // Loop through all pieces on the board.
        foreach (var kvp in boardPieces)
        {
            Vector2Int pos = kvp.Key;
            GameObject piece = kvp.Value;
            
            // Skip enemy pieces.
            bool pieceIsWhite = piece.tag.StartsWith("White");
            if (pieceIsWhite != playerIsWhite)
                continue;
            
            // Check if any enemy piece can capture this piece.
            bool threatened = false;
            foreach (var enemyKvp in boardPieces)
            {
                GameObject enemyPiece = enemyKvp.Value;
                bool enemyIsWhite = enemyPiece.tag.StartsWith("White");
                // Only consider enemy pieces.
                if (enemyIsWhite == playerIsWhite)
                    continue;
                
                // Get the legal moves for the enemy piece.
                List<Vector2Int> enemyMoves = GetValidMoves(enemyKvp.Key);
                if (enemyMoves.Contains(pos))
                {
                    threatened = true;
                    break;
                }
            }
            
            // If the piece is threatened, start a flashing coroutine.
            if (threatened)
            {
                // To avoid starting multiple coroutines on the same piece, you might check if one is already running.
                // For simplicity here, we'll simply start a coroutine.
                StartCoroutine(FlashThreatenedPiece(piece));
            }
        }
    }

    private Vector2Int GetGridPosition(GameObject piece)
    {
        return new Vector2Int(Mathf.RoundToInt(piece.transform.position.x),
                                Mathf.RoundToInt(piece.transform.position.y));
    }


    private bool IsPieceThreatened(GameObject piece)
    {
        Vector2Int pos = GetGridPosition(piece);
        bool pieceIsWhite = piece.tag.StartsWith("White");
        
        foreach (var kvp in boardPieces)
        {
            GameObject enemyPiece = kvp.Value;
            bool enemyIsWhite = enemyPiece.tag.StartsWith("White");
            if (enemyIsWhite == pieceIsWhite)
                continue;  // Same color; skip
            
            // Get the enemy piece’s legal moves (using GetValidMoves here)
            List<Vector2Int> enemyMoves = GetValidMoves(kvp.Key);
            if (enemyMoves.Contains(pos))
                return true;
        }
        return false;
    }

    private IEnumerator FadeBoardTiles()
    {
        // Duration for fade out/in (in seconds)
        float duration = 1f;
        float t = 0f;

        // --- Fade Out ---
        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / duration);
            // Loop through all board tiles and set their alpha
            for (int y = 0; y < tiles.GetLength(1); y++)
            {
                for (int x = 0; x < tiles.GetLength(0); x++)
                {
                    GameObject tile = tiles[x, y];
                    if (tile != null)
                    {
                        SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
                        if (sr != null)
                        {
                            Color c = sr.color;
                            c.a = alpha;
                            sr.color = c;
                        }
                    }
                }
            }
            yield return null;
        }

        // Wait for a short pause with the board faded out.
        yield return new WaitForSeconds(0.5f);

        // --- Fade In ---
        t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, t / duration);
            for (int y = 0; y < tiles.GetLength(1); y++)
            {
                for (int x = 0; x < tiles.GetLength(0); x++)
                {
                    GameObject tile = tiles[x, y];
                    if (tile != null)
                    {
                        SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
                        if (sr != null)
                        {
                            Color c = sr.color;
                            c.a = alpha;
                            sr.color = c;
                        }
                    }
                }
            }
            yield return null;
        }
    }


    // Sets up a simple checkmate scenario where Black is mated.
    public void SetupCheckmateScenario()
    {
        // First, remove all existing pieces.
        foreach (var kvp in boardPieces.ToList())
        {
            Destroy(kvp.Value);
        }
        boardPieces.Clear();
        whitePieces.Clear();
        blackPieces.Clear();

        // Now, set up the new positions.
        // Using 0-indexed coordinates with (0,0) as a1 (bottom left):
        // White King at f6: (5,5)
        // White Queen at h7: (7,6)
        // Black King at h8: (7,7)
        
        // Create and place the white king.
        Vector2Int whiteKingPos = new Vector2Int(5, 5);
        GameObject whiteKing = CreatePiece("K", new Vector2(whiteKingPos.x, whiteKingPos.y));
        whiteKing.tag = "WhiteKing";  // Ensure tag is set correctly.
        whitePieces.Add(whiteKing);
        boardPieces[whiteKingPos] = whiteKing;

        // Create and place the white queen.
        Vector2Int whiteQueenPos = new Vector2Int(7, 6);
        GameObject whiteQueen = CreatePiece("Q", new Vector2(whiteQueenPos.x, whiteQueenPos.y));
        whiteQueen.tag = "WhiteQueen";
        whitePieces.Add(whiteQueen);
        boardPieces[whiteQueenPos] = whiteQueen;

        // Create and place the black king.
        Vector2Int blackKingPos = new Vector2Int(7, 7);
        GameObject blackKing = CreatePiece("k", new Vector2(blackKingPos.x, blackKingPos.y));
        blackKing.tag = "BlackKing";
        blackPieces.Add(blackKing);
        boardPieces[blackKingPos] = blackKing;

        Debug.Log("Checkmate scenario set up: Black King at h8, White Queen at h7, White King at f6.");
    }
    IEnumerator AnimateMovement(GameObject piece, Vector3 start, Vector3 end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            piece.transform.position = Vector3.Lerp(start, end, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        piece.transform.position = end; // Ensure it reaches the exact target position.
    }

    float EvaluateMove(Vector2Int from, Vector2Int to)
    {
        // Save references to the moving piece and any captured piece.
        GameObject movingPiece = boardPieces[from];
        GameObject capturedPiece = boardPieces.ContainsKey(to) ? boardPieces[to] : null;

        // Simulate the move:
        boardPieces.Remove(from);
        boardPieces[to] = movingPiece;
        Vector3 originalPos = movingPiece.transform.position;
        movingPiece.transform.position = new Vector3(to.x, to.y, 0);

        // Immediate score: gain from capturing a piece (if any)
        float score = capturedPiece != null ? EvaluateCapture(capturedPiece) : 0;

        // Now simulate opponent's best counter-move:
        float opponentBestResponse = 0;
        // Determine enemy side based on the moving piece:
        bool enemyIsWhite = !movingPiece.tag.StartsWith("White");
        foreach (var kvp in boardPieces.ToList())
        {
            GameObject piece = kvp.Value;
            // Only consider enemy pieces
            if (piece.tag.StartsWith("White") != enemyIsWhite)
                continue;

            // Get legal moves for the enemy piece.
            List<Vector2Int> opponentMoves = GetLegalMoves(GetGridPosition(piece), piece.tag.StartsWith("White"));
            foreach (Vector2Int oppMove in opponentMoves)
            {
                GameObject target = GetPieceAtPosition(oppMove);
                float responseScore = target != null ? EvaluateCapture(target) : 0;
                if (responseScore > opponentBestResponse)
                    opponentBestResponse = responseScore;
            }
        }

        // Undo the move simulation:
        movingPiece.transform.position = originalPos;
        boardPieces.Remove(to);
        boardPieces[from] = movingPiece;
        if (capturedPiece != null)
            boardPieces[to] = capturedPiece;

        // Return net score: immediate gain minus the opponent's potential gain.
        return score - opponentBestResponse;
    }

    /// <summary>
    /// Updates the color of all board tiles by lerping between the original color and a loading color.
    /// </summary>
    /// <param name="progress">A value between 0 and 1; 0 = original, 1 = fully tinted.</param>
    private void UpdateBoardTileColors(float progress)
    {
        // Define your loading color (you can change this to any color you like).
        Color loadingColor = Color.red;
        
        // Loop through all tiles.
        for (int y = 0; y < tiles.GetLength(1); y++)
        {
            for (int x = 0; x < tiles.GetLength(0); x++)
            {
                // Compute the original color based on position.
                Color originalColor = ((x + y) % 2 == 0) 
                    ? new Color(0.85f, 0.85f, 0.85f) 
                    : new Color(0.25f, 0.25f, 0.25f);
                
                // Lerp from originalColor to loadingColor.
                Color newColor = Color.Lerp(originalColor, loadingColor, progress);
                
                // Update the tile's SpriteRenderer color.
                if (tiles[x, y] != null)
                {
                    SpriteRenderer sr = tiles[x, y].GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.color = newColor;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Resets the board tiles to their original colors.
    /// </summary>
    private void ResetBoardTileColors()
    {
        // Passing progress = 0 sets the color to the original color.
        UpdateBoardTileColors(0f);
    }

}
