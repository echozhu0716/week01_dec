﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum BlockState { Valid = 0, Intersecting = 1, OutOfBounds = 1, Placed = 2 }
public class Block
{
    public List<Voxel> Voxels;

    public PatternType Type;
    private Pattern _pattern => PatternManager.GetPatternByType(Type);
    private VoxelGrid _grid;
    private GameObject _goBlock;

    Dictionary<Voxel, List<Vector3Int>> possibleOrentations;

    public Vector3Int Anchor;
    public Quaternion Rotation;
    private bool _placed = false;
    /// <summary>
    /// Get the current state of the block. Can be Valid, Intersecting, OutOfBound or Placed
    /// </summary>
    public BlockState State
    {
        get
        {
            if (_placed) return BlockState.Placed;
            if (Voxels.Count < _pattern.Voxels.Count) return BlockState.OutOfBounds;
            if (Voxels.Count(v => v.Status != VoxelState.Available && v.Status != VoxelState.Connection) > 0)
            {
                if (Voxels[0].Status == VoxelState.Connection) return BlockState.Valid;
                return BlockState.Intersecting;
            }
            return BlockState.Valid;
        }
    }
    /// <summary>
    /// Block constructor. Will create block starting at an anchor with a certain rotation of a given type.
    /// </summary>
    /// <param name="type">The block type</param>
    /// <param name="anchor">The index where the block needs to be instantiated</param>
    /// <param name="rotation">The rotation the blocks needs to be instantiated in</param>
    public Block(PatternType type, Vector3Int anchor, Quaternion rotation, VoxelGrid grid)
    {
        Type = type;
        Anchor = anchor;
        Rotation = rotation;
        _grid = grid;
        

        PositionPattern();
    }

    /// <summary>
    /// Add all the relevant voxels to the block according to it's anchor point, pattern and rotation
    /// </summary>
    public void PositionPattern()
    {
        possibleOrentations = new Dictionary<Voxel, List<Vector3Int>>();
        Voxels = new List<Voxel>();
        foreach (var patternVoxel in _pattern.Voxels)
        {
            if (Util.TryOrientIndex(patternVoxel.Index, Anchor, Rotation, _grid, out var newIndex))
            {
                Voxel gridVoxel = _grid.Voxels[newIndex.x, newIndex.y, newIndex.z];
                Voxels.Add(gridVoxel);
                List<Vector3Int> voxelDirections = new List<Vector3Int>();
                foreach (var direction in patternVoxel.VoxelDirections)
                {
                    Util.TryOrientRotation(direction, Rotation, out var newDirection);
                    voxelDirections.Add(newDirection);
                }
                possibleOrentations.Add(gridVoxel, patternVoxel.VoxelDirections);
            }
        }
    }

    /// <summary>
    /// Try to activate all the voxels in the block. This method will always return false if the block is not in a valid state.
    /// </summary>
    /// <returns>Returns true if it managed to activate all the voxels in the grid</returns>
    public bool ActivateVoxels()
    {
        if (State != BlockState.Valid)
        {
            Debug.LogWarning("Block can't be placed");
            return false;
        }
        Color randomCol = Util.RandomColor;

        foreach (var voxel in Voxels)
        {
            voxel.VoxelDirections = possibleOrentations[voxel];
            if (voxel.HasAvailableConnetions)
            {
                voxel.Status = VoxelState.Connection;
            }
            else
            {
                voxel.Status = VoxelState.Alive;
            }
            voxel.SetColor(randomCol);
        }
        CreateGOBlock();
        _placed = true;
        return true;
    }

    public void CreateGOBlock()
    {
        _goBlock = GameObject.Instantiate(_grid.GOPatternPrefabs[Type], _grid.GetVoxelByIndex(Anchor).Centre, Rotation);
    }

    /// <summary>
    /// Remove the block from the grid
    /// </summary>
    public void DeactivateVoxels()
    {
        foreach (var voxel in Voxels)
            voxel.Status = VoxelState.Available;
    }

    /// <summary>
    /// Completely remove the block
    /// </summary>
    public void DestroyBlock()
    {
        DeactivateVoxels();
        if(_goBlock!= null)GameObject.Destroy(_goBlock);
    }
}
