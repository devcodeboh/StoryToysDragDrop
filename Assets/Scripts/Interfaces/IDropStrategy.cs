using UnityEngine;

public interface IDropStrategy
{
    void Execute(ItemController item, Vector3 target);
}
