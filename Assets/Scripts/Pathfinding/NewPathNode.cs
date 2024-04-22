using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewPathNode
{
    public int nodeNumber;

    private Grid<NewPathNode> grid;
    public int x;
    public int y;

    public float cost;

    public Vector3 pos;
    public Vector3 normal;

    public bool isWalkable;
    public NewPathNode cameFromNode;

    public bool endNode;

    public float estimatedSpeed;

    public NewPathNode(Grid<NewPathNode> grid, int x, int y)
    {
        this.grid = grid;
        this.x = x;
        this.y = y;
        isWalkable = true;

        // Calculate the percentage along the width and height
        float widthPercentage = (float)x / grid.GetWidth();
        float heightPercentage = (float)y / grid.GetHeight();

        // Check if the node is on the outer side based on the specified conditions
        if (widthPercentage > 0.85f || widthPercentage < 0.15f || heightPercentage > 0.85f || heightPercentage < 0.15f)
        {
            endNode = true;
        }
        else
        {
            endNode = false;
        }
    }

    public void SetIsWalkable(bool isWalkable)
    {
        this.isWalkable = isWalkable;
        grid.TriggerGridObjectChanged(x, y);
    }

    public override string ToString()
    {
        return x + "," + y;
    }

}
