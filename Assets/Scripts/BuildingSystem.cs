using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BuildingSystem : MonoBehaviour
{
    public static BuildingSystem current;

    public GridLayout gridLayout;
    private Grid grid;
    [SerializeField] private Tilemap mainTileMap;
    [SerializeField] private TileBase unplaceableTile;

    public GameObject prefab1;
    public GameObject prefab2;

    private PlaceableObject objectToPlace;

    //highlightmap
    [SerializeField] private Tilemap interactiveMap = null;
    private Vector3Int previousMousePos = new Vector3Int();
    [SerializeField] private Tile hoverTile = null;

    //dataMap
    [SerializeField] private Tilemap dataMap = null;
    [SerializeField] private List<TileData> tileDatas;
    private Dictionary<TileBase, TileData> dataFromTiles;

    //KNOWN ISSUES: 
    //Placing object at spawn makes it difficult to grab newly spawned objects. Because there's no real inputSystem yet.
    //maybe change drag script to not require button press, or use dynamic spawn positions -> this can be done when changing inputSystem.
    //
    //ObjectToPlace can block tile right behind it -> raycast error -> objectToPlace different physicsLayer ? -> overhaul dragScript when changing inputSystem

    #region Unity Methods

    private void Awake()
    {
        current = this;
        grid = gridLayout.gameObject.GetComponent<Grid>();

        //fill Dicitonary to read values from tiles later
        dataFromTiles = new Dictionary<TileBase, TileData>();

        foreach(var tileData in tileDatas)
        {
            foreach(var tile in tileData.tiles)
            {
                dataFromTiles.Add(tile, tileData);
            }
        }
    }

    private void Update()
    {
        // Mouse over -> highlight tile
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector3Int mouseCellPos = grid.WorldToCell(mouseWorldPos);

        if (!mouseCellPos.Equals(previousMousePos))
        {
            interactiveMap.SetTile(previousMousePos, null); // Remove old hoverTile
            interactiveMap.SetTile(mouseCellPos, hoverTile);
            previousMousePos = mouseCellPos;
        }
        //Highlight stuff done

        //just an example for getting tile + tileData by clicking
        if (Input.GetMouseButtonDown(0))
        {
            TileBase clickedTile = dataMap.GetTile(mouseCellPos);
            float data1 = dataFromTiles[clickedTile].data1;
            Debug.Log("tileInfos! tileData: " + data1 + " / position: " + mouseCellPos + " / type: " + clickedTile);
        }

        //Building stuff // Examples with KeyDown
        if (Input.GetKeyDown(KeyCode.A))
        {
            InitializeWithObject(prefab1);
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            InitializeWithObject(prefab2);
        }

        //return if no object is being placed
        if (!objectToPlace)
        {
            return;
        }

        //placement behavior examples - real game would need a better inputSystem, this is good enough to showcase

        //destroy object and return
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            //this exact code is also found at a later point -> optimize
            Destroy(objectToPlace.gameObject);
            objectToPlace = null;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            objectToPlace.Rotate();
        }
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (CanBePlaced(objectToPlace))
            {
                Debug.Log("Place object");
                objectToPlace.Place();
                Vector3Int start = gridLayout.WorldToCell(objectToPlace.GetStartPosition());
                TakeArea(start, objectToPlace.Size);

                objectToPlace = null;
            }
            else
            {
                //this exact part is can be found when pressing esc -> optimize
                //Debug.Log("cant be placed");
                Destroy(objectToPlace.gameObject);
                objectToPlace = null;
            }
        }
    }

    #endregion

    #region Utils

    public static Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if(Physics.Raycast(ray, out RaycastHit raycastHit))
        {
            return raycastHit.point;
        }
        else
        {
            return Vector3.zero;
        }
    }

    public Vector3 SnapCoordinateToGrid(Vector3 position)
    {
        Vector3Int cellPos = gridLayout.WorldToCell(position);
        position = grid.GetCellCenterWorld(cellPos);
        return position;
    }

    private static TileBase[] GetTilesBlock(BoundsInt area, Tilemap tilemap)
    {
        TileBase[] array = new TileBase[area.size.x * area.size.y * area.size.z];
        int counter = 0;

        foreach(var v in area.allPositionsWithin)
        {
            Vector3Int pos = new Vector3Int(v.x, v.y, 0);
            array[counter] = tilemap.GetTile(pos);
            counter++;
        }

        return array;
    }

    #endregion

    #region Building Placement

    public void InitializeWithObject(GameObject prefab)
    {
        //trying to create a second object to place
        if (objectToPlace)
        {
            Debug.Log("theres already something to place");
            return;
        }

        //instantiate prefab at grid position, set objectToPlace and add ObjectDrag script
        Vector3 position = SnapCoordinateToGrid(Vector3.zero);
        GameObject obj = Instantiate(prefab, position, Quaternion.identity);
        objectToPlace = obj.GetComponent<PlaceableObject>();
        obj.AddComponent<ObjectDrag>();
    }

    private bool CanBePlaced(PlaceableObject placeableObject)
    {
        //Get all tiles at the given area and check for unplaceableTiles there -> if none found, place object
        BoundsInt area = new BoundsInt();
        area.position = gridLayout.WorldToCell(objectToPlace.GetStartPosition());
        area.size = placeableObject.Size;
        area.size = new Vector3Int(area.size.x + 1, area.size.y + 1, area.size.z +1);

        TileBase[] baseArray = GetTilesBlock(area, mainTileMap);

        foreach(var b in baseArray)
        {
            if(b == unplaceableTile)
            {
                Debug.Log("cant be placed! - something already here");
                return false;
            }
        }
        return true;
    }

    public void TakeArea(Vector3Int start, Vector3Int size)
    {
        //fill area with unpalceableTiles
        mainTileMap.BoxFill(start, unplaceableTile, start.x, start.y, start.x + size.x, start.y + size.y);
    }

    #endregion

    #region TileDataGetter

    public float GetTileDataByWorldPosExample(Vector2 worldPosition)
    {
        //object calls this with its position -> gets back value
        Vector3Int gridPosition = dataMap.WorldToCell(worldPosition);
        TileBase tile = dataMap.GetTile(gridPosition);

        if(tile == null)
        {
            Debug.Log("no tile to read data");
            return 0f;
        }

        float data1 = dataFromTiles[tile].data1;
        return data1;
    }

    #endregion
}
