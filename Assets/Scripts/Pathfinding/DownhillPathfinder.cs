using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DownhillPathfinder : MonoBehaviour
{
    private const float MOVE_COST = 1f;

    private Grid<NewPathNode> grid;
    private List<NewPathNode> openList;
    private List<NewPathNode> closedList;

    private int pathSegmentMask;

    private AnimationCurve slopeCurve;
    private float maxSteepness;

    private float speedVariable;
    private float slowdownVariable;

    private bool euclideanCostOn;
    private bool slopeCostOn;
    private bool directionCostOn;
    private bool curvatureCostOn;

    public DownhillPathfinder(int width, int height, int pathSegmentNum, AnimationCurve slopeCurve, float speedVariable, float slowdownVariable, float maxSteepness, bool e, bool s, bool d, bool c)
    {
        grid = new Grid<NewPathNode>(width, height, 10f, Vector3.zero, (Grid<NewPathNode> grid, int x, int y) => new NewPathNode(grid, x, y));
        this.pathSegmentMask = pathSegmentNum;
        this.slopeCurve = slopeCurve;
        this.maxSteepness = maxSteepness;
        this.speedVariable = speedVariable;
        this.slowdownVariable = slowdownVariable;
        this.euclideanCostOn = e;
        this.slopeCostOn = s;
        this.directionCostOn = d;
        this.curvatureCostOn = c;
    }

    public Grid<NewPathNode> GetGrid()
    {
        return grid;
    }

    public List<NewPathNode> FindPath(int startX, int startY)
    {
        NewPathNode startNode = grid.GetGridObject(startX, startY);

        if (startNode == null)
        {
            // Invalid Path
            Debug.Log("Start node is invalid");
            return null;
        }

        //openList = new List<NewPathNode> { startNode };
        closedList = new List<NewPathNode>();

        for (int x = 0; x < grid.GetWidth(); x++)
        {
            for (int y = 0; y < grid.GetHeight(); y++)
            {
                NewPathNode pathNode = grid.GetGridObject(x, y);
                pathNode.cost = int.MaxValue;
                pathNode.cameFromNode = null;
            }
        }

        startNode.cost = 0;
        NewPathNode currentNode = startNode;

        // calculate neighbour list

        float currentEstimatedSpeed = 0f;
        currentNode.estimatedSpeed = currentEstimatedSpeed;

        int nodenumber = 0;

        // until it checked every node, or found a path
        while (closedList.Count != grid.GetWidth() * grid.GetWidth())
        {

            List<NewPathNode> neighbourNodes = GetNeighbourListSegmented(currentNode, 30, currentEstimatedSpeed);
            // if there are not possible neighbours to get to, end path
            if (neighbourNodes.Count == 0 || currentNode.endNode)
            {
                return CalculatePath(currentNode);
            }

            // changed this to segmented
            foreach (NewPathNode neighbourNode in neighbourNodes)
            {

                if (closedList.Contains(neighbourNode)) continue;

                if (!neighbourNode.isWalkable)
                {
                    closedList.Add(neighbourNode);
                }
                else
                {
                    float tentativeCost = currentNode.cost + CalculateEdgeWeight(nodenumber, currentNode, neighbourNode, maxSteepness, 45);
                    if (tentativeCost < neighbourNode.cost)
                    {
                        neighbourNode.cost = tentativeCost;
                    }
                }


            }

            NewPathNode minCostNode = neighbourNodes[0];

            foreach (NewPathNode neighbourNode in neighbourNodes)
            {
                // Check if the cost of the current neighbourNode is lower
                if (neighbourNode.cost < minCostNode.cost)
                {
                    minCostNode = neighbourNode;
                }
            }

            // set the next node
            minCostNode.cameFromNode = currentNode;
            currentNode = minCostNode;
            nodenumber++;

            //Debug.Log("Node num " + nodenumber);

            currentEstimatedSpeed += EstimateSpeedChange(currentNode.cameFromNode, currentNode, speedVariable, slowdownVariable, currentEstimatedSpeed, false);

            if (currentEstimatedSpeed > 65)
            {
                currentEstimatedSpeed = 65;
            }

            currentNode.estimatedSpeed = currentEstimatedSpeed;
            currentNode.nodeNumber = nodenumber;

            //Debug.Log("Estimated speed currently: " + currentNode.estimatedSpeed);

        }

        // Out of nodes on open list
        Debug.Log("Out of nodes on open list");
        return null;
    }

    private float EstimateSpeedChange(NewPathNode fromNode, NewPathNode toNode, float speedVariable, float slowdownVariable, float currentSpeed, bool debug)
    {
        // steepness
        float steepness = GetSteepness(fromNode, toNode);

        if (debug)
        {
            Debug.Log("steepness: " + steepness);
        }

        // distance
        Vector2 ap = new Vector2(toNode.x, toNode.y);
        Vector2 bp = new Vector2(fromNode.x, fromNode.y);

        float distance = Vector2.Distance(ap, bp);
        if (debug)
            Debug.Log("distance: " + distance);

        // angle change
        float angleChange = 0;
        if (fromNode.cameFromNode != null)
        {
            Vector2 currentVector = GetDirection(fromNode, toNode);
            Vector2 parentVector = GetDirection(fromNode.cameFromNode, fromNode);

            angleChange = Vector2.Angle(parentVector, currentVector);
        }
        if (debug)
            Debug.Log("angle change: " + angleChange);

        float targetSpeed = Mathf.Pow(Mathf.Abs(steepness), 1.7f);

        if (debug)
            Debug.Log("max speed: " + targetSpeed);

        // if going up, get the negative of the target speed
        if (steepness < 0)
        {
            targetSpeed *= -1;
        }

        // get the difference between the target speed and the current speed
        // if positive, the board will be speeding up most of the time (if there is a steep turn it might slow down in the next section overall)
        // if negative, the board will be slowing down
        float diff = targetSpeed - currentSpeed;
        if (debug)
            Debug.Log("diff: " + diff);

        diff /= speedVariable;

        float curveSlowdown = angleChange * currentSpeed / slowdownVariable;

        if (debug && curveSlowdown > 0)
            Debug.Log("Curve slowdown: " + curveSlowdown);

        float change = (distance * diff) - curveSlowdown;

        if (steepness < 0)
        {
            change *= 2f;
        }

        if (debug)
            Debug.Log("change: " + change);

        return change;
    }

    private float GetSteepness(NewPathNode fromNode, NewPathNode toNode)
    {
        // steepness
        Vector3 line = fromNode.pos - toNode.pos;
        Vector3 horizontalSurfaceNormal = Vector3.up; // Assuming the surface is parallel to the ground

        float dotProduct = Vector3.Dot(line.normalized, horizontalSurfaceNormal);
        float angleRadians = Mathf.Acos(dotProduct);
        float steepness = angleRadians * Mathf.Rad2Deg - 90;
        //steepness = Mathf.Abs(steepness);
        steepness *= -1;

        return steepness;
    }

    private List<NewPathNode> GetNeighbourListSegmented(NewPathNode currentNode, float maxAngle, float currentEstimatedSpeed)
    {
        List<NewPathNode> neighbourList = new List<NewPathNode>();

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
                    if (x >= 0 && x < grid.GetWidth() && y >= 0 && y < grid.GetHeight() && GetNode(x, y).isWalkable)
                    {
                        // check if there is enough speed for the next node
                        float estimation = currentEstimatedSpeed + EstimateSpeedChange(currentNode, GetNode(x, y), speedVariable, slowdownVariable, currentEstimatedSpeed, false);

                        float steepness = GetSteepness(currentNode, GetNode(x, y));

                        if ((steepness > 0 && estimation > 5) || (steepness < 0 && estimation > 12))
                        {
                            if (currentNode.cameFromNode == null)
                            {
                                // this means it is the first node, and just consider all angles
                                neighbourList.Add(GetNode(x, y));
                            }
                            else
                            {
                                // only add nodes that are within the maxAngle degree angle as to where the path is currently headed
                                Vector2 currentDir = GetDirection(currentNode.cameFromNode, currentNode);
                                Vector2 nextDir = GetDirection(currentNode, GetNode(x, y));

                                float angleDiff = Vector2.Angle(currentDir, nextDir);

                                if (angleDiff <= maxAngle)
                                {
                                    neighbourList.Add(GetNode(x, y));
                                }
                            }
                        }

                    }
                }
            }
        }

        return neighbourList;
    }

    private List<NewPathNode> GetNeighbourList(NewPathNode currentNode)
    {
        List<NewPathNode> neighbourList = new List<NewPathNode>();

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

    public NewPathNode GetNode(int x, int y)
    {
        return grid.GetGridObject(x, y);
    }

    Vector2 GetDirection(NewPathNode nodeA, NewPathNode nodeB)
    {
        Vector2 nodeAVector = new Vector2(nodeA.x, nodeA.y);
        Vector2 nodeBVector = new Vector2(nodeB.x, nodeB.y);

        Vector2 directionVector = nodeBVector - nodeAVector;
        directionVector.Normalize();
        return directionVector;
    }

    private List<NewPathNode> CalculatePath(NewPathNode endNode)
    {
        List<NewPathNode> path = new List<NewPathNode>();
        path.Add(endNode);
        NewPathNode currentNode = endNode;

        while (currentNode.cameFromNode != null)
        {
            path.Add(currentNode.cameFromNode);
            currentNode = currentNode.cameFromNode;
        }

        path.Reverse();

        List<float> slopes = new List<float>();

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

                slopes.Add(angleDegrees);
            }
        }

        float median = CalculateMedian(slopes);
        float average = CalculateAverage(slopes);
        //Debug.Log("median slope: " + median);
        //Debug.Log("average slope: " + average);

        return path;
    }

    float CalculateAverage(List<float> floatList)
    {
        // Ensure the list is not empty
        if (floatList.Count == 0)
        {
            Debug.LogError("Cannot calculate average for an empty list.");
            return 0.0f; // or throw an exception
        }

        // Sum up all the values in the list
        float sum = 0.0f;
        foreach (float value in floatList)
        {
            sum += value;
        }

        // Calculate the average
        float average = sum / floatList.Count;

        return average;
    }

    float CalculateMedian(List<float> floatList)
    {
        // Ensure the list is not empty
        if (floatList.Count == 0)
        {
            Debug.LogError("Cannot calculate median for an empty list.");
            return 0.0f; // or throw an exception
        }

        // Sort the list in ascending order
        List<float> sortedList = floatList.OrderBy(x => x).ToList();

        // Find the middle index
        int middleIndex = sortedList.Count / 2;

        // Check if the list has an even number of elements
        if (sortedList.Count % 2 == 0)
        {
            // If even, take the average of the two middle values
            float middleValue1 = sortedList[middleIndex - 1];
            float middleValue2 = sortedList[middleIndex];
            return (middleValue1 + middleValue2) / 2.0f;
        }
        else
        {
            // If odd, return the middle value
            return sortedList[middleIndex];
        }
    }

    // THIS IS WHERE THE REAL CALCULATION SHOULD HAPPEN !!!!
    private float CalculateEdgeWeight(int nodeNumber, NewPathNode a, NewPathNode b, float maxSteepness, float maxCurveAngle)
    {

        float edgeWeight = 1f;

        if (euclideanCostOn) { edgeWeight *= CalculateEuclideanCost(a, b); }
        if (slopeCostOn) { edgeWeight *= CalculateSlopeCost(a, b); }
        if (directionCostOn) { edgeWeight *= CalculateDirectionCost(a, b, maxSteepness); }
        if (curvatureCostOn && nodeNumber > 0) { edgeWeight *= CalculateCurvatureCost(a, b, maxCurveAngle); }

        return edgeWeight;
    }

    private float CalculateEuclideanCost(NewPathNode a, NewPathNode b)
    {
        int xDistance = Mathf.Abs(a.x - b.x);
        int yDistance = Mathf.Abs(a.y - b.y);

        float euclidean = MOVE_COST * Mathf.Sqrt(xDistance * xDistance + yDistance * yDistance);

        float xd1 = Mathf.Pow(pathSegmentMask, 2);

        float xd = Mathf.Sqrt(xd1 + 1);

        euclidean /= xd;

        return euclidean;
    }

    private float CalculateSlopeCost(NewPathNode a, NewPathNode b)
    {
        Vector3 line = b.pos - a.pos;
        Vector3 horizontalSurfaceNormal = Vector3.up; // Assuming the surface is parallel to the ground

        float dotProduct = Vector3.Dot(line.normalized, horizontalSurfaceNormal);
        float angleRadians = Mathf.Acos(dotProduct);
        float angleDegrees = angleRadians * Mathf.Rad2Deg - 90;

        float slopeCost = slopeCurve.Evaluate(angleDegrees);

        return slopeCost;
    }

    private float CalculateDirectionCost(NewPathNode a, NewPathNode b, float maxSteepness)
    {
        // get direction
        Vector2 dir = GetDirection(a, b);

        // get the average normal of the two nodes
        Vector3 avgNormal = (a.normal + b.normal) / 2;

        // translate the surface normal from 3d to 2d
        Vector2 normal2d = new Vector2(avgNormal.x, avgNormal.z);

        // get the difference in angles between the direction and the normal
        float angleDiff = Vector2.Angle(dir, normal2d);
        // angle diff: 0 - 180
        // want: 0 - 1
        float normalizedAngleDiff = 1 + (angleDiff / 180f);
        //normalizedAngleDiff = angleDiff / 180f;

        // get steepness of path at that point
        float dotProduct = Vector3.Dot(avgNormal, Vector3.up);
        float angleRadians = Mathf.Acos(dotProduct);
        float angleDegrees = angleRadians * Mathf.Rad2Deg - 90;
        float steepness = Mathf.Abs(angleDegrees);
        // want: 1 - 2
        float normalizedSteepness = 1 + (steepness / maxSteepness);

        // the less steep the surface is the less this matters

        return angleDiff * steepness;
    }

    private float CalculateCurvatureCost(NewPathNode a, NewPathNode b, float maxAngle)
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
        else
        {
            return 0; // No curvature cost if there's no previous node
        }

        // Normalize the angle to be between -180 and 180 degrees
        while (angle < -180) angle += 360;
        while (angle > 180) angle -= 360;

        // Calculate the absolute value of the angle
        float absAngle = Mathf.Abs(angle);

        // Normalize the angle to be between 0 and 180 degrees
        absAngle = Mathf.Min(absAngle, 360 - absAngle);

        // Scale the angle to be between 1 and 2 based on the maximum angle allowed
        float curvatureCost = 1 + (absAngle / maxAngle);

        return curvatureCost;
    }

    private NewPathNode GetLowestCostNode(List<NewPathNode> pathNodeList)
    {
        NewPathNode lowestCostNode = pathNodeList[0];

        for (int i = 0; i < pathNodeList.Count; i++)
        {
            if (pathNodeList[i].cost < lowestCostNode.cost)
            {
                lowestCostNode = pathNodeList[i];
            }
        }

        return lowestCostNode;
    }
}
