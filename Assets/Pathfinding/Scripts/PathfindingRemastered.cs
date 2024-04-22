using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathfindingRemastered
{
    private const float MOVE_STRAIGHT_COST = 1f;
    private const float MOVE_DIAGONAL_COST = 1.4f;
    private const float MOVE_COST = 1f;
    //private const float HEIGHT_FACTOR = 20f;

    private Grid<PathNode> grid;
    private List<PathNode> openList;
    private List<PathNode> closedList;

    private int pathSegmentMask;

    private AnimationCurve slopeCurve;

    private bool curvatureConstraint;

    private float curvatureCostMultiplier = 1f;

    private bool moveCostOn;

    public PathfindingRemastered(int width, int height, int pathSegmentNum, AnimationCurve slopeCurve, bool curvatureConstraint, float curvatureCostMultiplier, bool moveCostOn)
    {
        grid = new Grid<PathNode>(width, height, 10f, Vector3.zero, (Grid<PathNode> grid, int x, int y) => new PathNode(grid, x, y));
        this.pathSegmentMask = pathSegmentNum;
        this.slopeCurve = slopeCurve;
        this.curvatureConstraint = curvatureConstraint;
        this.curvatureCostMultiplier = curvatureCostMultiplier;
        this.moveCostOn = moveCostOn;
    }

    public Grid<PathNode> GetGrid()
    {
        return grid;
    }

    public List<PathNode> FindPath(int startX, int startY, int endX, int endY)
    {
        PathNode startNode = grid.GetGridObject(startX, startY);
        PathNode endNode = grid.GetGridObject(endX, endY);

        if (startNode == null || endNode == null)
        {
            // Invalid Path
            return null;
        }

        openList = new List<PathNode> { startNode };
        closedList = new List<PathNode>();

        for (int x = 0; x < grid.GetWidth(); x++)
        {
            for (int y = 0; y < grid.GetHeight(); y++)
            {
                PathNode pathNode = grid.GetGridObject(x, y);
                pathNode.gCost = int.MaxValue;
                pathNode.CalculateFCost();
                pathNode.cameFromNode = null;
            }
        }

        startNode.gCost = 0;
        startNode.hCost = CalculateHcost(startNode, endNode);
        startNode.CalculateFCost();

        // calculate neighbour list

        while (openList.Count > 0)
        {
            PathNode currentNode = GetLowestFCostNode(openList);

            if (currentNode == endNode)
            {
                return CalculatePath(endNode);
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            // changed this to segmented
            foreach (PathNode neighbourNode in GetNeighbourListSegmented(currentNode))
            {
                if (closedList.Contains(neighbourNode)) continue;

                if (!neighbourNode.isWalkable)
                {
                    closedList.Add(neighbourNode);
                    continue;
                }

                // speed estimation stuff for later

                //float estimatedSpeed = 0;

                //PathNode speedCheckNode = currentNode;
                //List<PathNode> speedCheckNodeList = new List<PathNode>();

                //while (speedCheckNode.cameFromNode != null)
                //{
                //    speedCheckNodeList.Add(speedCheckNode);
                //    speedCheckNode = speedCheckNode.cameFromNode;
                //}

                //for (int i = 0; i < speedCheckNodeList.Count - 1; i++)
                //{
                //    estimatedSpeed += speedCheckNodeList[i].pos.y - speedCheckNodeList[i + 1].pos.y;
                //}

                float tentativeGCost = currentNode.gCost + CalculateEdgeWeight(currentNode, neighbourNode);
                if (tentativeGCost < neighbourNode.gCost)
                {
                    neighbourNode.cameFromNode = currentNode;
                    neighbourNode.gCost = tentativeGCost;
                    neighbourNode.hCost += CalculateHcost(neighbourNode, endNode);
                    neighbourNode.CalculateFCost();

                    if (!openList.Contains(neighbourNode))
                    {
                        openList.Add(neighbourNode);
                    }
                }
            }
        }

        // Out of nodes on open list
        return null;
    }

    private List<PathNode> GetNeighbourListSegmented(PathNode currentNode)
    {
        List<PathNode> neighbourList = new List<PathNode>();

        // Iterate over the path segment masks
        for (int i = -pathSegmentMask; i <= pathSegmentMask; i++)
        {
            for (int j = -pathSegmentMask; j <= pathSegmentMask; j++)
            {
                if (System.Numerics.BigInteger.GreatestCommonDivisor(i, j) == 1)
                {
                    int x = currentNode.x + i;
                    int y = currentNode.y + j;

                    // Check if the calculated node is within bounds
                    if (x >= 0 && x < grid.GetWidth() && y >= 0 && y < grid.GetHeight())
                    {
                        neighbourList.Add(GetNode(x, y));
                    }
                }
            }
        }

        return neighbourList;
    }

    private List<PathNode> GetNeighbourList(PathNode currentNode)
    {
        List<PathNode> neighbourList = new List<PathNode>();

        if (currentNode.x - 1 >= 0)
        {
            // Left
            neighbourList.Add(GetNode(currentNode.x - 1, currentNode.y));
            // Left Down
            if (currentNode.y - 1 >= 0) neighbourList.Add(GetNode(currentNode.x - 1, currentNode.y - 1));
            // Left Up
            if (currentNode.y + 1 < grid.GetHeight()) neighbourList.Add(GetNode(currentNode.x - 1, currentNode.y + 1));
        }
        if (currentNode.x + 1 < grid.GetWidth())
        {
            // Right
            neighbourList.Add(GetNode(currentNode.x + 1, currentNode.y));
            // Right Down
            if (currentNode.y - 1 >= 0) neighbourList.Add(GetNode(currentNode.x + 1, currentNode.y - 1));
            // Right Up
            if (currentNode.y + 1 < grid.GetHeight()) neighbourList.Add(GetNode(currentNode.x + 1, currentNode.y + 1));
        }
        // Down
        if (currentNode.y - 1 >= 0) neighbourList.Add(GetNode(currentNode.x, currentNode.y - 1));
        // Up
        if (currentNode.y + 1 < grid.GetHeight()) neighbourList.Add(GetNode(currentNode.x, currentNode.y + 1));

        return neighbourList;
    }

    public PathNode GetNode(int x, int y)
    {
        return grid.GetGridObject(x, y);
    }

    private List<PathNode> CalculatePath(PathNode endNode)
    {
       List<PathNode> path = new List<PathNode>();
        path.Add(endNode);
        PathNode currentNode = endNode;

        while (currentNode.cameFromNode != null)
        {
            path.Add(currentNode.cameFromNode);
            currentNode = currentNode.cameFromNode;
        }

        path.Reverse();

        // debug stuff
        for (int i = 0; i < path.Count; i++)
        {
            if (i < path.Count - 1)
            {
                Vector3 line = path[i + 1].pos - path[i].pos;
                Vector3 horizontalSurfaceNormal = Vector3.up; // Assuming the surface is parallel to the ground

                float dotProduct = Vector3.Dot(line.normalized, horizontalSurfaceNormal);
                float angleRadians = Mathf.Acos(dotProduct);
                float angleDegrees = angleRadians * Mathf.Rad2Deg - 90;
                Debug.Log("Angle #" + i + ": " + angleDegrees);
                Debug.Log("Slope value #" + i + ": " + slopeCurve.Evaluate(angleDegrees));

                Debug.Log("Curvature cost #" + i + ": " + CalculateCurvatureCost(path[i], path[i + 1]));
            }

            Debug.Log("G cost #" + i + ": " + path[i].gCost);
            Debug.Log("H cost #" + i + ": " + path[i].hCost);
            Debug.Log("F cost #" + i + ": " + path[i].fCost);
        }

        return path;
    }

    // THIS IS WHERE THE REAL CALCULATION SHOULD HAPPEN !!!!
    private float CalculateEdgeWeight(PathNode a, PathNode b)
    {
        int xDistance = Mathf.Abs(a.x - b.x);
        int yDistance = Mathf.Abs(a.y - b.y);
        
        float euclidean = MOVE_COST * Mathf.Sqrt(xDistance * xDistance + yDistance * yDistance);

        float slopeCost = CalculateSlopeCost(a, b);

        //float curvatureCost = CalculateCurvatureCost(a, b);

        return MOVE_COST * euclidean * slopeCost;
    }

    // THIS IS FOR CHECKING CROM CURRENT NODE TO END NOED FOR AN ESTIMATED COST
    private float CalculateHcost(PathNode a, PathNode b)
    {
        int xDistance = Mathf.Abs(a.x - b.x);
        int yDistance = Mathf.Abs(a.y - b.y);

        // Euclidean distance
        return MOVE_STRAIGHT_COST * Mathf.Sqrt(xDistance * xDistance + yDistance * yDistance);

        // Diagonal distance
        int remaining = Mathf.Abs(xDistance - yDistance);
        //return MOVE_DIAGONAL_COST * Mathf.Min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;

    }

    private float CalculateSlopeCost(PathNode a, PathNode b)
    {

        Vector3 line = b.pos - a.pos;
        Vector3 horizontalSurfaceNormal = Vector3.up; // Assuming the surface is parallel to the ground

        float dotProduct = Vector3.Dot(line.normalized, horizontalSurfaceNormal);
        float angleRadians = Mathf.Acos(dotProduct);
        float angleDegrees = angleRadians * Mathf.Rad2Deg - 90;

        float slopeCost = slopeCurve.Evaluate(angleDegrees);

        return slopeCost;
    }

    private float CalculateCurvatureCost(PathNode a, PathNode b)
    {
        // Vector representing the direction from node 'a' to node 'b'
        Vector2 pathDirection = new Vector2(b.x - a.x, b.y - a.y).normalized;

        // Calculate the angle (in radians) between the current path and the previous path (if available)
        float angle = 0f;
        if (a.cameFromNode != null)
        {
            Vector2 previousPathDirection = new Vector2(a.x - a.cameFromNode.x, a.y - a.cameFromNode.y).normalized;
            angle = Vector2.SignedAngle(previousPathDirection, pathDirection) * Mathf.Deg2Rad;
        }

        // You may want to adjust the curvature cost based on the specific requirements of your application
        float curvatureCost = Mathf.Abs(angle);

        return curvatureCost * curvatureCostMultiplier;
    }

    private PathNode GetLowestFCostNode(List<PathNode> pathNodeList)
    {
        PathNode lowestFCostNode = pathNodeList[0];

        for (int i = 0; i < pathNodeList.Count; i++)
        {
            if (pathNodeList[i].fCost < lowestFCostNode.fCost)
            {
                lowestFCostNode = pathNodeList[i];
            }
        }

        return lowestFCostNode;
    }
}
