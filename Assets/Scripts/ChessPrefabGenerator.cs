using System.IO;
using UnityEditor;
using UnityEngine;

public class ChessPrefabGenerator : MonoBehaviour
{
    public Sprite[] whitePieceSprites; // Assign white piece sprites in Inspector
    public Sprite[] blackPieceSprites; // Assign black piece sprites in Inspector
    public Material whiteMaterial; // Optional material for white pieces
    public Material blackMaterial; // Optional material for black pieces
    public string savePath = "Assets/ChessPrefabs"; // Directory to save prefabs

    private readonly string[] pieceNames = { "Pawn", "Rook", "Knight", "Bishop", "Queen", "King" };

    void Start()
    {
        GeneratePrefabs();
    }

    public void GeneratePrefabs()
    {
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        if (whitePieceSprites.Length < 6 || blackPieceSprites.Length < 6)
        {
            Debug.LogError("Please assign 6 sprites each for white and black pieces.");
            return;
        }

        for (int i = 0; i < pieceNames.Length; i++)
        {
            CreateChessPiecePrefab(pieceNames[i], whitePieceSprites[i], whiteMaterial, true);
            CreateChessPiecePrefab(pieceNames[i], blackPieceSprites[i], blackMaterial, false);
        }

        Debug.Log("Chess piece prefabs successfully generated!");
    }

    private void CreateChessPiecePrefab(string pieceName, Sprite sprite, Material material, bool isWhite)
    {
        string colorPrefix = isWhite ? "White" : "Black";
        string fullName = $"{colorPrefix}_{pieceName}";

        // Create GameObject
        GameObject pieceObject = new GameObject(fullName);
        SpriteRenderer spriteRenderer = pieceObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.material = material;

        // Save as prefab
        string prefabPath = $"{savePath}/{fullName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(pieceObject, prefabPath);
        DestroyImmediate(pieceObject);
    }
}
