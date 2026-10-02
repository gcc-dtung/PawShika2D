using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GridSpawner : MonoBehaviour
{
    public const float CellSpacing = 1.2f; // cellGap + cellSize

    [SerializeField] private Cell cellPrefab;
    [SerializeField] private LevelDefinition levelData;
    public UnityEvent<List<Cell>> gridCellEvent = new UnityEvent<List<Cell>>();

    private List<Cell> gridCells = new List<Cell>();

    public LevelDefinition LevelData => levelData;

    private void Start()
    {
        // Có LevelManager thì để LevelManager quyết định level nào được spawn
        if (LevelManager.Instance == null)
        {
            Spawn(levelData);
        }
    }

    public void SetLevelData(LevelDefinition data)
    {
        levelData = data;
    }

    public void Spawn(LevelDefinition data)
    {
        if (data == null) return;

        levelData = data;
        DeleteGrid();

        int height = data.Height;
        int width = data.size;

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if (IsVoid(data, j, i)) continue;

                float xPos = (j - (float)(width - 1) / 2) * CellSpacing;
                float yPos = (i - (float)(height - 1) / 2) * CellSpacing;

                Cell cell = Instantiate(cellPrefab, new Vector2(xPos, yPos), Quaternion.identity);
                cell.transform.SetParent(transform);
                cell.SetPositionInCell(j, i);

                // Kiểm tra xem tọa độ (j, i) có phải là ClueCell không
                bool isClue = false;
                if (data.clues != null)
                {
                    for (int c = 0; c < data.clues.Length; c++)
                    {
                        if (data.clues[c].x == j && data.clues[c].y == i)
                        {
                            cell.SetClue(data.clues[c]);
                            isClue = true;
                            break;
                        }
                    }
                }

                if (!isClue)
                {
                    cell.ClearClue();
                }

                gridCells.Add(cell);
            }
        }
        gridCellEvent?.Invoke(gridCells);
    }

    private static bool IsVoid(LevelDefinition data, int x, int y)
    {
        if (data.cellOverrides == null) return false;
        foreach (CellOverrideData o in data.cellOverrides)
        {
            if (o.x == x && o.y == y && o.type == CellType.Void) return true;
        }
        return false;
    }

    public void DeleteGrid()
    {
        foreach (Cell cell in gridCells)
        {
            if (cell != null)
            {
                Destroy(cell.gameObject);
            }
        }
        gridCells.Clear();
    }

    public List<Cell> GetGrid => gridCells;
}
