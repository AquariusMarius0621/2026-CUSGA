using UnityEngine;

/// <summary>
/// SceneObjectFinder 是一个服务于原型阶段的轻量场景查找工具。
///
/// 它存在的主要原因是：
/// - GameObject.Find 通常只能可靠找到当前激活中的对象
/// - 但原型里有些对象会故意保持未激活状态，例如原型模板或 Game Over 面板
/// - 所以这里补了一层“连未激活场景对象也尽量查找”的逻辑
///
/// 这种做法的优点是搭原型速度快、改动少、使用直接。
/// 缺点也很明显：
/// - 依赖对象名称，改名就会失效
/// - 名字重复时容易产生歧义
/// - 项目变大后不如 Inspector 直拖引用可靠
///
/// 因此它更适合作为当前阶段的开发便利工具，
/// 而不是中大型项目里长期依赖的最终方案。
/// </summary>
public static class SceneObjectFinder
{
    /// <summary>
    /// 按名字查找场景中的 GameObject。
    ///
    /// 查询流程分两步：
    /// 1. 先用 GameObject.Find 查找激活中的对象。
    /// 2. 如果没找到，再遍历所有 Transform，把未激活但属于场景实例的对象也纳入搜索。
    ///
    /// 第二步之所以要检查 scene 是否有效，
    /// 是为了排除工程资源中的对象，只保留当前场景里的实例对象。
    /// </summary>
    /// <param name="objectName">要查找的对象名称。</param>
    /// <returns>找到则返回对象；找不到则返回 null。</returns>
    public static GameObject FindGameObject(string objectName)
    {
        GameObject activeObject = GameObject.Find(objectName);
        if (activeObject != null)
        {
            return activeObject;
        }

        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform candidate in allTransforms)
        {
            if (candidate.name != objectName)
            {
                continue;
            }

            // 只有 scene 有效，才说明它是当前场景中的实例对象，
            // 而不是工程窗口中的某个资源或预制体资源本身。
            if (!candidate.gameObject.scene.IsValid())
            {
                continue;
            }

            return candidate.gameObject;
        }

        return null;
    }

    /// <summary>
    /// 按对象名查找目标对象，并进一步获取指定类型的组件。
    ///
    /// 这是对 FindGameObject 的一个便捷封装，
    /// 可以减少调用方先找对象、再手动 GetComponent 的重复样板代码。
    /// </summary>
    public static T FindComponent<T>(string objectName) where T : Component
    {
        GameObject target = FindGameObject(objectName);
        return target != null ? target.GetComponent<T>() : null;
    }

    /// <summary>
    /// 按名字查找一个 Transform；如果不存在，就新建一个同名对象。
    ///
    /// 这个方法特别适合用来确保某些“运行时根节点”存在，
    /// 例如专门用于承载已放置塔、敌人实例等的父对象。
    ///
    /// 这样调用方不需要提前手动在场景里配置好这些容器对象，
    /// 也能在运行时拿到一个稳定可用的挂载点。
    /// </summary>
    public static Transform FindOrCreateTransform(string objectName)
    {
        GameObject existingObject = FindGameObject(objectName);
        if (existingObject != null)
        {
            return existingObject.transform;
        }

        GameObject createdObject = new GameObject(objectName);
        return createdObject.transform;
    }
}
