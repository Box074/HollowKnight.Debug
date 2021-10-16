热重载(HotReload)可以动态加载mod并重定向方法实现

除了继承于`UnityEngine.Component`的类型，每次将旧对象转换为新对象后，都会尝试调用新对象的`OnAfterHotReload`

```c#
void OnAfterHotReload(System.Collections.Generic.Dictionary<string, object> data);
```



---

配置文件(HotReload.json)结构

HotReload

	- mods: 需要加载的mod文件的路径
	- ingoreLastWriteTime: 是否忽略最后修改的时间强制热重载mod

---

使用方法

将要热重载的mod复制到`游戏目录\hollow_knight_Data\HKDebug\HotReloadMods`文件夹下或在配置文件中添加要热重载的mod的路径

启动游戏后会自动加载，在游戏中使用`HotReload->刷新`来热重载

---

警告：

1. 热重载器不会重复执行Initialize
2. 热重载器会忽略所有的GetPreloadNames，请自行获取

3. 热重载器会跳过泛型类和泛型方法，请尽量不要使用

4. 所有被热重载器加载的mod不可以依赖于其他被热重载器加载的mod
5. 所有Component在每次热重载时会被重新添加到GameObject，并销毁原Component
6. 请尽量不要更改方法签名